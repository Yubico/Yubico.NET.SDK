# Framework adjudication — D-08, C-008, A-014 and trailing CBOR

**Date/access date:** 2026-11-03. **Scope:** coordinator-authorized bounded offline framework
experiment; no SDK reference, SDK source/test changes, SDK tests, hardware, integrations,
commits or delegation. Root/module CLAUDE and available skills read; `workflow-experiment`
and `domain-build` loaded. Inputs: investigation A's A-014/A-006, D's D-08, C's C-008/C-006,
and relevant encoder/converter source. This report supplies evidence for coordinator adjudication;
it does not edit triage or authorize implementation changes.

## Outcomes

| Candidate | Adjudication | Basis and limit |
| --- | --- | --- |
| **D-08 noncanonical update-user wire defect** | **Rejected for the claimed key-order mechanism.** | Actual System.Formats.Cbor **10.0.2** Ctap2Canonical writer auto-sorts the requested text keys. Both SDK update-user paths construct this mode and close the user map. Noncanonical insertion is not noncanonical wire, and no ordering exception occurs in the probe. This does not validate the complete SDK update operation, authentication transcript or interoperability. |
| **D-08 explanatory comment** | **Confirmed DOC correction.** | `CredentialManagement.cs:468` calls alphabetical text insertion canonical. Actual order is `id,name,displayName`; the writer, not alphabetical insertion, supplies canonicalization. Reject the high-severity deferred ordering IMP and any update-operation limitation attributed solely to it; retain the comment correction. |
| **C-008 canonical COSE ordering prose** | **Confirmed, with its framework uncertainty resolved.** | Numeric insertion `-3,-2,-1,1,3` is not canonical encoded order; actual writer output is `1,3,-1,-2,-3`. Preserve writes and fixtures; explain automatic writer canonicalization rather than label numeric sorting canonical. |
| **A-014 AAGUID portability gap** | **Resolved as a source-established conditional-reversal portability defect, not merely a doc qualifier.** | Default Guid span constructor/ToByteArray/TryWriteBytes use architecture-independent mixed-endian byte conventions. SDK reversal only on little-endian hosts is therefore incorrect on a hypothetical big-endian runtime following the pinned implementation. Little-endian fixture passes. **No big-endian platform was executed**, and no supported deployed big-endian target or affected device was established. |
| **A-006/C-006 trailing envelope subcase** | **Framework mechanism confirmed, narrowly.** | For `A0 01`, ReadEndMap returns with BytesRemaining=1 in Ctap2Canonical and Lax. Default multiple-root=false refuses *reading another root* but does not enforce exhausted input at map completion. SDK decoders that stop here without a remaining-byte check cannot rely on this option alone for complete-envelope rejection. No SDK decoder was executed; required-field and other malformed-input issues remain separate. |

The coordinator reports a previous **492 passing CTAP unit tests**. They were not rerun here.
Automatic writer sorting reconciles apparently out-of-order writer fixtures with passing tests;
suite passage alone is not update-user wire/transcript or hardware qualification.

## Execution and reproducibility

Navigation update2026-11-04: discovery isolation renamed the current runnable harness. The original
executed project and this report's pre-navigation bytes are retained under evidence/framework/original/;
hashes.json explicitly maps original paths to those snapshots. Commands/results below remain original
execution evidence. Current rerun instructions are in tooling-discovery.md; no rerun is implied.

Retained small console probe: [experiment_framework.cs](evidence/framework/src/experiment_framework.cs),
[original executed project snapshot](evidence/framework/original/Yubico.YubiKit.Ctap2.FrameworkProbe.csproj.snapshot),
[props](evidence/framework/Directory.Build.props), [targets](evidence/framework/Directory.Build.targets),
[reproduction script](evidence/framework/run-probe.sh). Project explicitly IsPackable=false,
IsTestProject=false; no test packages or SDK ProjectReference. The local props/targets isolate it
from SDK analyzers/build targets; central package policy still resolves Cbor to **10.0.2**.
RestoreSources is the already populated local `/home/rupicola/.nuget/packages`; NuGetAudit=false
prevents network audit during this authorized offline probe. No global/repo policy was changed.
All generated **bin/obj** are under `/tmp/opencode/ctap2-001-framework-probe`.
The project name/layout permits toolchain build discovery but not unit/integration test discovery;
IsPackable=false excludes packaging. It remains a discoverable build utility in retained evidence.

Exact executed build/runtime commands, from repository root:

```sh
dotnet --info
dotnet toolchain.cs -- build --project Ctap2.FrameworkProbe > /tmp/opencode/ctap2-001-framework-build.log 2>&1
dotnet /tmp/opencode/ctap2-001-framework-probe/bin/Release/net10.0/Yubico.YubiKit.Ctap2.FrameworkProbe.dll > /tmp/opencode/ctap2-001-framework-probe.log 2>&1
```

