# Security Audit Report: Secure Credential Input

**PRD:** Secure PIN Input Implementation  
**Auditor:** security-auditor  
**Date:** 2026-01-25T18:30:00Z  
**Verdict:** 🔴 **FAIL** - Critical memory safety issues found

---

## Executive Summary

The implementation has **3 CRITICAL** vulnerabilities that must be fixed before production:

1. **Non-interactive mode leaks credentials** - String allocation violates core security requirement
2. **Missing ZeroMemory in critical paths** - `ClearCharBuffer()` not cryptographically secure
3. **ArrayPool buffer not zeroed** - `DisposableArrayPoolBuffer` missing `clearArray: true`

Additionally, **2 WARNINGS** require attention for defense-in-depth.

---

## Summary

| Severity | Count | Issues |
|----------|-------|--------|
| CRITICAL | 3 | String allocation, manual zeroing, missing clearArray |
| WARN | 2 | Char buffer pooling, hex buffer disposal |
| INFO | 1 | Defensive copy suggestion |

**Impact:** Credentials may persist in managed heap after use, violating the core security guarantee of this feature.

---

## Sensitive Data Inventory

| Data Type | Identified | Handling Specified | Status |
|-----------|------------|-------------------|--------|
| PIN (numeric) | ✅ | ✅ (partial) | ⚠️ String leak in non-interactive |
| PUK (numeric) | ✅ | ✅ (partial) | ⚠️ String leak in non-interactive |
| Passphrase (UTF-8) | ✅ | ✅ (partial) | ⚠️ String leak in non-interactive |
| Management Key (hex) | ✅ | ✅ (partial) | ⚠️ String leak in non-interactive |
| Derived Keys (PBKDF2) | ✅ | ✅ | ✅ Properly handled |

---

## CRITICAL Findings

### CRITICAL-001: Non-Interactive Mode Creates Immutable Strings

**File:** `ConsoleCredentialReader.cs:128`  
**Section:** `ReadNonInteractive()`

```csharp
string? line = _console.ReadLine();  // ❌ CRITICAL
if (line is null)
{
    return null;
}

try
{
    return ConvertToResult(line.AsSpan(), options);
}
finally
{
    // Can't zero a managed string, but we minimize exposure
    line = null;  // ❌ Does NOT zero memory
}
```

**Vulnerability:**  
The `Console.ReadLine()` call allocates an immutable .NET string containing the credential in plaintext. This string **cannot be zeroed** and will persist in the managed heap until garbage collected. Setting `line = null` only removes the reference; the memory remains.

**Impact:**
- Credentials remain in memory indefinitely
- Memory dumps will contain plaintext credentials
- Violates PRD requirement: "Never create a `string` containing the credential"
- Defeats the entire purpose of the secure credential reader

**Attack Vectors:**
1. Process memory dump captures credential
2. Debugger can inspect string in heap
3. Swap file may contain credential if memory is paged

**Recommendation:**
```csharp
private IMemoryOwner<byte>? ReadNonInteractive(CredentialReaderOptions options)
{
    _console.WriteLine("Warning: Running in non-interactive mode. Input will not be masked.");
    _console.Write(options.Prompt);

    // Allocate buffer for line-based input
    var charBuffer = ArrayPool<char>.Shared.Rent(options.MaxLength);
    var lineLength = 0;
    
    try
    {
        int ch;
        while ((ch = Console.Read()) >= 0 && ch != '\n')
        {
            if (ch == '\r') continue;  // Skip CR in CRLF
            if (lineLength >= options.MaxLength) break;
            charBuffer[lineLength++] = (char)ch;
        }
        
        if (lineLength == 0) return null;
        
        return ConvertToResult(charBuffer.AsSpan(0, lineLength), options);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(
            MemoryMarshal.AsBytes(charBuffer.AsSpan(0, lineLength)));
        ArrayPool<char>.Shared.Return(charBuffer, clearArray: true);
    }
}
```

---

### CRITICAL-002: Manual Zeroing Not Cryptographically Secure

**File:** `ConsoleCredentialReader.cs:315-321`  
**Section:** `ClearCharBuffer()`

