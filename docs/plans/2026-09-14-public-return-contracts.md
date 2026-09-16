# Public return-contract implementation plan

**Goal:** Make collection operations explicit, encapsulate storage, and introduce growable results where response-level information already exists before version 2 ships.

**Architecture:** Keep simple listings as read-only collections. Preserve named OpenPGP collection types using composition, use specific enumeration and retry results, and enforce public-contract conventions across shipping types. The separately reviewed raw-response follow-up is implemented in the same working tree with explicit secret ownership, pending packaging; no commit or follow-up branch exists yet.

**Technology:** C# 14, .NET 10, existing module tests and `dotnet toolchain.cs`, public declaration baselines, Craftsman cross-vendor review.

## Starting point and execution policy

- Fetch `origin/yubikit` and start from its latest commit. Initial base: `0501f516`.
- Isolated worktree: `public-return-contracts`; branch: `feature/public-return-contracts`.
- Re-read module guidance and current signatures: recent WebAuthn option changes have already landed.
- Three stages, completed sequentially. Each stage includes tests, consumer migrations, baselines, documentation and cross-vendor review before the next stage.
- The raw-response change received a separate review and is now implemented in this working tree. It remains a distinct packaging concern, but no commit or follow-up branch has been created yet.
- Preserve protocol bytes, validation, exception behavior, session ownership, cancellation and ordering. Do not opportunistically fix protocol interpretation.
- No new OpenPGP device convenience operation, no assertion-selection envelope, no `RequiresSelection` replacement, no public-suffix changes, no generic `IResult` or enumeration framework.

## Craftsman workflow for each stage

1. Write meaningful failing tests for new behavior, or migrate existing behavior tests for mechanical signature changes.
2. Implement the smallest working baseline and run focused tests.
3. Audit fit across the affected feature slice using the opposite-vendor reviewer; apply a read-only simplification lens.
4. For material findings, compare leaving as-is with two alternatives. Act only when value justifies cost, with at most two reshape passes.
5. Review the settled implementation for correctness cross-vendor, resolve substantive findings, and perform the final simplification pass.
6. Run targeted verification after changes; confirm full changed-consumer compilation before declaring the stage complete.

## Stage 1: key-information naming and OpenPGP collection ownership

### Problem

Security Domain abbreviates `Information` and uses a singular-looking lookup name for a list. OpenPGP has a similar operation over a key-slot map. OpenPGP's `KeyInformation`, `Fingerprints`, and `GenerationTimes` inherit mutable dictionaries. Bare `Fingerprints` is ambiguous alongside biometric operations.

### Remedy and pseudocode

```csharp
// Security Domain session and existing device extension:
Task<IReadOnlyList<KeyInfo>> ListKeyInformationAsync(CancellationToken cancellationToken = default);
Task<IReadOnlyList<KeyInfo>> ListKeyInformationAsync(
    SessionCreationOptions? options = null, CancellationToken cancellationToken = default);

// OpenPGP session only:
Task<KeyInformation> ListKeyInformationAsync(CancellationToken cancellationToken = default);
Task<KeyFingerprints> GetKeyFingerprintsAsync(CancellationToken cancellationToken = default);
Task SetKeyFingerprintAsync(KeyRef keyRef, ReadOnlyMemory<byte> fingerprint,
    CancellationToken cancellationToken = default);

sealed class KeyFingerprints : IReadOnlyDictionary<KeyRef, ReadOnlyMemory<byte>>
{
    private readonly Dictionary<KeyRef, ReadOnlyMemory<byte>> entries;
    // Parser constructs and transfers exclusive ownership of its dictionary.
    // Public construction, if retained, snapshots caller-owned collection storage.
    // Forward read indexer, Count, Keys, Values, TryGetValue, ContainsKey and enumeration.
    // No public mutable collection interface or exposed backing dictionary.
}
```

Apply the same encapsulation to `KeyInformation` and `GenerationTimes`. Retain dictionary lookup behavior and missing-key semantics. Keep parser byte-buffer behavior; do not promise deep immutability of arbitrary caller-owned `ReadOnlyMemory` values.

