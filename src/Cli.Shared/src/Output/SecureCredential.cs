// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace Yubico.YubiKit.Cli.Shared.Output;

/// <summary>
/// Owns credential bytes for the shortest practical CLI lifetime and zeros them on disposal.
/// </summary>
/// <remarks>
/// Implements <see cref="IMemoryOwner{T}"/> so prompted credentials flow directly into SDK
/// surfaces that take ownership of a secret buffer, including
/// <c>ICredentialPrompt.RequestSecretAsync</c>. <see cref="Memory"/> is always sized
/// <b>exactly</b> to the secret: the SDK transmits the whole of it, so a longer buffer would
/// send trailing padding as part of the credential.
/// </remarks>
public sealed class SecureCredential : IMemoryOwner<byte>
{
    /// <summary>Sentinel returned by the read helpers when the user abandoned the prompt.</summary>
    private const int Declined = -1;

    private readonly byte[] _buffer;
    private readonly int _length;
    private bool _disposed;

    private SecureCredential(byte[] buffer, int length)
    {
        _buffer = buffer;
        _length = length;
    }

    /// <summary>
    /// Gets the credential bytes, sized exactly to the secret.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>DisposableArrayPoolBuffer.Memory</c> in Core, which is the shape SDK surfaces
    /// taking ownership of a secret expect. Callers must not mutate the contents.
    /// </remarks>
    public Memory<byte> Memory
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _buffer.AsMemory(0, _length);
        }
    }

    public static SecureCredential FromUtf8String(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Credential value cannot be empty.", nameof(value));
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        return new SecureCredential(bytes, bytes.Length);
    }

    /// <summary>
    /// Prompts for a credential, masking interactive input and reading a single line when
    /// standard input is redirected.
    /// </summary>
    /// <returns>
    /// The credential, or <see langword="null"/> if the user declined to supply one — either by
    /// pressing Escape, or by supplying an empty line or end-of-input on redirected input.
    /// </returns>
    /// <remarks>
    /// <see langword="null"/> means exactly one thing: no credential was supplied. It never
    /// signals an input error. This mirrors the <c>ICredentialPrompt</c> contract so console
    /// prompts and SDK prompts agree on what a declined credential looks like.
    /// </remarks>
    public static SecureCredential? Prompt(string label, int maxByteLength = 128)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxByteLength);

        var buffer = new byte[maxByteLength];

        try
        {
            var length = Console.IsInputRedirected
                ? ReadRedirectedInput(buffer, Console.OpenStandardInput())
                : ReadMaskedConsoleInput(label, buffer, () => Console.ReadKey(intercept: true));

            if (length <= 0)
            {
                CryptographicOperations.ZeroMemory(buffer);
                return null;
            }

            return new SecureCredential(buffer, length);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(buffer);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_buffer);
        _disposed = true;
    }

    internal byte[] DangerousGetBufferForTesting() => _buffer;

    internal static SecureCredential? FromConsoleKeysForTesting(IReadOnlyList<ConsoleKeyInfo> keys, int maxByteLength = 128)
    {
        var index = 0;
        var buffer = new byte[maxByteLength];
        var length = ReadMaskedConsoleInput(
            label: string.Empty,
            buffer,
            () => keys[index++],
            writePrompt: false);

        if (length <= 0)
        {
            CryptographicOperations.ZeroMemory(buffer);
            return null;
        }

        return new SecureCredential(buffer, length);
    }

    /// <summary>
    /// Reads a masked line of console input.
    /// </summary>
    /// <returns>
    /// The number of bytes written to <paramref name="buffer"/>, or <see cref="Declined"/> if
    /// the user pressed Escape to abandon the prompt.
    /// </returns>
    private static int ReadMaskedConsoleInput(
        string label,
        Span<byte> buffer,
        Func<ConsoleKeyInfo> readKey,
        bool writePrompt = true)
    {
        if (writePrompt)
        {
            Console.Error.Write($"{label}: ");
        }

        var length = 0;
        var characterCount = 0;
        Span<int> byteCounts = stackalloc int[buffer.Length];
        Span<char> chars = stackalloc char[1];

        while (true)
        {
            var key = readKey();
            if (key.Key is ConsoleKey.Enter)
            {
                if (writePrompt)
                {
                    Console.Error.WriteLine();
                }

                return length;
            }

            if (key.Key is ConsoleKey.Escape)
            {
                buffer[..length].Clear();
                if (writePrompt)
                {
                    Console.Error.WriteLine();
                }

                return Declined;
            }

            if (key.Key is ConsoleKey.Backspace)
            {
                if (characterCount > 0)
                {
                    var previousByteCount = byteCounts[--characterCount];
                    buffer.Slice(length - previousByteCount, previousByteCount).Clear();
                    length -= previousByteCount;
                }

                continue;
            }

            if (char.IsControl(key.KeyChar))
            {
                continue;
            }

            chars[0] = key.KeyChar;
            var byteCount = Encoding.UTF8.GetByteCount(chars);
            if (length + byteCount > buffer.Length)
            {
                throw new InvalidOperationException("Credential value is too long.");
            }

            length += Encoding.UTF8.GetBytes(chars, buffer[length..]);
            byteCounts[characterCount++] = byteCount;
        }
    }

    /// <summary>
    /// Reads one line of redirected input.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Terminates on line feed or end-of-input only. Treating a carriage return as a terminator
    /// would leave the line feed of a CRLF pair unread, and the <b>next</b> prompt in the same
    /// process would then see an empty line and report the credential as declined — breaking
    /// every flow that reads two secrets, such as confirmation and change-PIN.
    /// </para>
    /// <para>
    /// Carriage returns are discarded rather than buffered, so a credential of exactly
    /// <c>buffer.Length</c> bytes followed by CRLF is accepted instead of being rejected as
    /// over-long. A carriage return is a line-terminator artifact and is never treated as
    /// credential content.
    /// </para>
    /// </remarks>
    internal static int ReadRedirectedInput(Span<byte> buffer, Stream input)
    {
        var length = 0;

        while (true)
        {
            var value = input.ReadByte();
            if (value < 0 || value is '\n')
            {
                return length;
            }

            if (value is '\r')
            {
                continue;
            }

            if (length == buffer.Length)
            {
                throw new InvalidOperationException("Credential value is too long.");
            }

            buffer[length++] = (byte)value;
        }
    }
}