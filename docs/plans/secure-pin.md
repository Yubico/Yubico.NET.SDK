# Plan: Secure Credential Input for YubiKit SDK

## Problem Statement

Current PIN/credential input in the example app uses Spectre.Console's `TextPrompt<string>`, which returns immutable .NET strings that cannot be securely zeroed from memory. Sensitive credentials may persist in the managed heap until garbage collection, creating a security vulnerability.

**Current flow:**
```
User types → string buffer → return string → convert to byte[] → zero byte[] → string still in memory ❌
```

**Desired flow:**
```
User types → byte[] buffer directly → return IMemoryOwner<byte> → use → dispose → zeroed ✅
```

## Requirements

### Functional Requirements

1. **Direct-to-buffer input**: Read keystrokes directly into a `byte[]` without string intermediary
2. **Masked display**: Show `*` (or configurable mask) for each character entered
3. **Standard editing**: Support backspace to delete characters
4. **Submission**: Enter key submits input
5. **Cancellation**: Escape key cancels and returns null (with buffer zeroed)
6. **Length validation**: Configurable min/max length with real-time feedback
7. **Character filtering**: Optional filter (e.g., digits only for PIN, hex chars only)
8. **Prompt text**: Display customizable prompt before input area
9. **Cross-platform**: Work on Windows, Linux, macOS terminals

### Security Requirements

1. **No string allocation**: Never create a `string` containing the credential
2. **Bounded buffer**: Use fixed-size buffer to prevent unbounded growth
3. **Automatic zeroing**: Return `IMemoryOwner<byte>` that zeros on dispose
4. **Zero on cancel**: If user cancels, zero buffer before returning
5. **Zero on error**: Any exception path must zero the buffer
6. **No logging**: Never log credential values, only metadata (length, validation result)
7. **Timing-safe comparison**: Provide helper for comparing credentials

### API Requirements

1. **SDK-level component**: Place in `Yubico.YubiKit.Core` for reuse across all applications
2. **Interface-based**: Define `ISecureCredentialReader` for testability/mocking
3. **Async support**: Support cancellation tokens
4. **Options pattern**: Use `CredentialReaderOptions` record with `with` syntax for customization
5. **Integration ready**: Easy to use from console apps, potentially extendable to GUI
6. **Testability**: Inject `IConsoleInputSource` for unit testing without interactive terminal

## Proposed API Design

### Core Interface

```csharp
namespace Yubico.YubiKit.Core.Credentials;

/// <summary>
/// Reads sensitive credentials directly into secure memory without string allocation.
/// </summary>
public interface ISecureCredentialReader
{
    /// <summary>
    /// Reads a credential from the console.
    /// </summary>
    /// <param name="options">Configuration options for the prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Memory owner containing the credential bytes, or null if cancelled.</returns>
    /// <remarks>
    /// The returned IMemoryOwner MUST be disposed to zero the credential from memory.
    /// </remarks>
    IMemoryOwner<byte>? ReadCredential(CredentialReaderOptions options, CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for credential reading.
/// </summary>
public sealed class CredentialReaderOptions
{
    /// <summary>Prompt text displayed before input area.</summary>
    public string Prompt { get; init; } = "Enter credential";
    
    /// <summary>Character to display for each typed character. Use '\0' for invisible.</summary>
    public char MaskCharacter { get; init; } = '*';
    
    /// <summary>Minimum credential length.</summary>
    public int MinLength { get; init; } = 1;
    
    /// <summary>Maximum credential length.</summary>
    public int MaxLength { get; init; } = 64;
    
    /// <summary>Character encoding for converting keystrokes to bytes.</summary>
    public Encoding Encoding { get; init; } = Encoding.UTF8;
    
    /// <summary>Optional filter predicate for allowed characters.</summary>
    public Func<char, bool>? CharacterFilter { get; init; }
    
    /// <summary>Whether to show length hint (e.g., "[3/8]").</summary>
    public bool ShowLengthHint { get; init; } = false;
    
    /// <summary>Error message for minimum length violation.</summary>
    public string MinLengthError { get; init; } = "Too short";
    
    /// <summary>Error message for maximum length violation.</summary>
    public string MaxLengthError { get; init; } = "Maximum length reached";
    
    // Preset factories
    public static CredentialReaderOptions ForPin() => new()
    {
        Prompt = "Enter PIN",
        MinLength = 6,
        MaxLength = 8,
        CharacterFilter = char.IsAsciiDigit,
        ShowLengthHint = true
    };
    
    public static CredentialReaderOptions ForPuk() => new()
    {
        Prompt = "Enter PUK", 
        MinLength = 6,
        MaxLength = 8,
        CharacterFilter = char.IsAsciiDigit,
        ShowLengthHint = true
    };
    
    public static CredentialReaderOptions ForPassphrase() => new()
    {
        Prompt = "Enter passphrase",
        MinLength = 1,
        MaxLength = 128
    };
    
    /// <summary>
    /// Preset for hex-encoded keys (e.g., PIV management key).
    /// Accepts hex digits and common separators (space, colon, hyphen).
    /// </summary>
    /// <param name="byteLength">Expected key length in bytes (e.g., 24 for 3DES).</param>
    public static CredentialReaderOptions ForHexKey(int byteLength)
    {
        int hexCharLength = byteLength * 2;
        return new()
        {
            Prompt = $"Enter {byteLength}-byte hex key ({hexCharLength} hex digits)",
            MinLength = hexCharLength,
            MaxLength = hexCharLength + (hexCharLength / 2), // Allow separators
            CharacterFilter = c => char.IsAsciiHexDigit(c) || c is ' ' or ':' or '-',
            ShowLengthHint = true,
            IsHexMode = true,
            ExpectedByteLength = byteLength
        };
    }
    
    /// <summary>Whether input is hex mode (strips separators, converts to bytes).</summary>
    public bool IsHexMode { get; init; } = false;
    
    /// <summary>Expected byte length for hex mode validation.</summary>
    public int? ExpectedByteLength { get; init; }
}
```

