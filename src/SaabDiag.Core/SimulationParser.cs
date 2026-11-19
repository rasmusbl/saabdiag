using System.Text.Json;
namespace SaabDiag.Core;

internal static class SimulationParser
{
    internal static Result Parse(string data, Operation expected)
    {
        if (data.Length > 4096) throw new FormatException();
        using var document = JsonDocument.Parse(data);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new FormatException();
        var keys = root.EnumerateObject().Select(p => p.Name).ToArray();
        if (keys.Distinct().Count() != keys.Length) throw new FormatException();
        if (root.GetProperty("version").GetInt32() != 1 || root.GetProperty("operation").GetString() != expected.ToString()) throw new FormatException();
        if (root.GetProperty("status").GetString() == "unsupported")
            return new(Outcome.Unsupported, "Operation unsupported; session ended.", []);
        if (root.GetProperty("status").GetString() != "ok") throw new FormatException();
        if (expected == Operation.ReadLiveData)
        {
            int Value(string key, int min, int max)
            {
                var value = root.GetProperty(key).GetInt32();
                if (value < min || value > max) throw new FormatException();
                return value;
            }
            return new(Outcome.Success, "Synthetic sample — not vehicle measurements.", [
                new("Engine speed", Value("rpm", 0, 10000).ToString(), "rpm"),
                new("Coolant", Value("coolantC", -40, 150).ToString(), "°C"),
                new("Vehicle speed", Value("speedKph", 0, 300).ToString(), "km/h")]);
        }
        if (expected == Operation.ReadVin)
        {
            var vin = root.GetProperty("vin").GetString();
            // Accept only the explicitly synthetic identifier; not a real VIN codec.
            if (vin != "DEMO0000000000000") throw new FormatException();
            return new(Outcome.Success, "Synthetic VIN placeholder; not a valid vehicle identity.", [new("Demo VIN", vin, "Synthetic")]);
        }
        if (expected == Operation.DiscoverModules)
        {
            var modules = root.GetProperty("modules");
            if (modules.ValueKind != JsonValueKind.Array || modules.GetArrayLength() != 3) throw new FormatException();
            var ids = modules.EnumerateArray().Select(m => m.GetString()).ToArray();
            if (!ids.SequenceEqual(new[] { "demo-engine", "demo-brakes", "demo-cabin" })) throw new FormatException();
            return new(Outcome.Success, "Synthetic inventory; no buses or addresses probed.", ids.Select(id => new Reading(id!, "Available in demo", "Synthetic module")).ToArray());
        }
        if (expected != Operation.ReadDtc) throw new FormatException();
        var codes = root.GetProperty("codes");
        if (codes.ValueKind != JsonValueKind.Array || codes.GetArrayLength() > 100) throw new FormatException();
        var rows = new List<Reading>();
        foreach (var code in codes.EnumerateArray())
        {
            var value = code.GetString();
            if (value is null || !System.Text.RegularExpressions.Regex.IsMatch(value, "^[PCBU][0-3][0-9A-F]{3}$")) throw new FormatException();
            rows.Add(new("Synthetic DTC", value, "Demo only"));
        }
        return new(Outcome.Success, "Synthetic DTC fixture — not a vehicle diagnosis.", rows.AsReadOnly());
    }
}
