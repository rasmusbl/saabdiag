# SaabDiag

A Windows diagnostic application project for the **Saab 9-3 II (2003–2012)**. The long-term goal is a Tech 2 alternative covering diagnostics, module configuration and authorized programming through J2534/GM MDI2.

**Status: development foundation only. Full Tech 2 support does not exist. This repository currently contains documentation and development guidance, not an executable scanner. No vehicle or adapter has been validated.**

## Current capability

| Capability | Status |
|---|---|
| Project documentation and Codex handoff | Available |
| Earlier Python ELM327 prototype | Described in the source conversation; archive unavailable for import |
| Generic OBD live data, VIN, DTC reads | Planned; no runnable implementation in this repository |
| Saab module discovery / manufacturer DTCs | Research and implementation required |
| J2534 / MDI2 transport | Planned; hardware/driver validation required |
| DTC clearing, actuators, service routines | Not implemented; state-changing operations |
| Module configuration, pairing and programming | Long-term gated milestones; not implemented |
| Tech 2 equivalence | Goal, not a compatibility claim |

The earlier chat linked `/mnt/data/saabdiag-phase1.zip`, reportedly containing `saabdiag.py`, `requirements.txt` and a README. Its contents were not accessible during this handoff. No replacement has been presented as recovered code. See [provenance](docs/PROVENANCE.md).

## Getting started

Clone with an account that can access this private repository:

```powershell
git clone https://github.com/rasmusbl/saabdiag.git
cd saabdiag
```

Open this folder as a local project in Codex. Read [AGENTS.md](AGENTS.md) and [development guide](docs/DEVELOPMENT.md). There is currently no application install, launch or test command.

Suggested next task:

> Read AGENTS.md and docs. Recover and review the Phase 1 ZIP if supplied; otherwise implement a clearly labeled new Python read-only prototype with a simulated transport and offline tests first. Keep vehicle writes, arbitrary commands and programming unavailable. Report exactly what was tested.

## Intended implementation

- Python for the initial ELM327 read-only experiment and protocol fixtures.
- C#/.NET with WPF for the planned Windows application; choose and pin a supported SDK when implementation begins.
- A vendor-neutral transport boundary, first simulated, then ELM327 for limited generic OBD reads, then a native J2534 broker.
- Versioned Saab module profiles backed by evidence, with explicit vehicle/module/driver coverage.
- Central operation policy and auditable sessions; future writes require separately reviewed workflows.

See [architecture](docs/ARCHITECTURE.md), [roadmap](docs/ROADMAP.md), [hardware plan](docs/HARDWARE.md) and [safety boundaries](docs/SAFETY.md).

## Hardware

An ELM327-compatible serial adapter is the proposed initial generic OBD interface. It is not a full Saab module interface. GM MDI2/J2534 is the long-term candidate, not a tested compatibility promise. Exact device, driver, Windows architecture and supported physical networks must be recorded before claiming support. Do not buy hardware based on an unverified project compatibility claim.

## Safety and limitations

Development starts offline and read-only. Read-only diagnostics still transmit requests and can affect network load. No DTC clearing, actuator control, configuration writes, security access, module pairing or flashing is included. Do not test while driving. Do not infer compatibility from model year alone. Future programming requires validated power, calibration identity, recovery procedures and authorized access.

## Repository map

- `AGENTS.md`: instructions for continued Codex work.
- `docs/ARCHITECTURE.md`: proposed layers and contracts.
- `docs/ROADMAP.md`: milestones with acceptance gates.
- `docs/SAFETY.md`: operation boundaries and failure handling.
- `docs/HARDWARE.md`: compatibility evidence template.
- `docs/DEVELOPMENT.md`: workflow, testing and next steps.
- `docs/PROVENANCE.md`: source conversation and missing archive.
- `.github/pull_request_template.md`: review checklist.

## License and affiliation

Private project; no open-source license has been selected. Do not redistribute third-party code without license review. No proprietary Saab/GM firmware, calibration files, workshop databases, security secrets or vendor drivers are bundled. SaabDiag is an independent project, not an official Saab, GM or Tech 2 product.
