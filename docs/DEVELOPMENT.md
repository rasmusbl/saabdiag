# Continued development in Codex

## Open the project

Clone the private repository using your existing GitHub access, then add/open the checkout folder as a local Codex project. Keep AGENTS.md at its root. Official project guidance: https://learn.chatgpt.com/docs/projects

A runnable .NET/WPF simulator and dependency-free console test runner now exist. See README.md for build/run commands and docs/VALIDATION.md for evidence. Do not run the historical `pip install -r requirements.txt` / `python saabdiag.py` instructions until those files have actually been recovered and reviewed.

## Development workflow

1. Read README.md, AGENTS.md and the safety/provenance documents.
2. Create a feature branch.
3. If the archive is supplied, inspect its file list for unsafe paths, record SHA-256 and retain original provenance. Import its application under `prototype/python/` while preserving the repository documentation.
4. Review every outgoing command, library initialization action and auto-detection path before hardware execution.
5. If missing, implement a new prototype explicitly labeled as new work. Start with fake transport responses and no real-device connection by default.
6. Add a reproducible dependency manifest and offline test commands only when code exists.
7. Review changes, run applicable tests and update the capability table with evidence.

## Planned test coverage

- Valid and malformed frames, partial replies and multiple responders.
- Supported/unsupported PIDs, unit conversions and unavailable data.
- Timeouts, disconnects, cancellation and adapter errors.
- Rejection of DTC clear, arbitrary commands and unsupported operations.
- Bounded polling and cleanup after exceptions.
- J2534 structure sizes, broker bitness, device removal and native failure isolation.
- No automatic hardware access during unit tests or CI.

Use a Windows CI job for implemented Windows components and offline tests. Do not configure self-hosted hardware runners or add a passing placeholder workflow that implies application validation.

## Change review

Use small changes with concrete before/after behavior. Include exact validation, evidence level and remaining limitations. Add dependencies only with source/license review. No open-source license is selected yet; keep third-party licensing intact.

## Next development task

Expand evidence-backed offline protocol fixtures before introducing a hardware transport. Exact adapter, year, engine, transmission and module details are needed for real vehicle coverage. Keep physical Tech 2 card integration outside scope. Never treat synthetic JSON fixture parsing as validated OBD/CAN parsing.

## Connectivity setup update, 2026-11-19

Six connection scenarios, manual Windows registry inventory and fail-closed configuration checks are implemented. Only simulation can connect. Physical Tech 2 companion/security routes are now represented for setup planning; no real transport or authorization implementation is included. See [CONNECTIVITY.md](CONNECTIVITY.md) for exact support, sources and remaining gates.