### Secure Memory Owner

**Note:** Reuse existing `DisposableArrayPoolBuffer` from `Yubico.YubiKit.Core/src/Utils/` instead of creating new class. Add `IMemoryOwner<byte>` interface to it:

```csharp
// Modify existing class in Yubico.YubiKit.Core/src/Utils/DisposableArrayPoolBuffer.cs
public sealed class DisposableArrayPoolBuffer : IDisposable, IMemoryOwner<byte>
{
    // Existing implementation already:
    // - Uses ArrayPool<byte>.Shared
    // - Calls CryptographicOperations.ZeroMemory on dispose
    // - Tracks actual length vs buffer size
    
    // Just add IMemoryOwner<byte> interface (already compatible)
}
```

For cases where we need to return owned memory without ArrayPool (e.g., hex conversion result), use internal helper:

```csharp
/// <summary>
/// Internal memory owner for non-pooled buffers. Users see only IMemoryOwner&lt;byte&gt;.
/// </summary>
internal sealed class SecureMemoryOwner : IMemoryOwner<byte>
{
    private readonly byte[] _buffer;
    private readonly int _length;
    private bool _disposed;
    
    internal SecureMemoryOwner(byte[] buffer, int length)
    {
        _buffer = buffer;
        _length = length;
    }
    
    public Memory<byte> Memory => _disposed 
        ? throw new ObjectDisposedException(nameof(SecureMemoryOwner))
        : _buffer.AsMemory(0, _length);
    
    public void Dispose()
    {
        if (!_disposed)
        {
            CryptographicOperations.ZeroMemory(_buffer);
            _disposed = true;
        }
    }
}
```

### Console Input Abstraction (for Testability)

```csharp
/// <summary>
/// Abstraction over console input for testability.
/// </summary>
internal interface IConsoleInputSource
{
    ConsoleKeyInfo ReadKey(bool intercept);
    bool IsInteractiveTerminal { get; }
}

internal sealed class RealConsoleInput : IConsoleInputSource
{
    public ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);
    
    public bool IsInteractiveTerminal
    {
        get
        {
            if (Console.IsInputRedirected) return false;
            try
            {
                _ = Console.KeyAvailable;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}

/// <summary>
/// Mock input source for unit tests.
/// </summary>
internal sealed class MockConsoleInput : IConsoleInputSource
{
    private readonly Queue<ConsoleKeyInfo> _keys = new();
    
    public bool IsInteractiveTerminal => true;
    
    public void EnqueueKeys(params ConsoleKeyInfo[] keys)
    {
        foreach (var key in keys) _keys.Enqueue(key);
    }
    
    public ConsoleKeyInfo ReadKey(bool intercept) => _keys.Dequeue();
}
```

