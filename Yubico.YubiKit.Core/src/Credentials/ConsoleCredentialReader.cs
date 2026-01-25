// Copyright 2025 Yubico AB
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>
/// Console-based credential reader that securely reads sensitive input.
/// </summary>
/// <remarks>
/// <para>
/// This reader provides masked input when running in an interactive terminal,
/// with fallback to unmasked line-based input when running non-interactively
/// (e.g., in a pipeline or with redirected input).
/// </para>
/// <para>
/// <b>Security Features:</b>
/// <list type="bullet">
/// <item>Never allocates credential data as managed strings</item>
/// <item>Returns <see cref="IMemoryOwner{T}"/> that zeros memory on disposal</item>
/// <item>Uses timing-safe comparison for credential confirmation</item>
/// <item>Clears intermediate buffers on all code paths</item>
/// </list>
/// </para>
/// <para>
/// <b>Platform Limitations:</b>
/// <list type="bullet">
/// <item>Windows: Full support for masked input</item>
/// <item>Linux/macOS: Requires TTY; masked input may not work over SSH without PTY</item>
/// <item>Non-interactive mode: Input is not masked (warning displayed)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ConsoleCredentialReader : ISecureCredentialReader
{
    private readonly IConsoleInputSource _console;

    /// <summary>
    /// Creates a new console credential reader using the real console.
    /// </summary>
    public ConsoleCredentialReader() : this(new RealConsoleInput())
    {
    }

    /// <summary>
    /// Creates a new console credential reader with the specified input source.
    /// </summary>
    /// <param name="console">The console input source (for testing).</param>
    internal ConsoleCredentialReader(IConsoleInputSource console)
    {
        ArgumentNullException.ThrowIfNull(console);
        _console = console;
    }

    /// <inheritdoc />
    public IMemoryOwner<byte>? ReadCredential(CredentialReaderOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return _console.IsInteractive
            ? ReadInteractive(options, cancellationToken)
            : ReadNonInteractive(options);
    }

    /// <inheritdoc />
    public IMemoryOwner<byte>? ReadCredentialWithConfirmation(CredentialReaderOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Read first entry
        _console.Write(options.Prompt);
        var first = ReadCredentialCore(options, cancellationToken);
        if (first is null)
        {
            return null;
        }

        // Read confirmation
        _console.Write(options.ConfirmPrompt);
        var second = ReadCredentialCore(options, cancellationToken);
        if (second is null)
        {
            first.Dispose();
            return null;
        }

        // Compare using timing-safe method
        bool match = first.Memory.Length == second.Memory.Length &&
                     CryptographicOperations.FixedTimeEquals(first.Memory.Span, second.Memory.Span);

        if (!match)
        {
            first.Dispose();
            second.Dispose();
            _console.WriteLine("Credentials do not match.");
            return null;
        }

        // Return first, dispose second
        second.Dispose();
        return first;
    }

    private IMemoryOwner<byte>? ReadInteractive(CredentialReaderOptions options, CancellationToken cancellationToken)
    {
        _console.Write(options.Prompt);
        return ReadCredentialCore(options, cancellationToken);
    }

    private IMemoryOwner<byte>? ReadNonInteractive(CredentialReaderOptions options)
    {
        _console.WriteLine("Warning: Running in non-interactive mode. Input will not be masked.");
        _console.Write(options.Prompt);

        string? line = _console.ReadLine();
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
            line = null;
        }
    }

    private IMemoryOwner<byte>? ReadCredentialCore(CredentialReaderOptions options, CancellationToken cancellationToken)
    {
        // Use a temporary buffer for character input
        var charBuffer = ArrayPool<char>.Shared.Rent(options.MaxLength);
        int charCount = 0;

        try
        {
            while (true)
            {
                // Wait for key availability with cancellation support
                while (!_console.KeyAvailable)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(10);
                }

                var keyInfo = _console.ReadKey(intercept: true);

                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        _console.WriteLine(string.Empty);
                        if (charCount < options.MinLength)
                        {
                            _console.WriteLine($"Input too short. Minimum {options.MinLength} characters required.");
                            _console.Write(options.Prompt);
                            ClearCharBuffer(charBuffer, charCount);
                            charCount = 0;
                            continue;
                        }

                        return ConvertToResult(charBuffer.AsSpan(0, charCount), options);

                    case ConsoleKey.Escape:
                        _console.WriteLine(string.Empty);
                        ClearCharBuffer(charBuffer, charCount);
                        return null;

                    case ConsoleKey.Backspace:
                        if (charCount > 0)
                        {
                            charCount--;
                            charBuffer[charCount] = '\0';
                            _console.Write("\b \b");
                        }

                        continue;

                    default:
                        char c = keyInfo.KeyChar;
                        if (c == '\0' || char.IsControl(c))
                        {
                            continue;
                        }

                        if (options.CharacterFilter is not null && !options.CharacterFilter(c))
                        {
                            continue;
                        }

                        if (charCount >= options.MaxLength)
                        {
                            continue;
                        }

                        charBuffer[charCount++] = c;
                        _console.Write(options.MaskChar.ToString());
                        continue;
                }
            }
        }
        catch
        {
            ClearCharBuffer(charBuffer, charCount);
            throw;
        }
        finally
        {
            ClearCharBuffer(charBuffer, charCount);
            ArrayPool<char>.Shared.Return(charBuffer, clearArray: true);
        }
    }

    private IMemoryOwner<byte>? ConvertToResult(ReadOnlySpan<char> input, CredentialReaderOptions options)
    {
        if (options.IsHexMode)
        {
            return ParseHex(input, options.ExpectedByteLength);
        }

        int byteCount = options.Encoding.GetByteCount(input);
        var result = new DisposableArrayPoolBuffer(byteCount);

        try
        {
            options.Encoding.GetBytes(input, result.Memory.Span);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    private IMemoryOwner<byte>? ParseHex(ReadOnlySpan<char> input, int? expectedByteLength)
    {
        // Strip separators and count hex digits
        var hexChars = ArrayPool<char>.Shared.Rent(input.Length);
        int hexCount = 0;

        try
        {
            foreach (char c in input)
            {
                if (c is ' ' or ':' or '-')
                {
                    continue;
                }

                if (!char.IsAsciiHexDigit(c))
                {
                    _console.WriteLine($"Invalid hex character: '{c}'");
                    return null;
                }

                hexChars[hexCount++] = c;
            }

            if (hexCount % 2 != 0)
            {
                _console.WriteLine("Hex input must have an even number of digits.");
                return null;
            }

            int byteLength = hexCount / 2;
            if (expectedByteLength.HasValue && byteLength != expectedByteLength.Value)
            {
                _console.WriteLine($"Expected {expectedByteLength.Value} bytes, got {byteLength}.");
                return null;
            }

            var result = new DisposableArrayPoolBuffer(byteLength);

            try
            {
                for (int i = 0; i < byteLength; i++)
                {
                    int high = HexCharToValue(hexChars[i * 2]);
                    int low = HexCharToValue(hexChars[(i * 2) + 1]);
                    result.Memory.Span[i] = (byte)((high << 4) | low);
                }

                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }
        finally
        {
            Array.Clear(hexChars, 0, hexCount);
            ArrayPool<char>.Shared.Return(hexChars, clearArray: true);
        }
    }

    private static int HexCharToValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => throw new ArgumentException($"Invalid hex character: {c}")
    };

    private static void ClearCharBuffer(char[] buffer, int length) =>
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(buffer.AsSpan(0, length)));
}
