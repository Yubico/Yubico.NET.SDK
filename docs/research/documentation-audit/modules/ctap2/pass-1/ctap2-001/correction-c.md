# C — review correction handoff

**Date/access date:** 2026-11-04. **Editor:** GPT-6.1 Sol. **Status:** RC-R1/RC-R2 and accepted
RC-O1/RC-O2 edits applied; **ready for independent review follow-up, not a follow-up PASS verdict**.
Checkout `/tmp/opencode/ctap2-001-edit-c`; unchanged HEAD/baseline
`5b09a9b6942ded3913b5c9340c3e126617e61a82`. Read the complete [fresh review](review-c.md), affected
source/test explanations and their implementation/fixture evidence. Loaded receive-review; root,
module/test guidance and canonical workflow/style from this editor session remain applicable.

Only **three existing C-owned source/test files** received correction edits, all comments/XML. The
original edit batch still changes 27 of the 30 owned paths against baseline. `edit-c.md` now explicitly
records the review correction and corrected agreement/disagreement and dependency interpretation.
This report is authored directly in the primary packet. No source was copied into integration.

**No runtime tokens, strings, directives, API, assertions, assignments, wire constants, constructors
or encoding writes changed. No builds, tests, hardware/PIN operations, commits or delegation occurred.**

## Original ARKG-10 evidence: exact agreements and differences

