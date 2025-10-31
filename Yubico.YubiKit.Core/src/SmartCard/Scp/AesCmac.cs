// Copyright (C) 2024 Yubico.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Buffers;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
/// Implements AES-CMAC (Cipher-based Message Authentication Code) as defined in NIST SP 800-38B.
/// </summary>
internal sealed class AesCmac : IDisposable
{
    private const int BlockSize = 16;                  // AES block size in bytes
    private const int Aes128KeyLength = 16;            // AES-128 key length in bytes
    private const byte PaddingByte = 0x80;             // ISO/IEC 9797-1 Padding Method 2
    private const byte ReductionPolynomial = 0x87;     // Rb for AES block size (NIST SP 800-38B)
    private readonly byte[] _key;
    private readonly byte[] _subkey1;
    private readonly byte[] _subkey2;
    private readonly byte[] _buffer;          // Buffers incomplete input data
    private readonly byte[] _cbcState;        // Tracks the running CBC-MAC state
    private int _bufferOffset;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesCmac"/> class.
    /// </summary>
    /// <param name="key">The AES key (must be 16 bytes for AES-128).</param>
    /// <exception cref="ArgumentException">Thrown if the key is not 16 bytes.</exception>
    public AesCmac(ReadOnlySpan<byte> key)
    {
        if (key.Length != Aes128KeyLength)
        {
            throw new ArgumentException($"AES-CMAC key must be {Aes128KeyLength} bytes", nameof(key));
        }

        _key = key.ToArray();
        _buffer = new byte[BlockSize];
        _cbcState = new byte[BlockSize];  // Starts as all zeros (initial IV)
        _subkey1 = new byte[BlockSize];
        _subkey2 = new byte[BlockSize];

        GenerateSubkeys();
    }

    /// <summary>
    /// Appends data to the CMAC computation.
    /// </summary>
    /// <param name="data">The data to append.</param>
    public void AppendData(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Console.WriteLine($"[CMAC DEBUG] AppendData called with {data.Length} bytes");
        Console.WriteLine($"[CMAC DEBUG] Data: {Convert.ToHexString(data)}");
        Console.WriteLine($"[CMAC DEBUG] Initial bufferOffset: {_bufferOffset}");

        int offset = 0;
        int remaining = data.Length;

        // Fill buffer if partially full
        if (_bufferOffset > 0)
        {
            int toCopy = Math.Min(BlockSize - _bufferOffset, remaining);
            data[offset..(offset + toCopy)].CopyTo(_buffer.AsSpan(_bufferOffset));
            _bufferOffset += toCopy;
            offset += toCopy;
            remaining -= toCopy;

            if (_bufferOffset == BlockSize)
            {
                ProcessBlock(_buffer);
                _bufferOffset = 0;
            }
        }

        // Process complete blocks, but always keep at least one block buffered for finalization
        while (remaining > BlockSize || (remaining == BlockSize && _bufferOffset == 0))
        {
            // If buffer is full, process it first
            if (_bufferOffset == BlockSize)
            {
                ProcessBlock(_buffer);
                _bufferOffset = 0;
            }

            // If we still have more than one block remaining, process directly from input
            if (remaining > BlockSize)
            {
                ProcessBlock(data[offset..(offset + BlockSize)]);
                offset += BlockSize;
                remaining -= BlockSize;
            }
            else
            {
                // This is the last complete block - buffer it for finalization
                data[offset..(offset + BlockSize)].CopyTo(_buffer);
                _bufferOffset = BlockSize;
                offset += BlockSize;
                remaining -= BlockSize;
                break;
            }
        }

        // Buffer remaining data (this handles the case where remaining < BlockSize)
        if (remaining > 0)
        {
            data[offset..(offset + remaining)].CopyTo(_buffer.AsSpan(_bufferOffset));
            _bufferOffset += remaining;
        }
    }

    /// <summary>
    /// Finalizes the CMAC computation, returns the MAC, and resets the state.
    /// </summary>
    /// <returns>The 16-byte CMAC value.</returns>
    public byte[] GetHashAndReset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Console.WriteLine($"[CMAC DEBUG] GetHashAndReset called");
        Console.WriteLine($"[CMAC DEBUG]   _bufferOffset: {_bufferOffset}");
        Console.WriteLine($"[CMAC DEBUG]   _buffer: {Convert.ToHexString(_buffer)}");
        Console.WriteLine($"[CMAC DEBUG]   _cbcState: {Convert.ToHexString(_cbcState)}");

        using var aes = Aes.Create();
        aes.Key = _key;

        // Prepare the final block according to RFC 4493
        Span<byte> lastBlock = stackalloc byte[BlockSize];

