# SaabDiag development instructions

## Scope and truthfulness
Target Windows and Saab 9-3 II (2003–2012). Read README.md and docs before changing behavior.
This is a documentation foundation; the historical prototype is missing. Never claim imported code, hardware validation, J2534 support or Tech 2 equivalence without evidence.
Preserve the distinction between planned, simulated, bench-tested and vehicle-validated capabilities.

## Implementation
Start with simulation and deterministic offline tests. Keep UI, diagnostic services, module profiles and transports separate.
Use a centrally enforced allowlist of named operations. Do not expose unrestricted serial/CAN commands through the production UI or public diagnostic API.
Deny unknown profiles and unsupported capabilities by default. Fail closed on malformed data, cancellation, timeout and disconnection.
Do not silently select a COM port, probe a real vehicle, install drivers or initiate vehicle operations during development.
Vehicle writes require a separately scoped implementation task and explicit authorization for the concrete hardware operation; repository development alone does not authorize vehicle changes.
Keep DTC clear, actuator routines, resets, configuration, security access, pairing and flashing disabled until their milestone gates are met.

## Verification and reporting
Test protocol parsing, timeouts, cancellation, disconnects, unsupported responses and operation policy using fake transports.
Never run hardware tests in CI. Record exact commands and limitations; do not equate compilation with vehicle validation.
Do not add fabricated protocol addresses or security algorithms. Record source provenance and licensing for external material.
Keep VINs, credentials, captures, calibrations and customer information out of commits; use synthetic or redacted fixtures.
Update capability status and documentation alongside implementation. Avoid unrelated changes.