Fetched anew from the original IETF URL using read-only HTTP on 2026-11-04 (HTTP200):
[The Asynchronous Remote Key Generation (ARKG) algorithm,
draft-bradleylundberg-cfrg-arkg-10](https://www.ietf.org/archive/id/draft-bradleylundberg-cfrg-arkg-10.html).
Read the full relevant §5.1/§5.2/§8.3 passages and status text, not just a publication preview or the
earlier investigation's extracted subset. This fixed Internet-Draft is **work in progress**, with stated
expiry **31 August 2026**; it is not an RFC/production standard or evidence of registry assignment.

Focused original passages:

- **§5.1:** “An ARKG public seed is represented as a COSE_Key structure … with kty value TBD
  (placeholder value -65537).” Table1 specifies **pkbl/-1/COSE_Key/Required**, **pkkem/-2/COSE_Key/Required**,
  and **dkalg/-3/int / tstr/Optional**.
- **§5.1 public-seed CDDL example:** `1: -65537` is annotated “kty: ARKG-pub (placeholder value)”;
  **`3: -65700`** is annotated “alg: ARKG-P256 (placeholder value)”. `-1: { ... }` is the BL public
  key, `-2: { ... }` the KEM public key, and **`-3: -9`** “Derived key algorithm: ESP256”. Braces here
  denote direct nested maps, not byte strings containing encoded maps. Ellipses omit coordinates
  only; the draft's original full example and its encoded CBOR were inspected.
- **§5.2 Table2:** **ARKG-P256 — TBD (placeholder -65700)**. Table3 separately identifies
  **ESP256-split-ARKG — TBD (placeholder -65539)** and its verification algorithm **-9 (ESP256)**.
- **§8.3:** “Name: ARKG-P256”; “Value: TBD (placeholder -65700)”; “Reference: Section 5.2 of this
  document.” Its proposed-registration wording is not evidence that assignment has occurred.
- **Status:** “It is inappropriate to use Internet-Drafts as reference material or to cite them
  other than as ‘work in progress.’” “This Internet-Draft will expire on 31 August 2026.”

| Aspect | Draft-10 original | Actual SDK/source fixture | Correct conclusion |
|---|---|---|---|
| Outer labels | kty at1, alg at3 | Same labels | Agree; the former test comment “not these outer labels” was false. |
| Seed algorithm placeholder | ARKG-P256=-65700 in §5.1 example, §5.2 Table2 and §8.3 | Profile/fixture uses alg=-65700 | Agree. The earlier claim that draft-10 does not substantiate this placeholder is withdrawn. This is still a placeholder, not registered/production approval. |
| BL/KEM routing | BL at-1, KEM at-2 | Same distinct roles | Agree; preserve the useful mapping rationale. |
| Derived ESP256 example | §5.1 example has -3:-9 | Fixture has -3:-9 | Example agrees; -9 is not made the only permissible draft derived algorithm by this example. |
| Outer kty value | Placeholder -65537 | Encoder emits2 | Differ; outer label1 itself is not different. |
| Nested map carrier | Direct COSE maps | Encoder emits bstr(encoded map) | Differ. Generic SDK parameter reading can also accept direct nested maps, but that does not prove the complete draft example satisfies all SDK modeled-field checks. |
| dkalg presence/type | Optional int/tstr | Decoder requires an integer at-3 | Differ; no acceptance/encoding change made. |
| Signing-operation identifier | ESP256-split-ARKG placeholder -65539 | Separate signing constant already uses-65539 | Distinct from seed -65700; neither changed or made interchangeable. |

The resulting source text names partial agreement explicitly and retains the actual profile differences,
the strong experimental/non-production warning, expiry, generic-carrier/point-validation limitations and
maintainer compatibility-profile decision. It does **not** claim full draft conformance, vendor/device
interoperability, mathematically valid points or cryptographic qualification.

## Applied corrections and complete local units

| Review ID | Exact owned path / corrected unit | Applied change and source verification |
|---|---|---|
| **RC-R1** | `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs`, type remarks on draft comparison | Names draft seed -65700, BL/KEM and derived -9 example agreement separately from kty/wrapping/dkalg differences. Decode still requires int-3; Encode still writes kty2 and byte-string wrappers. |
| **RC-R1** | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cose/CoseArkgP256SeedKeyTests.cs`, `Decode_WithValidArkgSeedKey_ReturnsCorrectVariant` fixture comment | Removes false outer-label disagreement, explicitly names shared seed/routing/example values and differing kty/map carrier/dkalg contract. All fixture bytes/assertions unchanged. |
| **RC-R2** | Same test path, complete `Decode_pkBlAtMinus1_pkKemAtMinus2_PerSpec` summary/body | Summary now describes **BL X=all-AA/Y=all-AB, KEM X=all-BB/Y=all-BC**. Read all four assignments, first-X-byte assertions and four complete X/Y-array assertions; no assignment/assertion change. Useful role-swapping rationale and synthetic/non-device limitations retained. |
| **RC-O1**, accepted | `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs`, `Encode` returns XML | “A new byte array containing the encoded COSE map …”, not “CBOR array”. Actual WriteStartMap(5) unchanged. |
| **RC-O2**, accepted | `src/Ctap2/src/Credentials/GetAssertionResponse.cs`, `User` remarks | Keeps normative identifying name/displayName/icon UV prohibition; separately says modeled name/displayName pass through and icon is skipped. Actual entity Parse switch models id/name/displayName and skips unknown fields; no privacy-filter or passthrough behavior changed. |

No adjacent broader stylistic rewrite was made. The existing snippet classification remains unchanged:
zero XML code/example blocks or fenced source snippets across all30 owned files; existing tests are
complete real-project-context fixtures, not consumer examples. No new compile prerequisites/hosts.

## Corrected source hashes

SHA256 over actual checkout bytes; reviewed hashes are the immutable pre-correction checkpoint in
`review-c.md`. Fresh reviewer should use **corrected** hashes for follow-up, with the same baseline.

| Exact path | Reviewed SHA256 | Corrected SHA256 |
|---|---|---|
| `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs` | `eeb894c603627a7198a2796f035987469a390ff16409f4c6b7bc4e864bb4bf73` | `cda6964cd8372bd805274f57f4d3fde51e00b9dd143becc6d87829888bf820ac` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cose/CoseArkgP256SeedKeyTests.cs` | `03f8ea9dfc5dc935c9dd3568d1d0b9de9f8bebe1f9b486ec89d6acfd22b11aef` | `0af3c9586c1f0462e0e618e9be4b0acf6b5b24fc3c2118e8fe07195b9c5d5865` |
| `src/Ctap2/src/Credentials/GetAssertionResponse.cs` | `e2dac366490a696ffe833038e1280bcb92fe78dcdcb37e08fc2082d3e0188a9d` | `3440938cf36d8db127535daf253e24e4568e2c22480d58205654a7db8369ee99` |

All **24 other changed files** match their reviewed SHA256 exactly. The three intentionally unchanged
owned files remain byte-identical to baseline. Thus only the three correction paths differ from the
reviewed source checkpoint, and the eligible source set remains exactly the 30 C-owned paths.

## Preserved history and proposed leading erratum

`investigation-c.md`, `evidence/c/spec-quotes.md` and `review-c.md` were **not edited**. Current hashes:

| Preserved primary path (relative to this packet) | SHA256 |
|---|---|
| `investigation-c.md` | `98d1606dcdabc029c22bd253d4e5f4397c7c8a1fad8bcf1750c55bfa64dd140e` |
| `evidence/c/spec-quotes.md` | `68a1f12f77da5f171203495adeba3fe09b4d4691aaa432836f3c535607ca41ea` |
| `review-c.md` | `28bb21dfcfe61b936f1213fb5860d39490274000fb0750023ea2677ed1a23db8` |

**Proposed leading erratum for coordinator insertion, without rewriting the original body:**

> 2026-11-04 erratum — C-009's claim that ARKG-10 does not substantiate seed algorithm placeholder
> -65700 is withdrawn. Original §§5.1/5.2/8.3 explicitly specify it; BL=-1/KEM=-2 and the §5.1 derived
> ESP256=-9 example also agree with the SDK fixture. Actual differences remain outer kty2 versus
> draft placeholder-65537, byte-string-wrapped versus direct nested maps, and SDK required integer
> dkalg versus draft optional int/tstr. The original investigation/evidence body remains historical;
> use correction-c.md and the follow-up review for the corrected authority interpretation. No full
> conformance, registry assignment, vendor compatibility or production guidance is established.

This also reconciles the incomplete authority interpretation in the original focused evidence. No
new engineering bug is counted for the unsupported-placeholder subclaim. C-009 / CTAP-DOC-016 /
CTAP-IMP-029 retain provenance but must use the corrected partial agreement/profile differences.
The maintainer target decision and validation/ownership work stay deferred; coordinator owns updates
to shared ledgers and E/G dependencies, not this editor's three-file correction.

## Local checks, not SDK execution

Executed in the editor checkout on 2026-11-04:

- `git diff --check`: exit0, no output.
- Python read inventory/baseline `git show` and the reviewed SHA256 table, then checked all30 owned
  files. Baseline hashes match30/30; source diff remains27 C paths; only the three corrections differ
  from the reviewed checkpoint (24 reviewed hashes unchanged plus three clean baseline files unchanged).
- Source-specific lexical comparison discarded only comments/whitespace and preserved literal text
  and all other characters; asserted no unhandled raw strings. **30/30 match baseline**. Directive
  lines separately compared: **30/30 identical**. All211 contiguous XML blocks parse with ElementTree;
  no source code/example/fence markers. This is the same bounded local mechanism recorded in edit-c.md.

```text
OWNED=30 BASELINE_CHANGED=27 CORRECTION_FILES=3
OTHER_REVIEWED_UNCHANGED=24 CLEAN_UNCHANGED=3
LEXICAL_PRESERVED=30 DIRECTIVES_PRESERVED=30 XML_BLOCKS=211 DOC_SNIPPET_MARKERS=0
```

These checks do not establish Roslyn classification, cref/param binding, compilation/analyzers,
tests, interoperability or hardware qualification. Fresh independent re-review of RC-R1/R2 and
the accepted clarifications remains required; coordinator final gates remain separate.
