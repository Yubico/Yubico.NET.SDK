# CTAP2/Core: completed WebAuthn source follow-up

**Disposition: SOURCE-RECONCILED WITH QUALIFICATIONS** at
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. The completed
[WebAuthn return](../../boundary-WebAuthn.md) and
[CTAP2 handoff](../WebAuthn/ctap2-handoff.md) agree with the exact caller/source anchors below.
The named WebAuthn return is received and reconciled; its engineering and actor decisions remain open
for their stated reasons, rather than awaiting another source packet. This addendum supersedes only
the earlier “WebAuthn return pending” source-follow-up status. Original records remain unchanged.

## Named dispositions and reopening map

| Question | Reconciled source outcome | Remaining action / existing IDs |
| --- | --- | --- |
| Ceremony versus exchange lifetime | WebAuthn has no whole-ceremony admission/disposal join. Backend disposes its shared PIN protocol before awaiting session disposal. Core drains an exchange, not probing, token acquisition, GetNext, extension output or blob workflows. | Preserve `CORE-CTAP-X002` / `CORE-WA-X002` / B5-C006. Add fully awaited, serialized ceremony and outer disposal/prompt scopes to WebAuthnClient lifetime XML and README Constraints; coordinate CTAP session/helper guidance. Workflow admission, reentrancy and shared-disposal design remain separately selected implementation work. |
| Connection membership | WebAuthn forwards session options to CTAP's one-transport factory. YubiKeyDevice claims its known InterfaceIds; Core exclusion requires overlapping member IDs in this process. | `CORE-CTAP-X001` / `CORE-WA-X001`: qualify both README all-interface headlines, CTAP CLAUDE and physical/discovery guidance. Disjoint scopes, direct factories and other processes need separate coordination; no new registry defect allocated. |
| `webauthn-001:WA-IMP-010` | Probe can identify a selected descriptor and send a narrowed singleton. Matcher and final response builder independently derive IDs from the optional final descriptor, publishing Empty when omitted. | Source-confirmed recovery gap remains. Reopen CredentialFilter, CredentialMatcher, WebAuthnClient.Authentication, MatchedCredential.Id, AuthenticationResponse.CredentialId and per-credential PRF/preview consumers on the chosen association/error-policy repair. Never guess the first original multi-list entry. `CORE-WA-X007` stays distinct from CTAP X011 structural parsing. |
| `ctap2-001:CTAP-DEC-002` | Modeled User passes from CTAP through matcher, match and AuthenticationResponse. SelectAsync retrieves the same already-built response; it performs no disclosure transformation or further device work. The documented RP handoff belongs to the application, with no SDK upload/response serializer. | **Actor trace reconciled; policy NEEDS MAINTAINER/COORDINATOR DECISION.** Decide whether native output is picker/raw application data or an intended platform-to-WebAuthn/browser-equivalent disclosure boundary. Absence of a serializer proves neither conformance nor permission to retain every field at a future RP boundary. No stripping authorized. Reopen User XML, match/result construction, both guides and any chosen serializer under `CORE-WA-X008`. |
| `webauthn-001:WA-XREF-001` / `ctap2-001:CTAP-XREF-001` | Current User/descriptor XML correctly separates opaque handle, identifying display fields and edition-specific descriptor rules. | **Original documentation resolution preserved.** Recovery and disclosure policy remain their separate keys; neither reopens this as an unfixed prose finding. |
| Response and notification ownership | CTAP resolves exchange status before typed decode; WebAuthn then performs response/extension construction. Token, auth-tag and shared-secret owners clear their adopted buffers, while report/APDU/CTAP/attestation/authData/PRF/blob copies retain different lifetimes. | `CORE-CTAP-X004/X014/X016/X019/X020` / `CORE-WA-X004/X006`; retain `ctap2-001:CTAP-IMP-004/027/035` and `webauthn-001:WA-IMP-006/013`. Select explicit owners and exceptional cleanup per buffer; reopen backend/result/codec/processor/XML and actual-array observers after each repair. No end-to-end cleanup closure. |

## Exact caller agreement

- **Ownership/factory:** `src/WebAuthn/src/IYubiKeyExtensions.cs:60–82` forwards sessionOptions and
  disposes the new CTAP session on client-construction failure. `WebAuthnClient.cs:65–75,94–102` adopts
  a supplied session only on successful construction. That adoption does not acquire a connection the
  direct CTAP factory already borrowed. CTAP `IYubiKeyExtensions.cs:140–207` retains one chosen route,
  no failover and hidden-connection ownership. B4/B6's already reconciled source outcomes remain intact.
