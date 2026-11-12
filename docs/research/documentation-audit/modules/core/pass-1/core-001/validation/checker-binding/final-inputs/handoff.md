# Final accepted toolchain input transition

**Fresh final profile prepared with the unchanged qualified binder. Independent review of the three
comment-input deltas precedes the coordinator's single checker rebuild.** No build, checker invocation,
SDK test, native/device/crypto operation, formatter, Git mutation or subagent ran here.

## Exact input change

The final profile retains the original **248 participating paths**. **245** inputs are byte-identical;
exactly these three current files match the independently accepted external transfer:

| Participant | Original SHA-256 | Accepted current SHA-256 |
| --- | --- | --- |
| `scripts/toolchain/Process.cs` | `70fbc9d528f094fc098ff6aa58bdc2ca33817445b0d529393fabd971d53b4f90` | `84fe870fabdf44a6033c514722bcc256f73a5cf7ef9268c6b0e5c433be4cf133` |
| `scripts/toolchain/Results.cs` | `80fe8ba0557664469545ea1e709e1a755cc6da546024c48f1e171088795b6279` | `90a595695e19a88a1f0e55a60708ffebcae8ee12a2753662b5444b3fecf48abe` |
| `scripts/toolchain/TestTargets.cs` | `4d6ce786a5fde41dbc66fafa6190daccc19f1bcd6c534cd789590413513cc1d5` | `b257b57172bbd9967b7ec46754deffdaa3f044f2288ea7d22ed3f3b3c7593337` |

`three-input-transition.json` binds each old profile row, its exact original retained source snapshot,
the accepted transfer receipt and a focused unified diff. The three diffs were read: they change XML
or ordinary explanatory comment lines before classes/methods, with executable lines, strings, imports,
directives and signatures preserved. The bounded non-full-line-comment comparison also agrees. The
external revision-4 review independently accepts the three script hashes and its prior source proof.
No arbitrary source/configuration drift or runtime-input replacement is repinned.

Source baseline remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; metadata HEAD is a separate observed
phase identity. Binder, logger, foundation, C# checker, SDK/compiler/runtime and root policy inputs retain
their qualified bytes. Existing 22 admission guards and process qualification remain unchanged; this
record neither rebuilds the proof framework nor relabels an old seal as current.

## Preserve tested originals before cleanup

`old-output-preservation.json` maps **45** mutable local DocCheck output/reference/generated-input files
(**35,318,002 bytes**) to exact home-backed copies beneath:

`/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/checker-output-preservation/checker-bound-build-third-final-transition/`

This includes runtime copies of the Roslyn DLLs that a clean checker build can remove, plus original
implementation/reference/PDB and generated compiler-input identities. It excludes global SDK/framework/
NuGet caches and reconstructible source corpus. Old profile, seal and captures remain exact. The copies
preserve old tested bytes; they supply no claim of later-path or runtime equivalence.

## Final profile and exact existing commands

The existing command was used unchanged to prepare the new profile:

```bash
V=docs/research/documentation-audit/modules/core/pass-1/core-001/validation
B="$V/checker-binding"
python3 -B "$B/bind.py" prepare \
  --sdk-directory /home/rupicola/.local/share/mise/dotnet-root/sdk/10.0.404 \
  --runtime-directory /home/rupicola/.local/share/mise/dotnet-root/shared/Microsoft.NETCore.App/10.0.15 \
  --output "$PWD/$B/reviewed-inputs-final-edits.json"
```

Preparation produced the new profile and its normal tool-owned textual snapshot directory
`reviewed-inputs-final-edits-inputs/`, leaving all prior profiles/snapshots unchanged. Preserve this
actual output rather than repeating the command over its existing name.

**Final profile SHA-256:**
`db8347bc9370f25bfc5c99edac854691d5baeca41ae4687fa794cb398a4a8ff1`

Bounded reviewer reproduction:

```bash
python3 -B "$B/final-inputs/verify.py"
```

After independent acceptance and exclusive checkout release, the coordinator uses only the existing
qualified binder/logger flow:

```bash
P=db8347bc9370f25bfc5c99edac854691d5baeca41ae4687fa794cb398a4a8ff1
python3 -B "$B/bind.py" build --label checker-bound-build-final-edits \
  --profile "$B/reviewed-inputs-final-edits.json" --profile-sha256 "$P"
```

Read the actual warning-free complete build/capture records and accept its newly emitted
`attempts/checker-bound-build-final-edits/sealed.json` SHA. With that actual SHA as `S`:

```bash
SEAL="$B/attempts/checker-bound-build-final-edits/sealed.json"
S=<accepted-actual-final-seal-sha256>
python3 -B "$B/bind.py" run --label source-tokens-final-edits \
  --profile-sha256 "$P" --seal "$SEAL" --seal-sha256 "$S" tokens
python3 -B "$B/bind.py" run --label api-final-edits \
  --profile-sha256 "$P" --seal "$SEAL" --seal-sha256 "$S" api
```

The same unchanged checker selfcheck mode is available through `bind.py run` if the coordinator assigns
it, but no repeated logger qualification or whole-framework selfqualification is required by these three
comment changes. Do not run current tokens/API under the old profile or modify an old seal. Preserve
any failed final attempt and correct it in a fresh label. Further accepted comment changes outside the
248 participants do not invalidate this tool profile; a participating input change requires a new
bounded transition.
