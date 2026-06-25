# CLAUDE.md

Guidance for AI agents working in this repository. Yubico.NET.SDK (YubiKit) is a .NET 10 / C# 14 SDK for YubiKey devices. The 2.0 rewrite lives on `develop` and `main`; the source-of-truth root is the `yubikit` branch.

**IMPORTANT:** When working under a subproject (`src/Piv/`, `src/Fido2/`, `src/SecurityDomain/`, etc.) you MUST also read that subproject's `CLAUDE.md` if present. Subproject files contain module-specific patterns, test harness wiring, and protocol context that overrides nothing here but extends it.

## Project Overview

`net10.0`, `LangVersion=14.0`, nullable reference types enabled throughout. Reference docs for new platform features live in `docs/net10/`. Use new .NET 10 library + C# 14 features actively.

**Modules** (all under `src/` with `Yubico.YubiKit.` prefix stripped from directory names; assembly/namespace/DLL names retain the prefix):

| Module | Purpose |
|---|---|
| `Core/` | Device management, connections, APDU pipeline, platform interop |
| `Management/` | Device info, capability detection, firmware version |
| `Piv/` | PIV smart-card functionality |
| `Fido2/` | FIDO2/WebAuthn |
| `WebAuthn/` | Higher-level WebAuthn surface (delegates to Fido2 — duplicate ZERO behavior) |
| `Oath/` | TOTP/HOTP |
| `YubiOtp/` | Yubico OTP |
| `OpenPgp/` | OpenPGP card |
| `SecurityDomain/` | SCP03 secure channel, key management |
| `YubiHsm/` | YubiHSM 2 integration |
| `Cli.Shared/` | Shared CLI infra for example tools |
| `Tests.Shared/` | Multi-transport test harness |
| `Tests.TestProject/` | xUnit v3 test project layout |

**Platform interop** lives in `Core/PlatformInterop/{Windows,macOS,Linux}/` with P/Invoke declarations. `UnmanagedDynamicLibrary` + `SafeLibraryHandle` manage native loading; `SdkPlatformInfo` detects runtime platform.

## Quick Reference — Critical Rules

These are the always-loaded mandates. Each section ends with a JIT pointer to deeper context that agents should load on demand.

**Documentation & Research:**
- ✅ ALWAYS use Context7 MCP (`context7-query-docs`) for library/API docs, code patterns, framework usage — without waiting to be asked
- ✅ Use Perplexity AI (`.claude/skills/tool-perplexity-search/SKILL.md`) for current events, recent releases, up-to-date web information

**Skills to apply when coding here** (load via Skill tool when intent matches):
- `domain-build` — building/compiling .NET code (NEVER `dotnet build` directly)
- `domain-test` — running tests (NEVER `dotnet test` directly)
- `domain-yubikit-compare` — porting between Java YubiKit and C# SDK (byte-level analysis)
- `workflow-interface-refactor` — refactoring classes to interfaces for testability
- `workflow-tdd` — test-driven implementation
- `workflow-debug` — systematic root-cause analysis
- `tool-perplexity-search` — up-to-date web information

**Memory Management:**
- ✅ Sync + ≤512 bytes → `Span<byte>` with `stackalloc`
- ✅ Sync + >512 bytes → `ArrayPool<byte>.Shared.Rent()`
- ✅ Async → `Memory<byte>` or `IMemoryOwner<byte>`
- ❌ NEVER use `.ToArray()` unless data must escape scope
- ❌ NEVER forget to return `ArrayPool` buffers (use try/finally)

> Deep dive: `docs/MEMORY-MANAGEMENT.md` (load when allocating buffers, working with APDU data, choosing Span vs Memory, or seeing `.ToArray()` / LINQ on bytes).

