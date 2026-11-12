# Packet C revision 2 focused independent re-review

**PASS — C-REVIEW-001 is resolved.** The full successor batch is accepted for the
bounded independent content/citation gate at the exact identities below. This
combines the predecessor review's unchanged acceptance with focused acceptance of
the AES correction; it is not a new broad investigation or compiler/runtime gate.

| Accepted artifact | SHA-256 |
| --- | --- |
| `src/Core/src/Cryptography/AesUtilities.cs` | `17f3932d55346e8e4d11c27b9137135e5d9283c93be9926f1af59d3979659425` |
| Full 54-file `product.patch`, 125,099 bytes | `159a59af1d631f1199e60510f0f85319c3ff58230c168d2a842058096d7b2047` |
| All 104 current primary identities, `current-identities.json` | `2906c9a9754a167e8019712c3c9ecae052ff41fd45c9c44eeee30f6f7c762402` |

Base: `da66f5d01ba594993974ddd26723e8c300c0a454`. Product-source pin:
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Worktree:
`/home/rupicola/Code/y/yubikit-core-docs-edit-crypto`.

## Focused content decision

Read `report.md`, the successor manifests/mapping/preservation records and correction
patch, and the complete current AES implementations and forwarding overloads.
Candidate `CORE-C-C013` / proposal `FPT-095`, original candidate/path row 80, remains
the correction's identity. The revised remarks at **AesUtilities.cs:21–26** match
the actual control flow:

- **Protected cleanup:** the claim now applies once the protected scope is entered.
  The existing finally blocks clear the explicit local key copy and, for CBC, IV
  copy. It no longer promises cleanup before entry.
- **CBC encryption:** key/IV allocations at 130–131 precede try at 133; clearing
  occurs at 152–156. **CBC decryption:** allocations at 200–201 precede try at 203;
  clearing occurs at 223–227. Failure of either CBC IV-copy allocation can abandon
  the preceding key copy without reaching its clearing finally. The revised text
  explicitly names this limitation.
- **BlockCipher:** its one pre-try key-copy allocation at 59 is followed directly
  by try at 61 and key clearing at 83–86. Its later zero-IV allocation at 70 is
  inside that scope. The remarks correctly distinguish this from CBC's second
  pre-try allocation; they do not imply that BlockCipher has no later allocations.
- **Independent storage:** plaintext input copies, MemoryStream backing storage,
  forwarded result copies and provider storage have separate lifetimes. Lines
  25–26 retain the plaintext/stream limitation and reject an all-buffer-erasure
  inference from crypto-object disposal. No new caller-input ownership is implied.

This is accurate current-behavior documentation, not endorsement of the CBC failure
limitation as intended policy, an allocation-failure experiment, a runtime cleanup
fix, or provider/native erasure evidence. No remaining correction is requested.

## Preservation and successor acceptance

Fresh read-only checks verified every one of the 104 current product hashes against
`current-identities.json` and compared decisions/candidate/proposal mappings with
the immutable predecessor ledger. **Only AES changed; the other 103 product
identities are byte-exact.** The batch remains **54 edited / 50 retained**.

Reversing only the reported XML comment replacement in memory reconstructs the exact
reviewed AES SHA-256
`697fca9dcdcba3f05bf92ea0af68d2c25d527bb4bec9c4e7a0b2a0eeab133b44`.
Thus the successor correction introduces no executable, import, directive, signature,
default, attribute, assertion or runtime-string change. The full transferable patch
also equals a fresh read-only full-index/binary diff of the 54 paths against the named
base, byte for byte.

All **13 predecessor editor/report/review records** and all seven editor revision-2
handoff artifacts match their preservation/output manifests. The old review remains
immutable historical evidence; this successor review closes its sole mandatory
correction. Its other content/citation acceptance carries forward only because those
identities are unchanged. The example map and classifications remain byte-exact;
AES contains no XML code body. No example was rebound or compiled here.

**C019/FPT-101 remains HOLD-FACET, with no released D facet. C020/FPT-102 remains
RETAIN-REFER-OWNER to B6/shared/applet owners.** The unchanged original candidate/path
map and successor mapping preserve those decisions. No native/provider/profile or
boundary acceptance is added.

## Remaining gates and stopping point

Coordinator token/API/cref/example binding and compilation/integration gates remain
pending on the final integrated bytes. Runtime, native producer/provider, physical
device and applet/profile qualifications retain the predecessor review's limits.
The CBC exceptional-copy concern stays in the deferred cleanup handoff; Windows
readiness and PIV physical deferrals remain separate.

Only this `review.md` and `review-identities.json` were created. No product/editor/old
review evidence was modified; no new research or tooling, build, tests, formatter,
native/device/crypto execution, Git mutation or subagent ran. Finish Core Pass 1
publication and the deferred handoff after the authorized coordinator gates, then
stop. This review authorizes no Pass-2 work.
