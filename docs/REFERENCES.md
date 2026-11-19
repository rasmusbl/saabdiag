# Repair reference library

Status: link catalog browser, source/applicability display, explicit website opening and user-selected local PDF opening implemented. No manuals are bundled or downloaded. Filtering, bookmarks, notes and managed local indexing remain planned.

`references/catalog.json` contains metadata for external resources. Start with Saab WIS for workshop procedures and the GM Tech 2 user's guide for scanner operation. WIS is a workshop information source, not a diagnostic transport specification or proof that SaabDiag supports an operation. Hosted historical manufacturer documents are distinguished from their hosting websites.

## Planned desktop experience

- Browse references without connecting a scanner.
- Filter by vehicle generation, model year, engine, system and document type; show unknown applicability explicitly.
- Open selected HTTPS references in the default browser. Do not silently download or scrape manuals.
- Let the user select legitimately obtained local PDFs; keep files and indexes in per-user application data outside the repository. Initial integration should open them with the user's PDF application rather than introduce a PDF engine dependency.
- Show title, publisher, host, edition/year, language, applicability, source and rights status. Verify applicability before linking a DTC to a repair procedure; do not imply a fault code proves a component must be replaced.
- Support bookmarks and user notes. Reference content cannot authorize or execute diagnostic operations.

Public availability does not establish permission to redistribute a manual or database. The GM guide explicitly reserves reproduction rights. Keep this catalog link-only unless permission or an applicable license is recorded. Local document selection does not grant redistribution rights. No WIS/EPC database, firmware or calibration package is included.

## Acceptance checks for implementation

Use offline fixtures to test catalog parsing, unknown fields/versions, missing local files, unsupported URL schemes, filtering and unknown applicability. No automatic network access on startup. Test reference browsing with no adapter installed. Record licensing and provenance before adding third-party document components or content.
