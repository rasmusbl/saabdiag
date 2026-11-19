using System.Windows;
using System.Windows.Controls;
using SaabDiag.Core;
namespace SaabDiag.Desktop;
public partial class MainWindow
{
    private LocalInventory inventory = new([], [], []);
    private CancellationTokenSource? inventoryRequest;
    private bool connectivityInitialized;
    private void InitializeConnectivity()
    {
        Scenario.ItemsSource = Connectivity.Scenarios;
        Scenario.SelectedIndex = 0;
        connectivityInitialized = true;
        UpdateConnectionAssessment();
    }
    private ConnectionSettings? CurrentSettings()
    {
        if (Scenario.SelectedItem is not ConnectionScenario scenario || Year.SelectedItem is not int year) return null;
        int.TryParse(Baud.Text, out var baud);
        int.TryParse(TcpPort.Text, out var tcpPort);
        return new(scenario.Kind, year, PortChoice.SelectedItem as string ?? "", baud, Host.Text.Trim(), tcpPort, (DriverChoice.SelectedItem as DriverRegistration)?.Id ?? "");
    }
    private ConnectionAssessment CurrentAssessment() => CurrentSettings() is { } settings ? Connectivity.Assess(settings, inventory) : new(false, false, "Select a scenario and model year.");
    private void ConnectionChanged(object sender, SelectionChangedEventArgs e) => UpdateConnectionAssessment();
    private void ConnectionTextChanged(object sender, TextChangedEventArgs e) => UpdateConnectionAssessment();
    private void UpdateConnectionAssessment()
    {
        if (!connectivityInitialized) return;
        authorization.Invalidate();
        // If changed programmatically while connected, invalidate the old session too.
        if (session.Connected) { session.Disconnect(); Readings.ItemsSource = null; Status.Text = "Connection settings changed; previous session ended."; }
        if (Scenario.SelectedItem is ConnectionScenario scenario)
        {
            ConnectionRoute.Text = scenario.Route;
            ConnectionRequirements.Text = scenario.Requirements;
            SerialPanel.Visibility = Connectivity.IsSerial(scenario.Kind) ? Visibility.Visible : Visibility.Collapsed;
            TcpPanel.Visibility = scenario.Kind == ConnectionKind.ElmTcp ? Visibility.Visible : Visibility.Collapsed;
            DriverPanel.Visibility = scenario.Kind == ConnectionKind.J2534 ? Visibility.Visible : Visibility.Collapsed;
            FaultPanel.Visibility = scenario.Kind == ConnectionKind.Simulator ? Visibility.Visible : Visibility.Collapsed;
        }
        DriverDetails.Text = DriverChoice.SelectedItem is DriverRegistration driver ? $"Vendor: {driver.Vendor}\nRegistered library: {driver.Library}\nRegistry view is metadata only; DLL architecture and API exports have not been checked." : "No driver selected.";
        var assessment = CurrentAssessment();
        ConnectionAssessmentText.Text = $"{(assessment.CanConnect ? "Available" : assessment.ConfigurationValid ? "Setup complete — implementation required" : "Setup incomplete")}\n{assessment.Message}";
        Buttons();
    }
    private async void RefreshInventory_Click(object sender, RoutedEventArgs e)
    {
        if (inventoryRequest is not null || request is not null || session.Connected) return;
        inventoryRequest = new();
        var cancellation = inventoryRequest.Token;
        inventory = new([], [], []);
        PortChoice.ItemsSource = null; DriverChoice.ItemsSource = null;
        InventoryStatus.Text = "Reading local Windows registration metadata…";
        CancelInventory.IsEnabled = true; Buttons();
        try
        {
            var result = await Task.Run(WindowsConnectionInventory.Read, cancellation).WaitAsync(TimeSpan.FromSeconds(3), cancellation);
            cancellation.ThrowIfCancellationRequested();
            inventory = result;
            PortChoice.ItemsSource = result.Ports;
            DriverChoice.ItemsSource = result.Drivers;
            PortChoice.SelectedIndex = DriverChoice.SelectedIndex = -1;
            InventoryStatus.Text = $"{result.Ports.Count} registered COM ports; {result.Drivers.Count} driver registrations. No devices opened. " + string.Join(" ", result.Warnings);
        }
        catch (OperationCanceledException) { InventoryStatus.Text = "Inventory refresh cancelled. No selection retained."; }
        catch (TimeoutException) { InventoryStatus.Text = "Inventory refresh timed out. Late results are discarded; refresh explicitly to try again."; }
        catch (Exception) { InventoryStatus.Text = "Inventory unavailable. Check local registry permissions and vendor installation."; }
        finally
        {
            inventoryRequest.Dispose(); inventoryRequest = null; CancelInventory.IsEnabled = false;
            UpdateConnectionAssessment();
        }
    }
    private void CancelInventory_Click(object sender, RoutedEventArgs e) => inventoryRequest?.Cancel();
    private void ShowConnection_Click(object sender, RoutedEventArgs e) => MainTabs.SelectedIndex = 0;
}
