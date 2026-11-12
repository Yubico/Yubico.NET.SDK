# Independent Sol review: third checker profile and original-source records

## Separate decisions

- **Checker compiler-file correction: PASS for static preparation.** Accept the exact third profile
  below for the coordinator's separately released third build. Actual producer-before-Csc ordering,
  successful compilation, final outputs and a new seal remain pending.
- **Registered-original record classifier: PASS.** The exact three PDFs and encoding-qualified original
  HTML are admitted with provenance/format/identity checks and byte-preserving Git policy. This is a
  record-tool change outside the checker profile and qualified logger participant lists.

No further correction is required by this bounded review. Neither decision admits either failed build
as successful or authorizes capture replay/checker execution. Source baseline remains
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed metadata HEAD was
`bb94f0884f63f260156227957d4cc112d984e300`. Later record/status commits may advance HEAD independently
of exact participating input identities. All prior reviews remain unchanged.

## 1. Compiler-file collection and argument semantics

The selected SDK `10.0.404/Sdks/Microsoft.SourceLink.Common/build/Microsoft.SourceLink.Common.targets`
defines both actual producers used by the corrected hook:

- `GenerateSourceLinkFile` depends on source-control initialization and `_GenerateSourceLinkFile`.
  The task in the latter sets the compiler `SourceLink` property and creates the source-map JSON.
- `_SetEmbeddedFilesFromSourceControlManagerUntrackedFiles` depends on `BeforeCompile` and the
  source-control provider's embedded-file producer. Both are normal before-CoreCompile operations.

`CoreDocCheckProperties` now explicitly depends on these producers before collecting properties and
`_CoreDocCheckOther`; the before capture depends on that properties target. The binder pins the selected
Source Link generator/provider/Git task paths and bytes, requires the expected generated JSON owner
and shape, and requires that JSON's identity among the actual before inputs. After Csc, the exact
consumed `/sourcelink:` path and bytes must match that before identity. This addresses an actually
missing compiler input rather than exempting the switch or supplying a retrospective before hash.

The second failure also exposed a separate parser error. The quoted `/embed:` filename
`.NETCoreApp,Version=v10.0.AssemblyAttributes.cs` is one of the four existing source files. It is not
an extra generated C# body. The revised descriptor parser preserves its complete filename, raw argument
text, spaces, commas and repeated roles. It applies trailing identifier/accessibility-field parsing
only to `/resource:` and `/linkresource:`. Current Context7 `/dotnet/docs` resource-option documentation
supports that resource metadata distinction; the actual retained arguments and installed SDK task
wiring supply the local evidence. No generic compiler-input exemption was added.

The diff retains exact source/reference/analyzer/config membership checks, before-input hash checks,
empty/response argument rejection, execution/warning/analyzer prerequisites and the late CoreCompile
hook. Factoring `compile_observation` separates observation from publishing; only the assigned actual
capture path adds `actual_csc` and writes `compile.json`. PDB output containment is also now checked
alongside the implementation/reference outputs. Successful build admission, final DLL/PDB/Roslyn output
comparisons, accepted seal hash and before/after bound-invocation checks remain mandatory. A successful
raw command alone still supplies no checker admission.

### Profile and historical evidence

- The submitted third profile has SHA-256
  `ceea8be4c6681c15c51f4611de9546c35f1428f3a6d44de893a434fdab7cd2f1`.
  Independent byte checks matched all **248 inputs / 30 text snapshots**. Relative to the second profile,
  exactly the existing binder/hook participants change, six selected SDK Source Link/Git producer and
  dependency files are added, and no existing input is removed.
- Reading the actual second-attempt arguments independently confirmed **214 arguments**, **four sources,
  169 references, eight analyzers and three configs**, with the before/actual memberships matching.
- The retained full read-only observation is explicitly a **PROPOSED corrected-before view**: 539
  retained/proposed input identities, all 214 arguments, output/reference/PDB identities and zero emitted
  generator C# files. It models the missing Source Link input using current observation and preserved
  original controller snapshots. It does not establish that input's historical pre-Csc bytes. This
  reviewer inspected its report/source and did not invoke the compile observation or any capture path.
- For **each** earlier attempt, independent hashes matched all **192 failed records** and **19 external
  job-file preservation copies**. Both original command receipts still report failure, and both attempt
  directories still lack `compile.json`, `finish.json` and `sealed.json`. Unsealed DLL/PDB/reference bytes
  remain failure evidence, not execution authority.
- The current report retains **22** meaningful binder negative-case outcomes. Their definitions and
  admission boundaries were reviewed, not rerun as tests. Original checker C#, foundation, logger and
  qualifier identities match the supplied unchanged-input hashes. No logger requalification is needed.

## 2. Registered PDF and ISO-8859-1 HTML originals

`sources/primary-binaries.json` registers exactly four originals: three specification PDFs and the
ISO-8859-1 X.690 metadata HTML. The loader checks unique safe run-relative source/authority paths,
regular non-symlinked ancestors, original-source kinds, explicit HTTPS URLs, exact length/hash and a
unique matching original author/fetch entry with corresponding URL/time/identity evidence. Missing
registered originals fail even before whole-run enumeration.

