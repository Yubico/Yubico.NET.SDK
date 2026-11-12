---
name: CryptoApis
description: Modern .NET Span-based cryptography APIs for the YubiKit SDK. READ WHEN computing hashes (SHA256/SHA1), HMAC, AES encryption/decryption, generating random bytes, RSA/ECDSA operations, replacing legacy SHA256.Create() patterns, zero-allocation crypto. Cross-references docs/MEMORY-MANAGEMENT.md, skills/domain-secure-credential-prompt.
---

# Cryptography APIs

Use .NET 10 destination-span APIs to avoid allocating a separate output array when suitable storage
already exists. Providers can still allocate internally. Dispose stateful cryptographic objects and
clear caller-owned secret buffers separately; neither a span nor `using` proves erasure of every copy.

## Canonical Patterns

These are API fragments, not one executable program. Supply the operation's key, IV, input, and
correctly sized destinations; the AES calls require the same explicitly configured key and an
appropriate IV. Their CBC mode alone supplies no authentication. Clear secret-bearing buffers in
the owning operation's `finally` scope.

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

Wrap stateful crypto objects in `using` so their provider resources are released deterministically:

```csharp
using var aes = Aes.Create();
using var rsa = RSA.Create();
using var hmac = new HMACSHA256(key);
// Dispose provider state; clear caller-owned key arrays and exports separately.
```

Core's public creator delegates cover the call sites that use them, not every direct BCL construction
or internal native helper. Configure them before concurrent use and treat each returned object as a
per-call owned resource. Actual provider support, native exports, and cleanup depend on the deployed
runtime/package. For example, the inspected .NET v10.0.0 macOS EC import extracts an additional private
representation when Q is absent; supplied X and Y coordinates skip that checked extraction route.
This source distinction supplies no installed-provider or native-erasure guarantee.

## Keys, encodings, and catalog metadata

Use `CryptographicDefinitions` to look up identities and structural relationships, not to choose a
default or establish provider/device/app-profile support. A registered COSE identifier and generic
curve applicability do not validate every algorithm/key pairing. Follow the consuming applet's profile.
Keep field/order bit lengths, scalar/seed widths, coordinate widths, and public encodings distinct.
SPKI encodes a public key; it is not an ECDSA signature format. DER ECDSA signatures and fixed-width
P1363/COSE concatenation are different representations selected by the operation's contract.

RSA/EC wrapper parameter arrays can alias key-owned mutable storage. Do not mutate them while the key
is in use. Encoded private-key output is another caller-owned secret buffer. AlgorithmIdentifier field
checks, selected RSA consistency relations, provider acceptance, and complete DER/mathematical key
validation are separate guarantees; current factories do not implement a uniform all-encoding validator.
In particular, current RSA admission includes a DP-width gate. The EC private encoder omits inner curve
parameters because the outer PKCS#8 AlgorithmIdentifier supplies them; RFC 5915 §3 nevertheless requires
those inner parameters for implementations conforming to that document. This remains a target-profile
limitation, not a claim that every external importer accepts the same subset.

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
