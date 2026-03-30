---
name: SecurityPatterns
description: Anti-patterns to detect in code examples across languages. Universal patterns (SP1-SP3) plus language-specific variants. Used by Q8 checks in DocsAudit.
type: reference
---

# Security Anti-Patterns for Code Examples

Code examples in documentation must model correct security practices. These patterns flag violations.

**Source:** If the project has its own security guidelines doc (auto-discovered), those supplement these universal patterns.

---

## Universal Anti-Patterns (All Languages)

### SP1: String storage of sensitive data
**Detect:** PINs, passwords, keys, or tokens stored in immutable string types.
**Why:** Strings cannot be securely wiped in most languages (immutable in C#, Java, Python, JS; interned by runtime).

| Language | Bad | Good |
|----------|-----|------|
| C# | `string pin = "123456";` | `byte[] pin = new byte[] { ... };` |
| Java | `String password = "secret";` | `char[] password = ...;` then `Arrays.fill(password, '\0');` |
| Python | `pin = "123456"` | `pin = bytearray(b"123456")` then `pin[:] = b'\x00' * len(pin)` |
| Go | `pin := "123456"` | `pin := make([]byte, 6)` then zero with loop |
| Rust | Generally safe with `Zeroize` trait | Flag raw `String` for secrets without `zeroize` |

### SP2: Missing buffer/memory zeroing
**Detect:** Sensitive buffers used without explicit cleanup after use.
**Why:** Data persists in memory after reference goes out of scope.

| Language | Cleanup Method |
|----------|---------------|
| C# | `CryptographicOperations.ZeroMemory(buffer)` |
| Java | `Arrays.fill(charArray, '\0')` or `Arrays.fill(byteArray, (byte)0)` |
| Python | `bytearray[:] = b'\x00' * len(bytearray)` |
| Go | `for i := range buf { buf[i] = 0 }` |
| Rust | `zeroize::Zeroize` trait |
| TypeScript/JS | Manual loop (no built-in); `crypto.timingSafeEqual` for comparison |

### SP3: Missing exception-safe cleanup
**Detect:** Sensitive buffer cleanup not guaranteed on error paths.
**Why:** If an exception/panic occurs between collection and zeroing, data remains.

| Language | Pattern |
|----------|---------|
| C# | `try/finally` with `ZeroMemory()` in finally |
| Java | `try/finally` with `Arrays.fill()` in finally |
| Python | `try/finally` with zeroing in finally |
| Go | `defer` with zeroing function |
| Rust | `Drop` trait / `Zeroize` on drop |

---

## Language-Specific Anti-Patterns

### SP4: Deprecated security APIs
| Language | Anti-Pattern | Why |
|----------|-------------|-----|
| C# | `SecureString` | No longer recommended by Microsoft |
| Java | `java.security.Certificate` (old) | Use `java.security.cert.Certificate` |
| Python | `md5` / `sha1` for security purposes | Use `hashlib.sha256` minimum |
| JS/TS | `crypto.createCipher()` | Use `crypto.createCipheriv()` |

### SP5: Unbounded sensitive buffers
**Detect:** Sensitive data in dynamically-sized collections rather than pre-allocated fixed-size buffers.
**Why:** Resizing creates copies in memory.

| Language | Bad | Good |
|----------|-----|------|
| C# | `List<byte>` for key material | `new byte[KeySize]` |
| Java | `ArrayList<Byte>` | `new byte[KEY_SIZE]` |
| Python | Appending to `list` | Pre-allocated `bytearray(size)` |

### SP6: Long-lived sensitive data
**Detect:** Sensitive data stored in class fields, static variables, singletons, or cached beyond immediate use.
**Why:** Increases exposure window. Collect just before use, clear immediately after.
**Applies to all languages equally.**

---

## Scope of Q8 Detection

### What to scan
- All fenced code blocks in documentation files (language auto-detected from fence tag)
- Variable names suggesting sensitive data: `pin`, `puk`, `password`, `key`, `managementKey`, `secret`, `credential`, `privateKey`, `token`, `apiKey`, `passphrase`
- Method parameters receiving sensitive data

### What to skip
- Code blocks that are clearly protocol-level illustrations (hex dumps, wire format)
- Historical changelog entries (auto-detected by DiscoveryAgent)
- Prose-only mentions of security concepts (not code examples)
- Languages without fenced code blocks (inline backtick references)

### Project-specific guidelines
If the DiscoveryAgent found a security guidelines document in the project:
1. Read it and extract any additional anti-patterns beyond SP1-SP6
2. Apply those patterns to code examples in docs
3. Reference the project's guideline in findings (e.g., "Guideline: sensitive-data.md §2")

If no guidelines doc found:
- Apply only universal SP1-SP3 (always valid)
- Note in report: "No project-specific security guidelines found. Only universal patterns checked."

### Reporting
Q8 findings reference the specific SP pattern violated:

```
[Q8/SP2] fips-mode.md:103 — PIN byte array not zeroed after use
  Evidence: `byte[] newPin = new byte[] { ... }` used in TrySetPin, never cleared
  Guideline: sensitive-data.md §2 (or "Universal SP2" if no project guidelines)
  Suggested fix: Add try/finally with CryptographicOperations.ZeroMemory(newPin)
```

---

## Judgment Notes

Not every code example needs full security ceremony. Apply these guidelines:

1. **Instructional focus** — If the example's purpose is demonstrating a specific API (e.g., how to call `GenerateKeyPair`), a brief comment like `// Clear sensitive data after use` is acceptable instead of full try/finally boilerplate.
2. **PIN/password examples** — Short inline values are acceptable for illustration. Flag only if no mention of cleanup exists anywhere in the surrounding prose.
3. **Private key / cryptographic material** — These should always model correct security. Private key material in code examples without cleanup is always a Q8 finding regardless of instructional context.
4. **Severity scaling** — SP1 (strings for secrets) and SP3 (missing exception-safe cleanup for keys) are High. SP5/SP6 are Low for short examples.
