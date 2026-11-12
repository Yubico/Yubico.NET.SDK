# Packet C independent content and citation review

**Verdict: mandatory correction C-REVIEW-001 before acceptance.** The remainder of the
reviewed documentation and the explicit retain/hold/referral dispositions are acceptable
for this bounded content/citation gate. This is not compiler, runtime, native, hardware,
boundary-wide, integration, or publication acceptance.

Reviewed in `/home/rupicola/Code/y/yubikit-core-docs-edit-crypto`, against base
`da66f5d01ba594993974ddd26723e8c300c0a454` and product-source pin
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Exact reviewed identities and the
acceptance exception are bound by [review-identities.json](review-identities.json).
Review labels here are local review comments, not allocated Core finding IDs.

## Mandatory correction

### C-REVIEW-001 — qualify the AES key-copy clearing claim

- **Location:** `src/Core/src/Cryptography/AesUtilities.cs:21–23`.
- **Candidate/proposal:** `CORE-C-C013` / `FPT-095`; original candidate/path decision
  row 80 (zero-based) in `path-decisions.json`.
- **Reviewed file SHA-256:**
  `697fca9dcdcba3f05bf92ea0af68d2c25d527bb4bec9c4e7a0b2a0eeab133b44`.
- **Problem:** “Local key/IV copies are cleared” is unqualified. In both CBC
  overload implementations, the key copy is allocated before the IV copy and before
  entry to `try`: encryption lines 127–130 and decryption lines 197–200. If the IV
  allocation fails after the key allocation succeeds, execution never enters the
  `try`/`finally` that clears the key. The later no-all-buffer-erasure sentence
  attributes the limitation to other storage, rather than qualifying this key-copy
  claim. This is a source-visible exceptional lifetime, not a provider hypothesis.
- **Source anchors:** the above implementation and its `finally` blocks at
  149–153 / 220–224; the original C013 deferred exceptional-allocation scope in
  `investigation/C/findings.json`; the separate pre-`try` clone concern in
  `investigation/C/final-review.json` → `allocation_failure_qualification`;
  `triage/full-family/rows.md` → FPT-095; `editing/C/shared-followups.md` → C013
  exceptional-allocation deferral. No original-source or provider experiment is
  needed to establish where these two allocations sit relative to `try`.
- **Required documentation-only correction:** say that the existing `finally`
  blocks clear key/IV copies **after entry to their protected scopes**, and identify
  that failure while allocating the CBC IV copy can leave the preceding key copy
  uncleared. Retain the independent plaintext/stream/provider-copy limitations.
  For example: “The finally blocks clear local key/IV copies once their protected
  scopes are entered. In the CBC paths, failure while allocating the IV copy can
  leave the preceding key copy uncleared.” Equivalent precise wording is acceptable.
- **Do not repair runtime code or inject a failure test in this documentation batch.**
  Retain the implementation concern as deferred, not as intended behavior. The editor
  must refresh its affected hashes/receipts after the wording change; this reviewer
  has not modified those records or the product file.

## Scope and preservation checks

Read root, Core, and nested Core-test instructions, the approved campaign editorial
reference and SDK house style, release/full-family triage and C rows, completed C
investigation/final source register/clarifications, `edit-C.md`, and the requested C
editing records. Inspected the actual complete diff, affected declarations and their
XML/internal implementation context, complete changed test methods/helpers and relevant
assertions, all active example bodies and their nearby ownership explanations, and
the retained candidate-associated primary surfaces. The records do not substitute
for the source and retained original passages.

Fresh read-only identity checks found:

- 104 primary paths: **54 edited / 50 retained**. All raw base/current/product-pin
  hashes match `files.json`.
- All **144 original candidate/path associations** match the original investigation
  unchanged-path set exactly; all current path hashes match `path-decisions.json`.
  The three additional primary associations remain explicit in `decisions.json`.
- Every non-comment source line is unchanged across all 104 paths, comparing exact
  line text after BOM/newline convention normalization. The actual patch changes
  standalone `//` / `///` lines and final newlines, not executable lines. Imports,
  directives, signatures, defaults, attributes, assertions, literals and executable
  strings remain unchanged. No API snapshot is in this patch.
- The editor output-identity manifest still matches all its referenced records.
  Its preliminary lexer/XML receipts remain preliminary: no bound compiler or
  semantic API check was run by this reviewer.
- **16 active XML bodies/fragments and 22 historical RsaFormat exclusions** remain
  distinctly classified. Historical body identities are not compiled-example claims.

These are static preservation observations, not a claim that the entire 104-file
population received a second clean-unit investigation. The independent semantic
review concerns the affected content and retained/held candidate decisions.

## Content conclusions

