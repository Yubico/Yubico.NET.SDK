# Packet C continuation entry point

**Superseded checkpoint instructions:** the approved continuation is complete. Current
results are in `../../investigation-C.md`, `final-summary.json` and
`sources/final-source-register.md`. The original pending list below is retained as
checkpoint provenance, not current work status.

This checkpoint is intentionally incomplete. Read `../../investigation-C.md`, then
`checkpoint-summary.json`, `findings.json` and `sources/review-register.md`. Use the
same source baseline and preserve all current records. Do not execute builds/tests,
cryptographic experiments, native/device calls or subagents without the coordinator's
separate authorization. Product editing remains a later fresh-editor task.

## First outstanding primary reads

All paths below are under `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/`:

1. `AlgorithmIdentifierParameterTests.cs`
2. `AsnPrivateKeyDecoderTests.cs`
3. `AsnPrivateKeyEncoderTests.cs`
4. `AsnPublicKeyDecoderTests.cs`
5. `AsnPublicKeyEncoderTests.cs`
6. `AsnUtilitiesTests.cs`
7. `CryptographyProviderExtensionTests.cs`
8. `PrivateKeyConsistencyTests.cs`
9. `PrivateKeyDecodingZeroingTests.cs`

Read all assertions and helpers before updating the explicit `READ_TEST_NAMES` list
in `checkpoint.py`. Several candidate questions may already have regression fixtures;
do not infer their coverage from a name or add duplicate accepted findings.

## Outstanding shared reads

Select the 110 `assignment == "shared-read" && status == "UNREAD"` rows from
`unit-dispositions.json`. Their exact original IDs, path/range hashes and labels are
retained. The current packet has 176 IDs, versus dispatch prose's 174; leave central
reconciliation to the coordinator. Read complete caller/helper context around selected
ranges while retaining the other owner's primary disposition.

The outstanding surfaces include migration notes/maps, the ARKG architecture ranges,
CLI command callers, device-selector/test infrastructure/recorder ranges, CLI credential
and presence tests, and PublicApi convention tests. Full CLI SecureCredential, PinPrompt
and ConsoleUserPresencePrompt source was read; their assigned tests remain outstanding.
Core README/CLAUDE, logging/crypto/memory guidance and TLV guide were read in full, but
only their assigned IDs have C's read state.

## Important pending qualifications

- Finish individual read/clean/candidate outcomes for the 940 already read units whose
  final triage remains pending. They are not clean by default.
- Original RFC 5915 §3 inner-parameters MUST versus current encoder omission is a
  target-profile decision; keep interoperable behavior distinct from conformance.
- IANA/RFC 9864 deprecation is not prohibition and grants no automatic applet algorithm
  migration. RFC 9053's ECDSA curve/profile constraints remain separate from generic
  structural catalog applicability.
- Exact NIST/SEC/Brainpool/OID table checks, original RSA/EC vector extraction provenance,
  full ARKG draft-09 derivation, Rust generator pin, .NET platform-provider sources and
  actual NativeShims package behavior remain unfinished. Download availability is not
  passage verification or execution.
- The shared Python history research has completed its initial pass plus focused
  follow-up for the OATH floor. Its introduction was located at `1b87964ba258...`;
  hardware rationale remains a bounded vendor/maintainer gap. Avoid repeating that
  research or deepening the shared clone.
- C020 is a narrow SCP containment/ownership referral. B6 owns its primary units and
  actual wire/profile behavior; no complete B6 review was undertaken.

## Last static verification

`python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/investigation/C/checkpoint.py`
completed successfully: 104 normalized primary baseline comparisons and 1,562
whole-file/range identity checks. Separate Python checks parsed owned JSON and verified
all candidate line ranges and all unchanged-path existence checks. No SDK code ran.
The observed metadata HEAD was `01a114d4e251e0f52b92f73393fbe1dca5d5c24e`.

The original protected archive populations and four user files were outside this
worker's write set. No blanket rehash or archive-count claim is made by this checkpoint.
