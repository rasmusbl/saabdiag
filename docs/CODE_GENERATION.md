# Offline Trionic 8 key calculation

Implemented 2026-11-19. The Authorization tab has a working local calculator for the published **Trionic 8 security level 0x01** transform. This is actual arithmetic, not a simulated random code. Evidence level: offline-tested only. No specific ECU software revision, scanner or vehicle has been validated.

Enter exactly four ASCII hexadecimal digits, high byte first. Calculate explicitly. Invalid input, unknown algorithm identifiers and zero seed return no key. Editing the seed clears the previous result. Clear removes the displayed input/output; nothing is persisted or logged by this feature. Managed strings are not a guarantee of forensic memory erasure.

The calculation rotates the 16-bit seed right five places, adds hexadecimal B988 and reduces modulo 65536. It does not contact an ECU, check an ECU response, begin a diagnostic session, or grant an authorization approval. It has no dependency on transport or provider services. A zero seed has special meaning in the upstream exchange and is deliberately not interpreted as authorization by an offline tool.

## Provenance and reuse approach

Algorithm evidence: [TrionicCANLib SeedToKey.cs](https://github.com/mattiasclaesson/Trionic/blob/e00dc21b352c4484e5b38ff7e37251d855ca15dd/TrionicCANLib/SeedToKey.cs). Known-answer evidence: [upstream SeedKeyGMTest.cs](https://github.com/mattiasclaesson/Trionic/blob/e00dc21b352c4484e5b38ff7e37251d855ca15dd/TrionicCANLibTest/SeedKeyGMTest.cs), which identifies level 0x01 and the synthetic/public example `5F94 -> 5C84`. These are different code paths in the same upstream project, not independent vehicle evidence.

SaabDiag's small arithmetic function and input/UI handling were written here; no upstream source file, dependency, firmware, lookup table or binary is incorporated. This is an implementation of the published mathematical relationship, not a relicensing of TrionicCANLib code. The upstream repository's reuse license remains unresolved, so importing its implementations is still blocked. No claim of an independent clean-room process is made.

Only level 0x01 is exposed. Levels FB/FD, CIM, other engines, physical Tech 2 card authorization and pairing/programming codes are unsupported. Model year is not used to infer a module or algorithm. Independent vectors, exact ECU software identity, reviewed transport and hardware milestone gates are required before integration with a vehicle security exchange.

## Live services

TIS2000, GlobalTIS and external provider connections remain unimplemented. No documented API, configured installation/version or provider access was supplied. No guessed endpoint or invented request schema has been introduced. The offline calculator does not change their status. See AUTHORIZATION_PROVIDERS.md for the missing integration evidence.
