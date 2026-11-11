# Independent FR-1 correction follow-up

## Decision

**PASS — FR-1 is resolved by the exact additive checker reviewed below. Remove the FR-1 publication
HOLD from `final-review.md`; retain that original review unchanged as the historical failure record.**
No required correction or optional change remains within this bounded follow-up.

The coordinator may use `validation/check-records-v2.py` for the final snapshot/check/staged workflow
after all writers finish. This acceptance does not assert that a full snapshot, final population,
staged-byte verification, commit or publication has already occurred. Those release checks remain
required. No SDK rebuild, recapture or repeated SDK/native gate is requested.

Independent GPT-6.1 Sol follow-up, November 11, 2026. Scope: only the seven FR-1 text-record classification
exceptions and their integration with the original checker. This reviewer wrote only this follow-up;
the original review, checker, receipt, seven retained inputs and product files were not edited.
Verification used read-only Python `-B`, in-memory path/content shadows and non-writing Git commands.
No SDK build/test/capture, native/device execution, formatter, index/ref mutation, commit, push,
subprocess signaling or delegation occurred. The whole 8,000-plus-record population was not rescanned.

## Exact accepted identities

Paths are relative to this integration packet. Values are freshly verified SHA-256.

| Record | SHA-256 |
| --- | --- |
| `final-review.md` | `7e53e938c3d396bba6ea8173e89696398408e3696d57a991c602fd23bb83b71f` |
| Original `validation/check-records.py` | `e287c3e35d6fec35d125c870125201816dc4d9822ae78ba94d7c0da167eeb3e4` |
| Additive `validation/check-records-v2.py` | `598d79ea82eb3de86afd9f1b9376f453ca20742a5e222863427d8005f8f5364f` |
| `validation/record-classification-correction/approved-text.json` | `eeb2b9a302f57d57569e77d9fd7a844735cc7867edbcc07d97d908cdc9c21d36` |
| `validation/record-classification-correction/verify.py` | `28a00f09177524eb1f71267bbf3c7209d28d7b3ca77dca409d1d33662284a254` |
| `validation/record-classification-correction/proof.json` | `07db6d523c87751ed98c8a75a5c711529b6fe8f9e4fcfdee3f5603db412df721` |
| `validation/record-classification-correction/handoff.md` | `a775e314884cda9d396b8f475ed778f5f07e593a91a5c24dbae94414c3aaad21` |
| `validation/record-classification-correction/invocation.json` | `e0671ba6a44f41909d92b6d202c5b8af7b1fa3b22c4a370db7fc8192b487585b` |

