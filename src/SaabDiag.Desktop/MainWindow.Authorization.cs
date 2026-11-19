using System.Windows;
using System.Windows.Controls;
using SaabDiag.Core;
namespace SaabDiag.Desktop;
public partial class MainWindow
{
    private readonly AuthorizationService authorization = new();
    private CancellationTokenSource? authorizationRequest;
    private readonly ProviderSettingsStore providerSettingsStore = new(System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SaabDiag", "provider-settings.json"));
    private readonly Dictionary<string, ProviderSettings> providerSettings = new(StringComparer.Ordinal);
    private bool providerSettingsLoaded;
    private void InitializeAuthorization()
    {
        AuthorizationProvider.ItemsSource = AuthorizationService.Providers;
        AuthorizationOperation.ItemsSource = new[] { Operation.SecurityAccess, Operation.Pair, Operation.Flash };
        try
        {
            foreach (var settings in providerSettingsStore.Load()) providerSettings.Add(settings.ProviderId, settings);
            providerSettingsLoaded = true;
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException)
        {
            ProviderSettingsStatus.Text = "Saved settings could not be loaded. Saving is disabled to preserve the file. No connection attempted.";
        }
        // Explicit selection: no default provider and no request on startup.
    }
    private void AuthorizationSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        authorization.Invalidate();
        if (RequestAuthorization is null) return;
        RequestAuthorization.IsEnabled = false;
        if (AuthorizationProvider.SelectedItem is not AuthorizationProviderInfo provider) return;
        AuthorizationRequirements.Text = $"Workflow: {provider.Workflow}\n{provider.Requirements}";
        AuthorizationStatusText.Text = "Integration unavailable. No validated provider or hardware session. No request sent.";
        if (ReferenceEquals(sender, AuthorizationProvider))
        {
            var settings = providerSettings.GetValueOrDefault(provider.Id) ?? new(provider.Id, "", "", "", "");
            ProviderVersion.Text = settings.SoftwareVersion;
            ProviderAddress.Text = settings.ServiceAddress;
            ProviderInstallation.Text = settings.InstallationPath;
            ProviderDocumentation.Text = settings.DocumentationAddress;
            SaveProviderSettings.IsEnabled = providerSettingsLoaded;
            if (providerSettingsLoaded) ProviderSettingsStatus.Text = "Settings loaded locally. Unsaved edits are discarded when you select another provider. No connection attempted.";
        }
    }
    private async void RequestAuthorization_Click(object sender, RoutedEventArgs e)
    {
        if (authorizationRequest is not null || AuthorizationProvider.SelectedItem is not AuthorizationProviderInfo provider || AuthorizationOperation.SelectedItem is not Operation operation) return;
        // Remains disabled in the shipped UI; service independently rejects unavailable providers.
        authorizationRequest = new(); CancelAuthorization.IsEnabled = true;
        try
        {
            var result = await authorization.RequestAsync(provider.Id, provider.Workflow, operation, authorizationRequest.Token);
            AuthorizationStatusText.Text = $"{result.Status}: {result.Message}";
        }
        finally { authorizationRequest.Dispose(); authorizationRequest = null; CancelAuthorization.IsEnabled = false; }
    }
    private void CancelAuthorization_Click(object sender, RoutedEventArgs e) => authorizationRequest?.Cancel();

    private void SaveProviderSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!providerSettingsLoaded || AuthorizationProvider.SelectedItem is not AuthorizationProviderInfo provider) return;
        var settings = new ProviderSettings(provider.Id, ProviderVersion.Text.Trim(), ProviderAddress.Text.Trim(),
            ProviderInstallation.Text.Trim(), ProviderDocumentation.Text.Trim());
        try
        {
            var updated = providerSettings.Values.Where(p => p.ProviderId != provider.Id).Append(settings).ToArray();
            providerSettingsStore.Save(updated);
            providerSettings[provider.Id] = settings;
            authorization.Invalidate();
            ProviderSettingsStatus.Text = "Saved locally in %LOCALAPPDATA%\\SaabDiag\\provider-settings.json. No connection or authorization performed.";
        }
        catch (ArgumentException ex) { ProviderSettingsStatus.Text = ex.Message; }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        { ProviderSettingsStatus.Text = "Could not save provider settings. Previous settings retained."; }
    }

    private void CalculateOfflineKey_Click(object sender, RoutedEventArgs e)
    {
        OfflineKeyCalculator.TryCalculate(OfflineKeyCalculator.Trionic8Level01, OfflineSeed.Text, out var key, out var message);
        OfflineKey.Text = key.Length == 0 ? "No key generated." : $"Calculated key: {key}";
        OfflineKeyStatus.Text = message;
    }
    private void OfflineSeedChanged(object sender, TextChangedEventArgs e)
    {
        if (OfflineKey is null || OfflineKeyStatus is null) return;
        OfflineKey.Text = "No key generated.";
        OfflineKeyStatus.Text = "Input changed. Calculate explicitly; nothing is sent or saved.";
    }
    private void ClearOfflineKey_Click(object sender, RoutedEventArgs e)
    {
        OfflineSeed.Clear();
        OfflineKey.Text = "No key generated.";
        OfflineKeyStatus.Text = "Cleared.";
    }
}
