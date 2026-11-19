using System.IO;
using System.Security;
using Microsoft.Win32;
using SaabDiag.Core;
namespace SaabDiag.Desktop;

// Metadata only. Never opens a COM handle, loads a DLL or calls a vendor API.
internal static class WindowsConnectionInventory
{
    public static LocalInventory Read()
    {
        var ports = new HashSet<string>(StringComparer.Ordinal);
        var drivers = new List<DriverRegistration>();
        var warnings = new List<string>();
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM", false);
            if (key is not null)
                foreach (var name in key.GetValueNames().Take(256))
                    if (key.GetValue(name) is string value && Connectivity.IsPort(value)) ports.Add(value);
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        { warnings.Add("Serial registry metadata could not be read; no serial ports are available for selection."); }
        foreach (var view in Environment.Is64BitOperatingSystem ? new[] { RegistryView.Registry32, RegistryView.Registry64 } : new[] { RegistryView.Registry32 })
        foreach (var api in new[] { "04.04", "05.00" })
        {
            try
            {
                using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var root = machine.OpenSubKey(@"SOFTWARE\PassThruSupport." + api, false);
                if (root is null) continue;
                foreach (var child in root.GetSubKeyNames().Take(256))
                {
                    try
                    {
                        using var entry = root.OpenSubKey(child, false);
                        if (entry is null) continue;
                        string Value(string name) => entry.GetValue(name, "", RegistryValueOptions.DoNotExpandEnvironmentNames) is string value ? value[..Math.Min(value.Length, 1024)] : "";
                        drivers.Add(new($"{view}/{api}/{child}", Value("Name") is { Length: > 0 } title ? title : child, Value("Vendor"), api, view.ToString(), Value("FunctionLibrary")));
                    }
                    catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
                    { warnings.Add($"One {view} API {api} registration could not be read."); }
                }
            }
            catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
            { warnings.Add($"J2534 {view} API {api} metadata could not be read."); }
        }
        return new(ports.Order().ToArray(), drivers.OrderBy(d => d.DisplayName).ToArray(), warnings.ToArray());
    }
}
