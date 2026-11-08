# Proposed architecture

Status: design only. None of these components is implemented.

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

The source conversation proposed C#/.NET 8 + WPF. Retain C#/WPF as the direction, but select a currently supported SDK and dependency versions at implementation time. Do not pin the historical SDK without checking its support lifecycle. Python remains suitable for the first experiment and offline fixtures.

## Read-only boundary

A hardware read normally sends traffic. Read-only means no intended changes to ECU persistent settings or operational state, not passive bus capture. ELM initialization commands also change adapter state. Review initialization, probing and library side effects, not only visible buttons.

Enforce capabilities below the UI. Prefer named operations over a general send-command API. Future write workflows must be separate state machines with identity checks, preview, authorization, bounded execution and post-operation verification.

## Future programming state machine

Proposed states: disconnected -> identified -> compatible -> preflight passed -> authorized -> executing -> verifying -> complete, with explicit failed/recovery-required outcomes.

Do not automatically retry interrupted erase/write/security operations. Recovery behavior is module-specific and must be designed and tested before enabling the workflow. Generic rollback cannot be promised for ECU flash operations.
