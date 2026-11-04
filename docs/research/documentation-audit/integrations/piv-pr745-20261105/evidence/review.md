# Independent pre-PIV integration review

## Outcome

The seven conflict resolutions preserve the relevant intent of both parents. The coordinator's
SecurityDomain `KeyInfo` qualification resolves the new catalog-name collision. The additional OATH
fixture migration directly addresses the clean-build CS8618 without weakening its cleanup assertions.
I found **no additional mandatory product correction** in this bounded integration review.

The coordinator must still include the final working-tree fixes in the merge index and complete the
fresh verification gates. The inspected build result is failed, not a passing merge gate.
This review establishes integration preservation, not PIV Pass 1 completion, PIV implementation
correctness, or a new whole-Core/application audit.

## Reviewed baseline and boundaries

- Primary checkout only: `/home/rupicola/Code/y/Yubico.Yubikit.NET`.
- Campaign parent `HEAD`: `0c301252d7e8abfd1e84cb0cbc0ce17925f0b963`.
- Incoming parent `MERGE_HEAD`: `b1362127203beda5e9c797e8648b28e2716c496c`.
- Merge base: `089b2393ec90fa2c09434e7b897f635373b46c29`.
- Read root and relevant Core, Ctap2, OpenPgp, Piv, SecurityDomain, Oath and Oath-test instructions,
  plus `/tmp/opencode/piv-001-integration/resolution.md`.
- Used source/snapshot comparisons and read complete affected example/helper bodies, including the
  OpenPGP metadata/generation/import/P-256 verification examples, OpenPgpTool generation/display,
  SecurityDomain session setup, PIV migration examples, and all OATH response-fixture consumers.
- Authored only this report. Ran no build, test, hardware operation, formatter, commit or subagent;
  made no product/index changes and used no other agent checkout.

## Seven conflicts and the qualification

| Path | Independent result |
| --- | --- |
| `src/Core/README.md` | Campaign Base32 section remains verbatim; cryptographic-metadata section equals incoming verbatim. Incoming protected `EnsureSupports` and reference-owned `ApduResponse.Clear()` explanations coexist with campaign ownership/discovery guidance. Read the entire catalog example and surrounding applicability/identifier/size qualifications; it preserves the upstream catalog/profile distinction. |
| `src/Core/src/PublicAPI.Unshipped.txt` | Exact incoming declaration set plus exactly the three campaign Base32 declarations below. No incoming declaration is missing; 1,413 nonempty declarations, zero duplicate declarations. The repeated blank line is not a duplicate API. |
| `src/Ctap2/src/Cose/CoseAlgorithm.cs` | Nonblank, noncomment runtime lines equal incoming. ES256/P-256 and ES384/P-384 wording retains the WebAuthn-profile context, while pure EdDSA separates curve metadata. ESP256 stays P-256/-9 and distinct from secp256k1/-47. Campaign experimental ARKG qualifications, open unknown values, aliases and layer distinctions remain. |
| `src/OpenPgp/README.md` | Campaign task structure, complete KDF method/lifetime warnings, signature/decipher cleanup, ATT/reset exceptions and provisioning caveats remain. Incoming representation metadata is integrated as a complete method. Generation and P-256 verification now use `KeyType.P256`/`EcAttributes.KeyType`; imports name the upstream modeled secp256k1/Brainpool identities with provider/card qualification. The changed methods retain their original session and output-cleanup scopes. |
| `src/OpenPgp/src/AlgorithmAttributes.cs` | Nonblank, noncomment runtime lines equal incoming. The factory uses concrete Core key identities and construction-based selection; parsing preserves the received algorithm byte/import format rather than reconstructing through the factory. Campaign XML retains parsing/write and caller-owned card/slot checks; upstream parameter/exception documentation remains. |
| `src/OpenPgp/src/CurveOid.cs` | Correctly deleted in working tree and index. Read the campaign file and replacement `OpenPgpKeyInfo.cs`: modeled curve identities and legacy OpenPGP 25519 OIDs survive through Core plus module-owned mappings. DER value-octets clarification survives in the replacement and README. The surviving `OpenPgpKeyInfo.CurveOid` is a dotted-string property, not the deleted enum. |
| `src/Piv/examples/PivTool/SDK_PAIN_POINTS.md` | Correctly deleted in working tree and index. Incoming commit `9028e653` expressly requests deletion. Read the campaign document, including its complete sequential Management/PIV disposal example; Core's current ownership instructions still require disposing the first borrowed session before opening another. Historical access remains through the campaign parent and untouched records. |
| `src/SecurityDomain/README.md:83` | `IReadOnlyList<Yubico.YubiKit.SecurityDomain.KeyInfo>` selects the actual list element type despite importing both Core.Cryptography and SecurityDomain. Read the complete setup block and shared directives; the qualification changes no session, authentication or ownership behavior. At review time this correction is in the working tree but not yet staged. |

The three retained campaign declarations are:

```text
Yubico.YubiKit.Core.Utilities.Base32
static Yubico.YubiKit.Core.Utilities.Base32.Decode(System.ReadOnlySpan<char> encoded) -> byte[]!
static Yubico.YubiKit.Core.Utilities.Base32.Decode(System.ReadOnlySpan<char> encoded, System.Span<byte> destination) -> int
```

## Actual merged identities and campaign preservation

- **PIV:** `git diff --name-only MERGE_HEAD -- src/Piv` is empty. The entire current PIV tree,
  including README, instructions, examples and tests, equals incoming; there are no campaign shared-usage
  exceptions inside that tree. Its semantics belong to the incoming PR745 baseline and subsequent audit.
