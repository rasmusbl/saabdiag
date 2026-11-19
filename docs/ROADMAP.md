# Roadmap and acceptance gates

Milestone 0 now has a runnable .NET/WPF simulation, offline tests and reference catalog browser. No real scanner functionality is delivered. Hardware milestones remain pending.

## 0. Preserve provenance and establish offline development
- Recover the Phase 1 archive if supplied; inspect its contents, dependencies and command paths before execution.
- If unavailable, author a clearly labeled new prototype; do not reconstruct a fictional import.
- Establish a C#/.NET core, WPF shell, simulated transport, synthetic fixtures and automated offline tests.
- Add a reference browser over `references/catalog.json` and user-selected local manuals; preserve source and reuse-rights metadata.
- Exit: reproducible setup, reviewed dependency versions, no hardware access on startup and accurate capability table.

## 1. Generic OBD read-only prototype
- Explicit serial adapter selection and connection/disconnection.
- Supported PID detection; RPM, coolant temperature, speed and throttle reads when supported.
- VIN and stored DTC reads only after implementing and testing their response formats.
- Bounded polling, cancellation, structured errors and redacted export.
- Exit: parser and deny-policy tests pass; bench evidence identifies adapter/driver; a supervised stationary vehicle session validates each advertised read.
- ELM327 support does not imply manufacturer module access.

## 2. Windows application and J2534 transport
- Extend the WPF solution with explicit hardware selection; preserve simulation-first operation.
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

Extend the new simulation-first C#/.NET prototype; the historical ZIP remains unavailable. Before real-device work, record the user's actual vehicle year/engine/module details, adapter model and driver version. Full Tech 2 coverage has no promised delivery date.

## Simulator extension

Synthetic discovery/VIN and virtual service demonstrations are implemented for UI development only. This does not satisfy hardware exit criteria for milestones 1–6. Real security algorithms, pairing and programming remain unimplemented and gated.

## Connectivity setup update, 2026-11-19

Six connection scenarios, manual Windows registry inventory and fail-closed configuration checks are implemented. Only simulation can connect. Physical Tech 2 companion/security routes are now represented for setup planning; no real transport or authorization implementation is included. See [CONNECTIVITY.md](CONNECTIVITY.md) for exact support, sources and remaining gates.

## Authorization framework update, 2026-11-19

The provider decision framework and all requested provider options are implemented; no executable real provider is registered. Tests use isolated fake providers. Decisions are scoped, expire and are invalidated by connection changes. They never bypass diagnostic operation policy. No algorithm or code generation is included. See AUTHORIZATION_PROVIDERS.md; milestones for hardware/security access remain unmet.
