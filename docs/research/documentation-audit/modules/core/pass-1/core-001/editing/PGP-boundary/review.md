# Independent OpenPGP boundary content/citation review

**Verdict: CORRECTIONS REQUIRED.** This is a fresh, bounded review of CORE-PGP-X001–X008,
not a new module audit. The reviewed patch SHA-256 is
`dac49659e9357569fd836922bd2f52fe9622a6f7794fcabe320d84d29933134a`.
No complete-batch accepted hash is issued until the three corrections below are rechecked.
`review-identities.json` binds this verdict to the exact product and editing-record bytes.

## Mandatory corrections

1. **Distinguish ResetAsync from the example's metadata return.**
   `src/OpenPgp/README.md:526–530`, CORE-PGP-X005/X008, retains “This method … returns
   fresh application metadata.” `OpenPgpSession.Reset.cs:25–75` implements `Task ResetAsync`
   and only updates `_appData`; `IOpenPgpSession.cs:686–687` also returns `Task`.
   It is `ResetAndInspectAsync` at README lines 539–545 that separately calls
   `GetApplicationRelatedDataAsync` and returns its result. Name that example as the actor,
   or describe ResetAsync as refreshing its cache without returning metadata. Preserve the
   unchecked-status, partial-state, ATT and retained-SCP qualifications. This is a retained
   inaccurate passage inside the reopened reset task, not a request for a runtime change.

2. **Use the actual shared-state member names.**
   `src/OpenPgp/CLAUDE.md:37–38`, CORE-PGP-X001/X006, still lists
   `OpenPgpBackend _backend` and `FirmwareVersion _version`. The actual field is
   `IOpenPgpBackend? _backend` (`OpenPgpSession.cs:72`); there is no `_version` field.
   Effective firmware is the inherited `ApplicationSession.FirmwareVersion` property
   (`ApplicationSession.cs:73,392–394`), configured at `OpenPgpSession.cs:138–144`.
   Correct the two entries without inventing a retained raw-version field or conflating
   the logged detected version, AID version and effective property.

3. **Account for the three omitted XML layout bodies.**
   `editing/PGP-boundary/example-inventory.json` inventories only Kdf and PrivateKeyTemplate
   XML fragments, and `static-verification.json:6` reports two. Scanning all 93 disposition
   paths finds **five**, all byte-identical to the source pin:

   | Omitted body | Exact `<code>` body SHA-256 |
   | --- | --- |
   | `src/OpenPgp/src/ExtendedCapabilities.cs:24–32` | `7f3c2b6be7ad1485d6a4a94ecac0f249349d41fd49a38776d545ae30c7233a1f` |
   | `src/OpenPgp/src/OpenPgpAid.cs:24–29` | `a0c9d14d41535ea38c4d268ed5a0052b6135aa7dc1003a091ccb5be7c3829f1f` |
   | `src/OpenPgp/src/PwStatus.cs:23–31` | `9cc46a1ddf217db965cafbbab45ccf6d2d32ca72afe34a9ed5e484902c906f6f` |

   These are illustrative protocol/layout fragments, not executable C#. Add their exact
   retained-body classifications and reconcile the export/report counts. The accurate combined
   denominator is **36 Markdown bodies and 28 XML bodies** (OpenPGP 17/5, PIV 19/23), not
   36/25. No body needs changing. This is X002/X003/X006/X008 example/coverage accounting;
   it does not allocate another implementation finding or add semantic-unit reading credit.

## Source and citation conclusions

I read both patches and delivery reports, the approved voice/house style and current primary
editing release, the latest primary OpenPGP candidate/disposition/source/provenance records,
all seventeen complete changed product files, and the actual relevant Core and applet callers,
helpers, result owners and assertions. Retained path reasons were checked against those
mechanisms and the completed source packet; this does not repeat its 328-unit investigation.

- Binding precedes wire I/O; direct creation borrows, convenience creation transfers hidden
  connection ownership only after initialization, and failed setup owns protocol cleanup.
  Configure/SCP precedes application-data parsing and readiness. No fallback transport is added.
- The first three version bytes are BCD-decoded only for OK responses of sufficient length.
  All other statuses, short successes and caught APDU exceptions reach 1.0.0. The Python
  implementation's narrower 6985 fallback is not substituted for this source behavior.
