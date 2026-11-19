# Tech 2 and security access research

Updated 2026-11-19. Authorization provider framework and an offline Trionic 8 level 0x01 calculator are implemented; no live provider or vehicle security exchange is implemented or validated. See CODE_GENERATION.md and AUTHORIZATION_PROVIDERS.md.

## Distinguish the workflows

The user confirmed a PC-based Tech 2 replacement using a PC-connected vehicle adapter. Physical Tech 2 setup/provider options are now represented, but actual companion communication and card unlocking remain unimplemented. The physical scanner references below are background research, not the proposed authorization implementation.

Do not model authorization as a single permanent code required before any scanning. Ordinary diagnostic reads and protected operations have different requirements. Each future module profile must declare those requirements from evidence. Neither simulation nor reference browsing requires vehicle security access.

Saab's 2008 9-3 (9440) workshop instructions describe obtaining Tech 2 security access through a PC running TIS 2000, with a VIN check before returning to the vehicle. This is historical workflow evidence, not proof of current service availability or Windows 10/11 compatibility:
https://saabwisonline.com/9-3-9440/2008/0-general/tech-2/technical-description/get-security-access

MySAAB.info's own Tech2 Card Manager documentation says its security feature reads seeds from a physical Tech 2 card and generates keys. This establishes a public implementation claim, not independently verified behavior, a reusable specification, or permission to copy code. Its page states all rights reserved:
https://sas.mysaab.info/help

Physical scanner/card authorization is not automatically the same protocol as an ECU's security access. No universal Saab seed/key algorithm, test vectors, licensing permission or direct ECU coverage has been established in this project. Do not copy website scripts, assume one algorithm covers all modules, or bundle scanner card images.

## Future integration gate

Before implementation, identify the exact direct-ECU or programming-service workflow, ECU identity, transport, documented request/response format, authorized provider and reuse terms. Obtain independently verifiable synthetic test vectors. Validate rejection of wrong identity, malformed responses, expired authorization, cancellation, timeout and disconnection. Do not log keys or credentials. Do not automatically retry security attempts.

Implement any approved integration behind a dedicated authorization provider and the central operation policy. Missing or unsupported providers must deny protected operations. Security access, pairing and programming remain disabled under roadmap milestones 5 and 6; actual hardware operations require their own concrete authorization.

The virtual workshop has a demo permission flag for illustrating UI prerequisites. It generates no seed, key, authorization code or credential and cannot authorize diagnostic or vehicle operations.