**All three exit codes: 0.** Build RESULT PASSED, Warnings:0; step log 0 warnings/0 errors.
Exactly **one isolated project** compiled; toolchain's mandatory hardware-declaration and Native
AOT contract gates were read-only checks, not integration execution or SDK compilation.
No direct dotnet build/test/restore was invoked by this agent. Toolchain internally invokes
its build command, preserved in the step log. No tests were discovered or executed.
Probe ends **PROBE PASSED**, after exact byte comparisons and bounded reader/Guid checks.

Retained [build console](evidence/framework/build.log), [build step](evidence/framework/build-step.log),
[RESULT JSON](evidence/framework/build-result.json), [probe output](evidence/framework/probe.log),
[assets](evidence/framework/project.assets.json), [environment](evidence/framework/dotnet-info.log)
and [SHA-256 manifest](evidence/framework/hashes.json).
Original toolchain run: `artifacts/toolchain/runs/20261103-234138-628-build/`.

Environment: .NET SDK **10.0.403** (commit `5a2162bea6`), MSBuild **18.9.15+5a2162bea**,
runtime **10.0.14**, Linux x64, BitConverter.IsLittleEndian=True.
System.Formats.Cbor assembly version `10.0.0.0` is not package version; informational version
`10.0.2+44525024595742ebe09023abe709df51de65009b` establishes the actual tested build.
CoreLib informational version `10.0.14-servicing.26522.126+0427b9abbead3fa64c6655eb4290c537397131fa`.
Exact assembly SHA-256 values are printed in probe.log and retained manifest. The NuGet assets
record retains package SHA-512/version; source/reports/commands are SHA-256 recorded separately.

## 1. Actual writer bytes, not insertion assumptions

Probe values are integer zero to isolate key-order behavior:

```text
Text insertion: displayName,id,name
Wire: A3 626964 00 646E616D65 00 6B646973706C61794E616D65 00
      map3  id   0   name    0       displayName          0
Integer insertion: -3,-2,-1,1,3
Wire: A5 01 00 03 00 20 00 21 00 22 00
      map5 1  0  3  0 -1  0 -2  0 -3  0
```

Pinned official [CborWriter.Map-v10.0.2.cs](evidence/framework/CborWriter.Map-v10.0.2.cs):
lines **121–140** record key/value ranges and mark out-of-order maps for sorting:

> “Once invalidated, declare that the map requires sorting, which will prompt a sorting of
> the encodings once map writes have completed.”

Lines **148–175** sort those ranges and copy entire key/value encodings back into the map buffer:

```csharp
if (_keysRequireSorting)
{
    _keyValuePairEncodingRanges.Sort(GetKeyEncodingComparer());
    // ... each complete key/value encoding copied to temporary span ...
    tmpSpan.CopyTo(source.Slice(_frameOffset, totalMapPayloadEncodingLength));
}
```

[CborWriter-v10.0.2.cs](evidence/framework/CborWriter-v10.0.2.cs) **288–322** calls CompleteMapWrite
while popping a map; WriteEndMap calls PopDataItem (Map.cs **53–61**). Thus sorting happens at
map completion, not merely when a caller later requests Encode. Source supports nested maps too;
the executed probe isolates one map of each requested key type, rather than running SDK methods.

SDK `CredentialManagement.cs` **358–403,426–484** shows both signed-parameter and final-request
construction use Ctap2Canonical and the same closed user-map helper. This is the narrow source
link rejecting D's insertion-order diagnosis. Shared `PublicKeyCredentialTypes.cs:399–416`
already inserts `id,name,displayName`; no serialization change is needed for that inspected unit.

## 2. Reader completion is not buffer exhaustion

Executed fixture is exactly **A0 01**: empty definite map followed by unsigned integer 1.
Both conformance modes tested produce:

| AllowMultipleRootLevelValues | ReadEndMap | BytesRemaining | PeekState | Later ReadInt32 |
| --- | --- | --- | --- | --- |
| default false | returns | 1 | Finished | InvalidOperationException: “No more CBOR data items to read in the current context.” |
| true | returns | 1 | UnsignedInteger | returns 1, remaining 0 |

Official [reader source](evidence/framework/CborReader-v10.0.2.cs) **46–63** documents
BytesRemaining as total unread bytes, computes `_data.Length - _offset`, and defaults
allowMultipleRootLevelValues=false with root item-count limit 1.
[ReadEndMap source](evidence/framework/CborReader.Map-v10.0.2.cs) **75–95** pops the map and advances
counters; for definite maps it does not inspect bytes after the map.
PopDataItem (**Reader.cs:200–231**) checks current container/type/completion and restores its parent,
not whole-buffer exhaustion. **Finished is not equivalent to BytesRemaining==0 in this case.**

For a standalone envelope intended to contain exactly one map, complete consumption needs an
explicit remaining-byte validation at that boundary (or equivalent enclosing contract). Do not
add it indiscriminately to reader-based nested parsers that intentionally consume one value.
This experiment does not claim every malformed trailing sequence is accepted, every SDK entrypoint
shares this policy, or that reader canonical checks never reject malformed maps.

