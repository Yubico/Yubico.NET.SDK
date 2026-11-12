# C revision 2 — C-REVIEW-001 correction

**Documentation correction verified by the editor; focused independent re-review pending.**
Only `src/Core/src/Cryptography/AesUtilities.cs` changed since the independently reviewed batch.
Candidate `CORE-C-C013` / proposal `FPT-095` retains its deferred runtime cleanup issue. No
implementation, assertion, fixture, import, directive, declaration, attribute or runtime string changed.

## Correction and exact identities

| Item | Identity |
| --- | --- |
| Review comment | `C-REVIEW-001`, `../review.md:16–47` |
| Product path | `src/Core/src/Cryptography/AesUtilities.cs` |
| Reviewed SHA-256 | `697fca9dcdcba3f05bf92ea0af68d2c25d527bb4bec9c4e7a0b2a0eeab133b44` |
| Revision-2 SHA-256 | `17f3932d55346e8e4d11c27b9137135e5d9283c93be9926f1af59d3979659425` |
| Changed lines | Reviewed lines 21–22 replaced by current lines 21–25; affected remarks span current lines 21–26 |
| Full transferable patch | [product.patch](product.patch), 125,099 bytes, all 54 edited primary product paths relative to the editor base |
| Full patch SHA-256 | `159a59af1d631f1199e60510f0f85319c3ff58230c168d2a842058096d7b2047` |
| Editor base | `da66f5d01ba594993974ddd26723e8c300c0a454` |
| Product-source pin | `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef` |

[correction.patch](correction.patch) shows only the reviewed-to-revision-2 comment change.
Use the full `product.patch` for transfer onto the named editor base, not onto an already edited tree.
No patch was applied to the primary checkout, and no Git index or reference was changed.

The revised remarks say:

> Inputs remain borrowed and returned arrays are independent. The finally blocks clear local
> key copies and, in the CBC paths, IV copies once their protected scopes are entered. In either
> CBC path, failure while allocating the IV copy can leave the preceding key copy uncleared.
> BlockCipher has one key-copy allocation before try, followed directly by its protected scope.
> Plaintext input copies and MemoryStream storage have separate lifetimes and are not explicitly
> cleared here. Disposing the crypto objects therefore does not establish all-buffer erasure.

## Actual control flow checked

The complete `AesUtilities.cs` implementations and forwarding overloads were read, rather than
inferring the exceptional path from the previous summary. Current coordinates are three lines
later than the reviewed coordinates because this correction adds three comment lines.

| Path | Reviewed coordinates → current coordinates | Source-supported boundary |
| --- | --- | --- |
| `BlockCipher(ReadOnlySpan<byte>, ReadOnlySpan<byte>)` | Key allocation / try at 56–58 → 59–61; finally at 80–83 → 83–86 | One key copy is allocated before try, with no second allocation between it and the protected scope. Later allocations, provider setup, transform/stream creation and writes occur inside the protected operation. The CBC-specific preceding-key/IV-allocation failure statement is not generalized to this path. |
| `AesCbcEncrypt` span implementation | Key copy, IV copy and try at 127–130 → 130–133; finally at 149–153 → 152–156 | Key allocation precedes IV allocation, and both precede try. If the IV copy allocation fails, the key's clearing finally is never entered. Once inside the protected scope, its finally clears both explicit local copies. |
| `AesCbcDecrypt` span implementation | Key copy, IV copy and try at 197–200 → 200–203; finally at 220–224 → 223–227 | Same separate pre-try allocation boundary as encryption. The existing finally covers the protected operation, not failure while creating the preceding copies. |
| Byte-array forwarding overloads | Source bodies unchanged | They delegate to the span implementations and copy the returned result. No new cleanup guarantee is inferred for those additional result copies. |

The plaintext input copies, MemoryStream backing storage and runtime/provider storage retain their
separate lifetime qualifications. This documents the current failure limitation; it neither endorses
it as intended policy nor claims a runtime fix, an allocation-failure experiment or provider erasure.

## Immutable predecessor and successor mapping

The original `edit-C.md`, editor records, independent `review.md` and `review-identities.json` are
preserved byte-exact. They describe the original reviewed snapshot and must not be read as refreshed
current identities for the corrected AES file. The old capture script was not rerun or modified.

| Successor record | Scope and immutable references |
| --- | --- |
| [preserved-prior-records.json](preserved-prior-records.json) | Thirteen original editor/report/review record identities, checked before and after the correction |
| [current-identities.json](current-identities.json) | All 104 current primary path hashes, prior file-row references and unchanged decisions/local-proposal mappings; only AES differs from the reviewed snapshot |
| [mapping.json](mapping.json) | References the immutable 20-row candidate map and 144-row candidate/path map, with one current identity/documentation-qualification override for row 80, C013/FPT-095; the immutable 77-row base/current example map is reused unchanged |
| [verification.json](verification.json) | Static token/non-comment-line/XML checks, current patch identity and full patch reconstruction, not compiler or runtime acceptance |
| `output-identities.json` | Identities of all revision-2 handoff files other than the manifest itself |

The other **103 primary path identities** match the reviewed ledger byte-exact. The batch remains
**54 edited / 50 retained** relative to the base. All **16 active current XML bodies/fragments** and
**22 historical exclusions** retain their exact recorded bodies and boundaries; AES has no XML code
body. No candidate numbering, original inherited identity, disposition or example classification changed.
**C019/FPT-101 remains held; C020/FPT-102 remains referred to its assigned owners.**

## Static verification and focused re-review handoff

- Exact non-comment source lines and non-comment lexical token streams match the original base for
  all 104 primary files and match the reviewed snapshot for the AES correction. XML blocks remain
  well formed. These are the existing bounded static check techniques, not new tooling or semantic
  compiler/API binding results.
- The complete unified product patch was reconstructed read-only against base blobs for all 54
  changed files, including original missing-final-newline markers; every reconstructed byte matches
  the current product file. This did not apply a patch to a checkout or mutate Git state.
- All original editor/review record hashes and the other 103 reviewed product identities remain
  unchanged. `git diff --check` passes. No SDK build, test, formatter, cryptographic experiment,
  native/device execution, Git mutation or subagent ran.

Focused reviewer action: re-read the current AES remarks at 21–26 against the complete three method
flows above, check the exact new AES hash, and decide C-REVIEW-001 for the bounded content/citation
gate. The existing review's acceptance of other content, retain decisions, holds and referrals remains
bound to its immutable identities; this editor does not grant independent re-review acceptance.

Coordinator compiler/API/example/integration gates remain separate. Keep the CBC exceptional key-copy
lifetime in the deferred cleanup handoff, alongside the existing provider/native qualifications.
Windows readiness and PIV physical deferrals stand. Finish Core Pass 1 publication plus the deferred
handoff after the authorized gates, then stop; no Pass-2 planning or implementation is started here.
