# Hardware and compatibility plan

No adapter or vehicle is currently verified with SaabDiag. Hardware names below are inherited design candidates, not purchasing recommendations.

| Candidate | Intended role | Unresolved checks |
|---|---|---|
| ELM327-compatible serial adapter | Limited generic OBD prototype | Genuine command behavior, COM driver, baud rate, supported protocols, stability |
| GM MDI2 with vendor J2534 driver | Long-term transport candidate | Driver/API version, bitness, required network support, timing and filters |
| Other J2534 device | Possible alternative | Same checks; J2534 branding alone does not establish Saab coverage |

Do not infer single-wire/low-speed CAN support from high-speed CAN support. Do not assume every module is reachable through a generic OBD adapter. Verify bus topology, pinout, timing and gateway behavior against authoritative documentation for the exact vehicle before implementation. Historical chat statements about network speeds are not validated protocol specifications.

## Confirmed connection model

Windows PC -> explicitly selected vehicle adapter -> vehicle. A Tech 2 handheld is not part of the intended connection chain. The exact adapter, vehicle year, engine and transmission remain unspecified. Those details are needed before hardware integration and validation, but do not block simulation-first development.

## Evidence record template

- Date and tester:
- Vehicle model year, engine and transmission (redact VIN):
- Module identity, hardware and software:
- Adapter model/revision and firmware:
- Windows version/architecture:
- Driver provider/version/API and DLL bitness:
- Network/channel and verified source:
- Operation/profile version:
- Evidence level (simulated / bench / vehicle):
- Expected result, actual result and sanitized fixture:
- Timeout/disconnect behavior:
- Known limitations:

Keep real captures outside the repository. Commit only reviewed synthetic or redacted fixtures with provenance. Establish one known-good combination before expanding coverage.

## Connectivity setup update, 2026-11-19

Six connection scenarios, manual Windows registry inventory and fail-closed configuration checks are implemented. Only simulation can connect. Physical Tech 2 companion/security routes are now represented for setup planning; no real transport or authorization implementation is included. See [CONNECTIVITY.md](CONNECTIVITY.md) for exact support, sources and remaining gates.
