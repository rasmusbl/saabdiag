using System.Net;
using System.Text.RegularExpressions;
namespace SaabDiag.Core;

public enum ConnectionKind { Simulator, ElmSerial, ElmTcp, J2534, Tech2Vehicle, Tech2Security }
public sealed record ConnectionScenario(ConnectionKind Kind, string Name, string Route, string Requirements);
public sealed record ConnectionSettings(ConnectionKind Kind, int Year, string SerialPort = "", int BaudRate = 0, string Host = "", int TcpPort = 0, string DriverId = "");
public sealed record ConnectionAssessment(bool ConfigurationValid, bool CanConnect, string Message);
public sealed record DriverRegistration(string Id, string Name, string Vendor, string ApiVersion, string RegistryView, string Library)
{
    public string DisplayName => $"{Name} | API {ApiVersion} | {RegistryView} registry";
}
public sealed record LocalInventory(IReadOnlyList<string> Ports, IReadOnlyList<DriverRegistration> Drivers, IReadOnlyList<string> Warnings);
public static class Connectivity
{
    public static IReadOnlyList<ConnectionScenario> Scenarios { get; } = Array.AsReadOnly(new[] {
        new ConnectionScenario(ConnectionKind.Simulator, "Offline simulator", "PC → synthetic fixtures (no adapter)", "Available: sample live data, DTCs, VIN and inventory. No real vehicle coverage."),
        new ConnectionScenario(ConnectionKind.ElmSerial, "ELM-compatible serial adapter", "PC → explicit COM port → adapter → vehicle", "USB or an already paired Bluetooth serial port. Requires exact adapter, baud rate, driver and reviewed generic OBD codec. Hardware communication is not implemented."),
        new ConnectionScenario(ConnectionKind.ElmTcp, "ELM-compatible TCP adapter", "PC → explicit IP address and TCP port → adapter → vehicle", "Requires a documented ELM-compatible TCP endpoint, adapter model and reviewed codec. No Wi-Fi scanning, pairing or network connection is performed."),
        new ConnectionScenario(ConnectionKind.J2534, "J2534 / MDI candidate", "PC → selected vendor driver → adapter → vehicle", "Requires installed vendor driver, API/bitness verification, native broker, network support and exact Saab module profiles. A registry entry is not verified compatibility."),
        new ConnectionScenario(ConnectionKind.Tech2Vehicle, "Physical Tech 2 — vehicle diagnostics", "PC companion → Tech 2 → appropriate vehicle interface → vehicle", "Separate companion integration, not the PC-only replacement. Requires scanner/card version and documented PC protocol. CANdi use depends on the vehicle/software instructions. Not implemented."),
        new ConnectionScenario(ConnectionKind.Tech2Security, "Physical Tech 2 — security-access setup", "PC authorization software → Tech 2", "Records the scanner-to-PC scenario described by your contact. Requires exact TIS/provider, card version and serial connection documentation. CANdi is not a PC authorization interface. Authorization provider options are listed in the Authorization tab; no real integration or code generator is implemented.")
    });
    public static bool IsSerial(ConnectionKind kind) => kind is ConnectionKind.ElmSerial or ConnectionKind.Tech2Vehicle or ConnectionKind.Tech2Security;
    public static bool IsPort(string? port) => port is not null && Regex.IsMatch(port, "^COM[1-9][0-9]{0,4}$", RegexOptions.CultureInvariant);
    public static ConnectionAssessment Assess(ConnectionSettings settings, LocalInventory inventory)
    {
        if (!Enum.IsDefined(settings.Kind)) return new(false, false, "Unknown connection scenario; no connection attempted.");
        if (!Profiles.Years.Contains(settings.Year)) return new(false, false, "Unsupported model year. Select 2003–2012.");
        if (settings.Kind == ConnectionKind.Simulator) return new(true, true, "Ready for explicit simulator connection. No hardware will be accessed.");
        if (IsSerial(settings.Kind))
        {
            if (!IsPort(settings.SerialPort)) return new(false, false, "Select an explicit COM port. No port is selected automatically.");
            if (!inventory.Ports.Contains(settings.SerialPort)) return new(false, false, "Selected port is absent from the latest local inventory. Refresh and select again.");
            if (settings.BaudRate < 300 || settings.BaudRate > 1000000) return new(false, false, "Enter the documented adapter/scanner baud rate (300–1000000). No default is assumed.");
        }
        if (settings.Kind == ConnectionKind.ElmTcp)
        {
            if (settings.Host.Contains('%') || !IPAddress.TryParse(settings.Host, out var ip) ||
                ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.Equals(IPAddress.Broadcast) ||
                ip.IsIPv6Multicast || (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && ip.GetAddressBytes()[0] >= 224))
                return new(false, false, "Enter an explicit unicast IP address from the adapter documentation; URLs and automatic discovery are not supported.");
            if (settings.TcpPort is < 1 or > 65535) return new(false, false, "Enter the documented TCP port (1–65535).");
        }
        if (settings.Kind == ConnectionKind.J2534)
        {
            var registrations = inventory.Drivers.Where(d => d.Id == settings.DriverId).ToArray();
            if (registrations.Length != 1) return new(false, false, "Select one driver from the latest inventory; missing or ambiguous registration.");
            var driver = registrations[0];
            if (driver.ApiVersion != "04.04") return new(false, false, "This driver uses a different J2534 API. API 05.00 is not interchangeable with 04.04; neither broker is implemented yet.");
            if (string.IsNullOrWhiteSpace(driver.Library)) return new(false, false, "Driver registration has no FunctionLibrary. Repair through the vendor's documented installer.");
        }
        return new(true, false, "Setup fields are complete, but hardware communication is not implemented or validated for this scenario. No port opened, DLL loaded, network request or vehicle command sent.");
    }
}