```csharp
private static void ClearCharBuffer(char[] buffer, int length)
{
    for (int i = 0; i < length; i++)
    {
        buffer[i] = '\0';  // ❌ NOT guaranteed secure
    }
}
```

**Vulnerability:**  
Manual loop-based zeroing can be optimized away by the JIT compiler or runtime. The C# compiler may determine that the buffer is not read after this loop and eliminate it as "dead code."

**Impact:**
- Credential data may remain in memory after supposedly being zeroed
- Timing attacks may be possible if JIT optimization is inconsistent
- Violates PRD requirement to use `CryptographicOperations.ZeroMemory()`

**PRD Requirement (Line 453):**
```csharp
CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(charBuffer.AsSpan()));
```

**Recommendation:**
```csharp
private static void ClearCharBuffer(char[] buffer, int length)
{
    CryptographicOperations.ZeroMemory(
        MemoryMarshal.AsBytes(buffer.AsSpan(0, length)));
}
```

Or better - eliminate the helper entirely and inline `CryptographicOperations.ZeroMemory()` at call sites for clarity.

---

### CRITICAL-003: ArrayPool Return Missing clearArray Flag

**File:** `DisposableArrayPoolBuffer.cs:52`  
**Section:** `Dispose()`

```csharp
public void Dispose()
{
    if (_rentedBuffer == null) return;

    CryptographicOperations.ZeroMemory(_rentedBuffer);
    ArrayPool<byte>.Shared.Return(_rentedBuffer);  // ❌ Missing clearArray: true
    _rentedBuffer = null;
}
```

**Vulnerability:**  
While `CryptographicOperations.ZeroMemory()` is called before returning the buffer, the `ArrayPool.Return()` call defaults to `clearArray: false`. This means:
1. The pool may zero the buffer itself (implementation detail)
2. Defense-in-depth is violated (PRD line 416: "Defense-in-depth")

**PRD Requirement (Line 67, 416, 456):**
```csharp
ArrayPool<byte>.Shared.Return(buffer, clearArray: true);  // Defense-in-depth
```

**Impact:**
- If `ZeroMemory()` fails silently (e.g., exception), buffer may be reused with credential data
- Violates defense-in-depth principle
- Inconsistent with other parts of codebase

**Recommendation:**
```csharp
public void Dispose()
{
    if (_rentedBuffer == null) return;

    CryptographicOperations.ZeroMemory(_rentedBuffer);
    ArrayPool<byte>.Shared.Return(_rentedBuffer, clearArray: true);  // ✅ Defense-in-depth
    _rentedBuffer = null;
}
```

---

## WARNING Findings

### WARN-001: Char Buffer Should Use ArrayPool

**File:** `ConsoleCredentialReader.cs:148`  
**Section:** `ReadCredentialCore()`

```csharp
var charBuffer = ArrayPool<char>.Shared.Rent(options.MaxLength);
```

**Concern:**  
Character buffers contain sensitive data (the credential before encoding to bytes). Using `ArrayPool<char>` means the buffer is returned to a shared pool and may be reused by other parts of the application.

**Risk:**
- If `ZeroMemory()` fails, credential may leak to other code paths
- If buffer is not properly zeroed on exception, next renter gets sensitive data

**Current Mitigation:** Code does zero and use `clearArray: true` (lines 218).

**Recommendation:** This is acceptable given the defense-in-depth with both `ZeroMemory()` and `clearArray: true`. Document the security rationale in comments:

```csharp
// Use ArrayPool for char buffer - SECURITY: We zero with CryptographicOperations.ZeroMemory()
// AND return with clearArray: true for defense-in-depth
var charBuffer = ArrayPool<char>.Shared.Rent(options.MaxLength);
```

---

### WARN-002: Hex Parsing Allocates Intermediate Char Array

**File:** `ConsoleCredentialReader.cs:247-303`  
**Section:** `ParseHex()`

```csharp
var hexChars = ArrayPool<char>.Shared.Rent(input.Length);
int hexCount = 0;

try
{
    // ... parsing logic ...
    
    for (int i = 0; i < byteLength; i++)
    {
        int high = HexCharToValue(hexChars[i * 2]);
        int low = HexCharToValue(hexChars[(i * 2) + 1]);
        result.Memory.Span[i] = (byte)((high << 4) | low);  // ✅ Good
    }
    
    return result;
}
catch
{
    result.Dispose();  // ✅ Good - zeroes on exception
    throw;
}
finally
{
    Array.Clear(hexChars, 0, hexCount);  // ⚠️ Same issue as CRITICAL-002
    ArrayPool<char>.Shared.Return(hexChars, clearArray: true);  // ✅ Good
}
```

