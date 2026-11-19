# Safety boundaries

The simulation implements named-operation allowlisting, unknown-profile rejection, bounded reads and failure-driven session invalidation. Hardware and write requirements below remain gates for future implementation.

## Default policy

Allow only reviewed named read operations for a supported profile. No arbitrary command console, raw send endpoint, automatic vehicle discovery or automatic reconnection replay. Unknown operations and mismatched identities fail closed.

The following real operations are state-changing and remain denied by the diagnostic API: DTC clearing, ECU resets, actuator routines, service adaptations, coding/configuration, key/security functions, module pairing, memory erase and firmware/calibration writes. A function labeled "diagnostics" is not necessarily read-only.

## Stationary and bench testing

Use an authorized vehicle or isolated bench setup. Never operate the application while driving. Follow workshop procedures for ignition, ventilation, secure parking and work around moving parts. Establish acceptable polling rates and stop on unexpected responses, bus errors or loss of communication. Do not infer a safe procedure from a generic internet example.

## Future write preflight

Before enabling any specific workflow:
- Validate vehicle, ECU hardware/software, adapter capabilities and selected profile.
- Review exact intended changes and confirm permission for that operation.
- Follow module-specific power supply requirements; do not substitute a guessed voltage threshold.
- Back up recoverable configuration where supported and verify the backup.
- Verify calibration origin, integrity, compatibility and authorization.
- Prevent competing diagnostic tools, OS sleep and accidental disconnects.
- Provide module-specific interruption/recovery procedures and a tested bench recovery path.

An emergency stop may stop further traffic but cannot guarantee safe interruption of a flash. Never advertise a universal undo or rollback.

## Failure behavior

Timeouts and cancellation end polling and release resources. Never silently retry state-changing requests. Reconnect invalidates prior identity and capability checks. Partial or malformed responses cannot count as successful writes. Persist enough non-sensitive journal information to identify a recovery-required state.

## Data, security and distribution

VINs, raw traces and module identifiers can be sensitive. Store them locally only when needed and redact exports. Do not commit calibration files, keys, credentials, proprietary databases or driver binaries. Review licenses and access rights before importing external work. Keep immobilizer/security work limited to authorized access paths.

## Release evidence

Every advertised capability must state its profile coverage and evidence level: simulated, bench-tested or vehicle-validated. A successful build or a responding adapter is not a vehicle safety certification.

## Virtual workshop boundary

`VirtualWorkshop` is an isolated in-memory demonstration with a separate `DemoAction` enum and no dependency on `ITransport` or `DiagnosticSession`. It cannot accept raw commands, addresses, keys, calibration files or firmware bytes. Its named actions change fixture values only. Single-use previews, confirmation, module/action matching, cancellation and generation checks protect demo state. Injected errors discard changes before commit. This does not model real flash interruption or promise vehicle rollback. The production diagnostic allowlist still denies every write operation.

## Authorization framework update, 2026-11-19

The provider decision framework and all requested provider options are implemented; no executable real provider is registered. Tests use isolated fake providers. Decisions are scoped, expire and are invalidated by connection changes. They never bypass diagnostic operation policy. A separate offline Trionic 8 level 0x01 calculator is available; it cannot grant approval or send vehicle commands. See CODE_GENERATION.md. See AUTHORIZATION_PROVIDERS.md; milestones for hardware/security access remain unmet.
