---
name: MemoryManagement
description: Span/Memory/ArrayPool decision rules and APDU buffer patterns for the YubiKit SDK. READ WHEN allocating byte buffers, working with APDU data, choosing between Span and Memory, sensitive data lifetime, ArrayPool rent/return, stackalloc sizing, zero-allocation hot paths, avoiding ToArray, LINQ on bytes. Cross-references skills/domain-secure-credential-prompt, skills/tool-codemapper.
---

# Memory Management

This is the JIT reference for buffer choices, APDU handling, and allocation anti-patterns. The Quick Reference in `CLAUDE.md` lists the mandates; this doc has the rationale, examples, and decision tree.

## Decision Tree

```
Need byte buffer?
├─ Synchronous?
│  ├─ ≤512 bytes? → Span<byte> with stackalloc ✅ BEST
│  └─ >512 bytes? → ArrayPool<byte>.Shared.Rent() ✅
├─ Async boundaries?
│  ├─ Temporary? → IMemoryOwner<byte> with MemoryPool ✅
│  └─ Parameter? → Memory<byte> ✅
└─ Must return/store?
   ├─ Caller provides? → Accept Span<byte> or Memory<byte> ✅
   └─ Must allocate? → new byte[] ⚠️ LAST RESORT
```

## Hierarchy (most preferred to least)

### 1. `Span<byte>` and `ReadOnlySpan<byte>` — BEST

For synchronous operations with stack-allocated or borrowed memory.

```csharp
// Zero allocation, stack-based
Span<byte> buffer = stackalloc byte[256];
ProcessApdu(buffer);

// Slicing without allocation
ReadOnlySpan<byte> header = apduData.AsSpan()[..5];
ReadOnlySpan<byte> body = apduData.AsSpan()[5..];

// Parameters for zero-copy
public void ProcessData(ReadOnlySpan<byte> data) { }
```

**Limitations:** cannot be used in async methods, cannot be stored in fields (ref struct), limit `stackalloc` to ≤512 bytes.

### 2. `Memory<byte>` and `ReadOnlyMemory<byte>`

When crossing async boundaries.

```csharp
public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken ct)
    => await stream.ReadAsync(buffer, ct);

// Convert to Span when sync context resumes
public async Task ProcessAsync(Memory<byte> data, CancellationToken ct)
{
    await SomeAsyncOperation();
    Span<byte> span = data.Span;
    ProcessData(span);
}

// IMemoryOwner for temporary buffers in async
using var owner = MemoryPool<byte>.Shared.Rent(4096);
Memory<byte> memory = owner.Memory[..actualSize];
await ProcessAsync(memory, ct);
```

### 3. `ArrayPool<T>` rented arrays

For temporary buffers >512 bytes.

```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    Span<byte> span = buffer.AsSpan(0, actualLength);
    ProcessData(span);
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}

// Zero sensitive data before returning
byte[] pinBuffer = ArrayPool<byte>.Shared.Rent(8);
try
{
    Span<byte> pin = pinBuffer.AsSpan(0, pinLength);
    // Use for PIN operations
}
finally
{
    CryptographicOperations.ZeroMemory(pinBuffer.AsSpan(0, pinLength));
    ArrayPool<byte>.Shared.Return(pinBuffer, clearArray: false); // already zeroed
}
```

Always use try/finally. Consider `clearArray: true` for defense-in-depth on sensitive data. Don't rent excessively large buffers.

### 4. Regular arrays — LAST RESORT

Only allocate when:
- Data must be returned and lifetime is unclear
- Storing in fields/properties
- Interop requires array type
- Collection initialization with known size

```csharp
// ❌ Allocates every call
public byte[] ProcessData(byte[] input) => new byte[input.Length];

// ✅ Use Span
public void ProcessData(ReadOnlySpan<byte> input, Span<byte> output) { }

// ✅ Or ArrayPool
public byte[] ProcessData(byte[] input)
{
    byte[] temp = ArrayPool<byte>.Shared.Rent(input.Length);
    try
    {
        // Process...
        byte[] result = new byte[actualLength];
        Array.Copy(temp, result, actualLength);
        return result;
    }
    finally { ArrayPool<byte>.Shared.Return(temp); }
}
```

## Anti-Patterns

### ❌ Unnecessary `.ToArray()`

```csharp
// ❌ BAD
byte[] data = someSpan.ToArray();
ProcessData(data);

// ✅ GOOD
ProcessData(someSpan);
```

### ❌ LINQ on byte spans

```csharp
// ❌ BAD
byte[] result = data.Select(b => (byte)(b ^ 0xFF)).ToArray();

// ✅ GOOD
Span<byte> result = stackalloc byte[data.Length];
for (int i = 0; i < data.Length; i++)
    result[i] = (byte)(data[i] ^ 0xFF);
```

### ❌ Forgetting to return rented arrays

```csharp
// ❌ BAD - memory leak
byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
ProcessData(buffer);
// Forgot to return!

// ✅ GOOD
byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
try { ProcessData(buffer); }
finally { ArrayPool<byte>.Shared.Return(buffer); }
```

## APDU and Protocol Buffers

Prefer Span for APDU data:

```csharp
public readonly struct CommandApdu
{
    private readonly ReadOnlyMemory<byte> _data;

    public ReadOnlySpan<byte> AsSpan() => _data.Span;

    public CommandApdu(ReadOnlySpan<byte> data) => _data = data.ToArray(); // only allocate for storage
}
```

Use Span slicing instead of `Take`/`Skip`:

```csharp
// ❌ BAD
byte[] header = apduData.Take(5).ToArray();
byte[] body = apduData.Skip(5).ToArray();

// ✅ GOOD
ReadOnlySpan<byte> apdu = apduData.AsSpan();
ReadOnlySpan<byte> header = apdu[..5];
ReadOnlySpan<byte> body = apdu[5..];
```

## Sensitive Data Lifetime

Cross-reference `.claude/skills/domain-secure-credential-prompt/SKILL.md` for the full PIN/key handling workflow. Key rules:

- `Span<byte>` on the stack → call `CryptographicOperations.ZeroMemory(span)` before scope exits
- `ArrayPool` rented buffer → zero before `Return(buffer, clearArray: false)`
- Sealed `IDisposable` class → zero in `Dispose()` (never store sensitive `byte[]` in a struct — copies can't all be zeroed)
- `using var` for crypto objects: `Aes.Create()`, `RSA.Create()`, `HMACSHA256` — keys auto-zeroed on dispose
