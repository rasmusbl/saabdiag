using SaabDiag.Core;

int passed = 0;
async Task Test(string name, Func<Task> run)
{
    try { await run(); Console.WriteLine($"PASS {name}"); passed++; }
    catch (Exception ex) { Console.Error.WriteLine($"FAIL {name}: {ex}"); Environment.ExitCode = 1; }
}
void Check(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
Task Sync(Action action) { action(); return Task.CompletedTask; }
await Test("Every target year has a simulation profile", () => Sync(() => {
    foreach (var year in Enumerable.Range(2003,10)) { using var s = new DiagnosticSession(); s.ConnectSimulation(year); Check(s.Connected); }
}));
await Test("Unknown profiles denied", () => Sync(() => {
    using var s = new DiagnosticSession();
    try { s.ConnectSimulation(2013); throw new Exception("Accepted unknown year"); } catch (ArgumentException) { Check(!s.Connected); }
}));
await Test("Reads require explicit connection", async () => { using var s = new DiagnosticSession(); Check((await s.ReadAsync(Operation.ReadLiveData)).Outcome == Outcome.Disconnected); });
await Test("Valid live fixture values", async () => { using var s = new DiagnosticSession(); s.ConnectSimulation(2008); var r = await s.ReadAsync(Operation.ReadLiveData); Check(r.Outcome == Outcome.Success && r.Readings.Count == 3 && r.Readings[0].Value == "850"); });
await Test("Valid synthetic DTC", async () => { using var s = new DiagnosticSession(); s.ConnectSimulation(2008); var r = await s.ReadAsync(Operation.ReadDtc); Check(r.Outcome == Outcome.Success && r.Readings.Single().Value == "P0300"); });
await Test("All writes and unknown operations denied before transport", async () => {
    var transport = new CountingTransport(); using var s = new DiagnosticSession(); s.Connect(Profiles.Simulation(2008), transport);
    foreach (var op in Enum.GetValues<Operation>().Where(o => o is not Operation.ReadDtc and not Operation.ReadLiveData and not Operation.DiscoverModules and not Operation.ReadVin).Append((Operation)999)) Check((await s.ReadAsync(op)).Outcome == Outcome.Denied);
    Check(transport.Calls == 0);
});
foreach (var (fault, outcome) in new[] { (Fault.Malformed, Outcome.Malformed), (Fault.Unsupported, Outcome.Unsupported), (Fault.Disconnect, Outcome.Disconnected), (Fault.Timeout, Outcome.Timeout) })
    await Test($"{fault} fails closed", async () => { using var s = new DiagnosticSession(); s.ConnectSimulation(2008, fault); var r = await s.ReadAsync(Operation.ReadLiveData); Check(r.Outcome == outcome && r.Readings.Count == 0 && !s.Connected); });
await Test("Precancelled read sends nothing", async () => {
    using var s = new DiagnosticSession(); var t = new CountingTransport(); s.Connect(Profiles.Simulation(2008), t);
    using var c = new CancellationTokenSource(); c.Cancel(); Check((await s.ReadAsync(Operation.ReadLiveData,c.Token)).Outcome == Outcome.Cancelled); Check(t.Calls == 0 && !s.Connected);
});
await Test("In-flight cancellation", async () => {
    using var s = new DiagnosticSession(); var t = new PendingTransport(); s.Connect(Profiles.Simulation(2008),t);
    using var c = new CancellationTokenSource(); var pending = s.ReadAsync(Operation.ReadLiveData,c.Token); c.Cancel();
    Check((await pending).Outcome == Outcome.Cancelled && !s.Connected);
});
await Test("Disconnect discards pending reply", async () => {
    using var s = new DiagnosticSession(); s.Connect(Profiles.Simulation(2008),new PendingTransport());
    var pending = s.ReadAsync(Operation.ReadLiveData); s.Disconnect(); var r = await pending;
    Check(r.Outcome == Outcome.Disconnected && r.Readings.Count == 0);
});
await Test("Reconnection cannot receive old results", async () => {
    using var s = new DiagnosticSession(); s.Connect(Profiles.Simulation(2008),new PendingTransport());
    var pending = s.ReadAsync(Operation.ReadLiveData); s.ConnectSimulation(2009);
    Check((await pending).Outcome == Outcome.Disconnected && s.Connected); Check((await s.ReadAsync(Operation.ReadDtc)).Outcome == Outcome.Success);
});
await Test("Concurrent requests rejected", async () => {
    using var s = new DiagnosticSession(); s.Connect(Profiles.Simulation(2008),new PendingTransport());
    var pending = s.ReadAsync(Operation.ReadLiveData); Check((await s.ReadAsync(Operation.ReadDtc)).Outcome == Outcome.Busy); s.Disconnect(); await pending;
});
foreach (var payload in new[] { "{}", "[]", "null", "{", "{\"version\":1,\"operation\":\"ReadDtc\",\"status\":\"ok\",\"codes\":[]}",
    "{\"version\":1,\"version\":1,\"operation\":\"ReadLiveData\",\"status\":\"ok\",\"rpm\":850,\"coolantC\":88,\"speedKph\":0}",
    "{\"version\":1,\"operation\":\"ReadLiveData\",\"status\":\"ok\",\"rpm\":-1,\"coolantC\":88,\"speedKph\":0}",
    "{\"version\":1,\"operation\":\"ReadLiveData\",\"status\":\"ok\",\"rpm\":850}", new string('x',4097) })
    await Test("Malformed, missing or mismatched fixture rejected", async () => {
        using var s = new DiagnosticSession(); s.Connect(Profiles.Simulation(2008),new PayloadTransport(payload));
        var r = await s.ReadAsync(Operation.ReadLiveData); Check(r.Outcome == Outcome.Malformed && r.Readings.Count == 0 && !s.Connected);
    });
await Test("Reference catalog parses", () => Sync(() => Check(ReferenceCatalog.Load(File.ReadAllText("references/catalog.json")).Count == 3)));
foreach (var url in new[] { "file:///C:/secret", "javascript:alert(1)", "http://example.com", "https://user:password@example.com" })
    await Test("Unsafe reference URL rejected", () => Sync(() => {
        var json = System.Text.Json.JsonSerializer.Serialize(new { schemaVersion=1, entries=new[] { new { id="x",title="Test",url,rights="Link only" } } });
        try { ReferenceCatalog.Load(json); throw new Exception("Accepted unsafe URL"); } catch (FormatException) { }
    }));
await Test("Unsupported DTC response", async () => { using var s = new DiagnosticSession(); s.ConnectSimulation(2008,Fault.Unsupported); Check((await s.ReadAsync(Operation.ReadDtc)).Outcome == Outcome.Unsupported && !s.Connected); });
await Test("Invalid DTC rejected", async () => {
    using var s = new DiagnosticSession(); s.Connect(Profiles.Simulation(2008),new PayloadTransport("{\"version\":1,\"operation\":\"ReadDtc\",\"status\":\"ok\",\"codes\":[\"INVALID\"]}"));
    Check((await s.ReadAsync(Operation.ReadDtc)).Outcome == Outcome.Malformed && !s.Connected);
});
await Test("Unknown catalog version rejected", () => Sync(() => {
    try { ReferenceCatalog.Load("{\"schemaVersion\":2,\"entries\":[]}"); throw new Exception("Accepted version"); } catch (FormatException) { }
}));
await Test("Null catalog entries rejected", () => Sync(() => {
    try { ReferenceCatalog.Load("{\"schemaVersion\":1,\"entries\":[null]}"); throw new Exception("Accepted null entry"); } catch (FormatException) { }
}));
await Test("Synthetic VIN read", async () => { using var s = new DiagnosticSession(); s.ConnectSimulation(2008); var r=await s.ReadAsync(Operation.ReadVin); Check(r.Outcome == Outcome.Success && r.Readings.Single().Value == "DEMO0000000000000"); });
await Test("Synthetic inventory read", async () => { using var s = new DiagnosticSession(); s.ConnectSimulation(2008); var r=await s.ReadAsync(Operation.DiscoverModules); Check(r.Outcome == Outcome.Success && r.Readings.Count == 3); });
await Test("Nonfixture VIN rejected", async () => { using var s = new DiagnosticSession(); s.Connect(Profiles.Simulation(2008),new PayloadTransport("{\"version\":1,\"operation\":\"ReadVin\",\"status\":\"ok\",\"vin\":\"WRONG\"}")); Check((await s.ReadAsync(Operation.ReadVin)).Outcome == Outcome.Malformed && !s.Connected); });
await Test("Unknown discovered module rejected", async () => { using var s = new DiagnosticSession(); s.Connect(Profiles.Simulation(2008),new PayloadTransport("{\"version\":1,\"operation\":\"DiscoverModules\",\"status\":\"ok\",\"modules\":[\"unknown\"]}")); Check((await s.ReadAsync(Operation.DiscoverModules)).Outcome == Outcome.Malformed); });
await Test("Every virtual workflow changes expected state", async () => {
    var w = new VirtualWorkshop();
    foreach(var action in new[] { DemoAction.ClearDtc, DemoAction.Actuate, DemoAction.Reset, DemoAction.Configure, DemoAction.SecurityAccess, DemoAction.Pair, DemoAction.Program })
    {
        var module = action switch { DemoAction.Actuate => "demo-brakes", DemoAction.Configure or DemoAction.SecurityAccess or DemoAction.Pair => "demo-cabin", _ => "demo-engine" };
        var p = w.Prepare(module,action); Check((await w.ApplyAsync(p.Id,true)).Applied);
        Check(!(await w.ApplyAsync(p.Id,true)).Applied);
    }
    Check(w.State == new DemoSnapshot(0,1,1,true,true,true,"DEMO-2"));
});
await Test("Unknown demo actions and modules denied", () => Sync(() => {
    var w = new VirtualWorkshop();
    foreach(var pair in new[] { ("unknown",DemoAction.ClearDtc), ("demo-engine",(DemoAction)999) })
    { try { w.Prepare(pair.Item1,pair.Item2); throw new Exception("Accepted unknown"); } catch(ArgumentException) {} }
}));
await Test("Unsupported module action denied", () => Sync(() => { try { new VirtualWorkshop().Prepare("demo-brakes",DemoAction.Program); throw new Exception("Accepted mismatch"); } catch(InvalidOperationException) {} }));
await Test("Demo security required for pairing and programming", () => Sync(() => {
    foreach(var pair in new[] { ("demo-cabin",DemoAction.Pair), ("demo-engine",DemoAction.Program) })
    { try { new VirtualWorkshop().Prepare(pair.Item1,pair.Item2); throw new Exception("Accepted locked"); } catch(InvalidOperationException) {} }
}));
await Test("Unconfirmed and forged demo previews denied", async () => {
    var w = new VirtualWorkshop(); var before=w.State; var p=w.Prepare("demo-engine",DemoAction.ClearDtc);
    Check(!(await w.ApplyAsync(p.Id,false)).Applied && !(await w.ApplyAsync(Guid.NewGuid(),true)).Applied && w.State==before);
});
await Test("Demo reset invalidates previews and security", async () => {
    var w = new VirtualWorkshop(); var p=w.Prepare("demo-cabin",DemoAction.SecurityAccess); await w.ApplyAsync(p.Id,true);
    p=w.Prepare("demo-engine",DemoAction.Program); w.ResetDemo(); Check(!(await w.ApplyAsync(p.Id,true)).Applied && !w.State.DemoAccess);
});
foreach(var fault in Enum.GetValues<Fault>().Where(f => f != Fault.None))
await Test($"Virtual {fault} does not mutate state", async () => {
    var w=new VirtualWorkshop(); var before=w.State; var p=w.Prepare("demo-engine",DemoAction.ClearDtc);
    Check(!(await w.ApplyAsync(p.Id,true,fault)).Applied && w.State==before && !(await w.ApplyAsync(p.Id,true)).Applied);
});
await Test("Virtual cancellation does not mutate state", async () => {
    var w=new VirtualWorkshop(); var before=w.State; var p=w.Prepare("demo-engine",DemoAction.ClearDtc);
    using var c=new CancellationTokenSource(); var task=w.ApplyAsync(p.Id,true,cancellation:c.Token); c.Cancel();
    Check(!(await task).Applied && w.State==before);
});
await Test("Virtual reset during operation discards pending state", async () => {
    var w=new VirtualWorkshop(); var p=w.Prepare("demo-engine",DemoAction.Reset); var pending=w.ApplyAsync(p.Id,true);
    w.ResetDemo(); Check(!(await pending).Applied && w.State.Resets==0);
});
await Test("Virtual concurrent action rejected", async () => {
    var w=new VirtualWorkshop(); var p=w.Prepare("demo-engine",DemoAction.Reset); var pending=w.ApplyAsync(p.Id,true);
    Check(!(await w.ApplyAsync(p.Id,true)).Applied); Check((await pending).Applied && w.State.Resets==1);
});
var inventory = new LocalInventory(["COM7"], [new("driver32","Demo driver","Synthetic vendor","04.04","Registry32",@"C:\synthetic\driver.dll"), new("driver5","Demo API 5","Synthetic vendor","05.00","Registry64",@"C:\synthetic\driver5.dll")], []);
await Test("Configured simulator connects", () => Sync(() => { using var s=new DiagnosticSession(); Check(s.ConnectConfigured(new(ConnectionKind.Simulator,2008),inventory).CanConnect && s.Connected); }));
await Test("Unknown connection scenario denied", () => Sync(() => { using var s=new DiagnosticSession(); Check(!s.ConnectConfigured(new((ConnectionKind)999,2008),inventory).CanConnect && !s.Connected); }));
await Test("Unsupported connection year denied", () => Sync(() => Check(!Connectivity.Assess(new(ConnectionKind.Simulator,2013),inventory).ConfigurationValid)));
foreach(var kind in new[] { ConnectionKind.ElmSerial, ConnectionKind.Tech2Vehicle, ConnectionKind.Tech2Security })
{
    await Test($"{kind} requires explicit port", () => Sync(() => Check(!Connectivity.Assess(new(kind,2008),inventory).ConfigurationValid)));
    await Test($"{kind} requires present port and baud", () => Sync(() => {
        Check(!Connectivity.Assess(new(kind,2008,"COM8",9600),inventory).ConfigurationValid);
        Check(!Connectivity.Assess(new(kind,2008,"COM7",0),inventory).ConfigurationValid);
    }));
    await Test($"{kind} complete setup cannot connect", () => Sync(() => {
        using var s=new DiagnosticSession(); var a=s.ConnectConfigured(new(kind,2008,"COM7",9600),inventory);
        Check(a.ConfigurationValid && !a.CanConnect && !s.Connected);
    }));
}
await Test("TCP setup validates without connecting", () => Sync(() => {
    using var s=new DiagnosticSession(); var a=s.ConnectConfigured(new(ConnectionKind.ElmTcp,2008,Host:"192.0.2.10",TcpPort:35000),inventory);
    Check(a.ConfigurationValid && !a.CanConnect && !s.Connected);
}));
foreach(var host in new[] { "", "example.com", "http://192.0.2.10", "0.0.0.0", "255.255.255.255", "239.0.0.1", "::", "ff02::1" })
    await Test("Invalid TCP destination rejected", () => Sync(() => Check(!Connectivity.Assess(new(ConnectionKind.ElmTcp,2008,Host:host,TcpPort:35000),inventory).ConfigurationValid)));
await Test("Invalid TCP port rejected", () => Sync(() => {
    foreach(var port in new[] {-1,0,65536}) Check(!Connectivity.Assess(new(ConnectionKind.ElmTcp,2008,Host:"192.0.2.10",TcpPort:port),inventory).ConfigurationValid);
}));
await Test("Driver must be explicitly selected and present", () => Sync(() => {
    Check(!Connectivity.Assess(new(ConnectionKind.J2534,2008),inventory).ConfigurationValid);
    Check(!Connectivity.Assess(new(ConnectionKind.J2534,2008,DriverId:"missing"),inventory).ConfigurationValid);
}));
await Test("Driver registration cannot grant connection", () => Sync(() => {
    using var s=new DiagnosticSession(); var a=s.ConnectConfigured(new(ConnectionKind.J2534,2008,DriverId:"driver32"),inventory);
    Check(a.ConfigurationValid && !a.CanConnect && !s.Connected);
}));
await Test("Different J2534 API denied", () => Sync(() => Check(!Connectivity.Assess(new(ConnectionKind.J2534,2008,DriverId:"driver5"),inventory).ConfigurationValid)));
await Test("Ambiguous driver registration rejected", () => Sync(() => {
    var duplicate=new LocalInventory([], [inventory.Drivers[0],inventory.Drivers[0]], []);
    Check(!Connectivity.Assess(new(ConnectionKind.J2534,2008,DriverId:"driver32"),duplicate).ConfigurationValid);
}));
await Test("Missing driver library rejected", () => Sync(() => {
    var missing=new LocalInventory([], [inventory.Drivers[0] with { Library="" }], []);
    Check(!Connectivity.Assess(new(ConnectionKind.J2534,2008,DriverId:"driver32"),missing).ConfigurationValid);
}));
await Test("Rejected hardware route invalidates simulator", async () => {
    using var s=new DiagnosticSession(); s.ConnectSimulation(2008);
    Check(!s.ConnectConfigured(new(ConnectionKind.J2534,2008,DriverId:"driver32"),inventory).CanConnect);
    Check(!s.Connected && (await s.ReadAsync(Operation.ReadVin)).Outcome==Outcome.Disconnected);
});
await Test("Route change discards in-flight synthetic reply", async () => {
    using var s=new DiagnosticSession(); s.Connect(Profiles.Simulation(2008),new PendingTransport());
    var pending=s.ReadAsync(Operation.ReadLiveData);
    s.ConnectConfigured(new(ConnectionKind.Tech2Security,2008,"COM7",9600),inventory);
    Check((await pending).Outcome==Outcome.Disconnected && !s.Connected);
});
await Test("Unknown configured simulation fault rejected", () => Sync(() => {
    using var s=new DiagnosticSession(); Check(!s.ConnectConfigured(new(ConnectionKind.Simulator,2008),inventory,(Fault)999).CanConnect && !s.Connected);
}));
foreach(var provider in AuthorizationService.Providers)
    await Test($"{provider.Id} unavailable by default", async () => {
        using var service=new AuthorizationService(); Check(!service.IsImplemented(provider.Id));
        Check((await service.RequestAsync(provider.Id,provider.Workflow,Operation.SecurityAccess)).Status==AuthorizationStatus.Unavailable);
    });
await Test("Unknown authorization provider rejected", async () => { using var a=new AuthorizationService(); Check((await a.RequestAsync("unknown",AuthorizationWorkflow.Tech2,Operation.SecurityAccess)).Status==AuthorizationStatus.Unsupported); });
await Test("Mismatched authorization workflow rejected", async () => { using var a=new AuthorizationService(); Check((await a.RequestAsync("tis2000",AuthorizationWorkflow.DirectEcu,Operation.SecurityAccess)).Status==AuthorizationStatus.Unsupported); });
await Test("Authorization requires bound session", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock);
    using var a=new AuthorizationService([fake],clock,TimeSpan.FromMilliseconds(50));
    Check((await a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess)).Status==AuthorizationStatus.Disconnected && fake.Calls==0);
});
await Test("Unsupported authorization profile denied before provider", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock); using var a=new AuthorizationService([fake],clock,TimeSpan.FromMilliseconds(50));
    a.BeginSession("unknown","test-target"); Check((await a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess)).Status==AuthorizationStatus.Unsupported && fake.Calls==0);
});
await Test("Provider decision is exact and expires", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock); using var a=new AuthorizationService([fake],clock,TimeSpan.FromMilliseconds(50)); a.BeginSession("test-profile","test-target");
    Check((await a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess)).Status==AuthorizationStatus.Approved);
    Check(a.HasApproval("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess));
    Check(!a.HasApproval("globaltis",AuthorizationWorkflow.Tech2,Operation.SecurityAccess) && !a.HasApproval("tis2000",AuthorizationWorkflow.DirectEcu,Operation.SecurityAccess) && !a.HasApproval("tis2000",AuthorizationWorkflow.Tech2,Operation.Flash));
    clock.Advance(TimeSpan.FromMinutes(1)); Check(!a.HasApproval("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess));
});
foreach(var change in new Func<AuthorizationResponse,AuthorizationResponse>[] {
    r=>r with { RequestId=Guid.NewGuid() }, r=>r with { ProviderId="other" }, r=>r with { Workflow=AuthorizationWorkflow.DirectEcu },
    r=>r with { Scope=r.Scope with { SessionId=Guid.NewGuid() } }, r=>r with { Scope=r.Scope with { ProfileId="other" } },
    r=>r with { Scope=r.Scope with { TargetReference="other" } }, r=>r with { Scope=r.Scope with { Operation=Operation.Flash } },
    r=>r with { ExpiresAt=r.IssuedAt }, r=>r with { IssuedAt=r.IssuedAt.AddHours(1) }, r=>r with { ExpiresAt=r.ExpiresAt.AddHours(1) }
})
await Test("Malformed provider approval rejected", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock) { Transform=change }; using var a=new AuthorizationService([fake],clock,TimeSpan.FromMilliseconds(50)); a.BeginSession("test-profile","test-target");
    Check((await a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess)).Status==AuthorizationStatus.InvalidResponse && !a.HasApproval("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess));
});
await Test("Provider rejection retains no approval", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock) { Transform=r=>r with { Approved=false } }; using var a=new AuthorizationService([fake],clock,TimeSpan.FromMilliseconds(50)); a.BeginSession("test-profile","test-target");
    Check((await a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess)).Status==AuthorizationStatus.Rejected && !a.HasApproval("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess));
});
await Test("Provider timeout is bounded even if cancellation ignored", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock) { Pending=true }; using var a=new AuthorizationService([fake],clock,TimeSpan.FromMilliseconds(20)); a.BeginSession("test-profile","test-target");
    Check((await a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess)).Status==AuthorizationStatus.Timeout && fake.Calls==1);
});
await Test("Precancelled authorization sends no request", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock); using var a=new AuthorizationService([fake],clock,TimeSpan.FromMilliseconds(50)); a.BeginSession("test-profile","test-target");
    using var c=new CancellationTokenSource(); c.Cancel(); Check((await a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess,c.Token)).Status==AuthorizationStatus.Cancelled && fake.Calls==0);
});
await Test("In-flight authorization cancellation", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock) { Pending=true }; using var a=new AuthorizationService([fake],clock,TimeSpan.FromSeconds(1)); a.BeginSession("test-profile","test-target");
    using var c=new CancellationTokenSource(); var pending=a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess,c.Token); c.Cancel(); Check((await pending).Status==AuthorizationStatus.Cancelled);
});
await Test("Disconnect discards late authorization", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock) { Pending=true }; using var a=new AuthorizationService([fake],clock,TimeSpan.FromSeconds(1)); a.BeginSession("test-profile","test-target");
    var pending=a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess); a.Invalidate(); fake.Complete();
    Check((await pending).Status==AuthorizationStatus.Disconnected && !a.HasApproval("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess));
});
await Test("Reconnection revokes previous authorization", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock); using var a=new AuthorizationService([fake],clock,TimeSpan.FromMilliseconds(50)); a.BeginSession("test-profile","test-target");
    await a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess); a.BeginSession("test-profile","test-target"); Check(!a.HasApproval("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess));
});
await Test("Concurrent authorization rejected without retry", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock) { Pending=true }; using var a=new AuthorizationService([fake],clock,TimeSpan.FromSeconds(1)); a.BeginSession("test-profile","test-target");
    var pending=a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess);
    Check((await a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess)).Status==AuthorizationStatus.Busy && fake.Calls==1); a.Invalidate(); await pending;
});
await Test("Provider exception text not exposed", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock) { Fail=true }; using var a=new AuthorizationService([fake],clock,TimeSpan.FromMilliseconds(50)); a.BeginSession("test-profile","test-target");
    var r=await a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess); Check(r.Status==AuthorizationStatus.Rejected && !r.Message.Contains("secret"));
});
await Test("Provider approval cannot enable diagnostic writes", async () => {
    var clock=new TestClock(); var fake=new TestAuthorizationProvider(clock); using var a=new AuthorizationService([fake],clock,TimeSpan.FromMilliseconds(50)); a.BeginSession("test-profile","test-target");
    await a.RequestAsync("tis2000",AuthorizationWorkflow.Tech2,Operation.SecurityAccess);
    using var s=new DiagnosticSession(); s.ConnectSimulation(2008); Check((await s.ReadAsync(Operation.SecurityAccess)).Outcome==Outcome.Denied);
});
await Test("T8 level 01 published upstream vector", () => Sync(() => {
    Check(OfflineKeyCalculator.TryCalculate(OfflineKeyCalculator.Trionic8Level01, "5F94", out var key, out _) && key == "5C84");
    Check(OfflineKeyCalculator.TryCalculate(OfflineKeyCalculator.Trionic8Level01, "5f94", out key, out _) && key == "5C84");
}));
await Test("Offline calculator rejects malformed and zero seeds without retaining a key", () => Sync(() => {
    foreach (var seed in new string?[] { null, "", "0000", "123", "12345", "0x12", " 123", "123 ", "GGGG", "１２３４", "5F94\n", new string('A', 10000) })
        Check(!OfflineKeyCalculator.TryCalculate(OfflineKeyCalculator.Trionic8Level01, seed, out var key, out _) && key == "");
}));
await Test("Unknown modules and levels cannot use the offline calculator", () => Sync(() => {
    foreach (var algorithm in new string?[] { null, "", "trionic8-levelFD", "trionic8-levelFB", "cim", "tech2", "TRIONIC8-LEVEL01" })
        Check(!OfflineKeyCalculator.TryCalculate(algorithm, "5F94", out var key, out _) && key == "");
}));
await Test("Offline calculation grants no provider approval or diagnostic write capability", async () => {
    using var auth = new AuthorizationService();
    using var session = new DiagnosticSession(); session.ConnectSimulation(2008);
    Check(OfflineKeyCalculator.TryCalculate(OfflineKeyCalculator.Trionic8Level01, "5F94", out _, out _));
    foreach (var op in new[] { Operation.SecurityAccess, Operation.Pair, Operation.Flash }) {
        Check(!auth.HasApproval("ecu-code-generator", AuthorizationWorkflow.DirectEcu, op));
        Check((await session.ReadAsync(op)).Outcome == Outcome.Denied);
    }
});
await Test("Provider settings roundtrip all routes without enabling implementations", () => Sync(() => {
    var file = Path.Combine(Path.GetTempPath(), "saabdiag-settings-" + Guid.NewGuid().ToString("N") + ".json");
    try {
        var store = new ProviderSettingsStore(file);
        Check(store.Load().Count == 0);
        var entries = AuthorizationService.Providers.Select(p => new ProviderSettings(p.Id, "test-version", "https://example.invalid/api", "", "https://example.invalid/docs")).ToArray();
        store.Save(entries);
        Check(store.Load().SequenceEqual(entries));
        using var service = new AuthorizationService();
        Check(entries.All(p => !service.IsImplemented(p.ProviderId)));
        var before = File.ReadAllText(file);
        try { store.Save(new[] { entries[0], entries[0] }); throw new Exception("Accepted duplicate"); } catch (InvalidDataException) { }
        Check(File.ReadAllText(file) == before);
    } finally { File.Delete(file); }
}));
await Test("Provider settings reject unsafe addresses and unknown provider", () => Sync(() => {
    foreach (var address in new[] { "http://example.invalid", "https://user:pass@example.invalid", "https://example.invalid/?token=secret", "https://example.invalid/#secret", "file:///C:/app.exe", "javascript:alert(1)", "not-a-url" }) {
        try { new ProviderSettings("globaltis", "", address, "", "").Validate(); throw new Exception("Accepted unsafe URL"); }
        catch (ArgumentException) { }
    }
    new ProviderSettings("globaltis", "", "http://localhost:8080", "", "").Validate();
    new ProviderSettings("globaltis", "", "", "", "").Validate();
    try { new ProviderSettings("unknown", "", "", "", "").Validate(); throw new Exception("Accepted unknown provider"); } catch (ArgumentException) { }
    try { new ProviderSettings("globaltis", "", "", "relative.exe", "").Validate(); throw new Exception("Accepted relative path"); } catch (ArgumentException) { }
}));
await Test("Malformed saved settings fail closed and are not overwritten by loading", () => Sync(() => {
    var file = Path.Combine(Path.GetTempPath(), "saabdiag-settings-" + Guid.NewGuid().ToString("N") + ".json");
    try {
        foreach (var json in new[] { "{", "null", "{}", "{\"Version\":2,\"Providers\":[]}", "{\"Version\":1,\"Providers\":[null]}", new string('x',32769) }) {
            File.WriteAllText(file, json);
            try { new ProviderSettingsStore(file).Load(); throw new Exception("Accepted malformed settings"); }
            catch (Exception ex) when (ex is InvalidDataException or System.Text.Json.JsonException or ArgumentException) { }
            Check(File.ReadAllText(file) == json);
        }
    } finally { File.Delete(file); }
}));
Console.WriteLine($"{passed} tests passed; exit code {Environment.ExitCode}.");

