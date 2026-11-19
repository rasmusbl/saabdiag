using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SaabDiag.Core;
namespace SaabDiag.Desktop;
public partial class MainWindow : Window
{
    private readonly DiagnosticSession session = new();
    private CancellationTokenSource? request;
    private readonly VirtualWorkshop workshop = new();
    private DemoPlan? demoPlan;
    private CancellationTokenSource? demoRequest;
    public MainWindow()
    {
        InitializeComponent();
        InitializeConnectivity();
        InitializeAuthorization();
        DemoModule.ItemsSource = VirtualWorkshop.Modules;
        DemoModule.SelectedItem = "demo-engine";
        DemoOperation.ItemsSource = Enum.GetValues<DemoAction>();
        DemoOperation.SelectedItem = DemoAction.ClearDtc;
        DemoFault.ItemsSource = Enum.GetValues<Fault>();
        DemoFault.SelectedItem = Fault.None;
        ShowDemoState();
        Year.ItemsSource = Profiles.Years;
        Year.SelectedItem = 2003;
        FaultMode.ItemsSource = Enum.GetValues<Fault>();
        FaultMode.SelectedItem = Fault.None;
        try { References.ItemsSource = ReferenceCatalog.Load(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "references", "catalog.json"))); }
        catch (Exception) { Status.Text = "Reference catalog unavailable or invalid. Diagnostics remain simulated."; }
        Closed += (_, _) => { request?.Cancel(); demoRequest?.Cancel(); inventoryRequest?.Cancel(); authorizationRequest?.Cancel(); authorization.Dispose(); session.Dispose(); };
    }
    private void Buttons()
    {
        if (!session.Connected) authorization.Invalidate();
        Live.IsEnabled = Dtc.IsEnabled = Modules.IsEnabled = Vin.IsEnabled = session.Connected && request is null;
        Cancel.IsEnabled = request is not null;
        var editable = !session.Connected && request is null && inventoryRequest is null;
        Scenario.IsEnabled = Year.IsEnabled = FaultMode.IsEnabled = PortChoice.IsEnabled = Baud.IsEnabled = Host.IsEnabled = TcpPort.IsEnabled = DriverChoice.IsEnabled = editable;
        RefreshInventory.IsEnabled = editable;
        ConnectSelected.IsEnabled = editable && CurrentAssessment().CanConnect;
        DiagnosticConnection.Text = session.Connected ? $"Connected: offline simulator · {Year.SelectedItem} · synthetic data only" : "Disconnected. Choose a scenario in Connection setup.";
    }
    private void Connect_Click(object sender, RoutedEventArgs e)
    {
        if (request is not null || inventoryRequest is not null) { Status.Text = "Finish or cancel the current operation first."; return; }
        var settings = CurrentSettings();
        if (settings is null) { Status.Text = "Select a scenario and model year."; return; }
        authorization.Invalidate();
        var assessment = session.ConnectConfigured(settings, inventory, FaultMode.SelectedItem is Fault fault ? fault : Fault.None);
        Readings.ItemsSource = null;
        Status.Text = session.Connected ? $"Simulator connected · {settings.Year} · no hardware" : assessment.Message;
        Buttons();
        if (session.Connected) MainTabs.SelectedIndex = 1;
    }
    private void Disconnect_Click(object sender, RoutedEventArgs e) { authorization.Invalidate(); session.Disconnect(); Readings.ItemsSource = null; Status.Text = "Disconnected · results cleared."; Buttons(); }
    private async Task Read(Operation operation)
    {
        request = new(); Buttons(); Readings.ItemsSource = null; Status.Text = "Reading synthetic fixture…";
        try { var result = await session.ReadAsync(operation, request.Token); Readings.ItemsSource = result.Readings; Status.Text = $"{result.Outcome}: {result.Message}"; }
        finally { request.Dispose(); request = null; Buttons(); }
    }
    private async void Live_Click(object sender, RoutedEventArgs e) => await Read(Operation.ReadLiveData);
    private async void Dtc_Click(object sender, RoutedEventArgs e) => await Read(Operation.ReadDtc);
    private async void Modules_Click(object sender, RoutedEventArgs e) => await Read(Operation.DiscoverModules);
    private async void Vin_Click(object sender, RoutedEventArgs e) => await Read(Operation.ReadVin);
    private void Cancel_Click(object sender, RoutedEventArgs e) => request?.Cancel();
    private void ShowDemoState()
    {
        var s = workshop.State;
        DemoState.Text = $"Virtual state only\nDTCs: {s.DtcCount} | Actuator cycles: {s.ActuatorCycles} | Resets: {s.Resets}\nComfort option: {s.ComfortOption} | Demo access: {s.DemoAccess}\nPaired: {s.Paired} | Firmware label: {s.Firmware}";
    }
    private void DemoSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ApplyDemo is null) return;
        demoPlan = null; ApplyDemo.IsEnabled = false; ConfirmDemo.IsChecked = false;
        DemoPreview.Text = "Selection changed. Preview the virtual change again.";
    }
    private void PreviewDemo_Click(object sender, RoutedEventArgs e)
    {
        demoPlan = null; ApplyDemo.IsEnabled = false; ConfirmDemo.IsChecked = false;
        try
        {
            demoPlan = workshop.Prepare((string)DemoModule.SelectedItem, (DemoAction)DemoOperation.SelectedItem);
            DemoPreview.Text = demoPlan.Preview; ApplyDemo.IsEnabled = true; DemoStatus.Text = "Preview ready. Only virtual state can change.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { DemoPreview.Text = ex.Message; }
    }
    private async void ApplyDemo_Click(object sender, RoutedEventArgs e)
    {
        if (demoPlan is null || ConfirmDemo.IsChecked != true) { DemoStatus.Text = "Preview and confirm the virtual change first."; return; }
        demoRequest = new(); ApplyDemo.IsEnabled = PreviewDemo.IsEnabled = ResetDemo.IsEnabled = false;
        DemoModule.IsEnabled = DemoOperation.IsEnabled = DemoFault.IsEnabled = ConfirmDemo.IsEnabled = false; CancelDemo.IsEnabled = true;
        DemoStatus.Text = "Applying virtual change…";
        try
        {
            var result = await workshop.ApplyAsync(demoPlan.Id, true, (Fault)DemoFault.SelectedItem, demoRequest.Token);
            DemoStatus.Text = result.Message; ShowDemoState();
        }
        finally
        {
            demoRequest.Dispose(); demoRequest = null; demoPlan = null; ConfirmDemo.IsChecked = false;
            PreviewDemo.IsEnabled = ResetDemo.IsEnabled = DemoModule.IsEnabled = DemoOperation.IsEnabled = DemoFault.IsEnabled = ConfirmDemo.IsEnabled = true;
            CancelDemo.IsEnabled = false;
        }
    }
    private void ResetDemo_Click(object sender, RoutedEventArgs e)
    {
        workshop.ResetDemo(); demoPlan = null; ApplyDemo.IsEnabled = false; ConfirmDemo.IsChecked = false;
        DemoPreview.Text = "Demo reset; preview a new virtual change."; DemoStatus.Text = "Virtual state restored."; ShowDemoState();
    }
    private void CancelDemo_Click(object sender, RoutedEventArgs e) => demoRequest?.Cancel();
    private void Reference_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (References.SelectedItem is ReferenceEntry item) ReferenceDetails.Text = $"{item.Publisher} · hosted by {item.Host}\n{item.Applicability}\n{item.Rights}";
    }
    private void Open(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch (Exception) { Status.Text = "Could not open the selected reference. Check your default browser or PDF application."; }
    }
    private void OpenReference_Click(object sender, RoutedEventArgs e)
    {
        if (References.SelectedItem is ReferenceEntry item && Uri.TryCreate(item.Url, UriKind.Absolute, out var uri) && uri.Scheme == "https") Open(uri.AbsoluteUri);
    }
    private void OpenPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "PDF manuals (*.pdf)|*.pdf", CheckFileExists = true, Multiselect = false };
        if (dialog.ShowDialog(this) == true && Path.GetExtension(dialog.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)) Open(dialog.FileName);
    }
}
