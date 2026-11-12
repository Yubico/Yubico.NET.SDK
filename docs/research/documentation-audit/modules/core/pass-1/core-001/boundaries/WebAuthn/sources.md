# Source authority and bounded research

Source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. `source-identities.json` records exact reused
report/original hashes; `sources/captures.json` records new HTTP captures, UTC access, bytes and hashes.
Reading historical captures preserves their original access dates and adds no runtime qualification.

The initial pass reused the completed CTAP/WebAuthn original-source checks and current A/B1/B2/B3/B4/
B5/B6/C reports/registers. It directly captured the existing dated W3C REC, vendor draft v4 and .NET
v10.0.0 Task/ReadOnlyMemory source. One focused follow-up inspected the exact retained CTAP 2.0
descriptor, 2.1/2.3 account-picker/PIN clauses, W3C PRF/blob/timer and vendor zero-length-handle passages,
and the pinned runtime WaitAsync implementation. No specification target was automatically upgraded.

| Original / exact version and status | Section, actor and strength | Application to this boundary |
| --- | --- | --- |
| Web Authentication Level 3, W3C Recommendation 25 August 2026; newly retained `sources/webauthn3.html` | §5.1.3/§5.1.4 client algorithms: adjust timeout to client range, start lifetimeTimer, cancel issued authenticator requests on expiry; algorithms ultimately reject when no response succeeds. §15.1 recommends 300000–600000 ms and default 300000 ms | SDK's no-default/no-clamp/cooperative signal/success-race behavior is narrower. Timers do not establish native completion, release or rollback. The expiry branch itself says cancel/remove issued requests; retain complete algorithm context rather than compressing adjacent user-cancel throw into an exact quotation of that branch. |
| Same REC | §5.8.1/§5.8.1.1 client CollectedClientData/CCDToString algorithm; exact field order, crossOrigin false when absent, Unicode control escaping | Existing manual short-escape gap remains WA-IMP-005. .NET JSON validity alone is no exact serialization proof. |
| Same REC | §5.2.2 AuthenticatorAssertionResponse exposes userHandle, not an entire user entity; §7.1/§7.2 RP verification algorithms | Application-facing native User DTO and a browser/RP response serialization target need an explicit actor decision. RP verifies challenge/origin/type/hash/signature/UP/UV/account independently. No field-filter policy is adopted. |
| Same REC | §10.1.4 client implementing hmac-secret PRF **MUST** use verified variant, overriding UV preference if needed; SHA-256 domain separation | PRF hash correctness is distinct from missing UV enforcement and sensitive-result ownership. |
| Same REC | §10.1.5 client authentication write: original allowCredentials must contain exactly one entry or return NotSupportedError; unsuccessful read keeps largeBlob present with blob absent | Later singleton narrowing is no original-input validation. Existing blob null-array conversion and read-error policy remain separate WebAuthn findings. |
| Same REC | §9.4 client **MUST NOT** return values for ignored extensions; §13.4.3 RP challenge **MUST** resist guessing, **SHOULD** be at least 16 bytes | Exact exception-isolation policy is SDK choice, not a quotation of §9.4. Local nonempty validation is no RP challenge verification. |
| WebAuthn Level 2, Recommendation 8 April 2021, prior original index WA-A C7; L2 attestation/entity references retained | §5.8.6 UV preference definition; §5.4.3 RP uses opaque handle rather than display fields | Keep existing Level 2 reference targets. Neither PIN-first SDK method policy nor universal token capability follows from Preferred's definition. |
| CTAP 2.0 Proposed Standard 30 January 2019; retained B5 `sources/ctap20.html/.txt` | §5.2 authenticator response table: credential Optional, may omit when actual allowList has exactly one credential | This applies to WebAuthn's narrowed singleton as well as an original singleton. Omission is not valid merely because only one credential matched a larger actual CTAP list. |
| CTAP 2.1 Proposed Standard 15 June 2021; errata edition 21 June 2022 separately retained B5 `ctap21.html/.txt`; CTAP 2.3 Proposed Standard 26 February 2026 retained `ctap23.html/.txt` | Later §6.2.2 tables mark descriptor Required. Authenticator identifying names/displayName/icon **MUST NOT** return without UV; server-side User optional, discoverable at least id. Multiple-account platform picker will **ONLY** return id back to WebAuthn and discard other user details | Preserve edition/actor distinctions. Corrected User/descriptor documentation stays resolved. Native selected-result actor and serializer contract remain DEC-002; recovery remains WA-IMP-010. |
| CTAP 2.1 errata as above | §6.5.1 platform **MUST** enforce minimum four Unicode characters, NFC, UTF-8 <=63 bytes and no final NUL; authenticator baseline four code points/63 bytes, greater minimum permitted. §6.5 note platform MAY manage token copies and SHOULD erase promptly when unneeded | Core UTF-16 counts, WebAuthn context units and ClientPin byte guard are different. Decide enforcement owner, preserve memory and device-token lifetimes separately. No silent normalization of existing PIN. |
| CTAP 2.1 errata / 2.3 as individually indexed by CTAP boundary originals | §6.5.5 supported PIN protocol and permission-token commands; §6.5.6/.7 strict EC2/P-256/-25 peer profile and V1/V2 derivation. §6.1/§6.2 platform MUST NOT combine uv key with pinUvAuthParam | Backend hardcodes V2. Legacy token command is no V1 negotiation and need not grant requested administrative permission union. Core catalog provides no missing strict profile enforcement. |
| CTAP 2.3 PS as above | §11.2.9.1.5 authenticator MUST cancel outstanding cancelable request and MUST NOT reply to CANCEL; original CBOR yields KEEPALIVE_CANCEL. Client MAY send during CBOR. §11.2.9.1.7 keepalive SHOULD be at least each 100 ms | Core keepalive-observed CANCEL/drain is no native read interruption or latency bound. Returning raced real success is SDK policy. |
| CTAP 2.3 PS as above | §12.1 platform SHOULD NOT create without requested policies 2/3 when enforcement requested; §12.3 largeBlobKey/storage route, §12.4 mutually exclusive direct route, §6.10.2 conditional write authorization; §12.5 code-point policy disclosure | Core transport support supplies none of the missing applet enforcement or optional direct-extension support. Strict sensitive credBlob requirements and storage-key non-RP exposure remain CTAP-owned conditions. |
| CTAP 2.1 PS; 2.2 Proposed Standard 14 July 2025; 2.3 PS as above, prior WA-B original index | hmac-secret §12.5 in 2.1 versus §12.7 in 2.2/2.3; hmac-secret-mc §12.8 in 2.2/2.3 | Existing version-specific references stay intact. Extension advertisement, actual UV, key agreement and cleanup retain separate scope. No whole 2.2 audit or feature qualification is claimed here. |
| Yubico DRAFT Web Authentication sign extension version 4, Editor's Draft 26 August 2025, newly retained `preview-sign-v4.html` | §10.2.1: generated key handle REQUIRED but MAY be zero-length; present additionalArgs MUST be encoded COSE_Sign_Args map; algorithm-specific TBS; RP requesting attestation SHOULD retrieve verified key/handle from it | Constructor's empty-handle rejection, passthrough semantics and unverified attestation remain distinct from cryptographic helper correctness. Draft is no adopted W3C/CTAP4 standard. |
| ARKG draft-bradleylundberg-cfrg-arkg-10, 27 February 2026, Internet-Draft intended Informational, expired 31 August 2026; prior CTAP originals | §5.1/.2 representation and §2.3 context bound; -65700 erratum, BL=-1/KEM=-2, seed/sign/verification roles | Preserve existing CTAP partial wire agreement and profile gap; no new profile upgrade. Core math remains deliberately draft-09, 7 July 2025 expired I-D, with C's own exact procedure/RFC9380 source scope. |
| .NET runtime tag v10.0.0, newly retained Task.cs and ReadOnlyMemory.cs | Task:2832–2940 completed-source fast path and separate CancellationPromise; ReadOnlyMemory array ctor / implicit conversion | Wait cancellation does not cancel source work; completed response may win canceled selection token. Null array → default nonnullable memory → present nullable memory supports WA-IMP-016 by source, not a new experiment. |
| FluentAssertions tag 7.0.0, matching Directory.Packages.props; retained objectgraphs.md and ByteArrayOrderingRule.cs | Docs:301–330; implementation:12–16 selects Strict for IEnumerable<byte> | Byte collections compare in exact order even under BeEquivalentTo/WithoutStrictOrdering. Preserve actual byte-value oracle; source mutation/cleanup and mathematical validity are separate limitations. |

