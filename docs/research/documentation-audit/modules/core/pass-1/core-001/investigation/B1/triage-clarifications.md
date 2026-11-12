# B1 triage provenance clarification

November 12, 2026. Records-only follow-up to [preliminary triage, Owner follow-up](../../triage-preliminary.md#owner-follow-up-and-input-consistency), concerning **CORE-B1-C001**. The product source pin remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; completed B1 records at checkpoint `c7ff868c6f1b89e2d72eaba526bf98d9c28adb05` remain unchanged.

## NIST passage: focused retrieval, original PDF review pending

Read C001's short “original-PDF extraction” label with this explicit provenance:

> The direct NIST PDF request returned **HTTP 403**. On November 11, 2026, the single focused follow-up through **Exa** returned passage text attributed to the original NIST PDF and its final-publication record. B1 retained that retrieval text; it did not download or independently verify the NIST PDF bytes. **Independent review of the original PDF remains required before accepting revised normative wording.**

The document is **NIST SP 800-73-5, Part 2, _Interfaces for Personal Identity Verification — PIV Card Application Card Command Interface_, July 2024 final, §2.4.2, Security Status**. Its original URL and the July 15, 2024 final-publication record are identified in the retained [source register](sources/register.md#nist-sp-800-73-5-part-2-citation). The [retained excerpt](sources/nist-security-status-excerpt.md) explicitly identifies its retrieval provenance; the [fetch manifest](sources/manifest.json) records the failed direct PDF request without a PDF hash.

The clause's actors are the **PIV card application and authenticable entity/security-status indicators**, rather than a host PC/SC handle, process or C# session. The retained text describes successful/failed authentication updating the associated status, distinguishes global status from application status, and identifies the PIV Card Application PIN as application status. Application status becomes FALSE when selection changes to another application; Global PIN status is distinguished separately. These are normative card-application statements in the retrieved passage, subject to the original-PDF review gap above.

This evidence supports qualifying the current “any handle … until the card is reset” rationale: the cited passage includes an application-deselection condition and supplies no PC/SC per-handle/per-process isolation rule or YubiKey-specific PIN=ONCE policy definition. It supplies **no observed power-off, physical PIN-state clearing or cross-process isolation result**.

## PC/SC Part 5: a separately downloaded original

B1 successfully downloaded **_Interoperability Specification for ICCs and Personal Computer Systems_, Part 5, _ICC Resource Manager Definition_, revision 2.01.01, September 2005**. Its retained original PDF is `sources/pcsc5-v2.01.01.pdf`, SHA-256 `524c59e88e50ec0bb2171a7d4ad6369a80d9ab46eb31a77ba603d6d2fe7cb0ff`; its adjacent text is a `pdftotext -layout` derivative. This successful PC/SC original-PDF acquisition is separate from the NIST-PIV PDF gap.

PC/SC Part 5 §3.2.5.2 describes the resource-manager communication/transaction interfaces: Disconnect accepts a **desired** card disposition, and EndTransaction performs its disposition **if possible**, with its own stated exclusive-access condition for power-down/eject. The retained Windows disconnect reference and pcsc-lite API reference describe their respective native API parameters/statuses. These sources identify API intent and conditions; they do not establish that the SDK's requested UNPOWER physically cleared a particular key's PIV state.

## Direction for final editors and reviewers

Use the qualified [NIST register entry](sources/register.md#nist-sp-800-73-5-part-2-citation), retained retrieval excerpt and this clarification together when following C001. Describe built-in release as **requesting `SCARD_UNPOWER_CARD` and checking native return statuses**. Describe the existing no-hardware test oracle as checking the requested disposition and managed policy, rather than measuring card power or PIN-state clearing. Keep borrowed-session lifetime distinct from connection lifetime.

Physical PIN state, observed power-off and cross-process behavior remain unverified for the recorded scope. Preserve `audit-v2-20261022-20261031:#22` and `piv-001:PIVCORE-001`, their platform/device qualifications, and the user's PIV physical-test deferral. No physical closure or new normative source match is added here.

This addendum changes no completed report, candidate, source-cache, ledger, source-unit ID or provisional-map hash. It uses only retained evidence; no new research, download, product edit, build, test, formatter, native/device operation, index/ref write, commit or subagent action accompanies it.
