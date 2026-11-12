# Second build: compiler-file input correction

**Both build attempts remain failed. This correction is ready for independent review before a third
coordinator build.** No checker selfcheck/token/API command has run and neither failed attempt has a
successful build seal.

## Actual diagnosis

The second attempt captured **214 literal Csc arguments**. Its four source files, 169 references,
eight analyzers and three analyzer configurations match their before populations. The failing file
predicate has two distinct causes:

1. `/sourcelink:` consumes the existing SDK-generated file
   `/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/jobs/Yubico.YubiKit.Audit20261111.DocCheck/obj/Release/net10.0/Yubico.YubiKit.Audit20261111.DocCheck.sourcelink.json`.
   Both before/actual `other` lists were empty. The before hook ran before the SDK Source Link producer
   set the `SourceLink` property and added embedded source descriptors. This JSON is the genuinely
   missing before input: **161 bytes**, SHA-256
   `579acca20afc059a3e099900cd9843cf5dc21f08daeec8e7c37957a6ed3b04d2`.
2. The quoted `/embed:` argument names
   `.../obj/Release/net10.0/.NETCoreApp,Version=v10.0.AssemblyAttributes.cs`.
   The complete file already appears among the four before sources. A generic comma split incorrectly
   shortened that filename to `.NETCoreApp`. Neither an extra generated C# body nor a missing reference
   caused this part of the rejection.

The SDK analyzer set includes System.Text.Json's generator, but this actual host emitted **zero**
source-generator C# files. The retained log reports only the capture rejection and `MSB3073`, with zero
warnings and no C# compiler diagnostic. Overall build status remains failed.

## Bounded fix and provenance

- The existing properties/before collection explicitly depends on the SDK's `GenerateSourceLinkFile`
  and `_SetEmbeddedFilesFromSourceControlManagerUntrackedFiles` targets. Their normal outputs are now
  collected and hashed **before Csc**. This follows the installed SDK's own producer targets rather
  than accepting arbitrary new input files after compilation.
- Source Link JSON has an explicit input/producer identity. The reviewed third profile adds the SDK
  Source Link Common/GitHub/Git task components and dependencies. Evaluated producer paths must match
  that profile; the consumed `/sourcelink:` path and bytes must equal the recorded before input.
- Compiler-file descriptors preserve complete literal filenames for `/embed:`, `/sourcelink:` and
  other single-file switches. Only `/resource:` and `/linkresource:` use their quoted metadata-field
  grammar. Literal argument text, spaces, commas and repeated descriptor roles remain retained.
- Exact source/reference/analyzer/config membership and unchanged before hashes remain mandatory.
  The full compile-observation predicate is factored for read-only inspection; only the real capture
  entry point publishes a compile record during a newly assigned build.

Selected SDK evidence is
`sdk/10.0.404/Sdks/Microsoft.SourceLink.Common/build/Microsoft.SourceLink.Common.targets`:
`GenerateSourceLinkFile` is a normal `BeforeTargets="CoreCompile"` producer (lines 81–84), its underlying
task sets the compiler `SourceLink` property (lines 50–63), and the embedded-file producer is another
before-CoreCompile target (lines 21–24). Microsoft.CSharp.Core.targets passes `SourceLink` and
`EmbeddedFiles` as actual Csc inputs. Context7 `/dotnet/docs` also confirms those inputs contribute
debug/source metadata to the PDB. Package, SDK, runtime and project versions retain separate identities.

Checker C#, project/local props and the qualified foundation/logger/qualifier remain unchanged. The
new profile has **248 fixed inputs / 30 textual control snapshots**. The first and second profiles,
their source snapshots, original reviews and failed records remain exact.

## Preservation and complete read-only inspection

`failure-preservation.json` hashes all **192** second-attempt/command records and preserves old binder,
hook, selfcheck and handoff/configuration text before editing. All **19** current checker-job files
were copied byte-exactly beneath the external home-backed directory:

`/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/checker-binding-failures/checker-bound-build-second/`

Unsealed DLL/PDB/reference bytes remain failed-build evidence. They supply no execution authority.

`read-only-compile-observation.json` runs every remaining compile predicate on the actual second
workspace with an identity-only retainer. It checks all 214 arguments, all 539 retained/proposed input
identities, output path/byte identities, reference/PDB, generator-output population and capture text.
Changed controller paths are checked through their explicitly preserved original snapshots.

The inspection clearly labels its **proposed corrected before population**. The old attempt lacked
Source Link's pre-Csc hash; current file existence cannot reconstruct that historical proof. Neither
`capture()` nor `save()` nor build admission is replayed, and the original attempt receives no new
`compile.json`, `finish.json` or `sealed.json`. A fresh build is still required to establish actual
producer-before-Csc ordering and final runtime outputs.

| Current record/participant | SHA-256 |
| --- | --- |
| `../bind.py` | `a6c2c0954772242d99fed2460f7c473780835605752c59732d32b76002cb8cee` |
| `../hook.targets` | `c1101cc66ec70a7c62801f9952d4c9e99401a08b2aeee97db953d06db993c989` |
| `failure-preservation.json` | `878d8601cbef5c17ad47e9b96c95b058e9c8e22de078f882247cffae9b42fb68` |
| `read-only-compile-observation.json` | `22065e9f51fa9666034910a8ad0f9e866f3ee2fdfcf5d7db765551e14fdb0e86` |
| `../reviewed-inputs-third.json` | `ceea8be4c6681c15c51f4611de9546c35f1428f3a6d44de893a434fdab7cd2f1` |

## Coordinator command after review

```bash
V=docs/research/documentation-audit/modules/core/pass-1/core-001/validation
B="$V/checker-binding"
P=ceea8be4c6681c15c51f4611de9546c35f1428f3a6d44de893a434fdab7cd2f1
python3 -B "$B/bind.py" build --label checker-bound-build-third \
  --profile "$B/reviewed-inputs-third.json" --profile-sha256 "$P"
```

The same unchanged v2 logger executes the canonical clean checker build. Inspect its actual new before
Source Link snapshot, complete Csc capture, zero-warning RESULT and final output binding before accepting
the emitted `attempts/checker-bound-build-third/sealed.json` SHA. Then use that seal and this profile for
the parent handoff's bound selfcheck/token/API commands. Logger requalification is unnecessary because
its original participants remain exact. No retry or SDK/native/device operation was performed by this
correction worker.
