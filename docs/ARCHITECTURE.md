# Proposed architecture

Status: simulation core, named-operation policy, synthetic parser, profile selection, reference catalog service and WPF shell implemented. Real protocols, hardware transports, session journal and programming remain planned.

## Layers

```text
Windows WPF UI / optional prototype CLI
           |
Application use cases + operation policy + session journal
           |
Diagnostic services + versioned vehicle/module profiles
           |
Protocol codecs and request scheduler
           |
Transport contract
  | simulated | ELM327 (limited OBD) | J2534 broker |
```

## Responsibilities and contracts

1. UI: show connection identity, simulated/real mode, capability status and structured errors. Never issue raw vehicle commands. Keep I/O off the UI thread.
2. Application layer: own sessions, cancellation, polling rates and allowed operations. A session binds an adapter, vehicle profile and capability snapshot. A reconnect requires revalidation.
3. Diagnostics: named reads such as supported PIDs, live values, VIN and DTCs. Return values with units, source module, timestamps and supported/unavailable/error status. Unsupported data must never become a plausible zero.
4. Profiles: versioned definitions identifying supported model/engine/module part number/software and source evidence. Addresses, buses, timing and encoding remain unknown until validated. Do not assume one profile covers all 2003–2012 cars.
5. Protocol layer: frame bounds, response correlation, negative response handling and bounded timeouts. Only implement segmentation, session control and retry behavior supported by reviewed evidence.
6. Transport: enumerate/open/close, report capabilities and exchange bounded requests under policy. Serialize access per channel, clean up handles deterministically and expose structured disconnect errors. Do not hide retries in the driver layer.
7. J2534 broker: isolate native vendor DLL failures in a process with matching driver bitness. Enumerate vendor installations, validate the selected library and API version, marshal structures explicitly and impose timeouts. A broker crash must terminate the session without automatic replay.
8. Journal: record operation identity, profile/version, timing and outcome. Redact VIN and sensitive data by default; raw captures require explicit local opt-in and must not enter source control.

## Stack decision

The current user preference supersedes the historical Python-first plan: use C#/.NET for the first simulated diagnostic core and WPF for the Windows 10/11 UI. Baseline: .NET 10 LTS, SDK 10.0.403 pinned. Exact OS/build/driver coverage still requires validation. Microsoft runtime support depends on Windows edition and lifecycle; application compatibility testing is separate. See https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md and https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core.

A reference-library service owns catalog metadata and user-selected local documents, independently of diagnostic transports. It must work without an adapter. See REFERENCES.md. A future authorization provider remains disabled until the security milestone; it is not a startup prerequisite for simulation or the reference library. See SECURITY_ACCESS.md.

## Read-only boundary

A hardware read normally sends traffic. Read-only means no intended changes to ECU persistent settings or operational state, not passive bus capture. ELM initialization commands also change adapter state. Review initialization, probing and library side effects, not only visible buttons.

Enforce capabilities below the UI. Prefer named operations over a general send-command API. Future write workflows must be separate state machines with identity checks, preview, authorization, bounded execution and post-operation verification.

## Future programming state machine

Proposed states: disconnected -> identified -> compatible -> preflight passed -> authorized -> executing -> verifying -> complete, with explicit failed/recovery-required outcomes.

Do not automatically retry interrupted erase/write/security operations. Recovery behavior is module-specific and must be designed and tested before enabling the workflow. Generic rollback cannot be promised for ECU flash operations.

## Virtual workshop implementation

A separate `VirtualWorkshop` class demonstrates named operations against immutable in-memory snapshots. It has no transport and shares no mutable state with diagnostic fixtures. Preview identity and workspace generation guard commits; unknown module/action pairs fail closed. The UI displays hardware execution as unavailable. The synthetic parser now accepts only the fixed demo VIN and known fixture inventory; it is not a VIN/ECU protocol implementation.

## Connectivity setup update, 2026-11-19

Six connection scenarios, manual Windows registry inventory and fail-closed configuration checks are implemented. Only simulation can connect. Physical Tech 2 companion/security routes are now represented for setup planning; no real transport or authorization implementation is included. See [CONNECTIVITY.md](CONNECTIVITY.md) for exact support, sources and remaining gates.

## Authorization framework update, 2026-11-19

The provider decision framework and all requested provider options are implemented; no executable real provider is registered. Tests use isolated fake providers. Decisions are scoped, expire and are invalidated by connection changes. They never bypass diagnostic operation policy. No algorithm or code generation is included. See AUTHORIZATION_PROVIDERS.md; milestones for hardware/security access remain unmet.
