# Roadmap and acceptance gates

All implementation milestones below are pending. Repository preparation is complete; no scanner functionality is delivered.

## 0. Preserve provenance and establish offline development
- Recover the Phase 1 archive if supplied; inspect its contents, dependencies and command paths before execution.
- If unavailable, author a clearly labeled new prototype; do not reconstruct a fictional import.
- Establish simulated transport, synthetic fixtures and automated offline tests.
- Exit: reproducible setup, reviewed dependency versions, no hardware access on startup and accurate capability table.

## 1. Generic OBD read-only prototype
- Explicit serial adapter selection and connection/disconnection.
- Supported PID detection; RPM, coolant temperature, speed and throttle reads when supported.
- VIN and stored DTC reads only after implementing and testing their response formats.
- Bounded polling, cancellation, structured errors and redacted export.
- Exit: parser and deny-policy tests pass; bench evidence identifies adapter/driver; a supervised stationary vehicle session validates each advertised read.
- ELM327 support does not imply manufacturer module access.

## 2. Windows application and J2534 transport
- Establish WPF solution and supported SDK; preserve simulation-first operation.
- Implement native broker, discovery, driver bitness handling and cleanup.
- Validate each needed network/channel using a documented device/driver combination.
- Exit: broker fault tests, simulated contract tests, bench connectivity evidence and a reviewed coverage matrix. Unsupported networks stay unavailable.

## 3. Saab-specific read-only diagnostics
- Research module addressing, identity reads, DTC decoding and session behavior.
- Add one evidence-backed profile at a time; no blind network-wide probing.
- Exit: known-good trace comparison, negative response tests, documented vehicle/module coverage and safe request rates.

## 4. Configuration and service operations
- Treat DTC clearing, resets and actuator tests as writes.
- Define per-operation preconditions, preview, backup where supported and read-back verification.
- Exit: separately reviewed operation design, documented stationary/bench procedure, fault injection and recovery plan. User confirmation alone is not an engineering gate.

## 5. Authorized module replacement and security workflows
- Research lawful access and any vendor tooling/service dependencies.
- Do not invent seed/key routines, bypass access controls or distribute secrets.
- Exit: validated exact module compatibility, authorized access path, recovery evidence and explicit operation authorization.

## 6. Programming
- Establish calibration provenance and integrity, exact ECU hardware/software matching, regulated vehicle power requirements and documented recovery.
- Test interruptions and failure states on recoverable bench hardware first.
- Exit: module-specific programming evidence, independent review, validated preflight and post-flash checks, and a tested recovery procedure.

## Next concrete task

Recover the ZIP if available; otherwise build a new offline-first Python prototype. Before real-device work, record the user's actual vehicle year/engine/module details, adapter model and driver version. Full Tech 2 coverage has no promised delivery date.
