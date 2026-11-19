namespace SaabDiag.Core;

// Deliberately separate from DiagnosticSession and ITransport. No wire commands,
// security keys, files or calibration bytes can enter this in-memory demonstration.
public enum DemoAction { ClearDtc, Actuate, Reset, Configure, SecurityAccess, Pair, Program }
public sealed record DemoSnapshot(int DtcCount, int ActuatorCycles, int Resets, bool ComfortOption, bool DemoAccess, bool Paired, string Firmware);
public sealed record DemoPlan(Guid Id, string Module, DemoAction Action, string Preview);
public sealed record DemoResult(bool Applied, string Message, DemoSnapshot State);
public sealed class VirtualWorkshop
{
    private readonly object gate = new();
    private DemoSnapshot state = Initial();
    private DemoPlan? plan;
    private bool busy;
    private int generation;
    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(new[] { "demo-engine", "demo-brakes", "demo-cabin" });
    private static DemoSnapshot Initial() => new(1, 0, 0, false, false, false, "DEMO-1");
    public DemoSnapshot State { get { lock(gate) return state; } }
    public void ResetDemo() { lock(gate) { generation++; state = Initial(); plan = null; } }
    public DemoPlan Prepare(string module, DemoAction action)
    {
        lock(gate)
        {
            plan = null;
            if (busy) throw new InvalidOperationException("A virtual operation is running.");
            if (!Modules.Contains(module) || !Enum.IsDefined(action)) throw new ArgumentException("Unknown demo module or action.");
            var expectedModule = action switch { DemoAction.Actuate => "demo-brakes", DemoAction.Configure or DemoAction.SecurityAccess or DemoAction.Pair => "demo-cabin", _ => "demo-engine" };
            if (module != expectedModule) throw new InvalidOperationException($"This sample action is defined only for {expectedModule}.");
            if (action is DemoAction.Pair or DemoAction.Program && !state.DemoAccess) throw new InvalidOperationException("Run the virtual security-access demonstration first. It generates no keys.");
            string preview = action switch
            {
                DemoAction.ClearDtc => $"Demo DTC count: {state.DtcCount} → 0. Only the virtual workshop fixture changes.",
                DemoAction.Actuate => "Run one fictional actuator cycle; increment the demo counter. No physical output.",
                DemoAction.Reset => "Increment the virtual module reset counter. No ECU restart.",
                DemoAction.Configure => $"Toggle fictional comfort option: {state.ComfortOption} → {!state.ComfortOption}.",
                DemoAction.SecurityAccess => "Set a memory-only demo permission flag. No seed/key algorithm, credentials or vehicle access.",
                DemoAction.Pair => "Mark the fictional cabin module paired. No keys or immobilizer changes.",
                DemoAction.Program => "Change virtual firmware label to DEMO-2. No firmware file, erase, transfer or flash.",
                _ => throw new ArgumentException("Unknown action")
            };
            plan = new(Guid.NewGuid(), module, action, preview);
            return plan;
        }
    }
    public async Task<DemoResult> ApplyAsync(Guid planId, bool confirmed, Fault fault = Fault.None, CancellationToken cancellation = default)
    {
        DemoPlan selected;
        int epoch;
        lock(gate)
        {
            if (busy || !confirmed || plan is null || plan.Id != planId) return new(false, "Demo preview missing, stale, busy or unconfirmed.", state);
            selected = plan; plan = null; busy = true; epoch = generation;
        }
        try
        {
            cancellation.ThrowIfCancellationRequested();
            // Finite local delay makes cancellation visible; no transport exists here.
            await Task.Delay(250, cancellation);
            lock(gate)
            {
                cancellation.ThrowIfCancellationRequested();
                if (epoch != generation) return new(false, "Demo session changed; pending result discarded.", state);
                if (fault != Fault.None) return new(false, $"Simulated {fault}; no virtual changes committed. Preview again.", state);
                state = selected.Action switch
                {
                    DemoAction.ClearDtc => state with { DtcCount = 0 },
                    DemoAction.Actuate => state with { ActuatorCycles = state.ActuatorCycles + 1 },
                    DemoAction.Reset => state with { Resets = state.Resets + 1 },
                    DemoAction.Configure => state with { ComfortOption = !state.ComfortOption },
                    DemoAction.SecurityAccess => state with { DemoAccess = true },
                    DemoAction.Pair => state with { Paired = true },
                    DemoAction.Program => state with { Firmware = "DEMO-2" },
                    _ => throw new InvalidOperationException("Unknown action")
                };
                return new(true, "Virtual change applied and checked in memory. No vehicle operation occurred.", state);
            }
        }
        catch(OperationCanceledException) { lock(gate) return new(false, "Virtual operation cancelled; no changes committed.", state); }
        finally { lock(gate) busy = false; }
    }
}
