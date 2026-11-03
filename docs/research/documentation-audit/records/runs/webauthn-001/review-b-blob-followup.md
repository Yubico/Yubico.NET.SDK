# Independent reviewer B — blob conversion follow-up

2026-11-03. **Verdict: REQUIRED CHANGES — one bounded wording qualification.**

The experiment supports replacing “unverified” with the observed conversion outcome. Its constructor shape matches the SDK path. However, the new claim that absent and stored-empty data cannot be distinguished **“through Blob alone”** is broader than the experiment establishes and than .NET's representation permits. Qualify the claim to nullable presence and length; no engineering fix or further execution is requested.

## Evidence reviewed

- `runs/webauthn-001/experiment_nullable_blob.cs`, complete 14-line file.
- `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs:143–150`, changed comment and unchanged assignment.
- `src/WebAuthn/src/Extensions/Outputs/LargeBlobOutput.cs:31–37,51–53`, changed public remarks and record constructor.
- `src/Ctap2/src/LargeBlobs/ILargeBlobStorage.cs:29–30` and `LargeBlobStorage.cs:171–189`, nullable array return contract/implementation.
- Fresh Context7 .NET API lookup, followed by the exact [.NET 10 ReadOnlyMemory.Equals documentation](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1.equals?view=net-10.0) and [pinned .NET 10.0.0 runtime source](https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/ReadOnlyMemory.cs), specifically the array constructor, implicit conversion, Length, and Equals implementations.

The coordinator reports that `dotnet run` printed `HasValue=True Length=0` and PASS. This reviewer inspected the experiment and supporting source; **did not rerun it** and did not independently witness a retained execution log. The reported observation is consistent with the inspected .NET source.

## Constructor-shape validation — supported

The experiment passes a variable declared `byte[]?`, whose value is null, as a named `Blob` argument to:

```csharp
sealed record Output(ReadOnlyMemory<byte>? Blob = null);
```

The SDK awaits `Task<byte[]?>` from `ILargeBlobStorage.GetBlobAsync`, then executes:

```csharp
outputs.LargeBlob = new LargeBlobAuthenticationOutput(Blob: blob);
```

The SDK parameter is also `ReadOnlyMemory<byte>? Blob = null`. Its additional optional `bool? Written = null` parameter is omitted and does not change the supplied Blob argument's conversion. Both declarations are record classes; no custom constructor, alternate overload, cast, or null-conditional conversion changes this path.

The experiment's `{ Length: 0 }` pattern verifies a nonnull nullable result with zero length. Its PASS text is guarded by that condition. This is appropriately isolated evidence for the conversion, not an SDK storage, device, error-policy, or conformance test.

The precise supported conclusion is: **a null array returned from storage becomes a present `ReadOnlyMemory<byte>` value of length zero inside `Blob`, rather than a null `Blob`.** This resolves the prior conversion-outcome uncertainty.

## Required correction: scope the lost distinction to presence/length

**Locations:**

- `LargeBlobOutput.cs:35–37`: “This cannot be distinguished from a stored empty blob through Blob alone.”
- `LargeBlobProcessor.cs:146–147`: “the result currently loses the distinction between absent data and a stored empty blob.”

The experiment contains only the null-array case. It does not compare the complete public memory representation with an actual empty-array value. In .NET 10 source:

- A null array produces `default(ReadOnlyMemory<byte>)`, with no backing object.
- A nonnull zero-length array produces a memory value retaining that array as its backing object.
- `ReadOnlyMemory<T>.Equals` compares backing object, index, and length, not only byte contents. The .NET API documentation expressly describes this identity-based equality.

Thus these cases have the same **nullable presence and length**, but are not necessarily indistinguishable as `ReadOnlyMemory<byte>` values. In particular, comparing a null-derived memory value with `default(ReadOnlyMemory<byte>)` differs from the same comparison on a nonnull-empty-array-derived value. This is a source-backed counterexample to the categorical wording, not a recommendation to make storage semantics depend on backing identity.

**Suggested public wording:**

> When storage returns no blob, the current array-to-memory conversion produces a present value of length zero. `Blob.HasValue` and the blob's length therefore do not distinguish that result from a stored empty blob.

**Suggested processor comment:**

> A null storage result converts to present empty memory below, so nullable presence and length do not distinguish absent data from a stored empty blob.

Retain the established conversion result and existing omission/error/cancellation/ownership qualifications. Do not restore “unverified,” prescribe a backing-identity workaround, change the assignment/API, or implement an engineering repair in this documentation batch.

## Static change-boundary checks

Reconstructing each original B-reviewed file in memory by restoring only the prior conversion remarks, and restoring the processor's prior using order, reproduced the recorded original-review hash prefixes:

- `LargeBlobOutput.cs`: SHA-256 `f35de9cda02b192307e4b2d410908d967453743cd0c47a052214534984991b05`.
- `LargeBlobProcessor.cs`: SHA-256 `9b8a79ccc967ac905350d5af00733cfc31f84d8ffecff9e5ed99cae653705b17`.

A separate static comparison against `e353b49afa15d1d104fce70bbf1e1abc59ddb132`, removing comments/whitespace and normalizing using order, matched for both files. The source assignment, constructor types/defaults, executable code and literals are unchanged. The using reorder is not a behavior change.

Only this report was authored. No repository changes, build, test, experiment execution, formatter, hardware operation, or delegation ran. All other B review dispositions and the coordinator's final gates remain outside this bounded follow-up.
