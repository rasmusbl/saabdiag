using System.Text.Json;
namespace SaabDiag.Core;
public sealed record ReferenceEntry(string Id, string Title, string Url, string Kind, string Publisher, string Host, string Applicability, string Rights);
public static class ReferenceCatalog
{
    public static IReadOnlyList<ReferenceEntry> Load(string json)
    {
        if (json.Length > 100000) throw new FormatException("Catalog too large.");
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.GetProperty("schemaVersion").GetInt32() != 1) throw new FormatException("Unsupported catalog version.");
        var entries = JsonSerializer.Deserialize<ReferenceEntry[]>(doc.RootElement.GetProperty("entries").GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new FormatException();
        if (entries.Any(e => e is null || string.IsNullOrWhiteSpace(e.Id) || string.IsNullOrWhiteSpace(e.Title) || string.IsNullOrWhiteSpace(e.Rights) || !Uri.TryCreate(e.Url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo)) || entries.Select(e => e.Id).Distinct().Count() != entries.Length) throw new FormatException("Invalid reference metadata.");
        return Array.AsReadOnly(entries);
    }
}