sealed class CountingTransport : ITransport { public int Calls; public Task<string> ReadAsync(Operation op, CancellationToken ct) { Calls++; return Task.FromResult("{}"); } }
sealed class PayloadTransport(string payload) : ITransport { public Task<string> ReadAsync(Operation op,CancellationToken ct) => Task.FromResult(payload); }
sealed class PendingTransport : ITransport { public Task<string> ReadAsync(Operation op,CancellationToken ct) => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously).Task; }

sealed class TestClock : TimeProvider
{
    private DateTimeOffset now=new(2026,11,19,12,0,0,TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow()=>now;
    public void Advance(TimeSpan span)=>now+=span;
}
// Test assembly only; never registered in the desktop app.
sealed class TestAuthorizationProvider(TestClock clock) : IAuthorizationProvider
{
    public string Id=>"tis2000";
    public int Calls;
    public bool Pending, Fail;
    public Func<AuthorizationResponse,AuthorizationResponse> Transform=r=>r;
    private readonly TaskCompletionSource<AuthorizationResponse> completion=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private AuthorizationResponse? response;
    public bool Supports(AuthorizationWorkflow workflow,string profileId,string targetReference,Operation op)=>workflow==AuthorizationWorkflow.Tech2 && profileId=="test-profile" && targetReference=="test-target" && op==Operation.SecurityAccess;
    public Task<AuthorizationResponse> AuthorizeAsync(AuthorizationRequest request,CancellationToken cancellation)
    {
        Calls++;
        if(Fail) throw new InvalidOperationException("secret provider data");
        response=Transform(new(request.RequestId,request.ProviderId,request.Workflow,request.Scope,true,clock.GetUtcNow(),clock.GetUtcNow().AddSeconds(30)));
        return Pending ? completion.Task : Task.FromResult(response);
    }
    public void Complete()=>completion.TrySetResult(response!);
}