**Concern:**  
`Array.Clear()` is used instead of `CryptographicOperations.ZeroMemory()`. While the hex characters are less sensitive than the final bytes (already visible to the user during input), defense-in-depth suggests using secure zeroing.

**Recommendation:**
```csharp
finally
{
    CryptographicOperations.ZeroMemory(
        MemoryMarshal.AsBytes(hexChars.AsSpan(0, hexCount)));
    ArrayPool<char>.Shared.Return(hexChars, clearArray: true);
}
```

---

## INFO Findings

### INFO-001: Consider Defensive Copy in ConvertToResult

**File:** `ConsoleCredentialReader.cs:229-241`  
**Section:** `ConvertToResult()`

```csharp
int byteCount = options.Encoding.GetByteCount(input);
var result = new SecureMemoryOwner(byteCount);

try
{
    options.Encoding.GetBytes(input, result.Memory.Span);
    return result;
}
catch
{
    result.Dispose();  // ✅ Good
    throw;
}
```

**Observation:**  
The code correctly disposes the result on exception. However, `SecureMemoryOwner` constructor zeros the buffer (line 41), then `GetBytes()` writes to it. If `GetBytes()` throws partway through, the buffer may contain partial credential data.

**Current Behavior:** Buffer is zeroed on dispose, so this is safe.

**Suggestion:** Document this pattern in a comment to clarify security rationale:

```csharp
try
{
    // Encoding may throw - buffer will be zeroed by Dispose() in catch block
    options.Encoding.GetBytes(input, result.Memory.Span);
    return result;
}
```

---

## Checklist Results

| Category | Check | Result | Notes |
|----------|-------|--------|-------|
| **Memory Safety** | ||||
| | Sensitive data zeroing | ❌ | CRITICAL-002: Manual loop not secure |
| | No string conversion | ❌ | CRITICAL-001: ReadLine() allocates string |
| | Span/Memory preference | ✅ | Well done - uses spans throughout |
| | ArrayPool handling | ❌ | CRITICAL-003: Missing clearArray in DisposableArrayPoolBuffer |
| | Exception safety | ✅ | All paths properly dispose/zero |
| **YubiKey Security** | ||||
| | PIN retry behavior | N/A | Not applicable to input component |
| | Touch policy | N/A | Not applicable to input component |
| | Attestation validation | N/A | Not applicable to input component |
| **OWASP** | ||||
| | Input validation | ✅ | Character filters, length checks |
| | Auth required | N/A | Input component doesn't perform auth |
| | No secret logging | ✅ | No credential values logged |
| | Secure defaults | ✅ | Mask char, min/max lengths |
| **PRD Compliance** | ||||
| | Direct-to-buffer input | ⚠️ | Interactive: YES, Non-interactive: NO |
| | Automatic zeroing | ⚠️ | Partial - missing secure zeroing method |
| | Timing-safe comparison | ✅ | Line 101: CryptographicOperations.FixedTimeEquals |
| | No logging | ✅ | Only metadata logged (lengths, errors) |

---

## Example App (PinPrompt.cs) - PASS

The example app correctly uses the credential reader infrastructure:

**✅ Strengths:**
1. **Proper disposal pattern** (lines 56, 77, 94, 115, 132, 149, 169, 230, 269)
2. **PBKDF2 with secure iterations** (line 35: 600,000 iterations, OWASP compliant)
3. **Zeroing in helper methods** (line 351: `CryptographicOperations.ZeroMemory`)
4. **ArrayPool with clearArray** (line 352: `clearArray: true`)

**⚠️ Minor Concerns:**
1. **Line 248:** `RandomNumberGenerator.Fill(key)` then displayed as hex - user must securely save it
2. **Line 287:** Fixed salt for PBKDF2 - documented as "example only" (line 286), production should use unique salt
3. **Lines 305, 316:** `CreateFromDefault()` and `CreateFromSpan()` allocate temporary arrays that are not zeroed after copy

