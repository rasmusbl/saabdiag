# Project provenance

Prepared 2026-11-08 from the user-authorized handoff.

Source chat: **Create Saab Scanner Tool**, conversation ID `6af0a491-df14-83ed-8fc4-d1bd8beee312`.

## Requirements carried forward

- Saab 9-3 II, model years 2003–2012.
- Windows PC application.
- Long-term Tech 2 alternative with diagnostics, module configuration and programming.
- Initial Python ELM327 read-only experiment; eventual C#/WPF and J2534/MDI2.
- New private GitHub repository named `saabdiag`.

## Archive status

The source assistant linked `sandbox:/mnt/data/saabdiag-phase1.zip` and described `saabdiag.py`, `requirements.txt` and `README.md`. It claimed syntax validation and explicitly said no real adapter/vehicle tests had occurred.

The retrieved conversation contained no attached archive or source-code contents. The referenced /mnt/data path was unavailable in this Windows environment. A filename search of Downloads and Documents/Codex found no SaabDiag archive or source. **No prototype code was imported, inspected or independently tested.** The earlier syntax-check claim cannot be verified here.

If recovered later, record its checksum, actual contents, dependency versions, command behavior and any modifications. Do not silently substitute newly generated code for the historical artifact.

## Research context

The earlier conversation mentioned external Saab configuration projects and workshop resources. Those claims and implementations have not been reviewed for correctness or licensing in this repository setup. They are research leads only. No third-party code, protocol values, security algorithms or firmware were copied into this foundation.

The former proposal called the project open-source. This handoff explicitly requests a private repository, and no license has been chosen. Privacy and license choice are independent; do not claim an open-source release.

## Requirements update, 2026-11-08

The user requested Windows 10/11, preferably .NET, research into Tech 2 authorization and Saab repair references. New development now starts in C#/.NET; the Python proposal above remains historical context. Added a link-only catalog and research notes; no external code, algorithms or manuals were imported. The existing Create SaabDiag repository chat was reviewed to confirm the PC-based alternative goal. The user subsequently confirmed: replace Tech 2 with a Windows PC application. Physical Tech 2 control/card unlocking is outside the current scope; exact vehicle and PC-connected adapter details remain unspecified.

## New implementation, 2026-11-08

Newly authored C#/.NET/WPF simulation; not a recovered prototype. Synthetic JSON replies have no Saab protocol provenance and are intentionally not used with hardware. No addresses, security algorithms or vehicle captures were imported. Microsoft .NET SDK 10.0.403 was downloaded from the Microsoft builds release metadata URL and verified against its published SHA-512 checksum; stored only in ignored `.tools/`. No external NuGet packages. See VALIDATION.md.

## Connectivity scenarios, 2026-11-19

User requested support for different connection scenarios after supplying a photograph and a contact report describing a physical Tech 2 and clone CANdi. Added scenario configuration and local registry metadata discovery, not hardware protocols. Contact claims remain unverified; no original-vs-clone assumption grants compatibility. Manufacturer registry-schema sources are recorded in CONNECTIVITY.md; no external code or driver binaries were imported.

## Authorization framework, 2026-11-19

User requested all provider/code-generation route options. Added original provider interfaces, decision validation and unavailable integration targets. No existing security algorithm was imported, reconstructed or claimed as verified. Research sources and integration requirements are recorded in AUTHORIZATION_PROVIDERS.md.
