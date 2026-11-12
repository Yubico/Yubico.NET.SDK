# Additive WebAuthn return for the CTAP2 boundary owner

The pinned WebAuthn source-reading dependency is **SOURCE-RECONCILED WITH QUALIFICATIONS**. This return
changes no CTAP2 record or its 590 unit dispositions / 23 local concerns. The CTAP2 owner can consume
this file in a later additive follow-up. Independent content acceptance, policy decisions and physical
qualification remain open.

| CTAP2 named question | Answer from actual WebAuthn callers | Resolution / remaining owner |
| --- | --- | --- |
| WA-XREF-001 / CTAP-XREF-001 User and descriptor prose | Current CTAP GetAssertionResponse:54–86 and WebAuthn result XML accurately distinguish handle, optional names and edition-specific descriptor rule. | Preserve prior documentation resolution. No reopening as an unfixed prose finding merely because recovery/privacy work remains. |
| WA-IMP-010 recovery | CredentialFilter:130–138 returns the known singleton probe descriptor; authentication request:164–172 sends selected or dummy descriptor. Matcher:59–74 discards request association on final omission; builder:216–232 independently reads final descriptor again. | Source-confirmed WebAuthn recovery gap retained. CTAP2.0 PS §5.2 allows omission for the actual singleton CTAP allowList, including a narrowed original multi-entry list. Later §6.2.2 tables require it. Future fix must keep selected association through both match/result and per-credential consumers; never guess first original entry. |
| CTAP-DEC-002 selected native User actor/privacy/serializer | CTAP parser copies handle and optional modeled name/displayName, skips icon. Matcher, MatchedCredential and AuthenticationResponse retain/pass the entity. SelectAsync returns the already-built same response; it performs no account-picker filtering, upload or WebAuthn response serialization. README explicitly leaves RP response format/upload to application. | Actor trace resolved; policy **NEEDS MAINTAINER/COORDINATOR DECISION**. Native application-facing DTO is not established as browser-equivalent output merely by its WebAuthn name. Decide raw/native/picker versus browser/RP handoff contract explicitly. No field stripping authorized. |
| WA-IMP-006 / CTAP-X004 response lifetime | Public attestation Decode borrows caller outer CBOR/statement slices; CTAP and WebAuthn authData copies are independent. Generated-key Create repacks outer CBOR and retains statement/inner objects. | Ownership trace agrees, snapshot-versus-borrow design remains WebAuthn-owned. Core response-copy repair alone cannot clear these results. |
| WA-IMP-007 / CTAP-X023 | WebAuthn default Aaguid empty storage/nonreflexive equality is separate from CTAP endian conversion; registration uses an explicit Guid constructor. | Preserve distinct IDs and tests; neither is Core math/catalog defect. No big-endian execution. |
| WA-IMP-013 / CTAP-X004/X014/X016/X019 | Ceremony token/tag and per-attempt shared secret clear their adopted owners. PRF input concatenation, double range/result arrays, CTAP builder clones, report/response copies, blob plaintext remain independent. | Agreement on layered ownership; retain each exceptional/success owner. No end-to-end cleanup closure or writable-output workaround invented. |
| WA-IMP-017 / CTAP-X010 | WebAuthn uses largeBlobKey/storage only; GetAssertion key8 unsigned output is dropped by CTAP model. Write requires SDK token even where CTAP conditionally permits pinless. | Retain optional-support/permission decisions and original #4/CTAP-IMP-030 aliases. Removing old API did not implement key8. No general requirement to implement every optional direct extension. |
| WA-IMP-019 / CTAP-X007/X008 | GeneratedSigningKey permits an empty handle; WebAuthn signing params rejects it before conversion to CTAP params. Chosen algorithm comes from signed outer previewSign output, while public key may carry seed algorithm. | Empty-handle round-trip remains WebAuthn constructor policy, separate from CTAP accessor propagation/draft profile/Core mathematics. |
| B5 per-exchange lifetime and presence | WebAuthn has no ceremony admission/disposal join. Backend disposes shared PIN protocol before waiting for session disposal. CTAP terminal success precedes WebAuthn decode/output processing. Silent probes and generic ClientPin work remain silent. | Agreement; X002/X018/X020 keep their Core/CTAP mechanisms. Add whole-ceremony caller guidance and preserve conditional logger-failure cleanup gap. |
| B4/B6 completed follow-ups | Actual WebAuthn factory forwards unchanged session options; all admitted operations use the same CTAP backends/native routes. No second fallback, SELECT, reset reinit, metadata substitution or APDU/SCP wrapper appears in WebAuthn. | Source agreement with completed mechanism follow-up. Native/ISO/final-GP/vendor/profile/producer gaps retain original owners. New Windows delta remains unready/unreviewed. |

## Explicit disagreements and scope corrections for integration

1. **Native DTO versus protocol layer:** CTAP's account-picker text in §6.2.2 says the platform will
   ONLY return id to the WebAuthn layer and discard other user details. That is a platform-to-WebAuthn
   processing statement for multiple-account selection, distinct from the authenticator's MUST NOT
   disclose identifying fields without UV. The SDK's `AuthenticationResponse.User` is application-facing
   native output and contains the full modeled entity. Source settles retention, not whether maintainers
   intend that DTO to be the protocol-equivalent disclosure boundary. Retain DEC-002; treating the
   absence of an upload serializer as automatic conformance would be as unsupported as automatic filtering.
2. **Recovery after narrowing:** CTAP's original-singleton advice is sufficient at its request boundary.
   WebAuthn caller sees original options, while the implementation knows a different narrowed singleton.
   Both need distinct guidance. Current public advice to recover only an original singleton is safe;
   it does not excuse implementation loss of the selected internal descriptor on a multi-entry request.
3. **Required token versus actual authorization:** WebAuthn requests the permission union, but ClientPin
   can use legacy token acquisition with V2 and drop requested permissions/RP. A token owner is a memory
   owner, not evidence that LargeBlobWrite was granted. Keep CTAP token/fallback qualifications in any
   future WebAuthn support table. Built-in UV token traffic has no inferred presence callback.
4. **Snapshot claims:** registration transports really are independent snapshots. GetInfo cache,
   attestation borrowed bytes, response arrays and discovery metadata have other lifetimes. Avoid
   promoting a single passing snapshot test to a result-wide or cache-freshness guarantee.

The outstanding decisions have precise evidence requests: maintainer-selected native/browser disclosure
actor and serialization target; selected descriptor-association/error policy; owned sensitive-result
design; selected PIN enforcement layer and protocol negotiation; vendor/draft/provider/OS/device
qualification under separately coordinated work. B4's pinned Windows findings are not the user's future
concurrency delta. The 4,442 integration units / 197 overlapping reruns and Linux native-only evidence
add no WebAuthn ceremony qualification. PIV physical deferral and later coordinated WebAuthn UP/UV work
remain unchanged.
