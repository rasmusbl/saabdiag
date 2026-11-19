# SaabDiag

A C#/.NET Windows 10/11 diagnostic application project for the **Saab 9-3 II (2003–2012)**. The long-term goal is a Tech 2 alternative covering diagnostics, module configuration and authorized programming through J2534/GM MDI2.

**Status: runnable simulation-only desktop prototype. Full Tech 2 support does not exist. No real vehicle transport, adapter or vehicle has been validated.**

## Current capability

| Capability | Status |
|---|---|
| Project documentation and Codex handoff | Available |
| Earlier Python ELM327 prototype | Described in the source conversation; archive unavailable for import |
| Sample live data and DTCs | Implemented with synthetic fixtures only; not an OBD wire protocol |
| Real OBD live data, VIN and DTC reads | Planned; no hardware transport |
| Module discovery and VIN | Synthetic inventory and demo VIN available; real protocols unimplemented |
| Saab manufacturer DTCs | Research and implementation required |
| Connection setup / local inventory | Six scenarios, explicit COM/J2534 registration listing and eligibility checks implemented |
| Real serial / TCP / J2534 / Tech 2 transports | Not implemented; setup completeness does not enable communication |
| DTC clearing, actuators, resets, configuration | Virtual workshop demonstrations only; diagnostic API denies writes |
| Pairing and programming | Virtual flags/firmware label demonstration only; no hardware implementation |
| Repair reference catalog | Desktop catalog browser and user-selected local PDF opening implemented; no manuals bundled |
| Authorization provider framework | Five integration options, scoped decision validation, expiry and failure handling; all real providers unavailable |
| Security code generation | Offline Trionic 8 level 0x01 calculator; no vehicle integration or validation |
| Tech 2 equivalence | Goal, not a compatibility claim |

The earlier chat linked `/mnt/data/saabdiag-phase1.zip`, reportedly containing `saabdiag.py`, `requirements.txt` and a README. Its contents were not accessible during this handoff. No replacement has been presented as recovered code. See [provenance](docs/PROVENANCE.md).

## Getting started

Clone with an account that can access this private repository:

```powershell
git clone https://github.com/rasmusbl/saabdiag.git
cd saabdiag
```

## Build, test and run

Requires Windows and .NET SDK 10.0.403 (pinned in `global.json`). No third-party NuGet packages are used. From the repository root:

```powershell
dotnet restore SaabDiag.slnx --configfile NuGet.Config
dotnet build SaabDiag.slnx -c Release --no-restore
dotnet run --project tests/SaabDiag.Tests -c Release --no-build --no-restore
dotnet run --project src/SaabDiag.Desktop -c Release --no-build --no-restore
```

This checkout has a local SDK under `.tools/dotnet`; substitute `.tools/dotnet/dotnet.exe` for `dotnet` if needed. After building, double-click `Run-SaabDiag.cmd` to launch. The launcher prefers that local SDK and never downloads or installs anything.

In **Connection setup**, select **Offline simulator**, a model year and **Connect selected scenario**, then read sample live data or DTCs. All years use the same synthetic dataset; selection does not establish engine/module coverage. Select a fault mode before connecting to exercise failure handling. A failed read ends the simulated session; reconnect explicitly. COM-port and J2534 registration metadata is listed only on explicit refresh; no ports are opened or vehicle traffic sent. Repair references work while disconnected.

See [validation record](docs/VALIDATION.md) for exact checks and limitations.

## Connection scenarios

See [connection setup and support matrix](docs/CONNECTIVITY.md) for simulator, ELM serial/TCP, J2534/MDI and physical Tech 2 routes. Only simulation can connect in this build. Real routes have validated setup fields and explicit missing requirements; they do not silently fall back to simulation.

## Virtual workshop and banner

The banner is an AI-generated Saab 9-3 II sedan illustration, embedded in the application. See [asset provenance and generation prompt](docs/BANNER.md).

The Diagnostics tab now supports **Discover demo modules** and **Read demo VIN**. `DEMO0000000000000` is a placeholder, not a valid vehicle identity; the module list has no real addresses.

