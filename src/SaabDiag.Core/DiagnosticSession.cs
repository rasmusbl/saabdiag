using System.Text.Json;
namespace SaabDiag.Core;

public sealed class DiagnosticSession : IDisposable
{
    private readonly object gate = new();
    private ITransport? transport;
    private string profile = "";
    private CancellationTokenSource? lifetime;
    private bool busy;
    public bool Connected { get { lock (gate) return lifetime is not null; } }
    public void ConnectSimulation(int year, Fault fault = Fault.None) => Connect(Profiles.Simulation(year), new SimulatedTransport(fault));
    public ConnectionAssessment ConnectConfigured(ConnectionSettings settings, LocalInventory inventory, Fault fault = Fault.None)
    {
        lock (gate)
        {
            // A rejected route must never retain an old simulated connection.
            Disconnect();
            var assessment = Connectivity.Assess(settings, inventory);
            if (!assessment.CanConnect) return assessment;
            if (settings.Kind != ConnectionKind.Simulator) return new(false, false, "Unsupported connection route.");
            if (!Enum.IsDefined(fault)) return new(false, false, "Unknown simulation fault.");
            ConnectSimulation(settings.Year, fault);
            return assessment;
        }
    }
    internal void Connect(string requestedProfile, ITransport selectedTransport)
    {
        lock (gate)
        {
            Disconnect();
            if (!Profiles.Supports(requestedProfile)) throw new ArgumentException("Unknown simulation profile.");
            profile = requestedProfile;
            transport = selectedTransport;
            lifetime = new();
        }
    }
    public void Disconnect()
    {
        lock (gate)
        {
            var previous = lifetime;
            lifetime = null;
            transport = null;
            profile = "";
            // Invalidate identity before cancellation callbacks can resume a read.
            previous?.Cancel();
            previous?.Dispose();
        }
    }
    public async Task<Result> ReadAsync(Operation operation, CancellationToken cancellation = default)
    {
        ITransport selected;
        CancellationTokenSource session;
        CancellationTokenSource linked;
        lock (gate)
        {
            if (lifetime is null || transport is null) return new(Outcome.Disconnected, "Connect the simulator first.", []);
            if (!Policy.Allows(profile, operation)) return new(Outcome.Denied, "Operation denied by policy.", []);
            if (busy) return new(Outcome.Busy, "Another read is running.", []);
            busy = true;
            selected = transport;
            session = lifetime;
            linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, session.Token);
        }
        using (linked)
        {
            Result result;
            try
            {
                linked.Token.ThrowIfCancellationRequested();
                var data = await selected.ReadAsync(operation, linked.Token).WaitAsync(TimeSpan.FromSeconds(2), linked.Token);
                linked.Token.ThrowIfCancellationRequested();
                result = SimulationParser.Parse(data, operation);
            }
            catch (OperationCanceledException) { result = new(Outcome.Cancelled, "Read cancelled; session ended.", []); }
            catch (TimeoutException) { result = new(Outcome.Timeout, "Read timed out; session ended.", []); }
            catch (IOException) { result = new(Outcome.Disconnected, "Transport disconnected; session ended.", []); }
            catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException or KeyNotFoundException or OverflowException)
            { result = new(Outcome.Malformed, "Invalid simulator response; session ended.", []); }
            catch (Exception) { result = new(Outcome.Malformed, "Transport failed; session ended.", []); }
            finally { linked.Cancel(); }
            lock (gate)
            {
                busy = false;
                if (!ReferenceEquals(lifetime, session)) return new(Outcome.Disconnected, "Session changed; result discarded.", []);
                if (result.Outcome != Outcome.Success) Disconnect();
                return result;
            }
        }
    }
    public void Dispose() => Disconnect();
}