| Candidates | Independent conclusion |
| --- | --- |
| C001–C004 | The reader implementation supports interactive pooled UTF-16 characters versus redirected managed strings, retained first confirmation entry, second-owner disposal and successful first-owner transfer. Length/code-point/UTF-8-byte/hex-decoded-byte units are separate. Ignored keystrokes, rejected redirected text, redirected hex bypass, replacement fallback, empty positive-size allocation failure, and waiting-key-only cancellation are accurately qualified. The retained no-queued-key cancellation test establishes only its waiting branch. No applet Unicode/PIN policy is silently selected. |
| C005–C007 | `Tlv.Value`/encoding views alias the cleared owned encoding; creation copies input; list construction copies the collection, not elements; returned encodings and sources retain separate owners. Pooled views cannot outlive return. The clear-only handle does not return rentals. Search/dictionary abandoned-copy limits and the existing disposable-list alternative are accurate. Bounded two-octet tags, unchecked single-item tails, definite-only lengths, negative BerLength admission and regression assertions are not promoted to a general BER codec or desired future contract. |
| C008 | Factory ownership, static logger retention, process-global temporary scopes and application callback exception provenance match implementation. No central redactor, general payload logging, or never-logged intent guarantee is implied. B6's ordinary header/length versus SELECT identifier distinction remains an owner handoff. |
| C009–C010 | Attribute-free/version-0 typed subsets, AlgorithmIdentifier exhaustion versus outer/nested exhaustion, ASN.1 exception distinctions, EC shape versus range/on-curve checking, selected RSA relations versus complete validation and DP-width admission are accurately separated. RSA public XML now describes public components. Parameter structs expose array aliases; the EC cached point is separate. Catalog identity is not provider/profile/device capability. |
| C011–C013 | BigInteger/managed/native copies have independent lifetimes; supplied-Q versus absent-Q and writer Encode versus Reset qualifications match pinned source. ECDH results, alternate CMAC output staging, KDF intermediate output and writer storage are not erased by unrelated disposal. **AES clearing wording needs C-REVIEW-001.** No cleanup repair or unrun failure-injection result is accepted. |
| C014–C016 | Version-guard, scalar-before-point, captured-array and pool-sentinel descriptions track the actual assertions and admission order. Registry zero sentinels and Key Types naming are correct. Curve structure, COSE alg mappings and complete profile pairings remain separate. RFC 5915's prose requirement for inner parameters is not confused with its OPTIONAL schema field or selected runtime importer behavior. |
| C017–C018 | Real key/PIV APIs replace stale helpers; concrete secret ownership and awaited lifetime are visible. Ordinary short-Weierstrass terminology and private-owner rationale are accurate. Draft-09 is explicitly expired work in progress. Context domain separation is not injectivity; A checks point plus handle, B/C selected points. Missing immutable Rust/generator provenance remains explicit. |

The seven explicitly retained candidate facets remain distinct from edits made in
the same file for other candidates. Retained internal creator tests, ICmacPrimitives,
operation catalog, KeyType and public checked-cast helper do not need an unrelated
rewrite. Base32 source/tests support the retained empty/case/space/padding/pad-bit,
overlap-admission, failure-clear and successful-tail statements; the separate shared
Base32 migration omission is routed, not silently closed. No optional style churn
is requested. Apart from the clearing qualification above, the prose is calm,
task/actor-oriented and keeps useful rationale and conditions nearby.

## Consequential original-source checks

Sources below are retained originals; PDF text coordinates refer to the existing
reading derivatives of the separately hash-bound original PDFs. Source identities
are in `review-identities.json`; no broad reinvestigation or new web retrieval ran.

