namespace SaabDiag.Core;

public enum AuthorizationWorkflow { Tech2, DirectEcu }
public enum AuthorizationStatus { Approved, Unavailable, Unsupported, Disconnected, Busy, Cancelled, Timeout, Rejected, InvalidResponse }
public sealed record AuthorizationProviderInfo(string Id, string Name, AuthorizationWorkflow Workflow, string Requirements);
public sealed record AuthorizationScope(Guid SessionId, string ProfileId, string TargetReference, Operation Operation);
public sealed record AuthorizationRequest(Guid RequestId, string ProviderId, AuthorizationWorkflow Workflow, AuthorizationScope Scope);
// No seed, key, credential, VIN or generated code is exposed by this decision contract.
// A future implementation needs a separately reviewed secret-handling contract if required.
public sealed record AuthorizationResponse(Guid RequestId, string ProviderId, AuthorizationWorkflow Workflow, AuthorizationScope Scope, bool Approved, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt);
public sealed record AuthorizationResult(AuthorizationStatus Status, string Message);
public interface IAuthorizationProvider
{
    string Id { get; }
    bool Supports(AuthorizationWorkflow workflow, string profileId, string targetReference, Operation operation);
    Task<AuthorizationResponse> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellation);
}

public sealed class AuthorizationService : IDisposable
{
    public static IReadOnlyList<AuthorizationProviderInfo> Providers { get; } = Array.AsReadOnly(new[] {
        new AuthorizationProviderInfo("tis2000", "TIS2000 — physical Tech 2", AuthorizationWorkflow.Tech2, "Needs the exact Saab TIS2000/card version, a documented PC-to-scanner integration and authorized access. No TIS2000 connector is implemented."),
        new AuthorizationProviderInfo("globaltis", "GlobalTIS — physical Tech 2", AuthorizationWorkflow.Tech2, "Needs the exact Saab GlobalTIS release, supported authorization interface, access requirements and scanner/card coverage. No GlobalTIS connector is implemented."),
        new AuthorizationProviderInfo("external-tech2", "External Tech 2 authorization provider", AuthorizationWorkflow.Tech2, "Needs a provider API/protocol, authentication method, reuse terms and verified scanner/card compatibility. No external service is configured or contacted."),
        new AuthorizationProviderInfo("ecu-provider", "Direct ECU — authorization provider", AuthorizationWorkflow.DirectEcu, "Needs exact ECU identity, security level/session, documented provider integration and validated transport. No direct ECU provider is implemented."),
        new AuthorizationProviderInfo("ecu-code-generator", "Direct ECU — reviewed code generator", AuthorizationWorkflow.DirectEcu, "Offline Trionic 8 level 0x01 calculation is available below. Vehicle integration still needs exact module/software evidence, independent vectors and a validated transport. Calculation cannot grant an authorization decision.")
    });
    private readonly Dictionary<string, IAuthorizationProvider> implementations;
    private readonly TimeProvider time;
    private readonly TimeSpan timeout;
    private readonly object gate = new();
    private CancellationTokenSource? session;
    private Guid sessionId;
    private string profile = "";
    private string target = "";
    private bool busy;
    private AuthorizationResponse? approval;

