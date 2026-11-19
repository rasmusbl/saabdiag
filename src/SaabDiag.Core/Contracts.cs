using System.Text.Json;
namespace SaabDiag.Core;

public enum Operation { ReadLiveData, ReadDtc, DiscoverModules, ReadVin, ClearDtc, Reset, Actuate, Configure, SecurityAccess, Pair, Flash }
public enum Fault { None, Malformed, Unsupported, Disconnect, Timeout }
public enum Outcome { Success, Denied, Disconnected, Busy, Cancelled, Timeout, Malformed, Unsupported }
public sealed record Reading(string Name, string Value, string Unit);
public sealed record Result(Outcome Outcome, string Message, IReadOnlyList<Reading> Readings);