| Exact original/status/section | Accepted applicability and normative strength |
| --- | --- |
| ITU-T X.690 (02/2021), ISO/IEC 8825-1:2021, §§8.1.2.2/8.1.2.4 and 8.1.3.2–8.1.3.6 | Identifier/length encoder and sender rules use “shall”; §8.1.3.3 allows short or long definite lengths as a sender option, §8.1.3.5(c) forbids FF, and Note 2 expressly allows extra long-form octets. This supports redundant long-form BER acceptance without general DER minimality or arbitrary tag support. C007's old pending confidence is superseded, not a current citation blocker. |
| RFC 5915, June 2010, IETF Informational, §3 and ASN.1 schema | Conforming implementations MUST include inner parameters despite OPTIONAL schema syntax. The publicKey SHOULD and external-distribution omission condition are different. The order-based octet width is representation, not proof of scalar range. The encoder's explicit [1] wrapper and omitted [0] parameters are reported separately from universal importer conformance. |
| RFC 5958, August 2010, Standards Track, §2 | Version v1(0)/v2(1), attributes and publicKey/version relationship concern package syntax. A recognized version or early rejection does not validate later fields or an all-zero mathematical key. |
| .NET runtime tag v10.0.0, EccSecurityTransforms.cs:184–232,242–269; EccSecurityTransforms.macOS.cs:52–79; ECDsaSecurityTransforms.cs:74–102 | Actual reference implementation, not a normative guarantee for every deployment. Missing Q triggers extraction and replacement with decoded parameters, including D; both supplied coordinates skip that checked extraction. Import staging is returned in finally and signing uses the imported SecKey. The revised ECPrivateKey wording accurately retains installed-runtime/provider/native-erasure limits. |
| .NET runtime tag v10.0.0, AsnWriter.cs:79–99,185–209 | Encode returns an independent copy without clearing writer storage; Reset clears the written prefix without releasing resources. SDK private encoder paths omit Reset. Existing INTEGER/nested-array observers do not observe writer storage. C013's earlier pending AsnWriter qualification is settled; actual provider and exceptional-copy behavior are separate. |
| .NET runtime tag v10.0.0, UTF8Encoding.cs:73–108 | Default replacement fallback is source-backed; selected strict encodings can differ. This does not equate UTF-16 code units with prompt code points or encoded byte limits. |
| Retained IANA COSE XML, Algorithms/Key Types/Elliptic Curves registries; RFC 9053 §2.1; RFC 9459 §5 | Registry zero reservation and identifiers do not grant capability or complete pairing validity. RFC 9459 requires the encrypting implementation's prescribed padding and companion authentication/integrity for that COSE construction, not automatically for raw SDK CBC or SCP. No applet algorithm upgrade is accepted. |
| SEC 2 retrieved January 27, 2010 v2.0 copy, §2.4.1, with Draft page headers; RFC 5639, March 2010 Independent Submission Informational, §§3.4,3.6,3.7 | Published generator points and widths support the selected secp256k1/Brainpool test inputs, not a newly executed OpenSSL oracle, host support, certification or inaccessible NIST-final table claim. Runtime-backed assertions occur after RequireHostCurve. |
| draft-bradleylundberg-cfrg-arkg-09, July 7, 2025, intended Informational, expired January 8, 2026; §§2.3,3.2,3.3,4.1 | Work in progress is the deliberate derivation target. Domain framing and tag/ephemeral-point handle interpretation do not establish full implementation conformance or immutable KAT provenance. |
| OpenSSL tag openssl-3.0.0 EC_POINT_new manual, DESCRIPTION | Ordinary free versus explicit clear-free is an API distinction. The SDK calls ordinary point free; the manual does not attest what the deployed NativeShims binary/provider actually does or authorize a C019 implementation fix. |

## Holds, examples and remaining gates

**C019 / FPT-101 hold is correct.** The exact triage row has P/E/Q/B, no D.
The three HOLD-FACET rows preserve that fact while allowing the same paths' separate
C011/C012/C018 documentation edits. No allocation/conversion outcome, native erasure,
deployed producer/export or provider acceptance is silently accepted.

**C020 / FPT-102 referral is correct.** Its paths are B6/shared/applet surfaces,
not C primary files. Actual Scp03/StaticKeys and Scp11 containment supports the
clone-versus-retained-owner handoff: source arrays are cloned by StaticKeys;
parameters retain the supplied owner; SCP11 copies the collection but retains
contained wrappers/certificates. Record with-clones share those owners. Optional
OCE-reference/profile/initializer and actual caller/reset lifetimes remain with
their named owners. This is referral acceptance, not independent B6/applet acceptance.

The DialogPrompt class uses a real asynchronous application-supplied delegate,
checks cancellation separately from decline, clears transferred byte input in
finally, and transfers an exactly sized owner only on success. Its nonempty policy
is local to the example. ReadAndVerifyPinAsync borrows an open IPivSession and keeps
the credential using alive through the awaited real VerifyPinAsync call.
InspectRsaKey uses a concrete disposable RSAPrivateKey; its interface/cast variables
are aliases, not new owners. The twelve provider snippets are resource/startup
fragments, not complete cryptographic applications. The unchanged intent fragment
borrows a WebAuthn client/options/token and contains its full owned scope. The native
C sketch and 22 historical RsaFormat bodies remain correctly outside active C#
example compilation. Source signature checks are not compiled binding results.

Coordinator gates remain: existing bound lexical/API checker, documentation/example
binding and compilation on the final integrated bytes, and any authorized runtime
qualification. No build, test, formatter, cryptographic experiment, native/device
execution, index/ref/commit/push or subagent ran in this review. Only this review and
its identity record were created. The four original user files are intentionally
absent here; their separately checked primary-location hashes are neither rerun nor
misrepresented as missing-data damage.

Keep deferred Unicode/empty-input, parser admission, validation depth, partial-copy
cleanup, native/provider/deployment, immutable Rust provenance, NIST-final and actual
applet/profile questions in the deferred handoff. Windows readiness and PIV physical
touch/hotplug/touch-dependent cancellation remain separate. After the one bounded
correction and coordinator gates, finish Core Pass 1 publication plus the deferred
handoff and stop; this review authorizes no Pass-2 work.
