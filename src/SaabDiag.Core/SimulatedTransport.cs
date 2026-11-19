using System.Text.Json;
namespace SaabDiag.Core;

// Internal transport exchanges named operations only. This JSON format is synthetic,
// not OBD, CAN, J2534 or a Saab wire protocol.
internal interface ITransport
{
    Task<string> ReadAsync(Operation operation, CancellationToken cancellation);
}
internal sealed class SimulatedTransport(Fault fault) : ITransport
{
    public async Task<string> ReadAsync(Operation operation, CancellationToken cancellation)
    {
        if (fault == Fault.Timeout) await Task.Delay(Timeout.Infinite, cancellation);
        cancellation.ThrowIfCancellationRequested();
        return fault switch
        {
            Fault.Malformed => "{broken",
            Fault.Unsupported => JsonSerializer.Serialize(new { version = 1, operation = operation.ToString(), status = "unsupported" }),
            Fault.Disconnect => throw new IOException("Simulated disconnect"),
            _ when operation == Operation.ReadLiveData => "{\"version\":1,\"operation\":\"ReadLiveData\",\"status\":\"ok\",\"rpm\":850,\"coolantC\":88,\"speedKph\":0}",
            _ when operation == Operation.ReadDtc => "{\"version\":1,\"operation\":\"ReadDtc\",\"status\":\"ok\",\"codes\":[\"P0300\"]}",
            _ when operation == Operation.ReadVin => JsonSerializer.Serialize(new { version = 1, operation = operation.ToString(), status = "ok", vin = "DEMO0000000000000" }),
            _ when operation == Operation.DiscoverModules => JsonSerializer.Serialize(new { version = 1, operation = operation.ToString(), status = "ok", modules = new[] { "demo-engine", "demo-brakes", "demo-cabin" } }),
            _ => throw new InvalidOperationException("Unsupported simulator operation")
        };
    }
}
