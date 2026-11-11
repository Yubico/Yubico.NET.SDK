# FR-1 bounded record-classification correction

**Ready for the same independent final reviewer to assess FR-1.** Sol added
`validation/check-records-v2.py`; the original checker and all retained evidence remain unchanged.
No complete packet snapshot, index mutation or SDK/compiler/native/device gate was run in this task.

## Exact scope and provenance

- Pinned `final-review.md`: `7e53e938c3d396bba6ea8173e89696398408e3696d57a991c602fd23bb83b71f`.
- Original `check-records.py`: `e287c3e35d6fec35d125c870125201816dc4d9822ae78ba94d7c0da167eeb3e4`.
- Exact seven-row `approved-text.json`: `eeb2b9a302f57d57569e77d9fd7a844735cc7867edbcc07d97d908cdc9c21d36`.
- Current additive checker: `598d79ea82eb3de86afd9f1b9376f453ca20742a5e222863427d8005f8f5364f`.

The receipt binds each packet-relative path, byte length, SHA-256 and text kind: four UTF-8 JSON
objects and three UTF-8 base64 SHA-512 records. It contains exactly the seven reviewed inputs.

The adapter verifies the original source/receipt/review identities, then applies **two exact in-memory
source transformations**, one to each coarse directory/suffix predicate. Each adds the exact reviewed
text predicate as an alternative. Every other original line remains unchanged. The original `__file__`
context is the old checker file in the **active home checkout**, so ROOT/PACKET/HERE, attributes and
index operations retain their original root-derived behavior. The inactive tmpfs copy is excluded.

The complete original population, UTF-8/no-NUL/no-runtime-magic, symlink/containment, clean-conversion,
attributes and staged-byte checks remain in force. Unlisted paths receive the original coarse rules.
Every approved input must also remain present before each CLI execution. A post-record identity check
binds the actual bytes read by the original `record()` function, closing a change-between-reads gap.
The adapter grants no filename pattern, `obj` subtree or generic snapshot exemption.

## Short-scope verification

`verify.py` exited 0; `invocation.json`, `proof.json` and `verification.stderr.log` retain the result.
The proof contains the exact two transformations and adapted-source hash, seven actual read-only
`record()` outputs, and **65 rejected process-local path/content shadows**. Negatives cover changed
hash/size, missing inputs, wrong names with identical bytes, symlinks, root escapes, changes between
classification and capture, invalid UTF-8/NUL/MZ/ELF, unlisted archive/cache paths and provenance pins.
The mandatory seven-file population check rejects a missing approved file before enumeration.

All seven retained paths/bytes and the original checker were rechecked afterward. Git index SHA-256
was unchanged before/after: `da6c0a626cc282347be2e4b5e3452aae5dacb8ad1ee3024c437620bd7e90ed14`.
The pinned review already records each original rejection; this task preserves and relies on that
source-backed failure record rather than modifying or relocating evidence.

Product/source and compiler acceptance remains exactly as stated in the pinned final review, including
the four disclosed strict-live failures. This correction changes publication classification only.

## Authoritative publication commands after reviewer acceptance and all writers

From `/home/rupicola/Code/y/yubikit-core-docs-integration`:

```bash
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
python3 -B "$V/check-records-v2.py" snapshot --manifest "$V/final-record-manifest.json"
python3 -B "$V/check-records-v2.py" check --manifest "$V/final-record-manifest.json"
# Coordinator stages the exact reviewed packet paths and manifest.
python3 -B "$V/check-records-v2.py" staged --manifest "$V/final-record-manifest.json"
```

The supported modes remain `snapshot`, `check` and `staged`. The same manifest path is the default.
The final reviewer/coordinator owns the complete final population and staged-byte verification;
this short-scope result supplies no premature final-manifest or publication claim.
