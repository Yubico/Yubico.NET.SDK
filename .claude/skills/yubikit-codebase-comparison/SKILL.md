---
name: yubikit-codebase-comparison
description: Expert analysis and comparison of Java (yubikit-android) and C# (Yubico.NET.SDK) YubiKey codebases for porting functionality. Use when analyzing implementation differences, porting features, or comparing protocol implementations between Java and C# codebases.
license: Yubico Internal Use
---

# YubiKit Codebase Comparison Skill

This skill provides **domain expertise** for comparing and porting functionality between the Java-based yubikit-android library and the C#-based Yubico.NET.SDK (YubiKit).

## Codebases Under Analysis

### Primary Codebases
1. **yubikit-android (Java)** - `../yubikit-android/` (sibling directory)
   - Android-focused YubiKey library
   - Java implementation
   - Reference implementation for protocol details

2. **Yubico.NET.SDK (C# - Current)** - This repository
   - Modern .NET implementation
   - Target: .NET 10, C# 14
   - Active development in branches prefixed with `yubikit-*`

3. **Yubico.NET.SDK (C# - Legacy)** - `./legacy-develop/` (git worktree)
   - Git worktree with legacy implementation
   - Contains older patterns and implementations
   - Reference for existing C# approaches

## Domain Expertise

### Security Protocols
- **SCP (Secure Channel Protocol)**
  - SCP03 (AES-based secure messaging)
  - SCP11a (ECC with authentication)
  - SCP11b (ECC with authentication and key agreement)
  - SCP11c (ECC with key derivation)
  - Command/response wrapping and MAC verification
  - Session key derivation

### Authentication Standards
- **FIDO (Fast IDentity Online)**
  - FIDO U2F (Universal 2nd Factor)
  - FIDO2/WebAuthn
  - CTAP1 and CTAP2 protocols
  - Attestation formats and verification

- **PIV (Personal Identity Verification)**
  - PIV slots and key management
  - Certificate handling
  - Authentication, signing, and decryption operations
  - PIN/PUK management

### Communication Interfaces
- **Smart Card Interfaces**
  - PC/SC (Personal Computer/Smart Card)
  - SCARD API on Windows
  - CCID (Chip Card Interface Device)
  - ISO 7816-4 APDU command/response

- **HID (Human Interface Device)**
  - HID FIDO protocol
  - Report descriptors and usage pages
  - Platform-specific implementations

- **Platform Support**
  - Windows: SCARD, WinUSB
  - macOS: PC/SC, IOKit
  - Linux: PC/SC Lite, libusb

## Analysis Methodology

When comparing codebases or porting functionality, follow this forensic approach:

### Byte-Level Protocol Analysis
```
✅ ALWAYS start with actual wire format
✅ ALWAYS trace concrete examples (input → output bytes)
✅ ALWAYS document TLV structures, byte order, and flags
✅ ALWAYS verify encoding details before implementing
❌ NEVER implement based on conceptual understanding alone
❌ NEVER skip reading actual source code line-by-line
```

### Java → C# Translation Patterns

**Memory Management:**
```java
// Java: byte arrays everywhere
byte[] buffer = new byte[256];
byte[] response = apdu.getData();
```

```csharp
// C#: Modern span-based approach
Span<byte> buffer = stackalloc byte[256];  // ≤512 bytes
ReadOnlySpan<byte> response = apdu.Data;    // No allocation
```

**Crypto Operations:**
```java
// Java: Traditional APIs
MessageDigest sha = MessageDigest.getInstance("SHA-256");
byte[] hash = sha.digest(data);
```

```csharp
// C#: Modern span-based crypto
Span<byte> hash = stackalloc byte[32];
SHA256.HashData(data, hash);
```

**Null Safety:**
```java
// Java: Traditional null checks
if (connection != null && connection.isConnected()) {
    connection.transmit(apdu);
}
```

```csharp
// C#: Pattern matching with nullable reference types
if (connection is { IsConnected: true }) 
{
    await connection.TransmitAsync(apdu, ct);
}
```

### Platform Abstraction Differences

**Java (Android-focused):**
- USB Manager for device communication
- Android-specific permissions model
- NFC reader integration

**C# (Cross-platform):**
- Abstracted connection layer (`IConnection`, `IProtocol`)
- Platform-specific P/Invoke in `PlatformInterop/`
- Dependency injection throughout

### Common Porting Pitfalls

🚩 **Byte Order:** Java often uses `ByteBuffer` with explicit endianness
   - C#: Use `BinaryPrimitives.ReadUInt16BigEndian(span)` etc.

🚩 **Array Slicing:** Java creates new arrays; C# should use spans
   - Java: `Arrays.copyOfRange(data, 0, 5)`
   - C#: `data.AsSpan()[..5]` (zero allocation)

🚩 **Synchronous vs Async:** Java often uses sync I/O
   - C#: Prefer async with `CancellationToken` throughout

🚩 **Exception Patterns:** Java checked exceptions vs C# runtime exceptions
   - Map Java's protocol exceptions to appropriate C# exception hierarchy

🚩 **Data Encoding:** Java uses `StandardCharsets.UTF_8`
   - C#: `Encoding.UTF8.GetBytes(str, span)` (span-based)

## Expert Consultation Mode

When asked to compare implementations:

1. **Load both files** (Java and C#) and read completely
2. **Identify protocol flow** with step numbers
3. **Document byte-level differences** in tables
4. **Highlight security implications** of any changes
5. **Recommend C# implementation** following modern patterns
6. **Point out test cases** that would verify correctness

This skill provides the **domain knowledge**. For the **porting workflow** (phases, experimentation, documentation), see the **yubikit-porter agent**.