## 3. Guid byte convention and conditional reversal

Official net10 API docs retained in [constructor HTML](evidence/framework/Guid-constructor-net10.html)
and [ToByteArray HTML](evidence/framework/Guid-ToByteArray-net10.html), with original URLs/hashes
in retrieval.json. Exact constructor-overload parameter documentation:

> “true to interpret b as big-endian; false to interpret it as little-endian.”

Default ToByteArray remarks:

> “The order of the beginning four-byte group and the next two two-byte groups is reversed,
> whereas the order of the last two-byte group and the closing six-byte group is the same.”

The default span constructor docs alone do not explicitly spell out architecture invariance;
the matching pinned official **runtime v10.0.14** implementation does:
[Guid-v10.0.14.cs](evidence/framework/Guid-v10.0.14.cs) **65–97**.

```csharp
// Default Guid(ReadOnlySpan<byte>)
this = MemoryMarshal.Read<Guid>(b);
if (!BitConverter.IsLittleEndian)
{
    _a = BinaryPrimitives.ReverseEndianness(_a);
    _b = BinaryPrimitives.ReverseEndianness(_b);
    _c = BinaryPrimitives.ReverseEndianness(_c);
}
```

That branch compensates for native memory order: the input convention remains first 4/2/2
bytes little-endian on **both** architectures. Explicit bigEndian overload instead reverses when
`BitConverter.IsLittleEndian == bigEndian`. Default ToByteArray **987–1000** and TryWriteBytes
**1022–1037** use the native write on little-endian and a compensating conversion on big-endian,
so their emitted first 4/2/2 bytes also remain little-endian on both. This concerns byte API
conventions, not a claim that Guid's internal native memory is architecture-independent.

Executed **little-endian** non-symmetric fixture:

```text
Network bytes:           00112233445566778899AABBCCDDEEFF
Expected Guid:           00112233-4455-6677-8899-aabbccddeeff
new Guid(network):       33221100-5544-7766-8899-aabbccddeeff
Reverse 4/2/2 then ctor:  00112233-4455-6677-8899-aabbccddeeff
new Guid(network,true):  expected Guid
Expected.ToByteArray():  33221100554477668899AABBCCDDEEFF
TryWriteBytes(default):  same mixed-endian bytes
Reverse 4/2/2 output:    original network bytes
ToByteArray(true):       original network bytes
```

**Inference, not executed big-endian result:** `AaguidConverter.cs:44–57` omits reversal when
IsLittleEndian=false, but the framework's default constructor still expects mixed-endian input.
The fixture's first 4/2/2 fields would be reversed relative to the intended Guid. Conversely
`:74–83` default TryWriteBytes still emits mixed-endian bytes on big-endian, and SDK skips the
needed network conversion. A symmetric/all-zero vector or round trip through both equally wrong
directions can hide this bug. Current little-endian behavior is coherent; this is not evidence
of a present Linux x64 failure. An eventual fix could use explicit bigEndian APIs (constructor
and TryWriteBytes), or unconditional component reversal, subject to coordinator authorization
and regression coverage. No implementation was changed here.

## Research bounds and preservation

Context7 resolved `.NET` and queried `/websites/learn_microsoft_en-us_dotnet` initially for writer/
reader behavior and separately Guid byte APIs, then one focused follow-up for span constructor/
writer sorting. Guid explicit-endian API snippets were relevant; the returned .NET11 CBOR depth
change and BitConverter examples were not used as .NET10 CBOR/Guid evidence. Exact queried
topics and limitations are retained in [Context7 record](evidence/framework/context7-record.md).

Original Microsoft net10 pages and runtime release-pinned source were fetched in a bounded batch.
A focused follow-up tried three Cbor source files at the **package informational commit** in
dotnet/runtime: all returned 404. That commit was not assumed to be a runtime-repository commit.
One corrected fetch of the known writer source at **v10.0.2** established the map-completion link;
no wider version/search sweep followed. Retrieval successes/failures, exact URLs and SHA-256
are retained in [retrieval.json](evidence/framework/retrieval.json). Release-tag source is pinned
and hashed; exact commit identity matching the tested package is **not** independently proved.
The executed 10.0.2 binary behavior remains direct evidence rather than relying on that identity.

Final inventory preservation record checks all 183 inventoried paths against their baseline hashes:
**179 match**, including the inspected converter/encoder C# files; four project files differ
(`FidoTool.csproj`, shipping Ctap2.csproj, unit/integration csproj), none edited by this agent.
These outside-scope project changes were left untouched, not classified as this probe's work.
Its exact per-file result and unchanged baseline HEAD are in
[preservation.json](evidence/framework/preservation.json). No triage,
SDK source/test or pre-existing user file was edited by this agent. Retained evidence/source snapshots
are audit data, not additions to the SDK test suite or a complete framework conformance assessment.