The original review is unchanged. The retained invocation records exit 0 and correctly binds the
proof stdout and empty stderr (`e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
The independently reconstructed adapted-original source hashes to
`6a8be98b003b21e0ef68ed14803255d3d18d704a01b7198ecd7997f424007916`.

## Exact exception boundary

Parsed all seven path/size/hash rows from the original FR-1 review and compared that complete mapping
with `approved-text.json`: **exact equality**, with no added, missing or renamed row. Every current
input retains its original path, length and raw SHA-256. The seven rows classify four JSON objects
(the two archived restore inputs and two package metadata snapshots) and three base64 SHA-512 texts.

The adapter pins the original checker, receipt and original review before loading. It requires seven
unique exception paths and the active home-root checker context. Its only changes to the original
source are two exact, single-occurrence in-memory predicate transformations: the coarse generated-
directory rule and compound binary/cache suffix rule each gain `or reviewed_text(path)`. Reconstructed
both substitutions independently and verified the adapted-source hash. Every other original line is
unchanged; no alternate `record`, enumeration, clean-conversion or staged-check implementation replaces it.

`reviewed_text` requires exact packet-relative path membership, containment, no symlink, exact byte
length and raw SHA-256 before granting an exemption. It then checks UTF-8/no-NUL/no-runtime-magic and
the declared JSON-object or validated 64-byte base64 kind. This admits the known historical text
records only; directory membership, a `.snapshot` ending or `.nupkg` inside a filename is insufficient.
No whitespace-normalized hash, reserialized JSON or substitute artifact is accepted.

The original `record()` subsequently performs its unchanged content, attributes, normalization and
actual Git-clean checks. The additive wrapper also compares that actual read's size/hash to the
approved row, rejecting changes between classification and capture. The CLI calls the mandatory
seven-file population check before **every** mode, so a missing approved record cannot disappear
silently from enumeration. An arbitrary `--manifest` filename does not bypass that prerequisite.
The manifest-location/existence and complete population/index checks remain the original implementation.

All seven accepted record outputs retain `text=unset`, with `eol`, `filter` and
`working-tree-encoding` unspecified. Their raw-preserved outputs, attributes and calculated staged OIDs
exactly match the retained proof. No attribute handling or `-text` scope change is introduced by this
adapter. UTF-8, binary magic/NUL, symlink/containment, filter/encoding, normalization, complete current
population, cached-attribute and staged-byte/OID guards remain intact. The approved paths and original
checker retain their bytes; no compiler manifest or historical receipt was repaired or rewritten.

## Fresh verification and negative boundaries

Imported `verify.py` and called `main()` with stdout captured in memory rather than written to a new
proof. **The entire returned JSON exactly equals retained `proof.json`**, including seven actual
read-only `record()` acceptances and **65 rejected** process-local negative cases:

- Changed raw hash, changed length, missing file, unlisted same-kind/same-bytes name, symlink,
  escaping resolved path, and a classification-to-record content change for every approved row.
- Invalid UTF-8, NUL, MZ and ELF content on an unlisted ordinary text path.
- Unlisted package/archive/binary/compound-metadata names and generated directories.
- A missing mandatory approved file before enumeration and altered receipt/original-source/review pins.

Independently ran **25 additional negative checks**, again with process-local shadows only:
all ten original forbidden suffix classes (`.dll`, `.exe`, `.pdb`, `.so`, `.a`, `.o`, `.dylib`, `.pyc`,
`.nupkg`, `.zip`) as both direct endings and compound `.snapshot` names, plus all five forbidden
directory classes (`bin`, `obj`, `node_modules`, `__pycache__`, `.git`). Every case was rejected.
These are checker-boundary checks, not SDK test executions or new compiler evidence.

The fresh verifier confirms the same index SHA-256 before/after:
`da6c0a626cc282347be2e4b5e3452aae5dacb8ad1ee3024c437620bd7e90ed14`.
It also rechecks the original checker and all seven input identities after verification.

The review's first read-only replay stopped because its own audit hook incorrectly treated the writable
anonymous pipe to `git check-attr` as a disk write. No file was written. Restricting that allowance to
integer descriptors verified as FIFOs, while retaining disk-write/mutation denial and an explicit
read-only Git-command allowlist, permitted the full replay above. This was a reviewer instrumentation
error, not an adapter failure or SDK result; its original output remains in the session transcript.

## Scope continuity and remaining coordinator gates

FR-1 acceptance changes publication classification only. The accepted product tree remains
`5dda4f4bbedadd8bca23bb4eb0fed5bf65d841ae`; product scope SHA-256 remains
`3f974a8216fbfa7690a62fca3cee035438816b004a15d8a0da715e0679d76420`.
The accepted final compiler scope remains **current-source/exact-retained-output**, scope SHA-256
`d6b559e263ed217de91bb9f0dd6a92a9fd0b1d51f198d7a26566fcfde30ce491`.
The four original strict-live failures remain disclosed; nothing here claims six strict-live passes,
runtime equivalence, hardware qualification or whole-Core completion. This follow-up does not repeat
the earlier product, compiler, preservation, SDK or Linux gates.

After all writers finish, the authoritative classification-corrected commands are:

```bash
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
python3 -B "$V/check-records-v2.py" snapshot --manifest "$V/final-record-manifest.json"
python3 -B "$V/check-records-v2.py" check --manifest "$V/final-record-manifest.json"
# Coordinator stages the exact reviewed product/packet paths and manifest.
python3 -B "$V/check-records-v2.py" staged --manifest "$V/final-record-manifest.json"
```

These full-population operations were reviewed, **not run** here. Retain the original checker/failure
and this acceptance; do not broaden the receipt automatically. Final record/staged verification,
product/user preservation, complexity-message justifications, pinned-parent publication and safe
campaign fast-forward remain coordinator responsibilities. User-announced Windows concurrency work
still awaits its separate ready handoff and does not gate this pin. Core audit dispatch remains after
actual publication and ancestry/preservation verification.
