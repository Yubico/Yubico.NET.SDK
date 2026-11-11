# Packet C investigation checkpoint

**Outcome:** the shared Python pin and bounded history context are available, and the
shipping Packet C source has received a complete file read. The full packet remains
**incomplete**: nine primary test files, 110 assigned shared units, final unit triage,
several original-source checks and independent review are still outstanding. This is a
durable checkpoint, not a whole-packet completion or clean-coverage claim.

## Authority, identities and writable boundary

- Source baseline: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`, after the incoming Core integration.
- Initial observed metadata HEAD: `d9cf31a17c7580ab580917ce201c4855c3cf89b4`.
  The static checkpoint later observed `01a114d4e251e0f52b92f73393fbe1dca5d5c24e`.
  Every primary Packet C file was freshly compared against the source baseline, with
  BOM/CRLF normalization explicit. All 104 normalized comparisons matched.
  The local ledger also freshly verified all 1,562 assigned unit whole-file and
  character-range SHA-256 identities against the inventory, including unread units;
  identity checks supply no additional semantic coverage.
- Exact scope: [C.json](inventory/packets/C.json), 104 primary files / 17,050 lines /
  1,386 primary units. Current JSON contains **176** shared unit IDs; dispatch prose
  and the release message say 174. The exact JSON assignment was retained. No central
  inventory, denominator or dispatch record was changed by C.
- Writes are confined to `investigation/C/`, this file, and `sources/python/`.
  Product files, API snapshots, central coverage/status/index records and preserved
  historical archives remain outside this worker's write scope.
- No build, test, formatter, cryptographic/runtime experiment, native/device command,
  index/ref write, commit, push or subagent ran. Python scripts performed static
  inventory/hash/annotation work and original-source HTTP retrieval only.

Root/Core/test instructions, the complete Core README/CLAUDE, campaign workflow/style,
SDK house style, plan/inventory/dispatch/source ownership, shared assignment and inherited
routing inputs were read. Applicable PublicApi and Tests.Shared instructions were also
read for dependency orientation. Instruction reads do not close unrelated source units.
No other worker's changes or protected tmpfs files were removed or overwritten.

## Durable records

| Record | Purpose and limit |
| --- | --- |
| [checkpoint-summary.json](investigation/C/checkpoint-summary.json) | Fresh counts, packet/inventory hashes, observed HEAD, explicit unfinished work and zero execution counters |
| [files.json](investigation/C/files.json) | All 104 primary raw SHA-256 values, pinned Git blobs, line counts and complete-file read status |
| [unit-dispositions.json](investigation/C/unit-dispositions.json) | Every primary and shared ID has a source/range hash and a truthful current read/candidate/clean-nominee/historical/unread state |
| [findings.json](investigation/C/findings.json) | Twenty local candidate groups, observed/desired distinction, exact locations, evidence confidence and reopening paths |
| [unchanged-paths.json](investigation/C/unchanged-paths.json) | Every candidate-associated unchanged path is retained for explicit later editor/reviewer disposition, including paths not yet semantically read |
| [Original-source register](investigation/C/sources/review-register.md) | Exact revision/status/actor/strength/application for inspected consequential passages; downloaded-but-unread sources stay distinct |
| [Probe plan](investigation/C/probe-plan.md) | Bounded synthetic inputs and observations proposed for coordinator approval; none executed |
| [Python pin](sources/python/manifest.json) and [context](sources/python/context.md) | Shared immutable implementation/test/history comparator for A/B and later boundaries |

`investigation/C/checkpoint.py` reproduces only C's local ledger. Its explicit read lists
record this session's semantic observations; hashing a file does not create a read claim.
Its complete-file reads map only onto assigned shared IDs. It never writes central
coverage or claims ownership of A/B primary units.

## Reading and sampling state

The session fully read 95 primary files: all 77 shipping files, including three wholly
commented historical files, plus 18 primary test/helper files. The remaining nine files
are explicitly named in the summary. Current per-unit states are:

| Assignment | Read candidate | Read, final triage pending | Historical exclusion | Clean nominee | Unread |
| --- | ---: | ---: | ---: | ---: | ---: |
| Primary, 1,386 units | 242 | 884 | 3 | 13 | 244 |
| Shared, 176 units | 10 | 56 | 0 | 0 | 110 |

The 13 clean nominees come only from the complete Base32 codec and its complete
behavior tests. They preserve method/assertion context, caller source/output ownership,
byte-range overlap admission, failure clearing and terminal-shape rules. They are
source-inspection nominees for later independent review/sampling, rather than passing
test results. Enum/accessor boilerplate was not used to inflate the clean denominator.
No sampling or independent-review gate has been completed.

The historical `RsaFormat.cs`, `CoseKeyOperations.cs` and `DisposableTlvDictionary.cs`
files were read completely for applicability. Their current active API/runtime status
is excluded, with source identities preserved. Historical defects or old documentation
inside them are not counted as current executable bugs or removed as cleanup.

## Findings ready for coordinator triage

All IDs below are **local candidates**, `CORE-C-C001` through `CORE-C-C020`. They allocate
zero accepted Core IDs. Inherited aliases are associations with the original run-qualified
registry, not duplicate independent bug counts or changes to its authoritative state.

| Local IDs | Proposed documentation work | Separate implementation/decision/qualification work |
| --- | --- | --- |
| C001–C004 | Explain interactive versus redirected storage, retained confirmation entry, UTF-16 length units, filtered versus rejected input, current cancellation polling and current examples | Unicode/byte/code-point policy, empty input, hex-path consistency and cancellation responsiveness |
| C005–C007 | Correct TLV copy/view wording; state source/result/list/handle ownership and bounded tag/framing behavior | Abandoned `TryFindValue`/dictionary copies, single-item remainder admission, tag validation and negative BER lengths |
| C008 | Explain application-owned logging factory, configure-before-first-use, static logger retention and temporary-scope limits | Actual cross-Core payload/exception provenance and redaction policy; A/B template review remains required |
| C009–C010 | Keep AlgorithmIdentifier consumption and representation checks narrowly scoped; correct RSA public parameter XML and key-owned mutable-array aliases | Trailing/nested ASN.1 framing, signed integer/profile admission, mathematical validation and public/private alias consistency |
| C011–C013 | Remove no-added-exposure and universal-provider/auto-zeroing assurances; explain actual per-call ownership and current factory reach | Secret scalar/shared-secret/intermediate copies, exception cleanup, BigInteger limitations, direct-runtime substitution and provider-specific cleanup |
| C014 | Describe current regression assertions and explicit guards, with original finding provenance | Pool sentinel identity assumptions need appropriate test/oracle qualification |
| C015–C016 | Use exact registry names/local sentinel meanings and exact standards/profile conditions | Preserve existing applet algorithm profiles; strict RFC 5915 inner-parameters target is a maintainer decision |
| C017–C018 | Repair stale type/method examples and C# span guidance; retain draft-09 status and describe finite ARKG assertions | Compile meaningful snippets later; pin Rust/generator provenance and independently review full ARKG mathematics |
| C019 | Keep ARKG native API explanations local to the actual wrapper calls | Checked allocation/conversion, native secret cleanup and exact package/export/runtime availability need later scoped evidence |
| C020 | Refer contained SCP key/private-key/certificate ownership to B6 and applet editors | Shared record-clone disposal, optional OCE-reference semantics and initializer/profile requirements remain B6/boundary work |

Particularly consequential source observations:

- `ISecureCredentialReader` says the first confirmation entry clears before the second
  prompt. Source retains it for comparison and returns that owner on a match.
- The Core redirected reader obtains a managed string; its class/interface's string-free
  assurance is broader than implementation. The CLI's separate byte/Rune reader should
  not be described as if it were the same mechanism.
- `Tlv.Value` aliases the owned encoding, while `TryFindValue` returns an independent copy
  after allocating another copy that it abandons. Caller cleanup reaches only the result.
- ASN.1 AlgorithmIdentifier field exhaustion is implemented, but exact outer/nested
  consumption differs between factories. Keep that claim separate from complete DER or
  mathematical key validation.
- The immutable catalog contains no key material. The RSA/EC key wrappers are different
  carriers whose exposed parameter structs contain mutable key-owned array references.
- `YubiKitLogging` selects factories/loggers, with no central redaction implementation.
  Application callback exception objects can reach providers. Generic assurances of
  never-logged secrets require actual template and external-payload provenance review.

## Independently inspected mathematical and standards distinctions

The inspected RFC 7748/8032/8410 passages support separate 255-bit field, 253-bit subgroup
order and 32-byte storage/encoding concepts, operation-time X25519 masking and distinct
Edwards/Montgomery public encodings. RFC 8017 supports the selected RSA algebraic
relations and PSS encoded-message capacity bound. These checks are independent of
Python and of the SDK's own catalog tests.

RFC 9053 supports fixed-width concatenated COSE ECDSA signatures, while SPKI is an ASN.1
**key** representation. Its ECDSA key/curve/optional-alg/operation constraints must still
be evaluated by a COSE/profile consumer. Generic structural applicability on a curve
does not validate every catalog-associated COSE combination. RFC 9459's CBC padding
and integrity requirements apply to that COSE construction, not automatically to SCP.

The current retained IANA snapshot confirms the inspected numeric mappings and marks
several polymorphic IDs deprecated. RFC 9864 distinguishes Deprecated's qualified
SHOULD preference in new deployments from Prohibited's MUST NOT. No applet/profile
algorithm IDs were automatically upgraded. Full NIST/SEC/Brainpool/OID table and
provider availability checks remain outstanding; the catalog is not fully qualified.

RFC 5915 §3 explicitly requires inner parameters for conforming implementations even
though its ASN.1 field is OPTIONAL. Core currently omits them because the outer
AlgorithmIdentifier identifies the curve. Preserve observed interoperable behavior,
the exact RFC prose requirement and the unresolved target-profile decision separately.

## Shared Python outcome

The earlier `4ca60f706af930459138d8dc0f0f953480e1c7a4` comparator is still the freshly
observed current remote `main`. Its project is `5.9.3-dev.0`; installed 5.9.2 is a
different artifact. Selected Core source/test bytes and bounded history are retained,
with no large history clone or mutation of the existing clean shallow checkout.

The focused follow-up located the OATH 5.6.3 floor addition at
`1b87964ba258e3ce39fd2959888dfe7cf81a6843`. This establishes source-policy origin,
without a hardware-rationale explanation. The reset/reselect/SCP-restoration origin,
short-APDU NFC rationale, development-version override change and Management page-count
change are separately identified in the shared context. A/B and applet investigators
must inspect their own exact call paths before adopting conclusions.

## Reopening and continuation contract

The machine-readable unchanged-path map is deliberately conservative. Every listed
path remains unchanged and awaits a later editor/reviewer decision. A path mentioned
only as a dependency is not silently counted as reviewed. Reopen task guides, public
XML, internal rationale, complete examples, test comments and migration rows together
when a candidate is accepted or later implementation changes its observed behavior.

Before a completed Packet C return can be accepted:

1. Read the nine remaining primary crypto test files in full, including helper and
   assertion context. Reconcile them with C009–C019 rather than treating test names as proof.
2. Read all 110 outstanding assigned shared units, especially CLI/test caller ranges,
   migration rows and actual callback/outcome use. The full user-interaction guide is
   a reopening dependency here; its primary disposition remains with its assigned owner.
3. Finish the 940 read-but-pending unit dispositions with individual claim evidence;
   source reading alone must not be relabeled clean wholesale.
4. Finish bounded original standards/provider checks and original vector provenance,
   or record precise evidence/policy gaps after the allowed focused follow-up.
5. Reconcile inherited group outcomes through the existing registry owner. Preserve
   aliases and do not duplicate accepted counts.
6. Submit candidate scope and unchanged-path dispositions for coordinator triage, then
   fresh editing and independent review under their later authorizations.

Native tests, compiler/example validation and physical qualification remain separate
later scopes. The user's Windows readiness checkpoint and PIV touch/hotplug/cancellation
deferral remain intact. This packet made no device or other worker checkout changes.
