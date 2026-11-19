using System.Text.Json;
using System.Text.Json.Serialization;

namespace SaabDiag.Core;

public sealed record ProviderSettings(string ProviderId, string SoftwareVersion, string ServiceAddress, string InstallationPath, string DocumentationAddress)
{
    public void Validate()
    {
        if (!AuthorizationService.Providers.Any(p => p.Id == ProviderId)) throw new ArgumentException("Unknown provider.");
        ValidateText(SoftwareVersion, 128);
        ValidateText(InstallationPath, 1024);
        if (InstallationPath.Length > 0 && !Path.IsPathFullyQualified(InstallationPath))
            throw new ArgumentException("Installation path must be an absolute path.");
        ValidateAddress(ServiceAddress);
        ValidateAddress(DocumentationAddress);
    }
    private static void ValidateText(string? value, int limit)
    {
        if (value is null || value.Length > limit || value.Any(char.IsControl))
            throw new ArgumentException("Setting is missing, too long or contains control characters.");
    }
    private static void ValidateAddress(string? value)
    {
        ValidateText(value, 2048);
        if (value!.Length == 0) return;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !(uri.Scheme == "https" || (uri.Scheme == "http" && uri.IsLoopback)) ||
            uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException("Use HTTPS (or HTTP on localhost), without credentials, query parameters or fragments.");
    }
}

/// <summary>Local, non-secret configuration only. Loading never connects or executes anything.</summary>
public sealed class ProviderSettingsStore(string filePath)
{
    private sealed record Document(int Version, ProviderSettings[] Providers);
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    public IReadOnlyList<ProviderSettings> Load()
    {
        if (!File.Exists(filePath)) return Array.Empty<ProviderSettings>();
        using var stream = File.OpenRead(filePath);
        if (stream.Length > 32768) throw new InvalidDataException("Provider settings file is too large.");
        var doc = JsonSerializer.Deserialize<Document>(stream, Json);
        if (doc is null || doc.Version != 1 || doc.Providers is null) throw new InvalidDataException("Invalid provider settings format.");
        Validate(doc.Providers);
        return Array.AsReadOnly(doc.Providers);
    }
    public void Save(IEnumerable<ProviderSettings> settings)
    {
        var entries = settings.ToArray();
        Validate(entries);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new Document(1, entries), Json);
        if (bytes.Length > 32768) throw new InvalidDataException("Provider settings file is too large.");
        var fullPath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, fullPath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static void Validate(ProviderSettings[] entries)
    {
        if (entries.Length > AuthorizationService.Providers.Count || entries.Any(p => p is null) ||
            entries.Select(p => p.ProviderId).Distinct(StringComparer.Ordinal).Count() != entries.Length)
            throw new InvalidDataException("Invalid or duplicate provider settings.");
        foreach (var entry in entries) entry.Validate();
    }
}