- **Membership/lifetime:** Core `YubiKeyDevice.cs:131–181` and
  `DeviceConnectionRegistry.cs:20–119` identify the known-member/process scope. Core
  `ExchangeGuard.cs:49–62,98–108` exits after its delegate; `ApplicationSession.cs:43–44` explicitly
  records no in-flight public-workflow lease. `WebAuthnBackend.cs:263–282` disposes protocol before
  session; WebAuthnClient has only a bool guard, not an operation join. Caller scopes must cover actual
  complete work, including any late prompt callbacks, before final disposal.
- **Recovery:** `CredentialFilter.cs:130–138` returns the singleton probe descriptor;
  `WebAuthnClient.Authentication.cs:164–172` forwards selected/dummy singleton to the final command.
  `CredentialMatcher.cs:59–74` loses that request association on descriptor omission, and
  `WebAuthnClient.Authentication.cs:216–232` reconstructs the result ID independently. CTAP's
  `GetAssertionResponse.cs:54–58,164–232` has no request/version context and leaves absence null.
  Safe application advice about an **original** singleton remains useful; internally, a narrowed
  singleton can also be known even when the original WebAuthn list contained several entries.
- **User/output:** CTAP `GetAssertionResponse.cs:80–86,197–199` passes modeled names/handle;
  WebAuthn authentication construction at `122–134,216–235` retains User and builds every response
  before return. `MatchedCredential.cs:60–94` caches that response without filtering. README
  `133–147` gives the application RP-format/upload task. Source settles this actor path, not the intended
  native/browser disclosure contract.
- **Notification/result:** CTAP `Ctap2Session.cs:265–385` clears its encoded Make/Get request, uses
  None for GetNext and silences up:false probes. Both backends resolve terminal status before transferring
  response views; typed decode and WebAuthn extension output can still fail afterward. Core notification
  `Create:92–107` captures caller-flow intent and `ResolveAsync:142–168` uses None for resolution.
  A Completed notification is successful CTAP exchange status, not whole-ceremony/RP acceptance.
  Presence service and credential-input prompt have different callers and ownership; neither becomes
  client-owned merely through retention. X020's conditional throwing-logger cleanup gap remains separate.

`PinUvAuthTokenSession.cs:70–86`, `PinUvSharedSecret.cs:30–54` and backend auth-parameter finally blocks
identify actual adopted secret scopes. `PrfCodec.cs:75–89` clears aggregate plaintext but materializes
independent range/result arrays. Shared ownership observations agree; they do not supply a repair for
borrowed attestation, unknown response preservation or successful sensitive results.

The routed `webauthn-001:WA-IMP-007/017/019` questions also receive their handoff dispositions:
default Aaguid remains separate from CTAP endian conversion (X023); optional direct-blob support and
key 8 handling retain X010 / `ctap2-001:CTAP-IMP-030` / `audit-v2-20261022-20261031:#4`; empty generated
handle round-trip policy remains separate from CTAP accessor propagation, draft profile and Core math
(X007/X008). These stay applet-owned engineering/decision items, not missing Core source returns.

## Authority, verification and stopping boundary

Retained CTAP **2.0 Proposed Standard, 30 January 2019, §5.2** permits authenticator descriptor omission
for the actual singleton CTAP allowList. CTAP **2.1 PS, 15 June 2021**, its **21 June 2022 errata edition**,
and **2.3 PS, 26 February 2026, §6.2.2** require the descriptor. Their authenticator MUST NOT disclose
identifying names without UV and multiple-account platform picker's ONLY-id return have distinct actors
and conditions. Reused original passage identities and WebAuthn's exact source register are in the JSON;
no edition upgrade, new conformance claim or unbounded research was introduced.

Static hash/pin/identity verification is recorded in [followup-WebAuthn.json](followup-WebAuthn.json).
The original CTAP 590 units / 26 keys / 23 concerns and WebAuthn 265 units / 31 keys / 12 concerns remain
unchanged. Independent review, active documentation editing, sampling/consumer validation, selected
implementation/policy and platform/device qualification remain open. The new Windows concurrency delta
is **UNREADY / UNREVIEWED**; B4/B6's producer/ABI/ISO/final-GP/vendor gaps and PIV physical deferrals remain.

This finishes CTAP2's pinned source-follow-up reconciliation. Work stops at this bounded return: no
new plan, tooling project, implementation, field stripping or further execution is initiated.