Rename parsed-result properties to `KeyInformation`, `KeyFingerprints`, and `CaKeyFingerprints`. Align the key-fingerprint helper name if publicly exposed; retain specification tag identifiers where they directly name protocol objects.

The device method deliberately has no applet prefix/suffix. It does not collide today. Document static-class qualification or explicit sessions as the resolution when equally applicable extensions from multiple modules are imported.

### Files and migration

- `src/SecurityDomain/src/{ISecurityDomainSession,SecurityDomainSession,IYubiKeyExtensions}.cs`
- `src/OpenPgp/src/{IOpenPgpSession,OpenPgpSession.Keys,DiscretionaryDataObjects,KeyRef}.cs`
- Co-locate the three collection types in individual files if consistent with nearby model layout; no shared generic base is needed.
- All actual callers, including `src/Cli.Commands/src/OpenPgp/OpenPgpInfoCommand.cs`, OpenPgpTool, unit and integration sources.
- Module `PublicAPI.Unshipped.txt`, README/CLAUDE notes and current migration/usage documentation.

### Benefits and tradeoffs

- Extensibility: named domain types can gain members without committing to dictionary inheritance.
- Maintainability: explicit terminology and one owner for collection mutation; existing reader syntax largely survives.
- Tradeoffs: breaking renames and removal of collection mutators/collection initializers; a small amount of read-only forwarding code. Unqualified device extensions accept potential future compile-time ambiguity.

### Tests and gate

- Existing Security Domain wire tests keep their behavioral assertions under new names.
- OpenPGP parser tests verify values, empty/missing slots, lookup, enumeration and repeated parsing.
- Contract checks catch the dictionary-inheritance defect; snapshot tests prevent caller collection mutations affecting results if public construction is offered.
- Run `dotnet toolchain.cs -- test --project SecurityDomain`, `--project OpenPgp`, and `--project PublicApi`.

## Stage 2: justified result contracts and output-model simplification

### 2.1 Enumeration envelopes

**Problem:** The reported total for an enumeration is discoverable only on its first entry.

```csharp
sealed class RelyingPartyEnumerationResult
{
    IReadOnlyList<RelyingPartyInfo> RelyingParties { get; }
    int? ReportedTotal { get; }
}
sealed class CredentialEnumerationResult
{
    IReadOnlyList<StoredCredentialInfo> Credentials { get; }
    int? ReportedTotal { get; }
}
Task<RelyingPartyEnumerationResult> EnumerateRelyingPartiesAsync(...);
Task<CredentialEnumerationResult> EnumerateCredentialsAsync(...);
```

Use owned read-only collection storage, internal construction and no publicly replaceable collection state. `ReportedTotal` is the original reported value, not `Items.Count`. No-credentials with no response total yields an empty collection and null total. Preserve public single-response decoder fields because they still describe that response; the aggregate exposes a convenient authoritative copy. No raw aggregate buffers.

**Benefit/tradeoff:** future operation-level properties have a home and callers avoid first-item conventions; costs one envelope and changed collection access. Wire and aggregate models require clear documentation.

Files: `src/Fido2/src/CredentialManagement/{CredentialManagement,CredentialManagementModels}.cs` plus specific result files. Tests: `CredentialManagementModelsTests.cs`, `CredentialManagementWireTests.cs`, affected tools and integration sources.

### 2.2 Named retry results

**Problem:** Tuple return types tie callers to unnamed structural shapes.

```csharp
sealed class PinRetryStatus
{
    int RetriesRemaining { get; }
    bool PowerCycleRequired { get; }
}
sealed class UserVerificationRetryStatus
{
    int RetriesRemaining { get; }
    bool PowerCycleRequired { get; }
}
Task<PinRetryStatus> GetPinRetriesAsync(...);
Task<UserVerificationRetryStatus> GetUvRetriesAsync(...);
```

Use stable construction with internal constructors and additive optional properties for future growth. Do not add `Deconstruct`; callers use the named properties. Preserve current parsing of PowerCycleState in both operations; protocol changes are not inferred from this refactor.

**Benefit/tradeoff:** clearer independently evolving domain results; small allocations and migration of tuple member access.

