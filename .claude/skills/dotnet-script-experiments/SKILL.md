---
name: dotnet-script-experiments
description: Guide for creating standalone C# script files for experiments and testing. Use when creating experiment files, testing hypotheses, or prototyping functionality in isolation.
---

# .NET Script Experiments

Quick reference for creating standalone C# scripts that run with `dotnet run` without project files.

## Script Syntax

```csharp
#!/usr/bin/env dotnet run

#:package PackageName    // Optional: Add NuGet packages

using System;
using System.Security.Cryptography;

// Top-level statements - no Main method needed
Console.WriteLine("Hello from script!");
```

**Run:** `dotnet run myscript.cs` or `./myscript.cs` (Unix, after `chmod +x`)

## Quick Templates

### Crypto/Protocol Testing
```csharp
#!/usr/bin/env dotnet run
using System;
using System.Security.Cryptography;

// Header: Document what you're testing and why
Span<byte> input = stackalloc byte[16];
RandomNumberGenerator.Fill(input);

Span<byte> output = stackalloc byte[32];
SHA256.HashData(input, output);

Console.WriteLine($"Output: {Convert.ToHexString(output)}");
```

### Byte-Level Comparison
```csharp
#!/usr/bin/env dotnet run
using System;

ReadOnlySpan<byte> expected = [0x90, 0x00];
ReadOnlySpan<byte> actual = [0x90, 0x00];

bool matches = expected.SequenceEqual(actual);
Console.WriteLine(matches ? "✓ Match" : "✗ Differ");
```

### Memory Management Pattern
```csharp
#!/usr/bin/env dotnet run
using System;
using System.Buffers;

// ≤512 bytes: stackalloc
Span<byte> small = stackalloc byte[256];

// >512 bytes: ArrayPool
byte[] rented = ArrayPool<byte>.Shared.Rent(4096);
try
{
    Span<byte> buffer = rented.AsSpan(0, 4096);
    // Use buffer...
}
finally
{
    ArrayPool<byte>.Shared.Return(rented);
}
```

## Best Practices

✅ **Name with `experiment_` prefix:** `experiment_scp03_keys.cs`
✅ **Document purpose in header comment**
✅ **Use known test vectors when available**
✅ **Self-verify with comparisons**
✅ **Keep as runnable documentation**

## When to Create

- Testing protocol logic before integration
- Validating crypto operations
- Exploring Span/Memory patterns
- Reproducing bugs in isolation
- Comparing implementations byte-by-byte

