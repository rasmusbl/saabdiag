# Safety boundaries

These are requirements for future implementation, not claims of existing enforcement code.

## Default policy

Allow only reviewed named read operations for a supported profile. No arbitrary command console, raw send endpoint, automatic vehicle discovery or automatic reconnection replay. Unknown operations and mismatched identities fail closed.

The following are state-changing and remain unavailable: DTC clearing, ECU resets, actuator routines, service adaptations, coding/configuration, key/security functions, module pairing, memory erase and firmware/calibration writes. A function labeled "diagnostics" is not necessarily read-only.

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
