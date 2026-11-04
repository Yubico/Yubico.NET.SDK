# Astra clean-unit sampling

## Method and scope

Source/assertion sampling on 2026-11-03/04 against the unchanged primary baseline `5b09a9b6`; editors
work in isolated checkouts. These are independent reads of investigator-clean units and supporting
implementation/assertions, not executed tests or device/standards qualification. Mixed neighboring
findings remain in their ledgers; no whole-file clean stamp is inferred.

## Completed samples

| Sample | Investigator unit | Actual independent check and bounded result |
| --- | --- | --- |
| S-01 | A-C02, HidBackend status-stripped result | Read complete send/resolve/catch/finally unit: full request forwarded, outer CTAP status checked, successful view slices off status only after terminal resolution. Clean; not a response-cleanup or typed-decode guarantee. |
| S-02 | A-C06, no-parameter builder/prefix | Read Build/BuildAsMemory complete: empty parameters produce command only; nonempty encoding gets command prefix and its intermediate Encode copy cleared. Clean. Adjacent ordering explanation is independently corrected by framework evidence. |
| S-03 | A-C29, GetInfo transport preservation test | Read full fixture/assertion: exact unknown/case/order/duplicate list is compared. Clean as SDK preservation evidence; duplicates do not make a conforming authenticator advertisement, and no transport was executed. |
| S-04 | A-C32, backend untransferred response clearing | Read complete HID/SmartCard callback-failure tests: same callback exception asserted and originally nonzero response views inspected after failure. Clean within fake terminal-resolution failure scope, not whole transport/decoder erasure. |
| S-05 | B-C02, ComputePinHash | Read private method and surrounding padding/validation: SHA256 of exact input, first16 copied out, stack hash cleared in finally. Clean; no Unicode validation or returned-array cleanup inferred. |
| S-06 | B-C13, V1 Verify | Read exact-length early return, Authenticate call, fixed-time comparison and expected-tag finally clearing. Clean; adjacent full-MAC stack cleanup gap remains B-08, and Verify is not device authorization. |
| S-07 | B-C23, V1 SHA256 test | Read Kdf source and independent expected SHA256 assertion. Clean algorithm comparison; no ECDH/provider/platform/token-state qualification. |
| S-08 | C-CL-05, authenticator fixed fields | Read Parse fixed prefix and full big-endian fixture/assertion: 32-byte RP hash, flag byte32, counter bytes33–36 and expected0x01020304. Clean; flags/hash parse do not verify a ceremony. |
| S-09 | C-CL-06, response nullable-memory conversion | Read both complete constructor call sites: explicit null branch preserves absent largeBlobKey; non-null input becomes present memory. Clean conversion rationale, consistent with prior retained language evidence; no key-length or secret-owner promise. |
| S-10 | C-CL-08, false-UP wire test | Read entire request/CBOR loop/assertion: outer key5 contains up=false, final found flag asserted. Clean mechanical encoding proof; no hardware presence claim. |
| S-11 | D-C07, config authentication prefix | Read builder's34-byte auth message and parameter map: ff×32, command0D, subcommand. Clean prefix only; the nearby wrong section citation is D-06 and not included as clean. |
| S-12 | D-C13, credential enumeration owner | Read complete result class: read-only list wrapper retains enumeration order and supplied total; Dispose visits each entry once under its flag. Clean; list is not a deep immutable snapshot or device transaction. |
| S-13 | D-C19, RP enumeration result | Read complete class: same list ordering/read-only wrapper and unrecomputed nullable total, no secret-owner Dispose contract. Clean within modeled metadata result. |
| S-14 | D-C20, stored-credential response owner | Read complete clone/view/parse/failure/dispose paths plus retained-view test: one owned envelope, key view into it, zeroing including unknowns, guarded access, borrowed source unchanged and independent public fields survive. Clean; caller-created copies need their own cleanup and transport is outside this guarantee. |
| S-15 | E-CLEAN-03, credBlob store/read distinction | Read both fluent methods and Encode conflict/branches: byte string for creation, true for retrieval, mixing rejected. Clean operation distinction; DTO/capacity/enforcement limits remain E-02/E-01. |
| S-16 | E-CLEAN-10, blob integrity fallback | Read read-array branch and HasValidHash definition: short/invalid-hash data maps to empty initial array; valid-hash data goes to Deserialize, whose errors are not normalized. Clean; malformed response/config handling remains separate. |
| S-17 | E-CLEAN-11, serialized-capacity gate | Read write path and test: compares serialized length with reported max before blob write and mock asserts no SendCborRequest call. Clean in that scope; GetInfo may precede the check, and executable test name is broader than a no-I/O guarantee. |
| S-18 | F-C01, package narrative prerequisites | Read full package page and project reference: alpha/naming/install/transitive Core statements align with current source/repository status. Clean; remote feed installation was not executed, and broad feature-scope text remains F-12. |
| S-19 (supplemental) | F-C10, sample result transfer/disposal | Read full wrapper class and async credential-enumeration method: ownership transfers only after scoped session/helper work; untransferred result and token cleaned in finally; wrapper disposes nested result idempotently. This behavior check is useful, but F-C10 was conservatively excluded from the clean-prose population; it is not counted toward the sample minimum. |
| S-20 | G-CLEAN-01, RP handoff narrative | Read complete registration/authentication handoff and actual AuthenticationResponse/constructor mapping: exact client-data bytes/raw response/signature/handle sent to RP; no invented upload/verifier; RP/account verification remains explicit. Clean boundary narrative, not RP integration execution or whole WebAuthn re-audit. |

All 20 checks are accepted within their stated bounds; no material clean-unit miss found. Independent
coverage reconciliation defines 132 eligible clean units from 162 nominations. Nineteen of these checks
are eligible samples (**19/132, 14.4%**, exceeding the required14); S-19 is supplemental and not counted.
Samples span API XML, narrative/examples, implementation comments and assertion explanations across A–G.
The conservative exclusions remain intact rather than changing the denominator to count extra work.

## Separate consequential adjudications (not counted as clean samples)

- Pure framework experiment rejected D-08's insertion-order-to-wire-order inference; canonical writer
  auto-sorts. Commentary correction remains; no encoding fix selected.
- Original PIN subcommand clauses distinguish legacy0x05 forcePINChange PIN_INVALID from permission0x09
  PIN_POLICY_VIOLATION. D-14's broad wrong-status inference is being withdrawn; other cleanup/evidence gaps remain.
- Guid source/probe resolves default mixed-endian serialization and code-inferred big-endian portability
  limitation, with no big-endian execution. ReadEndMap trailing-byte mechanism verified only in pure probe.

Original reports, rejected inferences, exact evidence and later corrections remain preserved, not silently
rewritten into a false clean investigation history.
