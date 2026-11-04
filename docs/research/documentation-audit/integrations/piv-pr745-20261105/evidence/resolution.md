# Pre-PIV integration conflict resolution

## Outcome and scope

Date: 2026-11-05. All seven assigned conflicts are resolved and staged in the primary checkout,
`/home/rupicola/Code/y/Yubico.Yubikit.NET`, branch `yubikit-docs-audit`.

- Campaign parent (`HEAD`): `0c301252d7e8abfd1e84cb0cbc0ce17925f0b963`.
- Incoming parent (`MERGE_HEAD`): `b1362127203beda5e9c797e8648b28e2716c496c`.
- Merge base: `089b2393ec90fa2c09434e7b897f635373b46c29`.
- The existing `--no-ff --no-commit` merge remains open for coordinator validation and commit.
- This assignment resolves integration conflicts. PIV's whole-module audit and Core's architecture audit remain separate work.

Only the seven assigned product paths were edited/staged. This report is the sole additional authored path.
Frozen `docs/research/documentation-audit/` records and all other product files retain their existing content.
Builds, test runs, hardware operations, formatting, commits, pushes and subagent dispatch were excluded by the task.
The coordinator retains ownership of preservation snapshots, root instructions and integration gates.

## Per-path decisions

| Path | Resolution |
| --- | --- |
| `src/Core/README.md` | Keep the campaign's entire Base32 section verbatim and append upstream's entire cryptographic-metadata section. Keep upstream's automatically merged protected-enforcement and response-ownership explanations, along with existing campaign discovery/transport guidance. |
| `src/Core/src/PublicAPI.Unshipped.txt` | Reconcile as the exact incoming snapshot plus the campaign's three Base32 declarations: its static type and two `Decode` overloads. Retain `ApduResponse.Clear`, its reference-type nullability/signature changes, protected-enforcement changes, removed obsolete crypto APIs and complete new catalog surface. |
| `src/Ctap2/src/Cose/CoseAlgorithm.cs` | Retain runtime identifiers and all campaign experimental-profile qualifications. Merge the four summary conflicts: ES256/ES384 identify WebAuthn credential-profile curve constraints, generic pure EdDSA keeps curve metadata separate while stating the WebAuthn Ed25519 profile, and ESP256 names P-256/-9 alongside ES256K's secp256k1/-47 identity. Executable lines match the incoming parent exactly. |
| `src/OpenPgp/README.md` | Preserve campaign task structure, complete KDF method and transition/lifetime limitations, signing/decipher/certificate/reset guidance and cleanup scopes. Integrate upstream metadata before generation as a composable `ShowKeyMetadata` method, matching the README's method-example convention. Migrate generation and P-256 checking from deleted `CurveOid`/`EcAttributes.Oid` to Core `KeyType`/`EcAttributes.KeyType`. Expand the modeled import list to include upstream secp256k1 and Brainpool with explicit provider/card qualification. Retain ordinary RSA/EC, ATT, KDF and X25519 caveats. Earlier upstream generic constraint bullets are already covered by campaign task sections; retain campaign's precise ATT-survives-reset qualification rather than restoring the superseded erase-everything claim. |
| `src/OpenPgp/src/AlgorithmAttributes.cs` | Keep upstream `Create(KeyRef, KeyType)` implementation, construction-based algorithm selection, parser and encoder unchanged. Preserve campaign base-class parsing/write warnings and factory card/slot checks in XML. Adapt summary and remarks to concrete key identity and retain upstream parameter/exception documentation. Executable lines match the incoming parent exactly. |
| `src/OpenPgp/src/CurveOid.cs` | Honor upstream deletion. Core `KeyType` and `CryptographicDefinitions` plus module-owned `OpenPgpKeyInfo`/internal `OpenPgpCurveOids` replace this API. Campaign's useful DER-value-octets clarification already exists in the replacement source and merged module guidance; preserve it in the resolved README too. Additional comment relocation is unnecessary. |
| `src/Piv/examples/PivTool/SDK_PAIN_POINTS.md` | Honor the explicitly requested upstream deletion after checking history. Keep parent references below for the original historical document and campaign correction; leave archive/frozen records unchanged. Existing Core session-ownership and PIV usage documentation cover the useful disposal guidance. |