**Recommendation for CreateFromSpan:**
```csharp
private static IMemoryOwner<byte> CreateFromSpan(ReadOnlySpan<byte> source)
{
    var buffer = ArrayPool<byte>.Shared.Rent(source.Length);
    try
    {
        source.CopyTo(buffer);
        return new ArrayPoolMemoryOwner(buffer, source.Length);
    }
    catch
    {
        CryptographicOperations.ZeroMemory(buffer.AsSpan(0, source.Length));
        ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        throw;
    }
}
```

Note: If `source` is `PivSession.DefaultManagementKey` or other constant, it cannot be zeroed (readonly static data).

---

## ZeroMemory Coverage Analysis

**Command:** `grep -rn "ZeroMemory" Yubico.YubiKit.Core/src/Credentials/`

**Results:**
- `SecureMemoryOwner.cs:41` - ✅ Constructor zero
- `SecureMemoryOwner.cs:66` - ✅ Dispose zero
- `ConsoleCredentialReader.cs:NONE` - ❌ Uses manual `ClearCharBuffer()`

**Expected (from PRD):**
- Line 377: `CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(charBuffer.AsSpan()));`
- Line 416: `CryptographicOperations.ZeroMemory(buffer);`
- Line 454: `CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(charBuffer.AsSpan()));`

**Missing:** `ConsoleCredentialReader` does not use `CryptographicOperations.ZeroMemory()` - uses manual loop instead (CRITICAL-002).

---

## Verdict Justification

**FAIL** - Implementation has 3 CRITICAL security vulnerabilities:

1. **CRITICAL-001** violates the core requirement "No string allocation" (PRD line 33, 638)
2. **CRITICAL-002** violates the requirement to use `CryptographicOperations.ZeroMemory()` (PRD line 643)
3. **CRITICAL-003** violates defense-in-depth requirement (PRD line 647)

These issues **MUST** be fixed before the code can be considered production-ready. The non-interactive mode completely defeats the security purpose of this feature by allocating immutable strings.

**Impact on Security Posture:**
- **High:** Credentials in non-interactive mode remain in memory indefinitely
- **Medium:** Manual zeroing may be optimized away by JIT
- **Low:** Missing defense-in-depth in one location

**Required Actions:**
1. Rewrite `ReadNonInteractive()` to avoid `Console.ReadLine()` string allocation
2. Replace all instances of manual zeroing loops with `CryptographicOperations.ZeroMemory()`
3. Add `clearArray: true` to `DisposableArrayPoolBuffer.Dispose()`
4. Add security-focused comments explaining zeroing patterns

**After Fixes:** Re-audit to verify all CRITICAL issues resolved.

---

## Additional Recommendations

### 1. Add Debug Assertions
```csharp
#if DEBUG
[Conditional("DEBUG")]
private static void AssertZeroed(ReadOnlySpan<byte> buffer)
{
    foreach (byte b in buffer)
    {
        Debug.Assert(b == 0, "Buffer not properly zeroed!");
    }
}
#endif
```

### 2. Document Platform Limitations
The PRD correctly identifies platform limitations (lines 631-641). Consider adding runtime warnings:

```csharp
if (Debugger.IsAttached)
{
    _console.WriteLine("WARNING: Debugger attached. Credentials visible in memory inspector.");
}
```

### 3. Consider Memory Locking (Future Enhancement)
For maximum security, consider using `VirtualLock()` (Windows) or `mlock()` (Linux) to prevent credential pages from being swapped to disk. This is mentioned in PRD line 638 but not implemented.

---

## Sign-Off

**Security Auditor:** security-auditor agent  
**Audit Date:** 2026-01-25  
**Next Review Required:** After CRITICAL issues are fixed  

This audit is based on:
- PRD: `./docs/plans/secure-pin.md`
- Implementation files in `./Yubico.YubiKit.Core/src/Credentials/`
- Example usage in `./Yubico.YubiKit.Piv/examples/PivTool/Cli/Prompts/PinPrompt.cs`
- Security guidelines from `./.claude/skills/domain-security-guidelines/SKILL.md`