In **Virtual workshop**, select a fictional module and action, preview, acknowledge the demo-only scope, then apply. Engine actions: DTC clear, reset and programming label. Brakes: fictional actuator cycle. Cabin: fictional comfort setting, demo security flag and pairing flag. The pairing/programming demonstrations require the demo security flag first. Select a failure outcome to verify no changes are committed; **Reset demo workspace** restores initial values.

This workspace is separate from the read-only Diagnostics fixtures: clearing its demo DTC does not change the independent sample DTC response. It never calls a transport. Programming changes only a label, and security access sets only a boolean. Neither represents a real Saab procedure. All state disappears when the application closes.

## Intended implementation

Confirmed scope: a standalone Windows PC application connected to a vehicle through a suitable adapter. A physical Tech 2 scanner is not required by the product design; physical Tech 2 vehicle/security connection planning is available as separate setup scenarios, but controlling or unlocking the handheld is not implemented.

- C#/.NET from the first simulation and offline tests; Python is historical prototype context only.
- WPF for the Windows desktop UI; .NET 10 SDK 10.0.403 is pinned. Continue to review SDK updates and validate exact Windows editions/builds and adapter drivers; Windows 10 compatibility is not a blanket Microsoft support claim.
- A vendor-neutral transport boundary, first simulated, then ELM327 for limited generic OBD reads, then a native J2534 broker.
- Versioned Saab module profiles backed by evidence, with explicit vehicle/module/driver coverage.
- Central operation policy and auditable sessions; future writes require separately reviewed workflows.

See [architecture](docs/ARCHITECTURE.md), [roadmap](docs/ROADMAP.md), [hardware plan](docs/HARDWARE.md) and [safety boundaries](docs/SAFETY.md).

## Authorization and repair references

See [security access research](docs/SECURITY_ACCESS.md) and [reference library plan](docs/REFERENCES.md). The initial reference catalog contains links, not bundled workshop manuals. Physical Tech 2 authorization and direct ECU security access are separate integration problems.

## Authorization options

The **Authorization** tab lists TIS2000, GlobalTIS, external Tech 2 providers, direct ECU providers and a reviewed ECU code-generator integration target. Select an option to see its prerequisites. **No live provider is installed; Request authorization remains disabled.** A separate working [offline Trionic 8 level 0x01 calculator](docs/CODE_GENERATION.md) is available in the same tab. The implemented framework enforces context, expiry and failure handling using offline test providers. See [provider contracts and status](docs/AUTHORIZATION_PROVIDERS.md).

Provider details can now be entered under **Authorization → Provider connection settings** and saved separately for each provider. Version, service address, installation path and API documentation link persist on this PC. No credentials are stored. Saving configuration does not implement or activate a live provider connection.

## Hardware

An ELM327-compatible serial adapter is the proposed initial generic OBD interface. It is not a full Saab module interface. GM MDI2/J2534 is the long-term candidate, not a tested compatibility promise. Exact device, driver, Windows architecture and supported physical networks must be recorded before claiming support. Do not buy hardware based on an unverified project compatibility claim.

## Safety and limitations

Development starts offline and read-only. Read-only diagnostics still transmit requests and can affect network load. No real DTC clearing, actuator control, configuration writes, security access, module pairing or flashing is included. The separate virtual workshop changes in-memory fixtures only. Do not test while driving. Do not infer compatibility from model year alone. Future programming requires validated power, calibration identity, recovery procedures and authorized access.

## Repository map

- `AGENTS.md`: instructions for continued Codex work.
- `docs/ARCHITECTURE.md`: proposed layers and contracts.
- `docs/ROADMAP.md`: milestones with acceptance gates.
- `docs/SAFETY.md`: operation boundaries and failure handling.
- `docs/HARDWARE.md`: compatibility evidence template.
- `docs/DEVELOPMENT.md`: workflow, testing and next steps.
- `docs/PROVENANCE.md`: source conversation and missing archive.
- `.github/pull_request_template.md`: review checklist.

## License and affiliation

Private project; no open-source license has been selected. Do not redistribute third-party code without license review. No proprietary Saab/GM firmware, calibration files, workshop databases, security secrets or vendor drivers are bundled. SaabDiag is an independent project, not an official Saab, GM or Tech 2 product.
