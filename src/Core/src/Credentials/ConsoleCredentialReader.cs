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
using Yubico.YubiKit.Core.Utilities;

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
/// Interactive input uses a pooled character buffer that is cleared and returned after reading.
/// Redirected input uses <c>ReadLine</c>, which creates a managed string that this reader cannot clear.
/// Dispose the returned <see cref="IMemoryOwner{T}"/> to clear its independent credential bytes.
/// Confirmation compares byte lengths and then uses a fixed-time byte comparison.
/// </para>
/// <para>
/// Interactive input requires console key access; remote terminals may require a pseudo-terminal.
/// Disallowed or excess UTF-16 code units are ignored, and Backspace removes one code unit.
/// Redirected text instead rejects a whole line that violates the text policy. Redirected hex input
/// checks hex syntax and decoded byte length without applying the text length or character filter.
/// The selected encoding determines text conversion; the default UTF-8 encoding replaces malformed
/// surrogate input. These input options do not replace the consuming operation's applet-specific checks.
/// </para>
/// <para>
/// Cancellation is checked while waiting for an interactive key. Queued keys and redirected line reads
/// do not check the token. Empty input that reaches result allocation currently throws because the
/// pooled byte owner requires a positive size; use a positive minimum and a positive expected hex length.
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

        IMemoryOwner<byte>? first = null;
        IMemoryOwner<byte>? second = null;
        var returnFirst = false;

        try
        {
            // Same mode selection as ReadCredential: masked keystrokes on a console, line input when
            // redirected (Console.KeyAvailable throws there). The non-interactive warning prints once.
            var interactive = _console.IsInteractive;
            if (!interactive)
            {
                WriteNonInteractiveWarning();
            }

            first = ReadEntry(options.Prompt, interactive, options, cancellationToken);
            if (first is null)
            {
                return null;
            }

            second = ReadEntry(options.ConfirmPrompt, interactive, options, cancellationToken);
            if (second is null)
            {
                return null;
            }

            bool match = first.Memory.Length == second.Memory.Length &&
                         CryptographicOperations.FixedTimeEquals(first.Memory.Span, second.Memory.Span);
            if (!match)
            {
                _console.WriteLine("Credentials do not match.");
                return null;
            }

            returnFirst = true;
            return first;
        }
        finally
        {
            // Both entries are cleared on every path except the one that hands `first` to the caller.
            second?.Dispose();
            if (!returnFirst)
            {
                first?.Dispose();
            }
        }
    }

    private IMemoryOwner<byte>? ReadInteractive(CredentialReaderOptions options, CancellationToken cancellationToken)
    {
        _console.Write(options.Prompt);
        return ReadCredentialCore(options, cancellationToken);
    }

    private IMemoryOwner<byte>? ReadEntry(string prompt, bool interactive, CredentialReaderOptions options, CancellationToken cancellationToken)
    {
        _console.Write(prompt);
        return interactive
            ? ReadCredentialCore(options, cancellationToken)
            : ReadLineEntry(options);
    }

    private void WriteNonInteractiveWarning() =>
        _console.WriteLine("Warning: Running in non-interactive mode. Input will not be masked.");

    private IMemoryOwner<byte>? ReadNonInteractive(CredentialReaderOptions options)
    {
        WriteNonInteractiveWarning();
        _console.Write(options.Prompt);
        return ReadLineEntry(options);
    }

    private IMemoryOwner<byte>? ReadLineEntry(CredentialReaderOptions options)
    {
        string? line = _console.ReadLine();
        if (line is null)
        {
            return null;
        }

        try
        {
            // Hex mode validates in ParseHex. Text mode must apply the same policy the interactive
            // path enforces keystroke by keystroke; reject rather than filter, so the credential the
            // device receives is always exactly what was supplied.
            if (!options.IsHexMode && !SatisfiesPolicy(line.AsSpan(), options))
            {
                return null;
            }

            return ConvertToResult(line.AsSpan(), options);
        }
        finally
        {
            // Dropping this reference does not erase the managed string's contents.
            line = null;
        }
    }

    private bool SatisfiesPolicy(ReadOnlySpan<char> input, CredentialReaderOptions options)
    {
        if (input.Length < options.MinLength)
        {
            _console.WriteLine($"Input too short. Minimum {options.MinLength} characters required.");
            return false;
        }

        if (input.Length > options.MaxLength)
        {
            _console.WriteLine($"Input too long. Maximum {options.MaxLength} characters allowed.");
            return false;
        }

        foreach (char c in input)
        {
            if (char.IsControl(c) || (options.CharacterFilter is not null && !options.CharacterFilter(c)))
            {
                _console.WriteLine("Input contains characters that are not allowed.");
                return false;
            }
        }

        return true;
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
                // Poll cancellation while waiting; already queued keys bypass this wait.
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
                        _console.Write(options.MaskCharacter.ToString());
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