The patch tool added terminal newlines to the two retained C# files; executable-line comparison ignores
blank lines and comment lines and confirms their runtime content remains identical to the incoming parent.

## Deletion provenance and preserved historical access

`56d721288273b8ec12d93a808327d998b46bc581` (`feat(core): add cryptographic catalog and OpenPGP key mappings`)
deletes `CurveOid.cs` and introduces `OpenPgpKeyInfo.cs`. The replacement's source lines 48–49 explicitly
state DER OID value octets and protocol-owned legacy 25519 OIDs. `OpenPgpCurveOids` derives ordinary OIDs
from Core and retains OpenPGP's legacy Ed25519/X25519 identities locally.

`9028e653e6d51464210d571e619e5404d8202fc5` (`refactor(piv)!: normalize local API names and remove helper logger plumbing`)
states: “Delete the obsolete SDK pain-points file as requested.” Its predecessor
`e8fe874f88074c663d09ab57d385a3ab3fbefbe2` updates the old document and active PIV migration guidance;
the later deletion is deliberate, rather than a lost rename. Incoming merge `b1362127` preserves that deletion.

Read either deleted document directly from the original campaign parent:

```bash
git show 0c301252d7e8abfd1e84cb0cbc0ce17925f0b963:src/OpenPgp/src/CurveOid.cs
git show 0c301252d7e8abfd1e84cb0cbc0ce17925f0b963:src/Piv/examples/PivTool/SDK_PAIN_POINTS.md
```

The PivTool campaign document's additional correction explains sequential disposal of a borrowed Management
session before opening PIV on the same connection. `src/Core/README.md` already explains caller-owned
connections, and `src/Core/CLAUDE.md` explicitly requires sequential session disposal. The corrected original
remains available from the campaign parent, including its exact example. The report preserves provenance
without publishing obsolete API names as current documentation.

## Exact original conflict blobs

The following Git object IDs preserve the pre-resolution index stages without copying historical material
into active documentation. `—` means the incoming parent deleted the path. Use `git show <blob>` to inspect.

| Path | Base blob | Campaign blob | Incoming blob |
| --- | --- | --- | --- |
| `src/Core/README.md` | `47372206720d63d6c3a19d514081f6ea748d4fb8` | `88fecedcc951121cef8fcb1102e87fad29fb7e77` | `d6f5ed880ccf682c257a0fdad797cb5edc08f944` |
| `src/Core/src/PublicAPI.Unshipped.txt` | `724eea344c04799d2db615f446043c77802f8541` | `0f0aa3ccc149a41543b8dcbee8ce395259ee5ff5` | `55eff451b052725b7c2274a85281a822fa1d1241` |
| `src/Ctap2/src/Cose/CoseAlgorithm.cs` | `e455506c79b687489ea4e06ebccee0cb7da08ef0` | `4fc9e538ccc7e8c7ef0114d609af1603489cdee9` | `4c2fadb1792f561a73b809b68a1aff2299053971` |
| `src/OpenPgp/README.md` | `1f0a9c0adf4ccea9d0220983b11d41d9a6cde265` | `bea04e1dcdde186e26e5829de43017f6ac305c0f` | `a6faa7fc57e8ddd11226046272aa7f58606c2bfd` |
| `src/OpenPgp/src/AlgorithmAttributes.cs` | `b2317abbbb40dc67d924715285b268049ac8d8b5` | `91f431ccab37b805c5a00659aa19d87af995160f` | `4fd92d4e12f3b6307b6a958671cd9bb0c86d77a6` |
| `src/OpenPgp/src/CurveOid.cs` | `7a8d920169ab8a34535135b0b8b76435eb96b24a` | `8fd7caf760349604b151a254c5b87d47838a87c9` | — |
| `src/Piv/examples/PivTool/SDK_PAIN_POINTS.md` | `b2f78c66f377cdb12a02504a3774189a644a9ccf` | `95d8a57caf21924a29420c4a8334c2c9ba82c8aa` | — |

