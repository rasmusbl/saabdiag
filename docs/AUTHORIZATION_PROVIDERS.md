# Authorization provider framework

Implemented 2026-11-19: provider contract, decision coordinator and an Authorization tab listing all requested integration targets. **No live provider is shipped. A separate offline Trionic 8 level 0x01 calculator is implemented; see [code generation](CODE_GENERATION.md).** Options are not compatibility claims.

| Option | Workflow | Required before implementation |
|---|---|---|
| TIS2000 | Physical Tech 2 | Exact Saab TIS/card versions, reviewed PC/scanner protocol and authorized access |
| GlobalTIS | Physical Tech 2 | Exact Saab release, supported interface, access requirements and coverage |
| External Tech 2 provider | Physical Tech 2 | Documented API/protocol, authentication, reuse rights and scanner/card compatibility |
| Direct ECU provider | PC-to-ECU | Exact module identity, security level/session, provider API and validated transport |
| Reviewed ECU code generator | PC-to-ECU | Verified algorithm, provenance, reuse rights and independent test vectors |

## Implemented contracts

`IAuthorizationProvider` declares exact-context support and accepts a correlated `AuthorizationRequest`. `AuthorizationService` binds each decision to provider ID, workflow, profile, target reference, requested operation and a new session ID. Capability checks reject unsupported profiles before invoking a provider. Responses must match every scope field and the request ID, and have valid issue/expiry times. Approval lasts no longer than the provider response permits, capped at five minutes by this application framework; this cap is application policy, not a claim about Saab authorization lifetimes.

Cancellation, timeout, rejection, provider exceptions and invalid responses retain no approval. Reconnect/disconnect/settings changes invalidate the context before cancellation is signalled. Late responses cannot authorize a new session. No automatic retries occur. Provider exception text is not exposed to the UI. Approval for one operation/provider/workflow cannot approve another.

The contract currently handles decisions only: no seeds, keys, credentials, VINs or generated codes. It is not a complete TIS/ECU transport, token format or cryptographic verification implementation. A future integration may require a separately reviewed secret-handling contract. In-memory decisions are never persisted or exported.

## Registration and UI

Production construction registers zero executable providers. The UI lists five explicitly unavailable targets and requires explicit selection; it does not default a provider or issue requests at startup. Request authorization remains disabled. Provider injection and session binding are internal to the core; only offline tests inject fake providers. No dynamic DLL/plugin loading or mock provider is enabled in the desktop build.

The provider settings panel accepts and saves each target's software version, service address, absolute installation path and documentation URL. Blank fields are allowed for incomplete setup. Settings reload from `%LOCALAPPDATA%\SaabDiag\provider-settings.json`; save before changing provider selection. URLs require HTTPS except loopback HTTP, and reject embedded credentials, query parameters and fragments. No password/token storage is implemented. Save validates the whole configuration and replaces the local file through a temporary file. Malformed/oversized files fail closed; the UI disables saving if loading failed so existing data is not silently replaced. Configuration never opens an executable, URL, socket or vehicle connection and cannot enable a provider. Actual integration still requires provider-specific protocol implementation after those details are available.

Diagnostic writes remain denied independently of provider decisions. The Virtual Workshop's demo permission flag is separate and cannot grant real authorization. The framework is not wired to vehicle security access, pairing or flashing. Adding a provider requires source changes, review and milestone gates, not merely selecting an option.

## Evidence and remaining requirements

Offline tests cover unavailable/mismatched providers, unsupported context, expiry and response correlation, rejection, exceptions, timeout, cancellation, disconnect/reconnect, concurrency and continued denial of diagnostic writes. No hardware authorization was attempted.

On 2026-11-19, the [Saab workshop procedure](https://saabwisonline.com/9-3-9440/2008/0-general/tech-2/technical-description/get-security-access) and [MySAAB.info provider help](https://sas.mysaab.info/help) were checked again. The former describes a historical TIS2000 workflow; the latter claims card seed/key generation and reserves rights. Neither reviewed page supplies an implementation specification or reuse license for a SaabDiag algorithm. No code, key material or proprietary databases were copied.

### Real integration investigation (2026-11-19)

The request to make connections real remains incomplete. No service endpoint or documented provider protocol has been supplied. A read-only check of Windows machine uninstall registrations found no TIS2000/GlobalTIS entry; this does not exclude portable software or installations on another computer. No service, scanner or vehicle was contacted.

- **TIS2000 / GlobalTIS:** obtain the actual installed Saab release and its integration documentation or a reviewable installation. A product name alone does not define the request protocol. Do not implement a guessed HTTP endpoint.
- **External Tech 2 service:** the [MySAAB.info privacy policy](https://sas.mysaab.info/privacy) states that security seed and VIN-related data are processed server-side for key calculation. Its reviewed public help/privacy pages do not document a third-party API. Integration needs provider documentation and access; opening the website is not an authorization connector.
- **Local ECU generation:** published code exists in [TrionicCANLib](https://github.com/mattiasclaesson/Trionic/blob/e00dc21b352c4484e5b38ff7e37251d855ca15dd/TrionicCANLib/SeedToKey.cs). The inspected revision is `e00dc21b352c4484e5b38ff7e37251d855ca15dd`. Its [tests](https://github.com/mattiasclaesson/Trionic/blob/e00dc21b352c4484e5b38ff7e37251d855ca15dd/TrionicCANLibTest/SeedKeyGMTest.cs) include module-specific examples for T8, ME9.6 and EDC16C39. These are upstream examples, not independent SaabDiag hardware validation. The inspected tree contains no LICENSE/COPYING file and the inspected source has no license header, despite the README describing the project as open source. Reuse terms remain unresolved; no source or constants were imported. Exact module/software/security-level coverage and independent vectors are also required before enabling a vehicle-integrated generator.

The subsequent offline-only implementation is documented in CODE_GENERATION.md. Local seed/key calculation is not a universal Saab authorization code. It does not establish physical Tech 2 card authorization, pairing permission or programming support. The current decision-only contract cannot carry challenges or generated keys and must be extended for a specific verified integration, rather than treating a local approval flag as a successful ECU unlock.