- Core's actual virtual dispatch confirms synchronous current-KDF cleanup before protocol drain,
  versus asynchronous Core teardown followed by derived cleanup in finally. Neither drains
  complete PIN/KDF workflows. Alias/replacement/reset, serialization/parser and allocation-before-
  cleanup-scope limitations remain; an independently copied encoding is not cleared by disposing
  the model or a different copy. PrivateKeyTemplate explicitly limits its cleanup to after
  GetComponents returns; it does not promise cleanup of partially constructed component lists.
- Reset retains effective firmware and the secure graph. PcscProtocolScp.SelectAsync sends plain
  SELECT through that graph; later traffic uses retained secure state, without a new handshake.
  The helper's separate plain reset followed by an optional SCP action session is not an SCP-reset oracle.
- Signature transfer, DER conversion, decipher-copy cleanup and Attest's completion-callback
  certificate leak are described at their actual owners. Per-exchange admission, complete-task
  input/prompt lifetimes and request/terminal callback scope are not native interruption guarantees.
  The Unverify test leaves policy uncontrolled and checks an exception, not isolated causal clearing.

Original consequential passages were checked in the retained Card 3.4.1 derivative (§§4.3–4.3.2,
7.2.2–4 and occurrence/GET DATA context), RFC 9580 §3.7.1.3, the manufacturer attestation HTML,
and the pinned Python comparison. Card 3.4.1 is March 18, 2020 despite its URL; card/terminal
actors and shall/should strengths stay distinct. RFC 9580 is Standards Track, July 2024;
its coded-octet packet count is not F9's expanded four-byte count. The attestation page is an
unversioned manufacturer extension, not an RFC/ISO rule: B6 03 84 01 81, overwrite, verifier
trust/binding and reset persistence retain their local applicability. The Python pin remains
`4ca60f706af930459138d8dc0f0f953480e1c7a4`, comparison evidence rather than normative authority.
Unchanged mathematical/vendor claims retain their original source-register qualification; this
review does not claim a new cover-to-cover acceptance of every original archive.

The B6 source register's distinctions remain intact: SCP03 1.1.2 Public Release (March 2019)
is not 1.1.2.6 Public Review (December 2019); SCP11 1.3.0.13 is Public Review (August 2023).
SCP11a/c host authorization includes its draft's first correct-MAC command and differs from b;
receipt/setup flags alone are narrower. Final 1.4, full YubiKey profile and exact unavailable
ISO clauses remain gaps, not presumed conformance. Generic Core catalog data adds no
provider, device, applet, slot or profile support.

## Preservation and review limits

Fresh read-only checks verified all seventeen product before/after hashes and the exact patch,
all 93 path decisions, 25 unchanged external referrals, 17 Markdown bodies, **five** XML bodies,
146 well-formed XML blocks and `git diff --check`. All changed C# lines are standalone comments;
all remaining complete source lines compare exactly to pinned Git, preserving executable tokens,
imports/directives, signatures/defaults/attributes, assertions and runtime help/output. This
static preservation proof does not bind XML crefs or compile examples/API consumers.

The preserved-provenance key lists exactly match the primary packet: 15 routed keys, 21 PGP-DEP
dependencies and fourteen engineering groups. Their original definitions/statuses and exact
reopening maps remain in the hash-bound primary records. PGP-IMP-001, -007 and -013 remain
confirmed for the historical PIN/KDF, X25519 and ATT-read scopes; documentation fixes none of them.
Other original numeric gaps, failed setup, historical successes and deferred facets remain intact.

The only editor-input hash that changed afterward is the live editing release; I reread its
additive PIV/external ownership grants. This does not alter source pin or frozen evidence.
Current primary Core product documentation was not substituted for the pinned implementation.

No SDK build/test/formatter, crypto/provider/native/device operation, Git index/ref/commit/push,
subagent, product edit or editor-record rewrite ran. Compile/token/API/cref/example/runtime
acceptance remains with the coordinator. Physical release/PIN state and cross-process behavior
are not inferred from disposal/UNPOWER. All PIV physical deferrals and the unready future Windows
delta remain. Recheck only the corrections, finish/publish Core Pass 1 and the deferred handoff,
then stop; no broader audit, implementation or planning project is requested.
