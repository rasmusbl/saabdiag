# Offline calculator and provider settings — 2026-11-19

Standard Release build succeeded with zero warnings/errors; 116 offline tests passed, exit code 0. New tests cover the published T8 level 0x01 vector, invalid/zero input, unsupported algorithms, continued denial of vehicle operations, all-provider settings roundtrips, invalid addresses, unknown providers, duplicate entries, malformed/oversized files and preservation of files after rejected loads/saves.

Commands (local SDK, DOTNET_CLI_HOME set to `.tools/cli`):

```powershell
.tools/dotnet/dotnet.exe build SaabDiag.slnx -c Release --no-restore -m:1 --disable-build-servers -p:UseSharedCompilation=false
.tools/dotnet/dotnet.exe tests/SaabDiag.Tests/bin/Release/net10.0/SaabDiag.Tests.dll
```

Native desktop inspection confirmed provider settings fields and the offline calculator. Entering the public test seed `5F94` and clicking Calculate produced `5C84`; Request authorization remained disabled. UI persistence was covered through store tests, not a real provider installation. No external authorization request or hardware operation ran. The existing launcher uses this rebuilt standard Release output.

# Simulation validation — 2026-11-08

New implementation, with no historical prototype imported. Microsoft .NET SDK 10.0.403, Windows 11 Pro for Workstations 10.0.26300, x64. No third-party NuGet packages. No adapter connected, enumerated or probed by the application or tests.

## Commands and results

Executed from the repository root (PowerShell); local SDK path used because `dotnet` was not on PATH:

```powershell
$env:DOTNET_CLI_HOME = "$PWD/.tools/cli"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
.tools/dotnet/dotnet.exe restore SaabDiag.slnx --configfile NuGet.Config --packages .tools/packages -m:1
.tools/dotnet/dotnet.exe build SaabDiag.slnx -c Release --no-restore -m:1
.tools/dotnet/dotnet.exe run --project tests/SaabDiag.Tests -c Release --no-build --no-restore
git diff --check
```

Restore passed with network package sources disabled. Restore required execution outside the filesystem sandbox because NuGet attempted to read its standard user configuration. Release build: zero warnings, zero errors. Console test harness: **33 passed**, exit code 0. This is a dependency-free test executable; use `dotnet run`, not `dotnet test`. A Windows CI workflow contains the equivalent offline checks; remote CI has not been run.

Coverage includes all 10 simulation year profiles, unknown profiles, explicit connection, fixture values, named-operation policy (all writes and unknown operations), malformed/partial/oversized/mismatched replies, duplicate JSON properties, invalid DTC format, unsupported responses, timeout, cancellation before and during a read, disconnection, stale-session rejection, concurrency, catalog versions, null entries and unsafe reference URLs. A cancellation reentrancy defect discovered by tests was corrected by invalidating session identity before signalling cancellation.

## Desktop checks

Launched the WPF app and inspected its window through native UI automation. Verified disconnected startup, disabled read buttons before connection, explicit simulator connection, sample live values (850 rpm, 88 degrees C, 0 km/h), sample P0300 DTC and reference list/source/applicability display. Visual check found a text encoding issue, subsequently corrected. No external websites or local manuals were opened during testing.

## Limits

The synthetic JSON schema is an internal simulator fixture, **not a vehicle protocol**. Model-year choices share one sample dataset and do not establish real vehicle, engine or module coverage. Windows 10 has not been tested. No ELM327, J2534, MDI2, ECU addressing, real VIN/DTC parsing, programming, authorization, pairing or vehicle operations have been implemented or validated. Local PDF launching and website launching are implemented but not exercised end to end. Reference filtering, bookmarks, session export and an audit journal remain planned.

## Banner and virtual workshop extension

Release build passed with zero warnings/errors; **50 tests passed**, exit code 0. Additional tests cover synthetic VIN/inventory, unknown identities/modules, all seven virtual actions, stale/forged/unconfirmed previews, module matching, demo permission gates, failure outcomes, cancellation, concurrency and reset during an operation. The diagnostic write-denial test remains passing. No hardware tests ran.

Commands (same local SDK/environment as above):

```powershell
.tools/dotnet/dotnet.exe build SaabDiag.slnx -c Release --no-restore -m:1 --disable-build-servers -p:UseSharedCompilation=false -p:OutDir="$PWD/artifacts/banner-build/"
.tools/dotnet/dotnet.exe artifacts/banner-build/SaabDiag.Tests.dll
.tools/dotnet/dotnet.exe build SaabDiag.slnx -c Release --no-restore -m:1 --disable-build-servers -p:UseSharedCompilation=false
```

A sandboxed build stalled and was stopped; build-server-disabled compilation outside the sandbox succeeded. The final standard Release output was rebuilt for `Run-SaabDiag.cmd`. Native UI inspection verified banner rendering, the workshop tab, readable controls and visible disabled hardware execution. Virtual workflow state transitions were exercised by automated tests. Windows 10 and physical adapters remain untested.

## Connectivity setup — 2026-11-19

Release build passed with zero warnings/errors. **80 offline tests passed**, exit code 0. Added synthetic-inventory tests for all six routes, missing/unknown serial ports, baud validation, invalid TCP destinations/ports, missing/ambiguous J2534 registrations, API mismatch, missing library metadata, unknown scenarios/faults, model-year rejection, no hardware-to-simulator fallback, invalidation of an existing simulator and rejection of pending replies after route changes. No test enumerates hardware or loads drivers.

```powershell
.tools/dotnet/dotnet.exe build SaabDiag.slnx -c Release --no-restore -m:1 --disable-build-servers -p:UseSharedCompilation=false -p:OutDir="$PWD/artifacts/connectivity-build/"
.tools/dotnet/dotnet.exe artifacts/connectivity-build/SaabDiag.Tests.dll
```

Native UI inspection verified connection setup rendering and disconnected startup with no automatic inventory read. Real ports, sockets and vendor DLLs remain unopened; vehicle tests did not run.

The connection UI check explicitly refreshed Windows registration metadata (one registered COM port, zero J2534 registrations on this host). No port was auto-selected. Selecting physical Tech 2 security setup displayed the separate PC-to-scanner route and disabled Connect. Standard Release output was also rebuilt with the same command omitting `-p:OutDir`, with zero warnings/errors, so the existing launcher includes the update. This is metadata/UI verification, not a hardware test.

## Authorization providers — 2026-11-19

Build: zero warnings/errors. **109 offline tests passed**, exit code 0. Added 29 tests for provider availability, workflows, context, response correlation, issue/expiry times, rejection, exceptions, cancellation, timeout, disconnect/reconnect, concurrency and independent diagnostic write denial. Fake providers exist only in the test assembly. No credentials, codes, remote requests or vehicle operations were used.

```powershell
.tools/dotnet/dotnet.exe build SaabDiag.slnx -c Release --no-restore -m:1 --disable-build-servers -p:UseSharedCompilation=false -p:OutDir="$PWD/artifacts/authorization-build/"
.tools/dotnet/dotnet.exe artifacts/authorization-build/SaabDiag.Tests.dll
```

Standard Release output was rebuilt with the same build command omitting `-p:OutDir`; zero warnings/errors. Native UI readback showed the Authorization tab, GlobalTIS selection, integration requirements and disabled Request authorization button. No request was submitted. User input was detected during inspection, so only a fresh readback was used afterward.