- **Core catalog:** `src/Core/src/Cryptography` equals incoming exactly. Concrete Core identities remain
  `P256=1`, `P384=2`, `P521=3`, `X25519=4`, `Ed25519=5`, RSA 1024/2048/3072/4096=6–9,
  Triple DES/AES=10–13, secp256k1=14 and Brainpool=15–17. The merge does not restore old `KeyDefinitions`
  or recast these numbers as protocol identifiers.
- **CTAP2:** resolved runtime constants remain -7/-8/-9/-35/-257, signing-operation placeholder -65539,
  and seed-key placeholder -65700. The campaign alias and unknown-value behavior remain; -9 does not
  acquire secp256k1 meaning. The auto-merged key-family aliases retain EC2=2, OKP=1 and RSA=3 routing.
- **OpenPGP:** inspected the replacement mapping and full attributes implementation. EC wire algorithms
  remain ECDH 0x12, ECDSA 0x13 and EdDSA 0x16; legacy Ed25519/X25519 OIDs remain protocol-owned.
  Auto-merged configuration/crypto/import changes consistently use the new key identities and retain
  campaign qualifications rather than implying the catalog fixes existing protocol limitations.
- **OATH/Base32:** campaign runtime and CLI OATH sources remain unchanged. Core's Base32 source equals
  campaign HEAD. Before the fixture follow-up the whole OATH tree equaled HEAD; afterward its sole
  difference is `OathResponseTests.cs`, reviewed below. The CLI OATH test adaptation also only removes
  obsolete public `EnsureSupports` from its fake plus blank lines; it preserves the campaign test bodies.

## OATH clean-build failure and minimal fixture migration

`/tmp/opencode/piv-001-integration/integration-gates.log:25` reports CS8618 for the uninitialized
`ResponseBackend.LastResponse`; its RESULT is FAILED (exit 1). Campaign HEAD declares `ApduResponse`
as `public readonly record struct`; incoming/current source declares a sealed class owning copied bytes.
A backend constructed before any `SendAsync` therefore genuinely has no response. A nonnullable,
uninitialized property ceased to describe that state after the incoming type migration.

The coordinator's current fix is proportionate:

- `LastResponse` becomes `ApduResponse?`, matching its pre-send state.
- Only `SendAsync` assigns it, from the nonnullable `respond(command)` result, and returns that response.
- All 16 `backend.LastResponse` consumers pass it directly to `AssertCleared`; none dereference it elsewhere.
- `AssertCleared(ApduResponse?)` explicitly asserts presence before the original
  `Assert.All(response.RawData.ToArray(), value => Assert.Equal(0, value))`.
- The separate aggregate-response assertion also remains. The fixture still observes the actual response
  owner rather than a transport copy. A missing send now fails explicitly instead of satisfying a vacuous
  empty/default cleanup assertion.

Read every affected test body and the complete backend/helper. There is no null-forgiving operator,
fabricated default response, warning suppression, assertion removal or OATH production behavior change.
This is integration fixture migration, not a PIV Pass 1 runtime fix or a reopened OATH audit.
Successful compilation/execution of the revised fixture remains the coordinator's fresh gate.

## Auto-merge documentation and frozen history

A bounded active source/Markdown scan covered deleted `CurveOid`, `KeyDefinitions`, former uppercase key
identities and renamed PIV model identifiers. Inspected relevant hits and affected example bodies rather
than treating grep alone as proof. Active OpenPGP references now use Core identities; remaining `CurveOid`
references are the valid new property. Remaining old PIV names are migration labels or explicit V1 bodies;
the corresponding V2 bodies use `Slot`, `KeyType`, `KeyCreationOptions` and `PinOnlyMode`. No consequential
merge-created stale compile reference or contradictory runtime claim was identified in this bounded scope.
Historical plans/research are not current compilable recipes.

Independently byte-compared **all 3,439 HEAD-tracked files** under
`docs/research/documentation-audit/` against current working files: **zero mismatches**. Also confirmed no
tracked diff from HEAD in that directory. Frozen OpenPGP source documentation, patches and original hashes
therefore remain historical and unchanged. New upstream changes to active source are described by this
separate integration report/resolution record; historical hashes must not be rewritten to match them.

## Mandatory remaining integration actions versus optional polish

**Mandatory coordinator actions before committing:**

1. Stage the reviewed SecurityDomain qualification and OATH fixture migration, and the final formatter
   output for the two conflict C# files. At this snapshot all four paths differ between index and working
   tree; the two conflict C# differences are terminal-newline removal only. Zero unmerged entries and a
   passing scoped `git diff --check HEAD` do not imply those final contents are already staged.
2. Complete the planned scoped formatting and fresh clean build/test/docs/architecture/complexity gates.
   The failed build does not establish that the revised fixture compiles or that later targets ran.
3. Keep frozen archives unchanged when recording final integration evidence.

**Optional, nonblocking documentation polish:** `src/OpenPgp/README.md:45–47` says every later method
opens/disposes a session. The newly added synchronous `ShowKeyMetadata` intentionally needs no device
session. Narrowing that sentence to device-operation methods would make the introduction exact; the
metadata example itself is complete and its behavior is clear.

No additional mandatory source correction is requested. Final gates and staging are coordinator-owned;
this report makes no broader Pass 1, hardware-interoperability or whole-Core correctness claim.