        if (_bufferOffset == BlockSize)
        {
            Console.WriteLine($"[CMAC DEBUG]   Complete final block - use K1");
            Console.WriteLine($"[CMAC DEBUG]   K1: {Convert.ToHexString(_subkey1)}");

            // Complete block: XOR with CBC state, then XOR with K1
            for (int i = 0; i < BlockSize; i++)
            {
                lastBlock[i] = (byte)(_cbcState[i] ^ _buffer[i] ^ _subkey1[i]);
            }
        }
        else
        {
            Console.WriteLine($"[CMAC DEBUG]   Incomplete final block - pad and use K2");
            Console.WriteLine($"[CMAC DEBUG]   K2: {Convert.ToHexString(_subkey2)}");

            // Incomplete block: pad with padding byte, then XOR with CBC state and K2
            for (int i = 0; i < _bufferOffset; i++)
            {
                lastBlock[i] = _buffer[i];
            }
            lastBlock[_bufferOffset] = PaddingByte;
            // Rest is already zero

            for (int i = 0; i < BlockSize; i++)
            {
                lastBlock[i] = (byte)(lastBlock[i] ^ _cbcState[i] ^ _subkey2[i]);
            }
        }

        Console.WriteLine($"[CMAC DEBUG]   lastBlock (ready for encryption): {Convert.ToHexString(lastBlock)}");

        // Final encryption
        Span<byte> iv = stackalloc byte[BlockSize];
        var result = new byte[BlockSize];
        var bytesWritten = aes.EncryptCbc(lastBlock, iv, result, PaddingMode.None);
        if (bytesWritten != BlockSize)
            throw new InvalidOperationException("Final CMAC encryption failed");

        Console.WriteLine($"[CMAC DEBUG]   Final MAC: {Convert.ToHexString(result)}");

        // Reset state
        Array.Clear(_buffer);
        Array.Clear(_cbcState);
        _bufferOffset = 0;

        return result;
    }

    /// <summary>
    /// Releases the resources used by this instance and securely zeroes all key material.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_key);
        CryptographicOperations.ZeroMemory(_subkey1);
        CryptographicOperations.ZeroMemory(_subkey2);
        CryptographicOperations.ZeroMemory(_buffer);
        CryptographicOperations.ZeroMemory(_cbcState);

        _disposed = true;
    }

    private void GenerateSubkeys()
    {
        using var aes = Aes.Create();
        aes.Key = _key;

        // Encrypt zero block to get L
        Span<byte> zero = stackalloc byte[BlockSize];
        var l = new byte[BlockSize];
        var bytesWritten = aes.EncryptEcb(zero, l, PaddingMode.None);
        if (bytesWritten != BlockSize)
            throw new InvalidOperationException("Subkey generation failed");

        // Generate K1 by left-shifting L and conditionally XORing with Rb
        LeftShiftOneAnd(l, _subkey1);

        // Generate K2 by left-shifting K1 and conditionally XORing with Rb
        LeftShiftOneAnd(_subkey1, _subkey2);

        CryptographicOperations.ZeroMemory(l);
    }

    private static void LeftShiftOneAnd(ReadOnlySpan<byte> input, Span<byte> output)
    {
        byte overflow = 0;
        for (int i = BlockSize - 1; i >= 0; i--)
        {
            output[i] = (byte)((input[i] << 1) | overflow);
            overflow = (byte)((input[i] >> 7) & 1);
        }

        // If MSB of input was 1, XOR with Rb
        if (overflow != 0)
        {
            output[BlockSize - 1] ^= ReductionPolynomial;
        }
    }

    private void ProcessBlock(ReadOnlySpan<byte> block)
    {
        Console.WriteLine($"[CMAC DEBUG] ProcessBlock called");
        Console.WriteLine($"[CMAC DEBUG]   Input block: {Convert.ToHexString(block)}");
        Console.WriteLine($"[CMAC DEBUG]   _cbcState before: {Convert.ToHexString(_cbcState)}");

        using var aes = Aes.Create();
        aes.Key = _key;

        // XOR block with previous CBC state
        Span<byte> temp = stackalloc byte[BlockSize];
        for (int i = 0; i < BlockSize; i++)
        {
            temp[i] = (byte)(_cbcState[i] ^ block[i]);
        }

        Console.WriteLine($"[CMAC DEBUG]   After XOR with state: {Convert.ToHexString(temp)}");

        // Encrypt to get new CBC state
        Span<byte> iv = stackalloc byte[BlockSize]; // Zero IV for ECB-like operation
        Span<byte> encrypted = stackalloc byte[BlockSize];
        var bytesWritten = aes.EncryptCbc(temp, iv, encrypted, PaddingMode.None);
        if (bytesWritten != BlockSize)
            throw new InvalidOperationException("Block encryption failed");

        // Update CBC state
        encrypted.CopyTo(_cbcState);
        Console.WriteLine($"[CMAC DEBUG]   _cbcState after: {Convert.ToHexString(_cbcState)}");
    }
}
