// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using System.Text;

namespace Yubico.YubiKit.Cli.Shared.Output;

/// <summary>
/// Owns credential bytes for the shortest practical CLI lifetime and zeros them on disposal.
/// </summary>
public sealed class SecureCredential : IDisposable
{
    private readonly byte[] _buffer;
    private readonly int _length;
    private bool _disposed;

    private SecureCredential(byte[] buffer, int length)
    {
        _buffer = buffer;
        _length = length;
    }

    public ReadOnlyMemory<byte> Memory
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

    public static SecureCredential Prompt(string label, int maxByteLength = 128)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        if (maxByteLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxByteLength));
        }

        var buffer = new byte[maxByteLength];
        var length = 0;

        try
        {
            length = Console.IsInputRedirected
                ? ReadRedirectedInput(buffer)
                : ReadMaskedConsoleInput(label, buffer, () => Console.ReadKey(intercept: true));

            if (length == 0)
            {
                throw new ArgumentException("Credential value cannot be empty.", nameof(label));
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

    internal static SecureCredential FromConsoleKeysForTesting(IReadOnlyList<ConsoleKeyInfo> keys, int maxByteLength = 128)
    {
        var index = 0;
        var buffer = new byte[maxByteLength];
        var length = ReadMaskedConsoleInput(
            label: string.Empty,
            buffer,
            () => keys[index++],
            writePrompt: false);

        return new SecureCredential(buffer, length);
    }

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

    private static int ReadRedirectedInput(Span<byte> buffer)
    {
        var input = Console.OpenStandardInput();
        var length = 0;

        while (true)
        {
            var value = input.ReadByte();
            if (value < 0 || value is '\n' or '\r')
            {
                return length;
            }

            if (length == buffer.Length)
            {
                throw new InvalidOperationException("Credential value is too long.");
            }

            buffer[length++] = (byte)value;
        }
    }
}