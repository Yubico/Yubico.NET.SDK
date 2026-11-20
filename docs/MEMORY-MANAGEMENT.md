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

**Lifetime:** with C# 14, span/ref-struct locals can be used in synchronous segments of an async method,
provided they do not cross an `await` or `yield` suspension boundary. They cannot be stored in ordinary
class fields. Keep `stackalloc` bounded to ≤512 bytes and initialize every byte before reading it;
stack allocation alone does not promise zero-filled storage.

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

`ApduCommand` borrows its `ReadOnlyMemory<byte>` payload instead of cloning it. Copying that command
copies the view, so all copies refer to the same caller-owned storage. Keep it alive and unchanged until
the original exchange task is terminal; cancellation of an outer wait does not end the borrow. This
complete method borrows a raw session and clears the request buffer it allocates:

```csharp
using System.Security.Cryptography;

static async Task SendAsync(
    Yubico.YubiKit.Core.Sessions.RawSmartCardSession session, CancellationToken cancellationToken)
{
    // Illustrative request bytes: choose a valid command/payload for the selected applet.
    byte[] request = [0x01];
    try
    {
        var command = new Yubico.YubiKit.Core.Protocols.SmartCard.Apdu.ApduCommand(
            0x00, 0xCA, 0x00, 0x00, request);
        var response = await session.TransmitAndReceiveAsync(
            command, throwOnError: false, cancellationToken: cancellationToken);
        try
        {
            Console.WriteLine($"SW={response.SW:X4}, {response.Data.Length} bytes");
        }
        finally
        {
            response.Clear();
        }
    }
    finally
    {
        CryptographicOperations.ZeroMemory(request);
    }
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
- Sealed `IDisposable` class → use one reference owner for privately cloned secrets and zero in `Dispose()`.
  Copying an array-bearing struct copies references, not array contents: zeroing shared storage reaches
  its aliases. The hazard is shared mutation and ambiguous disposal/lifetime ownership, rather than an
  inability to reach those aliases. A borrowed view such as `ApduCommand.Data` has a different contract.
- `using var` for crypto objects → release provider-owned state deterministically. Separately clear
  caller arrays, exports, SDK temporaries, and other copies. Disposal is not a universal native-erasure proof.

`ApduResponse` owns a constructor copy; `Clear()` reaches that array and its views, including status
bytes, but leaves source/transport/decrypted-temporary owners separate. `Tlv.Value` borrows the TLV's
encoding; dispose the TLV/list and clear source, intermediate, and returned encodings separately. Use
the [TLV guide](usage/tlv-processing.md) for complete nested cleanup scopes.

Key parameter structs can expose mutable arrays owned by an RSA/EC wrapper. Treat them as aliases,
not fresh exports to mutate independently. `AsnWriter.Encode()` returns a copy and retains writer storage;
the inspected .NET v10.0.0 `Reset()` clears its written prefix. Current private-key encoders do not
reset that writer, so clearing only the returned encoding establishes no all-copy erasure. Provider
and actual native memory behavior require their own qualification.

Exceptional paths need the same owner-by-owner review. For example, current AES-CBC helpers allocate
key and IV copies before entering their cleanup `try`; a later allocation failure can bypass that
protected cleanup scope. Describe the actual scope rather than promising every local copy is always erased.