    // Production ships with no executable provider. No reflection/plugin/DLL/URL loading.
    public AuthorizationService() : this([], TimeProvider.System, TimeSpan.FromSeconds(5)) { }
    internal AuthorizationService(IEnumerable<IAuthorizationProvider> providers, TimeProvider time, TimeSpan timeout)
    {
        implementations = providers.ToDictionary(p => p.Id, StringComparer.Ordinal);
        this.time = time;
        this.timeout = timeout > TimeSpan.Zero && timeout <= TimeSpan.FromSeconds(30) ? timeout : throw new ArgumentOutOfRangeException(nameof(timeout));
    }
    public bool IsImplemented(string id) => Providers.Any(p => p.Id == id) && implementations.ContainsKey(id);
    // Future validated connection code owns session binding, not UI text fields.
    internal void BeginSession(string profileId, string targetReference)
    {
        lock (gate)
        {
            Invalidate();
            if (string.IsNullOrWhiteSpace(profileId) || profileId.Length > 128 || string.IsNullOrWhiteSpace(targetReference) || targetReference.Length > 128)
                throw new ArgumentException("Invalid authorization context.");
            profile = profileId; target = targetReference; sessionId = Guid.NewGuid(); session = new();
        }
    }
    public void Invalidate()
    {
        lock(gate)
        {
            var previous = session;
            session = null; sessionId = Guid.Empty; approval = null; profile = target = "";
            previous?.Cancel(); previous?.Dispose();
        }
    }
    public bool HasApproval(string providerId, AuthorizationWorkflow workflow, Operation operation)
    {
        lock(gate)
        {
            var now = time.GetUtcNow();
            return session is not null && approval is { Approved: true } a && a.ProviderId == providerId && a.Workflow == workflow &&
                a.Scope == new AuthorizationScope(sessionId, profile, target, operation) && a.IssuedAt <= now && a.ExpiresAt > now;
        }
    }
    public async Task<AuthorizationResult> RequestAsync(string providerId, AuthorizationWorkflow workflow, Operation operation, CancellationToken cancellation = default)
    {
        IAuthorizationProvider provider;
        AuthorizationRequest request;
        CancellationTokenSource linked;
        CancellationTokenSource selectedSession;
        lock(gate)
        {
            if (busy) return new(AuthorizationStatus.Busy, "An authorization request is already running.");
            approval = null;
            var info = Providers.SingleOrDefault(p => p.Id == providerId);
            if (info is null || info.Workflow != workflow || operation is not (Operation.SecurityAccess or Operation.Pair or Operation.Flash))
                return new(AuthorizationStatus.Unsupported, "Unknown provider, mismatched workflow or unsupported operation.");
            if (!implementations.TryGetValue(providerId, out provider!)) return new(AuthorizationStatus.Unavailable, "Provider integration is not implemented. No request sent and no code generated.");
            if (session is null) return new(AuthorizationStatus.Disconnected, "No validated authorization session.");
            try
            {
                if (!provider.Supports(workflow, profile, target, operation)) return new(AuthorizationStatus.Unsupported, "Provider does not support this exact context.");
            }
            catch(Exception) { return new(AuthorizationStatus.Rejected, "Provider capability check failed."); }
            selectedSession = session;
            request = new(Guid.NewGuid(), providerId, workflow, new(sessionId, profile, target, operation));
            linked = CancellationTokenSource.CreateLinkedTokenSource(session.Token, cancellation);
            busy = true;
        }
        using(linked)
        {
            AuthorizationResponse? response = null;
            AuthorizationResult result;
            try
            {
                linked.Token.ThrowIfCancellationRequested();
                response = await provider.AuthorizeAsync(request, linked.Token).WaitAsync(timeout, linked.Token);
                linked.Token.ThrowIfCancellationRequested();
                var now = time.GetUtcNow();
                if (response is null || response.RequestId != request.RequestId || response.ProviderId != providerId || response.Workflow != workflow ||
                    response.Scope != request.Scope || response.IssuedAt > now || response.ExpiresAt <= now || response.ExpiresAt <= response.IssuedAt ||
                    response.ExpiresAt - response.IssuedAt > TimeSpan.FromMinutes(5))
                    result = new(AuthorizationStatus.InvalidResponse, "Provider response has invalid identity, scope or expiry.");
                else result = response.Approved ? new(AuthorizationStatus.Approved, "Provider decision accepted. Vehicle operations remain subject to their separate policy gates.") : new(AuthorizationStatus.Rejected, "Provider rejected the request.");
            }
            catch(OperationCanceledException) { result = new(AuthorizationStatus.Cancelled, "Authorization cancelled. No approval retained."); }
            catch(TimeoutException) { result = new(AuthorizationStatus.Timeout, "Authorization timed out. No automatic retry."); }
            catch(Exception) { result = new(AuthorizationStatus.Rejected, "Provider failed. No approval retained."); }
            finally { linked.Cancel(); }
            lock(gate)
            {
                busy = false;
                if (!ReferenceEquals(session, selectedSession)) return new(AuthorizationStatus.Disconnected, "Session changed; late authorization discarded.");
                if (cancellation.IsCancellationRequested) return new(AuthorizationStatus.Cancelled, "Authorization cancelled. No approval retained.");
                if (result.Status == AuthorizationStatus.Approved && response!.ExpiresAt <= time.GetUtcNow())
                    return new(AuthorizationStatus.InvalidResponse, "Authorization expired before acceptance.");
                if (result.Status == AuthorizationStatus.Approved) approval = response;
                return result;
            }
        }
    }
    public void Dispose() => Invalidate();
}
