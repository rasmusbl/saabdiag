# Continued development in Codex

## Open the project

Clone the private repository using your existing GitHub access, then add/open the checkout folder as a local Codex project. Keep AGENTS.md at its root. Official project guidance: https://learn.chatgpt.com/docs/projects

There is no runnable application, dependency manifest or test suite yet. Do not run the historical `pip install -r requirements.txt` / `python saabdiag.py` instructions until those files have actually been recovered and reviewed.

## First implementation branch

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

## Suggested next Codex prompt

"Read AGENTS.md and docs. Establish a new simulation-first Python read-only prototype unless the original ZIP is supplied. Implement named generic OBD reads behind a transport interface with deny-by-default operation policy. Add offline tests for parsing, failures and prohibited operations. Do not connect to a real vehicle or implement writes. Update README with exact setup, test commands and remaining limitations."