**Security:**
- ✅ ALWAYS zero sensitive data: `CryptographicOperations.ZeroMemory()`
- ✅ ALWAYS dispose crypto objects: `using var aes = Aes.Create()`
- ✅ ALWAYS use `CryptographicOperations.FixedTimeEquals` for secret-derived comparisons
- ❌ NEVER log PINs, keys, or sensitive payloads
- ❌ NEVER use timing-vulnerable comparisons (`SequenceEqual`) on secrets
- ❌ NEVER store a privately-cloned `byte[]` of sensitive data in a `struct`. Struct copies each hold their own reference — you cannot zero all copies. Use a `sealed class` with `IDisposable` and call `ZeroMemory` in `Dispose()`.
- ✅ `ReadOnlyMemory<byte>` passthrough **is** safe in a `readonly record struct` — all copies reference the same caller-owned memory, so zeroing the source zeroes all views. Caller is responsible for zeroing after transmission. See `ApduCommand` as the canonical passthrough example.

> Deep dive: `.claude/skills/domain-security-guidelines/SKILL.md` and `.claude/skills/domain-secure-credential-prompt/SKILL.md` (load when handling PIN/PUK/keys, designing a sensitive-data type, or auditing for memory hygiene).

**Modern C#:**
- ✅ ALWAYS use `is null` / `is not null` (never `== null`)
- ✅ ALWAYS use switch expressions (never old switch statements)
- ✅ ALWAYS use file-scoped namespaces
- ✅ ALWAYS use collection expressions `[..]` (C# 12)
- ❌ NEVER suppress nullable warnings with `!` without justification

> Deep dive: `docs/CSHARP-PATTERNS.md` (load when designing new types, choosing property accessors, writing switch expressions, or using primary constructors / records).

**Code Quality:**
- ✅ ALWAYS follow `.editorconfig` (run `dotnet format` before commit)
- ✅ ALWAYS handle `CancellationToken` in async methods
- ✅ ALWAYS use `readonly` on fields that don't change
- ❌ NEVER use `#region` (split large classes instead)
- ❌ NEVER use exceptions for control flow

**Legacy Code Reference (Java/C# Implementations):**
- ✅ ALWAYS do forensic byte-level analysis before implementing
- ✅ ALWAYS read actual source code line-by-line, not just documentation
- ✅ ALWAYS trace through with concrete examples (input → output bytes)
- ✅ ALWAYS document the exact wire format/data structure before coding
- ❌ NEVER implement based on conceptual understanding alone
- ❌ NEVER skip verifying exact encoding details (TLV structure, byte order, flags)

> Deep dive: `.claude/skills/domain-yubikit-compare/SKILL.md` (load when porting from yubikit-android or comparing protocol implementations).

**Crypto APIs:**
- ✅ USE: `SHA256.HashData(data, outputSpan)` (Span-based)
- ❌ AVOID: `SHA256.Create().ComputeHash(data)` (allocates array)

> Deep dive: `docs/CRYPTO-APIS.md` (load when computing hashes/HMAC, AES, RNG, or replacing legacy crypto patterns).

**Logging:**
- ✅ Use static `YubiKitLogging.CreateLogger<T>()` — NEVER inject `ILogger`
- ❌ NEVER log PIN/key/credential values; log lengths and IDs only

> Deep dive: `docs/LOGGING.md` (load when adding logging, choosing log level, or configuring providers).

**Testing:**
- ✅ ALWAYS use `dotnet toolchain.cs test` (handles xUnit v2/v3 runner differences)
- ❌ NEVER use `dotnet test` directly (fails on xUnit v3 with wrong syntax)
- ❌ NEVER write validation-only or skipped-as-placeholder tests (see Test Philosophy below — verbatim policy)

> Deep dive: `docs/TESTING.md` (load when authoring tests, picking traits, or running integration tests).

**Codebase Orientation:**
- ✅ Run `codemapper .` to generate API surface maps (~1.5s for entire repo)
- ✅ Maps output to `./codebase_ast/` — one file per project (gitignored, agent-readable)
- ✅ Find symbols: `grep -rn "IYubiKey" ./codebase_ast/`
- ✅ Load context: `cat ./codebase_ast/Yubico.YubiKit.Core.txt`

> Deep dive: `.claude/skills/tool-codemapper/SKILL.md`.

## Build and Test

**Use the `domain-build` and `domain-test` skills.** They wrap `dotnet toolchain.cs` (Bullseye-based) with the right flags for xUnit v2/v3 detection, smoke filtering, and per-module scoping. NEVER call `dotnet build` or `dotnet test` directly.

Common patterns (skill files have full reference):

```bash
dotnet toolchain.cs build
dotnet toolchain.cs test --project WebAuthn --smoke
dotnet toolchain.cs test --project Piv --filter "FullyQualifiedName~Sign"
dotnet toolchain.cs pack --include-docs
```

## Architecture

`codemapper .` generates the full surface map. The non-obvious patterns:

- **Device discovery** — `IDeviceRepository` + `DeviceMonitorService` (hosted) + `DeviceListenerService` (background). Events flow as `IObservable<DeviceEvent>` via System.Reactive.
- **Connection abstraction** — `IConnection` base, `IProtocol` per transport (e.g., `ISmartCardProtocol`). Factories: `ISmartCardConnectionFactory`, `IProtocolFactory<T>`, `IYubiKeyFactory`.
- **APDU pipeline** — `IApduFormatter` (`Short`/`Extended`) → `IApduProcessor` decorators (`CommandChainingProcessor`, `ChainedResponseProcessor`, `ApduFormatProcessor`). Transparent size-limit + chaining handling.
- **Application sessions** — `ApplicationSession` base; protocol-specific sessions like `ManagementSession<TConnection>` are generic over connection type.
- **DI entry point** — `AddYubiKeyManagerCore()` in `src/Core/src/DependencyInjection.cs`.

### Type Selection: readonly struct vs struct vs class

Choose the appropriate type based on size, mutability, and usage patterns.

#### Use `readonly struct` (PREFERRED for value types)

**✅ When:**
- Type is ≤16 bytes (2 machine words)
- Immutable data (no fields change after construction)
- Passed by value frequently
- Used in hot paths (APDU processing, protocol handling)

**✅ Benefits:**
- Prevents defensive copies
- Clear immutability contract
- Potential stack allocation
- No GC pressure
```csharp
// ✅ BEST - Small, immutable, value semantics
public readonly struct StatusWord
{
    public StatusWord(byte sw1, byte sw2) => (SW1, SW2) = (sw1, sw2);
    
    public byte SW1 { get; }
    public byte SW2 { get; }
    public ushort Value => (ushort)((SW1 << 8) | SW2);
    
    public bool IsSuccess => Value == 0x9000;
}

// ✅ GOOD - Wraps small data
public readonly struct SlotNumber
{
    public SlotNumber(byte value) => Value = value;
    public byte Value { get; }
}

// ✅ GOOD - Protocol metadata
public readonly struct ApduHeader
{
    public ApduHeader(byte cla, byte ins, byte p1, byte p2)
    {
        CLA = cla;
        INS = ins;
        P1 = p1;
        P2 = p2;
    }
    
    public byte CLA { get; }
    public byte INS { get; }
    public byte P1 { get; }
    public byte P2 { get; }
}
```

**❌ Don't use for:**
- Types >16 bytes (expensive copying)
- Mutable data
- Types stored in collections (boxing overhead)

#### Use `struct` (mutable value types)

**⚠️ Use sparingly - only when mutation is truly needed:**
- Temporary computation buffers
- Performance-critical mutable state (rare)
```csharp
// ⚠️ Acceptable - Mutable builder pattern
public struct ApduBuilder
{
    private byte _cla;
    private byte _ins;
    private byte[] _data;
    
    public ApduBuilder WithCommand(byte cla, byte ins)
    {
        _cla = cla;
        _ins = ins;
        return this;
    }
    
    public CommandApdu Build() => new(_cla, _ins, 0, 0, _data);
}
```

**❌ Problems with mutable structs:**
- Defensive copies hurt performance
- Confusing semantics (copy on assign)
- Hard to reason about

**Better alternatives:**
```csharp
// ✅ BETTER - Immutable with builder
public readonly struct CommandApdu
{
    // ... immutable properties
    
    public static Builder CreateBuilder() => new();
    
    public class Builder  // Class builder for mutable state
    {
        private byte _cla;
        private byte _ins;
        
        public Builder WithCommand(byte cla, byte ins)
        {
            _cla = cla;
            _ins = ins;
            return this;
        }
        
        public CommandApdu Build() => new(_cla, _ins, 0, 0);
    }
}
```

#### Use `class` (reference types)

**✅ When:**
- Type is >16 bytes
- Contains reference types (arrays, strings, objects)
- Needs identity semantics (not value semantics)
- Represents entities or services
- Implements interfaces with mutable operations
```csharp
// ✅ GOOD - Large data structure
public class DeviceInfo
{
    public string SerialNumber { get; init; }
    public Version FirmwareVersion { get; init; }
    public FormFactor FormFactor { get; init; }
    public IReadOnlyList<Transport> AvailableTransports { get; init; }
    // ... more properties (>16 bytes total)
}

// ✅ GOOD - Contains managed resources
public sealed class SmartCardConnection : IConnection
{
    private readonly SafeHandle _handle;
    private readonly ILogger _logger;
    
    // ... implementation
}

// ✅ GOOD - Service/manager
public class YubiKeyManager : IYubiKeyManager
{
    private readonly IDeviceRepository _repository;
    private readonly IYubiKeyFactory _factory;
    
    // ... implementation
}
```

#### Special Case: APDU Types

For APDU-related types in this codebase:
```csharp
// ✅ CommandApdu - Use readonly struct if small OR class if contains byte[]
// Option A: Small header-only commands
public readonly struct CommandApduHeader
{
    public CommandApduHeader(byte cla, byte ins, byte p1, byte p2)
    {
        CLA = cla; INS = ins; P1 = p1; P2 = p2;
    }
    
    public byte CLA { get; }
    public byte INS { get; }
    public byte P1 { get; }
    public byte P2 { get; }
}

// Option B: Full APDU with data - use class (contains byte array)
public sealed class CommandApdu
{
    private readonly ReadOnlyMemory<byte> _data;
    
    public CommandApdu(ReadOnlySpan<byte> data)
    {
        _data = data.ToArray();
    }
    
    public ReadOnlySpan<byte> AsSpan() => _data.Span;
}

// ✅ ResponseApdu - Class (contains byte array)
public sealed class ResponseApdu
{
    private readonly ReadOnlyMemory<byte> _data;
    
    public ResponseApdu(ReadOnlySpan<byte> data)
    {
        _data = data.ToArray();
    }
    
    public ReadOnlySpan<byte> Data => _data.Span[..^2];
    public StatusWord SW => new(_data.Span[^2], _data.Span[^1]);
}
```

#### Size Guidelines

**16-byte threshold explained:**
```csharp
// ✅ 16 bytes - OK for readonly struct
public readonly struct DeviceId
{
    public Guid Value { get; }  // 16 bytes (128 bits)
}

// ✅ 8 bytes - Excellent for readonly struct
public readonly struct Timestamp
{
    public long Ticks { get; }  // 8 bytes
}

// ❌ 24+ bytes - Use class instead
public struct DeviceInfo  // BAD - too large, expensive to copy
{
    public Guid Id { get; }              // 16 bytes
    public long Timestamp { get; }        // 8 bytes
    public int FirmwareVersion { get; }   // 4 bytes
    // Total: 28 bytes - too large!
}

// ✅ Convert to class
public sealed class DeviceInfo
{
    public Guid Id { get; init; }
    public long Timestamp { get; init; }
    public int FirmwareVersion { get; init; }
}
```

#### Common Mistakes

**❌ BAD: Large struct with owned byte[] clone**
```csharp
public struct SensitivePayload  // 32+ bytes, owns private clone!
{
    private readonly byte[] _data;  // Each struct copy has its own reference — can't zero all copies
    public DateTime Timestamp { get; set; }
    public Guid CorrelationId { get; set; }
}
```

**❌ BAD: Mutable struct in collection**
```csharp
var list = new List<MutableStruct>();
list[0].Value = 10;  // Modifies COPY, not original!
```

**✅ GOOD: readonly struct or class**
```csharp
public readonly struct ApduHeader { /* 4 bytes, no sensitive payload */ }
public readonly record struct ApduCommand { /* ReadOnlyMemory<byte> passthrough — caller owns and zeroes */ }
public sealed class SessionKey : IDisposable { /* Owns private byte[] clone — zeroes in Dispose() */ }
```

#### Decision Matrix

| Criterion | readonly struct | struct | class |
|-----------|----------------|--------|-------|
| Size | ≤16 bytes | ≤16 bytes | Any size |
| Mutability | Immutable | Mutable | Either |
| Contains references | No | No | Yes |
| Hot path | ✅ Ideal | ⚠️ Careful | ✅ OK |
| Stack allocation | ✅ Yes | ✅ Yes | ❌ Heap only |
| Defensive copies | ✅ None | ❌ Many | N/A |
| GC pressure | ✅ None | ✅ None | ❌ Yes |

**When in doubt:** Use `class` for correctness, profile if performance critical, then consider `readonly struct` if ≤16 bytes and immutable.

## Testing

### Integration Test Strategy

**Run only what's affected.** Don't run the full integration suite unless you're finishing a module or touching shared infrastructure.

| Phase | What to run | Command |
|-------|------------|---------|
| **During development** | Smoke test on affected module only | `dotnet toolchain.cs -- test --integration --project Piv --smoke` |
| **Targeted check** | Specific test you touched | `dotnet toolchain.cs -- test --integration --project Oath --filter "FullyQualifiedName~Calculate"` |
| **Finishing a module** | Full integration for that module | `dotnet toolchain.cs -- test --integration --project Piv` |
| **Before PR** | Full integration for all affected modules | Run per-module, not all modules |

`--smoke` skips: `Slow` tests (RSA 3072/4096 keygen, 30+ sec each) and `RequiresUserPresence` tests (need physical touch). Mark any integration test that generates RSA 3072+ keys or has delays >5s with `[Trait(TestCategories.Category, TestCategories.Slow)]`.

### Test Philosophy: Value Over Coverage

**CRITICAL: Only write tests that provide real value. Don't create tests just to increase coverage metrics.**

#### ❌ DON'T Write These Tests

**Validation-Only Tests** - Tests that only check input validation provide minimal value:
```csharp
// ❌ BAD - Only tests ArgumentNullException.ThrowIfNull()
[Fact]
public void Method_WithNullInput_ThrowsArgumentNullException()
{
    var sut = new MyClass();
    Assert.Throws<ArgumentNullException>(() => sut.Process(null));
}

// ❌ BAD - Only tests basic type checking
[Fact]
public void Method_WithWrongType_ThrowsArgumentException()
{
    var sut = new MyClass();
    Assert.Throws<ArgumentException>(() => sut.Process(wrongType));
}
```

**Why these are bad:**
- They test framework behavior, not your code
- They give false sense of security ("95% coverage!")
- They don't catch real bugs
- They add maintenance burden without value

**Skipped Tests** - Don't create tests you know will never run:
```csharp
// ❌ BAD - Creating a test that will never be implemented
[Fact(Skip = "Requires mocking static methods which can't be done")]
public void Method_ActualBehavior_WorksCorrectly()
{
    // This will never run
}
```

**Why this is bad:**
- False advertising - looks like you have tests but they don't run
- Maintenance burden - someone has to read and understand why it's skipped
- If you can't test it, that's valuable information - document it, don't fake it

#### ✅ DO Write These Tests

**Behavior Tests** - Tests that verify actual functionality:
```csharp
// ✅ GOOD - Tests real protocol configuration logic
[Fact]
public void Configure_FirmwareBelow400_UsesNeoMaxSize()
{
    var protocol = new PcscProtocol(logger, connection);

    protocol.Configure(new FirmwareVersion(3, 5, 0));

    Assert.Equal(254, protocol.MaxApduSize);
}
```

**Integration Tests** - When unit testing is impossible/impractical:
```csharp
// ✅ GOOD - Tests actual SCP handshake with real hardware
[Fact]
public async Task CreateSession_WithSCP03_AuthenticatesAndCommunicates()
{
    var device = await YubiKey.FindFirstAsync();
    using var session = await ManagementSession.CreateAsync(
        device, scpKeyParams: scp03Keys);

    var deviceInfo = await session.GetDeviceInfoAsync();
    Assert.NotEqual(0, deviceInfo.SerialNumber);
}
```

#### When You Can't Write Meaningful Tests

If you can't test actual behavior due to:
- Static methods that can't be mocked
- Complex external dependencies
- Architectural limitations

**Then:**
1. **Document the limitation** clearly in code comments
2. **Point to integration tests** that exercise the code path
3. **Don't create fake tests** just to have coverage

**Example:**
```csharp
/// <summary>
///     ScpInitializer routes SCP initialization requests.
///
///     LIMITATION: Cannot unit test actual SCP initialization because
///     ScpState.Scp03InitAsync is a static method. This is tested via
///     integration tests in ManagementTests.CreateManagementSession_with_SCP03_DefaultKeys.
/// </summary>
public class ScpInitializer
{
    // Only test the routing logic, not the static method calls
}
```

#### Red Flags in Test Reviews

🚩 **"This test only validates inputs"** - Remove it or test real behavior
🚩 **"Skip = 'requires mocking X'"** - Either use integration tests or document why it's not testable
🚩 **"Test passes but doesn't verify functionality"** - You're testing the wrong thing
🚩 **"Need to mock 5+ dependencies to test this"** - Architectural problem, not a testing problem
🚩 **"This increases coverage from 85% to 90%"** - Coverage metrics are not the goal

#### Test Value Checklist

Before writing a test, ask:
- ✅ Does this test actual behavior or just input validation?
- ✅ Would this catch a real bug if behavior changed?
- ✅ Can I explain what value this test provides?
- ✅ Would I trust this test to catch regressions?

If you answered "no" to any of these, don't write the test.

> Test structure (UnitTests/IntegrationTests/TestProject), naming patterns, and cleanup recipes live in `docs/TESTING.md`. Trait usage and the multi-transport harness are also documented there.

## What NOT to Do

- ❌ String concatenation in loops — use `StringBuilder`
- ❌ Suppress nullable warnings with `!` without justification — use `?? throw`
- ❌ Exceptions for control flow — use `FirstOrDefault`, `TryGet`, etc.
- ❌ Public mutable state (`public byte[] Data;`) — use `{ get; init; }` or `ReadOnlyMemory<byte>`
- ❌ `#region` — split the class instead
- ❌ `var` when the type isn't obvious from the right-hand side

> Examples for each: `docs/CSHARP-PATTERNS.md` ("What NOT to Do" section).

## Git Workflow

- Base branch for PRs: **`develop`** (not `main`)
- Only commit files YOU created or modified in the current session
- Stage explicitly with `git add path/to/file` — NEVER `git add .` or `git add -A`

Full rules: `docs/COMMIT_GUIDELINES.md`. Skill: `.claude/skills/git-commit/SKILL.md`.

## Pre-Commit Checklist

1. ✅ `git status` — only your files staged
2. ✅ Build clean: `dotnet toolchain.cs build`
3. ✅ Tests pass: `dotnet toolchain.cs test`
4. ✅ Formatted: `dotnet format`
5. ✅ No nullable warnings
6. ✅ Sensitive data zeroed (`ZeroMemory` / `Dispose`)
7. ✅ No unnecessary allocations in hot paths
8. ✅ Modern C# (`is null`, switch expressions, file-scoped namespaces)
9. ✅ EditorConfig followed