Context7 resolved `/dotnet/docs` and supplied the wait-only versus operation-cancellation documentation
at `docs/standard/asynchronous-programming-patterns/cancel-non-cancelable-async-operations.md` and the
coalescing-timeouts snippet. This is current API orientation; the retained v10.0.0 source pins consequential
mechanics. .NET generic Task WaitAsync and CTS behavior also retain B1/B5's original-source evidence.
No returned documentation sample was executed or substituted for an SDK helper.

A focused Context7 FluentAssertions ordering query identified the special byte-collection exception to
ordinary order-insensitive equivalency. The repository pins 7.0.0; its original tagged documentation and
ByteArrayOrderingRule were then captured/read. An initial draft assumption that the byte comparison was
order-insensitive was corrected before handoff. This correction adds no test execution or weaker-oracle
finding. The true default-Aaguid/mutation/profile gaps retain their existing scope.

## Explicit external limits and research stop

- B4's exact Windows source/header and SafeHandle contracts support pinned synchronous caller/thread
  behavior only. Deployed NativeShims producer/header and reader encoding, historical Windows artifacts,
  class-GUID/HIDAPI source gaps and serial-9681620 failure cause remain B4 Q001–Q005.
- B6 retains final SCP03 1.1.2 Public Release March 2019 versus code-cited 1.1.2.6 Public Review;
  SCP11 1.3.0.13 Public Review August 2023 versus unavailable final 1.4/profile clauses; exact ISO7816-4:2020
  APDU clauses remain unavailable. This WebAuthn return inherits no unseen ISO/GP MUST or supported FIDO/SCP
  product matrix. No repeated gated-source search was performed.
- C retains NIST final PDF access gaps, SEC2 retrieved Draft headers, pinned OpenSSL/.NET/provider scope,
  ARKG math/side-channel/generator-provenance gaps. Generic catalog applicability remains structural.
- Shared YubiKey Manager `4ca60f706af930459138d8dc0f0f953480e1c7a4` (5.9.3-dev.0) is general Core/history
  context; CTAP's attempted `yubikit/fido.py` lookup found no module. WebAuthn's separate python-fido2
  `9d5991d6785e20bd487c8b6c996df6ce2226642a` RP callback comparison is implementation policy, not normative
  browser authority. Unpinned `_filter_creds`/5.8 re-mint comments retain qualified provenance; no new
  clone/import or exhaustive history hunt was used to bless them.
- The remaining consequential gap is **maintainer intent for the native selected-result disclosure
  actor**, plus the enumerated implementation choices and exact platform/device/profile qualification.
  Initial research and one focused follow-up are complete. No additional unbounded search is needed
  to return the assigned source packet.