PDF admission additionally requires the PDF suffix and actual `%PDF-` magic. HTML admission requires
the supported explicit encoding, original response media type, actual HTML markup and matching literal
Content-Type META declaration evidence. Encoding is not guessed from a filename or used as a fallback
for unregistered text. PE/ELF/archive magic and NUL remain rejected for that HTML lane.

The bypass of UTF-8/NUL text interpretation applies only to exact registered originals after those
checks. Ordinary records retain generated-directory, direct binary/cache suffix, PDF registration,
UTF-8 and binary-content guards. The complete population still rejects symlinks. The underlying pinned
raw/actual-clean/attribute/staged-set/index-object algorithm is unchanged. For registered originals the
wrapper additionally requires `text=unset` and raw preservation, labels normalization
`byte-exact-original-source`, and records the source registration. Nothing transcodes HTML or rewrites
internal PDF newlines.

Independent pure checks loaded all four real registrations and reproduced their accepted record
identities and actual Git-clean hashes; every raw SHA-256 equals its expected staged SHA-256. Additional
ordinary-path checks rejected an unregistered PDF, non-UTF8 HTML, direct DLL payload, PE and ELF bytes.
The **final** addendum report has **18** negative cases, including wrong original HTML response media
type; the earlier 17-case report is retained separately. The safe-path/symlink/provenance checks and
their negative-case definitions were also inspected. No final staged-byte result is claimed before
the coordinator's complete-run staging checkpoint.

The run-local `.gitattributes` uses `* -text -whitespace` below this Core run only. It preserves source
captures and record bytes without changing shipping `src/` or sibling frozen integration/app archives.
No ancestor/frozen attribute change was found in the tracked attribute delta check. Earlier frozen
manifests retain their own scope; this review does not repeat the campaign-wide preservation scan.

The classifier, registry, author manifests and run-local attributes are record-proof inputs, not
checker compiler participants. The current third profile contains neither `validation/check-records.py`
nor the registry. Qualified command receipts bind the unchanged logger/foundation stack rather than
this classifier. Thus this independently reviewed addendum does not invalidate the reviewed checker
profile or previously qualified logger. Fresh record manifests must capture its actual new behavior
and attributes after the relevant writers stop.

## Exact third-build command

After exclusive execution release and loading the current build skill, the coordinator runs from the
primary checkout root:

```bash
V=docs/research/documentation-audit/modules/core/pass-1/core-001/validation
B="$V/checker-binding"
P=ceea8be4c6681c15c51f4611de9546c35f1428f3a6d44de893a434fdab7cd2f1
python3 -B "$B/bind.py" build --label checker-bound-build-third \
  --profile "$B/reviewed-inputs-third.json" --profile-sha256 "$P"
```

Do not repeat logger qualification or `sdk-info-first`. Require the actual new before Source Link
snapshot/producer evidence, complete Csc capture, zero-warning successful RESULT, original complete
quiescent command receipt and final output binding. Accept the actual newly emitted
`attempts/checker-bound-build-third/sealed.json` SHA-256 only after those checks pass. No seal value is
predicted here. Checker selfcheck/token/API executions remain a separate release and must use that
profile/seal plus their successful original receipts and passed invocation sidecars. Preserve another
failure as a fresh failed attempt; never retrofit either earlier before record or manufacture a seal.

## Reviewed identities and limits

Paths below are relative to this Core run.

| File | SHA-256 |
| --- | --- |
| `validation/checker-binding/bind.py` | `a6c2c0954772242d99fed2460f7c473780835605752c59732d32b76002cb8cee` |
| `validation/checker-binding/hook.targets` | `c1101cc66ec70a7c62801f9952d4c9e99401a08b2aeee97db953d06db993c989` |
| `validation/checker-binding/correction-file-inputs/static-verification.json` | `6331a5da6231893f9fbf13c6f194ac0a76ae973b71e2a19f4daee457270c21ce` |
| `validation/checker-binding/correction-file-inputs/read-only-compile-observation.json` | `22065e9f51fa9666034910a8ad0f9e866f3ee2fdfcf5d7db765551e14fdb0e86` |
| `validation/check-records.py` | `e115552ca79243091ad160bddb0667e4bb1dc9d2d1cd86800b4152ac8ab2f552` |
| `sources/primary-binaries.json` | `1ebcbc30fa343a906158473c8c33686143f711fe6759fcf141f73b140af34421` |
| `.gitattributes` | `599885303332ce607eba16101ba296708e4c0423f5af58b8b2b87aa15791d0d6` |
| `validation/primary-source-addendum/static-verification-final.json` | `e939f34619af64b0f579f28b28ce10028ae67013e93b592cde82629c5c97b1fa` |

This reviewer used source reads, JSON/XML/AST inspection, byte/hash checks and pure record-classification
checks. No SDK build/test, compiled checker, compiler-capture replay, writing helper, process
qualification, native/device operation, formatter, product edit or Git mutation ran. Only
`validation-review-third.md` is written. Prior reviews and both failed attempts remain intact. Active
family investigations, final coverage/examples, PIV physical deferral and later Windows handoff remain
with their existing owners.
