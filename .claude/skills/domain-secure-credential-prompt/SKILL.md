---
name: secure-credential-prompt
description: Use when handling PIN, PUK, passwords, or sensitive user input - ensures memory zeroing and proper lifetime management
---

# Secure Credential Prompt

## Overview

Provides patterns for handling sensitive user input (PIN, PUK, passwords, management keys) with proper memory hygiene. Credentials must never leak through managed heap, intermediate strings, or forgotten buffers.

**Core principle:** Sensitive data must be zeroed immediately after use, with no copies left in memory.

## Use when

**Use this skill when:**
- Prompting user for PIN, PUK, or password
- Handling management keys or other secrets
- Converting user input to `ReadOnlyMemory<byte>` for SDK APIs
- Creating credential input helpers or utilities

**Don't use when:**
- Handling non-sensitive input (usernames, serial numbers)
- Working with data that's already in SDK types

## Key Patterns

### 1. Stack-Allocated Buffers (≤512 bytes)

Best for synchronous operations with small credentials (PINs, PUKs typically 8 bytes).

```csharp
public static void VerifyPin(IPivSession session)
{
    Span<byte> pinBuffer = stackalloc byte[8];
    try
    {
        Console.Write("Enter PIN: ");
        int length = ReadSecretToSpan(pinBuffer);
        
        session.VerifyPin(pinBuffer[..length]);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(pinBuffer);
    }
}

private static int ReadSecretToSpan(Span<byte> buffer)
{
    int pos = 0;
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
            break;
        if (key.Key == ConsoleKey.Backspace && pos > 0)
        {
            pos--;
            continue;
        }
        if (pos < buffer.Length)
        {
            buffer[pos++] = (byte)key.KeyChar;
        }
    }
    Console.WriteLine();
    return pos;
}
```

### 2. ArrayPool for Larger Buffers

Use when buffer size exceeds 512 bytes or when async boundaries exist.

```csharp
public static async Task AuthenticateManagementKeyAsync(
    IPivSession session,
    CancellationToken ct = default)
{
    byte[]? keyBuffer = null;
    try
    {
        keyBuffer = ArrayPool<byte>.Shared.Rent(32); // 24 for 3DES, 32 for AES-256
        Span<byte> key = keyBuffer.AsSpan(0, 24);
        
        Console.Write("Enter management key (hex): ");
        int length = ReadHexToSpan(key);
        
        await session.AuthenticateManagementKeyAsync(
            key[..length].ToArray().AsMemory(), // SDK requires Memory<byte>
            ct);
    }
    finally
    {
        if (keyBuffer is not null)
        {
            CryptographicOperations.ZeroMemory(keyBuffer);
            ArrayPool<byte>.Shared.Return(keyBuffer, clearArray: false);
        }
    }
}
```

### 3. IDisposable Wrapper for Credential Lifetime

When credentials need to cross method boundaries, wrap in a disposable type.

```csharp
public sealed class SecureCredential : IDisposable
{
    private readonly byte[] _buffer;
    private readonly int _length;
    private bool _disposed;

    private SecureCredential(byte[] buffer, int length)
    {
        _buffer = buffer;
        _length = length;
    }

    public static SecureCredential FromConsole(string prompt, int maxLength = 64)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(maxLength);
        try
        {
            Console.Write(prompt);
            int length = ReadSecretToBuffer(buffer);
            
            // Transfer ownership - don't return to pool here
            return new SecureCredential(buffer, length);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(buffer);
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }

    public ReadOnlyMemory<byte> Value
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _buffer.AsMemory(0, _length);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        CryptographicOperations.ZeroMemory(_buffer);
        ArrayPool<byte>.Shared.Return(_buffer, clearArray: false);
        _disposed = true;
    }

    private static int ReadSecretToBuffer(byte[] buffer)
    {
        int pos = 0;
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace && pos > 0)
            {
                pos--;
                continue;
            }
            if (pos < buffer.Length)
            {
                buffer[pos++] = (byte)key.KeyChar;
            }
        }
        Console.WriteLine();
        return pos;
    }
}

// Usage:
using var pin = SecureCredential.FromConsole("Enter PIN: ");
session.VerifyPin(pin.Value.Span);
// Automatically zeroed when disposed
```

### 4. UTF-8 Encoding Without Intermediate Strings

**❌ WRONG - String stays in managed heap:**
```csharp
string password = Console.ReadLine()!;
byte[] bytes = Encoding.UTF8.GetBytes(password);
// 'password' string remains in memory indefinitely!
```

**✅ CORRECT - Direct to bytes:**
```csharp
Span<byte> buffer = stackalloc byte[64];
int length = 0;

while (true)
{
    var key = Console.ReadKey(intercept: true);
    if (key.Key == ConsoleKey.Enter) break;
    
    // Encode single char directly to span
    Span<char> charSpan = [key.KeyChar];
    length += Encoding.UTF8.GetBytes(charSpan, buffer[length..]);
}
```

## Common Mistakes

**❌ Using string for secrets:**
```csharp
string pin = Console.ReadLine()!; // String in heap forever
```

**❌ Forgetting ZeroMemory on error path:**
```csharp
Span<byte> pin = stackalloc byte[8];
int len = ReadPin(pin);
session.VerifyPin(pin[..len]); // If this throws, pin not zeroed!
```

**❌ Returning rented buffer without zeroing:**
```csharp
finally
{
    ArrayPool<byte>.Shared.Return(buffer); // NOT zeroed!
}
```

**❌ Converting to array unnecessarily:**
```csharp
byte[] pinArray = pinSpan.ToArray(); // Extra copy not zeroed
```

## Verification

Before completing credential-handling code:

- [ ] No `string` used for sensitive input
- [ ] All `Span<byte>` buffers wrapped in try/finally with `ZeroMemory`
- [ ] All `ArrayPool` rentals returned in finally block
- [ ] `ZeroMemory` called BEFORE returning to pool
- [ ] No `.ToArray()` calls that create unzeroed copies
- [ ] IDisposable types implement `ZeroMemory` in `Dispose()`

## Related Skills

- `security-guidelines` - Broader security patterns for YubiKey operations
- `domain-build` - Building projects with security-sensitive code
