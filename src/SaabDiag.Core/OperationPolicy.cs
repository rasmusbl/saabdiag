using System.Text.Json;
namespace SaabDiag.Core;

internal static class Policy
{
    internal static bool Allows(string profile, Operation operation) => Profiles.Supports(profile) &&
        operation is Operation.ReadLiveData or Operation.ReadDtc or Operation.DiscoverModules or Operation.ReadVin;
}