## Active-document integration follow-up for coordinator assignment

**One concrete integration collision remains outside this assignment:**

- `src/SecurityDomain/README.md:83` uses `IReadOnlyList<KeyInfo>` under the shared directives at lines 53
  (`Yubico.YubiKit.Core.Cryptography`) and 57 (`Yubico.YubiKit.SecurityDomain`). The new Core catalog
  introduces `Core.Cryptography.KeyInfo`, while `SecurityDomain.KeyInfo` remains the element type of
  `ListKeyInformationAsync`. A consumer snippet compiled outside the SecurityDomain namespace needs
  `IReadOnlyList<Yubico.YubiKit.SecurityDomain.KeyInfo>` or inferred `var` at this location. This is a
  source-level collision identified from the merged declarations; compilation remains a coordinator gate.
  The file was read and left unchanged pending assignment.

Names reconciled in owned documentation:

- `EcAttributes.Create(KeyRef, CurveOid)` → `EcAttributes.Create(KeyRef, Core.Cryptography.KeyType)`.
- `CurveOid.Secp256R1` → `KeyType.P256`; `EcAttributes.Oid` → `EcAttributes.KeyType`.
- `KeyInfo.GetOpenPgpInfo()` exposes module-owned representation metadata, including `CurveOid` as a
  dotted-string property; this surviving property is distinct from the deleted enum.
- Shared catalog identity is now `CryptographicDefinitions.Keys/Curves/Algorithms`, with `KeyInfo` on
  key objects; `ApduResponse.Clear()` supplies response-owner cleanup and `EnsureSupports` stays protected.

A bounded active-Markdown search found no remaining deleted `CurveOid` enum or legacy Core
`KeyDefinitions`/uppercase `KeyType` usages outside the owned README corrections. `PivSlot`,
`PivAlgorithm` and prefixed policies remain intentionally in V1 example blocks and migration descriptions.
Historical plans and frozen campaign records retain their original terminology. This check is a naming
integration scan, separate from a semantic audit of all auto-merged documentation.

## Verification performed

- `git rev-parse HEAD MERGE_HEAD`: confirms the two expected original parents above.
- Python read-only source/snapshot comparison: Core API equals the incoming snapshot plus exactly the three
  Base32 declarations; 1,413 unique API declaration lines, zero duplicate entries.
- The same comparison confirms executable lines in both owned C# files equal incoming source; all retained
  owned files have zero conflict markers, both Markdown documents have balanced code fences, the campaign
  Base32 section is verbatim, and both intentional deletions are absent.
- `git add --` was called with exactly the seven owned paths.
- Fresh `git ls-files -u`: empty, so the index has zero remaining unmerged paths.
- Fresh scoped `git diff --cached --check -- <seven owned paths>`: exit 0, empty output.
- Fresh scoped `git diff --name-only -- <seven owned paths>`: empty; working files match staged resolutions.

Staged retained-file blob IDs:

| Path | Resolved blob |
| --- | --- |
| `src/Core/README.md` | `198cd11da928cc0a76d7aa4cb98bdeaf0b5aa97b` |
| `src/Core/src/PublicAPI.Unshipped.txt` | `e6a89c6a87a87e5346f5a6039ab4b33c2c0394d3` |
| `src/Ctap2/src/Cose/CoseAlgorithm.cs` | `a81c884a05606354ebc5f70af2245667de859de5` |
| `src/OpenPgp/README.md` | `184db1741be5428869ead8051a801f7fe3b4f62e` |
| `src/OpenPgp/src/AlgorithmAttributes.cs` | `1b21bf96367be2ab415c342b1d8c58d8897aa42c` |

Build/test/analyzer, executable README compilation, full frozen-record preservation verification and final
merge validation remain coordinator-owned. Incoming commit messages describe earlier verification; those
results belong to their original revisions and provide no fresh verification of this combined merge.