### Console Implementation

```csharp
/// <summary>
/// Console-based secure credential reader.
/// </summary>
/// <remarks>
/// <para><b>Security Limitations:</b></para>
/// <list type="bullet">
/// <item>OS-level console buffers temporarily store keystrokes before ReadKey() retrieves them</item>
/// <item>Terminal emulators, SSH sessions, and input method editors introduce additional buffer layers</item>
/// <item>This provides application-level protection but cannot control OS or hardware buffers</item>
/// </list>
/// <para><b>Platform Support:</b></para>
/// <list type="bullet">
/// <item>Requires interactive terminal (TTY). Falls back to line-based input in non-interactive mode.</item>
/// <item>Non-interactive environments (Docker, CI/CD, piped input) use fallback without masking.</item>
/// </list>
/// </remarks>
public sealed class ConsoleCredentialReader : ISecureCredentialReader
{
    private readonly IConsoleInputSource _inputSource;
    
    public ConsoleCredentialReader() : this(new RealConsoleInput()) { }
    
    internal ConsoleCredentialReader(IConsoleInputSource inputSource)
    {
        _inputSource = inputSource;
    }
    
    public IMemoryOwner<byte>? ReadCredential(CredentialReaderOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        // Platform detection: use interactive or fallback mode
        if (!_inputSource.IsInteractiveTerminal)
        {
            return ReadNonInteractive(options, cancellationToken);
        }
        
        return ReadInteractive(options, cancellationToken);
    }
    
    public IMemoryOwner<byte>? ReadCredentialWithConfirmation(
        CredentialReaderOptions options,
        CancellationToken cancellationToken = default)
    {
        var first = ReadCredential(options, cancellationToken);
        if (first is null) return null;
        
        try
        {
            var confirmOptions = options with { Prompt = "Confirm " + options.Prompt };
            var second = ReadCredential(confirmOptions, cancellationToken);
            if (second is null)
            {
                return null; // first disposed in finally
            }
            
            try
            {
                // CRITICAL: Timing-safe comparison to prevent timing attacks
                if (!CryptographicOperations.FixedTimeEquals(first.Memory.Span, second.Memory.Span))
                {
                    Console.WriteLine("  Credentials do not match");
                    return null;
                }
                
                // Match - return first, prevent disposal
                var result = first;
                first = null;
                return result;
            }
            finally
            {
                second?.Dispose();
            }
        }
        finally
        {
            first?.Dispose();
        }
    }
    
    private IMemoryOwner<byte>? ReadInteractive(CredentialReaderOptions options, CancellationToken cancellationToken)
    {
        Console.Write($"{options.Prompt}: ");
        
        var maxBytes = options.Encoding.GetMaxByteCount(options.MaxLength);
        var buffer = ArrayPool<byte>.Shared.Rent(maxBytes);
        var charBuffer = ArrayPool<char>.Shared.Rent(options.MaxLength);
        var charCount = 0;
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var keyInfo = _inputSource.ReadKey(intercept: true);
                
                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    return null;
                }
                
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    
                    if (charCount < options.MinLength)
                    {
                        // Clear input and re-prompt (matches sudo/passwd behavior)
                        Console.WriteLine($"  {options.MinLengthError}");
                        Console.Write($"{options.Prompt}: ");
                        charCount = 0;
                        CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(charBuffer.AsSpan()));
                        continue;
                    }
                    
                    return ConvertToResult(charBuffer.AsSpan(0, charCount), options, buffer);
                }
                
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (charCount > 0)
                    {
                        charCount--;
                        charBuffer[charCount] = '\0';
                        Console.Write("\b \b");
                    }
                    continue;
                }
                
                var ch = keyInfo.KeyChar;
                
                if (options.CharacterFilter is not null && !options.CharacterFilter(ch))
                {
                    continue;
                }
                
                if (charCount >= options.MaxLength)
                {
                    continue;
                }
                
                charBuffer[charCount++] = ch;
                Console.Write(options.MaskCharacter);
            }
            
            return null;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(charBuffer.AsSpan()));
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);  // Defense-in-depth
            ArrayPool<char>.Shared.Return(charBuffer, clearArray: true);
        }
    }
    
    private IMemoryOwner<byte>? ReadNonInteractive(CredentialReaderOptions options, CancellationToken cancellationToken)
    {
        // Fallback for non-TTY: line-based input without masking
        Console.Write($"{options.Prompt} (no masking - non-interactive terminal): ");
        
        var charBuffer = ArrayPool<char>.Shared.Rent(options.MaxLength);
        var maxBytes = options.Encoding.GetMaxByteCount(options.MaxLength);
        var buffer = ArrayPool<byte>.Shared.Rent(maxBytes);
        
        try
        {
            var line = Console.ReadLine();
            if (line is null || cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            
            if (line.Length < options.MinLength)
            {
                Console.WriteLine($"  {options.MinLengthError}");
                return null;
            }
            
            // Copy to char buffer (avoid keeping string reference)
            var charCount = Math.Min(line.Length, options.MaxLength);
            line.AsSpan(0, charCount).CopyTo(charBuffer);
            
            return ConvertToResult(charBuffer.AsSpan(0, charCount), options, buffer);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(charBuffer.AsSpan()));
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            ArrayPool<char>.Shared.Return(charBuffer, clearArray: true);
        }
    }
    
    private static IMemoryOwner<byte> ConvertToResult(
        ReadOnlySpan<char> input, 
        CredentialReaderOptions options, 
        byte[] workBuffer)
    {
        byte[] result;
        
        if (options.IsHexMode)
        {
            result = ParseHex(input, options.ExpectedByteLength!.Value);
        }
        else
        {
            var byteCount = options.Encoding.GetBytes(input, workBuffer);
            result = new byte[byteCount];
            
            try
            {
                workBuffer.AsSpan(0, byteCount).CopyTo(result);
            }
            catch
            {
                // CRITICAL: Zero result if exception during copy
                CryptographicOperations.ZeroMemory(result);
                throw;
            }
        }
        
        return new SecureMemoryOwner(result, result.Length);
    }
    
    private static byte[] ParseHex(ReadOnlySpan<char> input, int expectedBytes)
    {
        // Strip separators (space, colon, hyphen)
        Span<char> cleaned = stackalloc char[input.Length];
        var cleanedCount = 0;
        
        foreach (var c in input)
        {
            if (c is ' ' or ':' or '-') continue;
            
            if (!char.IsAsciiHexDigit(c))
            {
                throw new FormatException($"Invalid hex character: '{c}'");
            }
            
            cleaned[cleanedCount++] = c;
        }
        
        if (cleanedCount != expectedBytes * 2)
        {
            throw new ArgumentException(
                $"Expected {expectedBytes * 2} hex digits, got {cleanedCount}");
        }
        
        return Convert.FromHexString(cleaned[..cleanedCount]);
    }
}
```