Files: `src/Fido2/src/Pin/ClientPin.cs`, new result models, `ClientPinTests.cs`, WebAuthnBackend and FidoTool/Cli.Commands consumers.

### 2.3 Supported-algorithm entries

```csharp
sealed class SupportedAlgorithm
{
    KeyRef KeyRef { get; }
    AlgorithmAttributes Attributes { get; }
}
Task<IReadOnlyList<SupportedAlgorithm>> GetSupportedAlgorithmsAsync(...);
```

Replace `GetAlgorithmInformationAsync` and its tuple entries. Preserve repeated KeyRef entries: several algorithms can belong to one slot. Use a named reference type with stable construction rather than growing positional tuple/record arity.

**Benefit/tradeoff:** element documentation and additive properties; allocation per entry and source migration. A dictionary keyed only by KeyRef is invalid because it loses multiplicity.

Files: `src/OpenPgp/src/{IOpenPgpSession,OpenPgpSession.Config}.cs`, new `SupportedAlgorithm.cs`, wire/model tests and the existing supported-algorithm integration source.

### 2.4 WebAuthn outputs and selection cleanup

Convert only `RegistrationExtensionOutputs` and `AuthenticationExtensionOutputs` into non-positional records with optional init properties, preserving all existing defaults and fields. Migrate construction in ExtensionPipeline and tests. Adding future optional properties must preserve existing construction; equality/serialization changes still need review.

```csharp
sealed record AuthenticationExtensionOutputs
{
    CredBlobAssertionOutput? CredBlob { get; init; }
    // Existing LargeBlob, Prf and PreviewSign properties remain optional.
}
```

Remove `MatchedCredential.RequiresSelection`, its constructor parameter and the repeated producer flag. Keep `GetAssertionAsync` returning `IReadOnlyList<MatchedCredential>`. Keep `SelectAsync`, lazy execution, same-result identity, failure caching and cancellation unchanged. Callers use Count to handle zero, one and many matches.

**Benefit/tradeoff:** output records grow through named properties; removes redundant selection state. Positional callers must migrate; no replacement selection abstraction is introduced.

### Stage 2 verification

- Test no-credentials, absent/present totals, item ordering, independent returned count and reported count, failures, and unchanged loop behavior.
- Retry tests retain decoded values and cancellation/error behavior.
- Test repeated algorithm slots and attributes survive.
- Existing WebAuthn extension parsing and assertion lazy/cancellation/idempotence tests remain meaningful.
- Update module public baselines and all actual consumers, including unified command projects and legacy example sources still built.
- Run `dotnet toolchain.cs -- test --project Fido2`, `--project OpenPgp`, `--project WebAuthn`, and `--project PublicApi`.

## Stage 3: public-contract regression prevention

### Problem and remedy

The existing memory/collection convention test examines selected session methods and exact mutable collection types, missing subclass inheritance and auxiliary return models. Add a separate role-appropriate public return-contract scan rather than forcing all public classes into applet-session conventions.

```text
for each exported type in shipping assemblies:
    inspect public method returns and property types
    unwrap Task<T>/ValueTask<T>
    inspect nested collection element/tuple types with a visited set
    detect known mutable collection contracts and collection base classes
    flag public tuples for explicit review
    honour member-specific reviewed exceptions with reasons
```

Do not classify every ICollection implementation as mutable; read-only wrappers may implement it. Do not ban binary payloads, explicit builders, internal tuples, or valid collection names by English grammar. Avoid arbitrary framework graph traversal. Record remaining existing intentional exceptions rather than silently changing unrelated low-level contracts.

Add scanner fixtures proving direct and inherited mutable dictionaries are detected, read-only wrappers pass, nested tuples are found, and recursion terminates. Compare full public constructor/member signatures using public baselines, not constructor arity alone.

The PublicApi test project emits an assembly marker for every project reference and discovers the shipping scan set from those markers at runtime. A newly referenced shipping module is included automatically; a separately added shipping project still needs a PublicApi project reference before this test can cover it.

**Benefits/tradeoffs:** catches the specific defect family across more public types and makes new exceptions reviewable; requires a small explicit exception set and semantic review of naming/ownership.

