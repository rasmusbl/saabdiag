using System.Globalization;

namespace SaabDiag.Core;

/// <summary>Offline calculation only; never grants an AuthorizationService approval.</summary>
public static class OfflineKeyCalculator
{
    public const string Trionic8Level01 = "trionic8-level01";

    // Original implementation of the published mathematical transform.
    // Provenance, scope and test-vector limitations: docs/CODE_GENERATION.md.
    public static bool TryCalculate(string? algorithm, string? seedHex, out string keyHex, out string message)
    {
        keyHex = "";
        if (algorithm != Trionic8Level01)
        {
            message = "Unsupported algorithm. Only Trionic 8 level 0x01 is available offline.";
            return false;
        }
        if (seedHex is null || seedHex.Length != 4 || !seedHex.All(char.IsAsciiHexDigit) ||
            !ushort.TryParse(seedHex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var seed))
        {
            message = "Enter exactly four hexadecimal digits, high byte first (no spaces or 0x prefix).";
            return false;
        }
        if (seed == 0)
        {
            message = "Zero seed: no key generated. Offline input cannot establish ECU authorization state.";
            return false;
        }
        // Rotate a 16-bit value right by five; add the published offset modulo 65536.
        var rotated = (seed / 32) + (seed % 32) * 2048;
        keyHex = ((rotated + 0xB988) % 65536).ToString("X4", CultureInfo.InvariantCulture);
        message = "Calculated offline. No ECU response checked; no vehicle or Tech 2 authorization granted.";
        return true;
    }
}