### Usage Example

```csharp
// In application code
var reader = new ConsoleCredentialReader();

using var pin = reader.ReadCredential(CredentialReaderOptions.ForPin());
if (pin is null)
{
    Console.WriteLine("Cancelled");
    return;
}

// Use the PIN
await session.VerifyPinAsync(pin.Memory);
// pin.Dispose() automatically zeros the memory
```

## Integration with Example App

Update `PinPrompt.cs` to use the new secure reader:

```csharp
public static class PinPrompt
{
    private static readonly ISecureCredentialReader Reader = new ConsoleCredentialReader();
    
    public static IMemoryOwner<byte>? GetPin(string prompt = "Enter PIN")
    {
        return Reader.ReadCredential(CredentialReaderOptions.ForPin() with { Prompt = prompt });
    }
    
    public static IMemoryOwner<byte>? GetPinWithDefault(string prompt = "PIN")
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[blue]{prompt}:[/]")
                .AddChoices(["Use default", "Enter custom PIN"]));
        
        if (choice == "Use default")
        {
            // Return default in SecureMemoryOwner
            var defaultPin = Encoding.UTF8.GetBytes("123456");
            return new SecureMemoryOwner(defaultPin, defaultPin.Length);
        }
        
        return Reader.ReadCredential(CredentialReaderOptions.ForPin());
    }
}
```

## Tasks

### Phase 1: Core Infrastructure (Yubico.YubiKit.Core)

