# Protocol editor citation decisions

Use the retained original identities and completed source registers below. This editor retrieved no new
source, changed no source register and executed no protocol or cryptographic operation. Existing registers
remain evidence inputs, not blanket conformance certificates.

| Correction | Exact retained authority | Actor, strength and applicability |
| --- | --- | --- |
| CTAPHID report/header/control grammar | CTAP **2.3 Proposed Standard**, February 26, 2026, §§11.2.3–11.2.5, 11.2.9.1.3 | Authenticator allocates the CID; host SHALL accept INIT responses longer than 17 bytes. Protocol-version byte, vendor-defined device version and CTAP edition are distinct. SDK64 is a supported connection shape, not every descriptor-defined HID size. Same-CID INIT resynchronization differs from another CID's busy response. |
| CTAPHID cancellation | Same 2.3 edition §11.2.9.1.5; corroborating **2.0 Proposed Standard**, January 30, 2019, §8.1.9.1.5 and **2.1 errata Proposed Standard**, June 21, 2022, §11.2.9.1.5 | Client MAY send during active CBOR; authenticator MUST cancel outstanding cancelable work, MUST NOT reply to CANCEL itself, and returns KEEPALIVE_CANCEL on the original canceled request. Idle/inactive-CID cancellation is ignored. Vendor-command scripts do not extend these CBOR rules. Raced-success return is SDK policy, not an invented host MUST. |
| Keep-alive versus deadline | CTAP 2.3 §11.2.9.1.6/.7 | Authenticator SHOULD send keep-alives at least every 100 ms/status change. This is not an SDK native-read timeout. Existing MSG/CBOR source wording is not generalized to vendor command semantics. |
| OTP report/control/CRC | Yubico **yubikey-personalization implementation/header**, pin `db0c0d641d47ee52e43af94dcee603d76186b6d3`, `ykdef.h:56–79,253–265`, `ykcore.c:490–752`; pinned Python comparator in B5 register | Vendor implementation evidence, not a revisioned normative OTP standard or deployed-device proof. Seven data bytes plus control per report; padded 64-byte outbound payload CRC, command-specific inbound expected length. C#/vendor timing and NEO exception policies remain distinct. |
| SCP03 derivation and minimum response parsing | GlobalPlatform Card Spec v2.3 Amendment D **SCP03 1.1.2 Public Release**, March 2019, §§4.1.5,6.2.1,7.1.1.6 | The session-key context is host challenge concatenated with card challenge (§6.2.1, not the old §6.2.2.1 card-challenge citation). §7.1.1.6 conditionally includes three sequence-counter bytes; current parser's 29-byte minimum is not exact universal response conformance. |
| SCP03 empty input | Same Public Release §6.2.6 | Off-card entity advances the counter per protected command; no encryption SHALL be applied to an absent command data field. Current empty-input padding/encryption remains an implementation/profile discrepancy, not an intended guarantee. |
| SCP03 response statuses | Same Public Release §6.2.5 | Card protects 9000/62xx/63xx except EXTERNAL AUTHENTICATE; other statuses are status-only unprotected errors. Current INS82 exemption is instruction-based rather than phase-based. Synthetic signed 61xx/6A82 does not validate the standard's error rule. |
| SCP11 roles and auth phase | Amendment F **SCP11 1.3.0.13 Public Review**, August 2023, §§3.1.1/.2,4.3,4.4,5.3.2,7.5.1,7.6.2.3/Table 7-18 | Draft clauses apply to their actors: host selects trusted SD key/verifies receipt; SD verifies submitted OCE chain/policy. For a/c, SD authentication of OCE includes first correct-MAC APDU after establishment (§4.3). Receipt seeds first command MAC (§5.3.2). c returns static SD key, unlike a/b ephemeral role. Host/Card/CardGroup requirements depend on requested option bit, not every setup. |
| CMAC test value scope | **RFC 4493, Informational**, June 2006, §4 | Published full-value examples are distinct from length/equivalence/disposed-refusal assertions. They do not prove native erasure or certification. |
| Framework/storage explanations | Completed B5 register's pinned **dotnet/runtime v10.0.0** Task/ValueTask/ExecutionContext/SemaphoreSlim sources; completed B6 register's stackalloc guidance; C final provider/AsnWriter addenda | Outer WaitAsync can end a wait without terminating the original task/borrow. Stack declarations alone do not guarantee initialized content. C's pinned provider-copy/Reset conclusions stay scoped to the inspected route; no installed runtime/native cleanup assurance is added. |

Retained PDF SHA-256 identities:

- SCP03 Public Release 1.1.2: `973a10834f7fe8befd98da9af65393622234b80745092bd85248804c57b648dd`.
- SCP11 Public Review 1.3.0.13: `5db47b3df3c4e565a5ad91257395604f3482ed5db70c64853544c3b0e289fde0`.

The code-cited SCP03 **1.1.2.6 Public Review**, December 2019, target 1.2, is not the inspected
March 2019 release. Exact review clauses remain unavailable. Final SCP11 **1.4**, March 2024, has
confirmed catalogue publication but uninspected final/vendor-profile clauses. C# comments identify the
actual retained editions rather than transferring old section numbers or claiming final conformance.

SCP11 receipt chaining is cited specifically to **§5.3.2**, whose retained text explicitly sets the first
command's chaining value to the returned receipt; §4.4 supplies the broader post-establishment messaging
rule. This is an editor localization within the retained derivative, not new broad source investigation.

ISO/IEC 7816-4:2020 catalogue metadata/abstract does not supply unseen normative APDU case/Le/channel/6C
clauses. Formatter and chaining documentation states current C# output only. NIST samples retain the
completed B6 100-row/twenty-embedded-tuple review and original archive-byte identity gap; no digest/KDF
was recomputed and no formal CAVP qualification is implied. The older semantic-notes pending-vector
sentence is interpreted through B6's completed report/addendum, not revived as unfinished investigation.

Evidence entry points (unchanged):

- [B5 original register](../../investigation/B5/sources/register.md) and captures/reuse identities.
- [B6 final register](../../investigation/B6/sources/final-source-register.md), final manifest, retained
  original PDFs and text derivatives.
- [C triage clarifications](../../investigation/C/triage-clarifications.md) and final provider register.

Fresh independent reviewers still need to compare each affected complete unit with those exact originals,
including normative actor/strength and profile applicability. This editor's citation table is not that review.