Files: `src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/MemoryAndCollectionConventionTests.cs`, new return-shape scanner/tests as needed, existing factory-shape expected method names. Update all relevant public baselines and current migration documentation.

Gate: `dotnet toolchain.cs -- test --project PublicApi`, affected command-project tests when present, and `dotnet toolchain.cs build` to prove consumer and integration-source compilation. Confirm project selection actually compiled intended consumers. Run scoped formatting and `git diff --check`.

## Separately reviewed follow-up: complete stored-credential RawData

### Problem

`RelyingPartyInfo` preserves complete RawData; `StoredCredentialInfo` does not. A credential response may include a large-blob secret key, so retaining the complete response introduces an ownership obligation.

### Settled remedy

```csharp
sealed class StoredCredentialInfo : IDisposable
{
    private readonly byte[] ownedResponse;
    ReadOnlyMemory<byte> RawData { get; }       // guarded, view of ownedResponse
    ReadOnlyMemory<byte>? LargeBlobKey { get; } // slice of ownedResponse
    // Preserve ordinary decoded fields; avoid extra secret copies.

    static StoredCredentialInfo Decode(ReadOnlyMemory<byte> input)
    {
        clone input into ownedResponse;
        try { parse, retaining secret byte-string slices of ownedResponse; return owner; }
        catch { zero ownedResponse; throw; }
    }
    void Dispose() { zero ownedResponse once; reject later sensitive/raw access; }
}
```

Public Decode borrows input and never clears caller-owned memory. Previously captured owned-buffer views observe zeroing after disposal. Clearly document copying needed for data retained beyond result lifetime.

`PublicKey` remains independent of the disposable raw-response owner and stays usable after disposal. `RawData` and `LargeBlobKey` are the bounded disposable views: disposal clears the private complete-response clone while leaving the upstream borrowed response buffer untouched. This is an explicit bounded cleanup guarantee, not an end-to-end transport-buffer cleanup claim.

Because Stage 2 introduces `CredentialEnumerationResult`, prefer making that result disposable and the owner of its credential entries: `using var result = await EnumerateCredentialsAsync(...)`. Each individual entry remains independently disposable for direct Decode callers. On enumeration failure, dispose all entries already decoded; on successful completion, ownership transfers to the result. Empty results dispose harmlessly. No aggregate RawData property exists.

The source response ownership review settled on treating arbitrary `ReadOnlyMemory` returned by a public interface or custom implementation as borrowed and non-writable. A broader transport ownership change is out of scope. Complete-raw preservation is byte-exact, not redacted, and the remaining upstream response copy prevents any end-to-end zeroing claim.

### Benefits, tradeoffs and tests

- Extensibility: unknown fields remain inspectable on each response.
- Maintainability: one explicit owner for retained sensitive response bytes; aggregate disposal makes correct caller cleanup straightforward.
- Tradeoff: adds a disposal obligation and lifetime semantics. Complete envelope storage can be larger than the currently retained fields; do not claim it reduces total retained bytes.
- Tests: exact unknown-field preservation, input independence, secret-view zeroing, repeated disposal, access after disposal, malformed parse cleanup, aggregate cleanup, mid-enumeration failure cleanup, and zero-item behavior. Use genuine seams; don't add public test hooks or assert inaccessible buffers via contrived tests.
- Update all owned-result call sites to dispose, including tools/examples/tests. Run Fido2 and dependent caller tests, public-contract tests, and cross-vendor correctness/security review before shipping this follow-up.

## Completion criteria

- Each stage has a working baseline, completed fit review and passing settled-shape correctness review.
- No unintended protocol/cancellation/lifetime changes in the three main stages.
- Approved public names and return shapes are consistent across interfaces, implementations, current documentation and baselines.
- Built examples, command projects and integration sources use the new contracts.
- Hardware actions are selected explicitly; Security Domain resets and user-presence operations are not run merely because a smoke flag exists.
- Raw-response work reports its bounded ownership guarantee explicitly: the private response clone is cleared, the upstream borrowed buffer is untouched, and no end-to-end cleanup is claimed.

Abbreviations: API means application programming interface; PIN means personal identification number.
