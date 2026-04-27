---
name: CryptoApis
description: Modern .NET Span-based cryptography APIs for the YubiKit SDK. READ WHEN computing hashes (SHA256/SHA1), HMAC, AES encryption/decryption, generating random bytes, RSA/ECDSA operations, replacing legacy SHA256.Create() patterns, zero-allocation crypto. Cross-references docs/MEMORY-MANAGEMENT.md, skills/domain-secure-credential-prompt.
---

# Cryptography APIs

Use modern .NET 8/9/10 Span-based APIs. They are zero-allocation, faster, and the `using var` pattern auto-zeroes key material on dispose.

## Canonical Patterns

```csharp
// Hashing
Span<byte> hash = stackalloc byte[32];
SHA256.HashData(inputData, hash);

// HMAC
Span<byte> hmac = stackalloc byte[32];
HMACSHA256.HashData(key, data, hmac);

// Random
Span<byte> random = stackalloc byte[16];
RandomNumberGenerator.Fill(random);

// AES
using var aes = Aes.Create();
aes.EncryptCbc(plaintext, iv, ciphertext, PaddingMode.PKCS7);
aes.DecryptCbc(ciphertext, iv, plaintext, PaddingMode.PKCS7);
```

## Avoid Legacy APIs

```csharp
// ❌ OLD - allocates a new byte[] every call
using var sha = SHA256.Create();
byte[] hash = sha.ComputeHash(data);

// ✅ NEW - zero allocation
Span<byte> hash = stackalloc byte[32];
SHA256.HashData(data, hash);
```

## Constant-Time Comparison

Comparison of MACs, signatures, or any secret-derived bytes MUST use a constant-time comparator to prevent timing attacks:

```csharp
// ✅ Prevents timing attacks
bool isValid = CryptographicOperations.FixedTimeEquals(expected, actual);

// ❌ Timing attack vulnerable
bool isValid = expected.SequenceEqual(actual);
```

## Disposable Crypto Objects

Always wrap stateful crypto objects in `using` so the underlying key material is zeroed on dispose:

```csharp
using var aes = Aes.Create();
using var rsa = RSA.Create();
using var hmac = new HMACSHA256(key);
// Keys automatically zeroed on dispose
```

## Buffer Sizing for Hashes

| Algorithm | Output bytes |
|---|---|
| SHA-1 | 20 |
| SHA-256 | 32 |
| SHA-384 | 48 |
| SHA-512 | 64 |
| HMAC-SHA256 | 32 |

Always size the output `Span<byte>` to the algorithm's natural output. The `HashData` family will throw if the destination is too small.

## See Also

- `docs/MEMORY-MANAGEMENT.md` — Span/stackalloc/ArrayPool rules used by all crypto patterns above
- `.claude/skills/domain-secure-credential-prompt/SKILL.md` — PIN handling lifetime