- [ ] Create `Yubico.YubiKit.Core/src/Credentials/` directory
- [ ] Add `IMemoryOwner<byte>` interface to existing `DisposableArrayPoolBuffer`
- [ ] Implement `ISecureCredentialReader` interface with `ReadCredential()` and `ReadCredentialWithConfirmation()`
- [ ] Implement `CredentialReaderOptions` with preset factories (`ForPin`, `ForPuk`, `ForPassphrase`, `ForHexKey`)
- [ ] Implement internal `SecureMemoryOwner` for non-pooled buffers
- [ ] Implement internal `IConsoleInputSource` and `RealConsoleInput` for testability
- [ ] Implement `ConsoleCredentialReader` with platform detection and fallback
- [ ] Implement hex parsing with separator stripping and validation
- [ ] Add unit tests using `MockConsoleInput` for all input scenarios
- [ ] Add unit tests for `SecureMemoryOwner` disposal and zeroing
- [ ] Add unit tests for hex parsing edge cases (separators, case, invalid chars)

### Phase 2: Example App Migration

- [ ] Update `PinPrompt.cs` to use new secure reader
- [ ] Update all menu call sites to use `IMemoryOwner<byte>` pattern
- [ ] Remove string-based credential handling
- [ ] Add `GetManagementKey()` using `ForHexKey(24)`
- [ ] Test all credential flows work correctly

### Phase 3: Documentation & Polish

- [ ] XML documentation for all public APIs
- [ ] Add platform limitation remarks to `ConsoleCredentialReader`
- [ ] Add example code in remarks
- [ ] Update SDK_PAIN_POINTS.md with resolution

### Phase 4: Security Verification

- [ ] Audit: All code paths zero buffers (grep for ZeroMemory usage)
- [ ] Audit: All ArrayPool returns use `clearArray: true`
- [ ] Audit: No credential values logged
- [ ] Audit: Timing-safe comparison used in confirmation
- [ ] Audit: Exception paths zero allocated result buffers
- [ ] Run `dotnet build.cs test` - all tests pass

## Design Decisions (Resolved 2026-01-25)

1. **Enter on invalid length**: ✅ Show error message and let user continue typing (don't cancel)

2. **Hex input**: ✅ Add `CredentialReaderOptions.ForHexKey(int byteLength)` preset that:
   - Filters to hex chars only (0-9, a-f, A-F)
   - Validates length is even
   - Converts hex → raw bytes on return
   - Use case: Management keys (48 hex chars → 24 bytes)

3. **Confirmation prompts**: ✅ Add `ReadCredentialWithConfirmation()` method to interface that:
   - Prompts twice ("Enter" then "Confirm")
   - Uses timing-safe comparison
   - Returns null if mismatch (with both buffers zeroed)

4. **GUI extensibility**: ✅ Keep sync-only for now. Console is inherently sync. If GUI needed later, create `IAsyncSecureCredentialReader` interface.

5. **DI integration**: ✅ Simple instantiation (`new ConsoleCredentialReader()`). No DI registration. Apps can wrap in DI if desired.

## Security Considerations

### Unavoidable Platform Limitations

- **OS keyboard buffer**: Terminals buffer keystrokes at OS/kernel level before `Console.ReadKey()` retrieves them. This is unavoidable at application level.
- **Terminal emulator buffers**: SSH, tmux, screen, and IME (Input Method Editors) introduce additional buffer layers.
- **Memory dumps**: If process crashes, credential may appear in dump. Zeroing reduces the exposure window.
- **Swap file**: If system swaps memory to disk, credential may persist. Consider `mlock()`/`VirtualLock()` for production hardening.
- **Screen capture**: Mask character doesn't protect against screen recording - user responsibility.
- **Accessibility tools**: Screen readers and magnification tools may capture input.
- **Debuggers**: If running under debugger, credential visible in memory inspector. Consider warning if `Debugger.IsAttached`.

### Mitigations Implemented

- **No string allocation**: Credentials never stored as immutable .NET strings
- **Bounded buffers**: Fixed-size allocation prevents unbounded growth
- **Zero on all paths**: try/finally ensures zeroing even on exceptions
- **ArrayPool clearArray**: Double-zero with `clearArray: true` as defense-in-depth
- **Timing-safe comparison**: `CryptographicOperations.FixedTimeEquals()` prevents timing attacks on confirmation
- **Platform fallback**: Graceful degradation for non-interactive terminals

## Notes

- This replaces the current `PinPrompt.GetCredential()` which uses Spectre.Console strings
- Spectre.Console can still be used for non-sensitive prompts (slot selection, etc.)
- The `IMemoryOwner<byte>` pattern aligns with existing SDK memory management
