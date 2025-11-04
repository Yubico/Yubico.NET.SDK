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

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
///     Manages static SCP03 keys and derives session keys from them.
/// </summary>
public sealed class StaticKeys : IDisposable
{
    // Key Derivation Constants
    private const byte DerivationTypeSEnc = 0x04; // Derivation type for S-ENC (session encryption key)
    private const byte DerivationTypeSMac = 0x06; // Derivation type for S-MAC (session MAC key)
    private const byte DerivationTypeSRMac = 0x07; // Derivation type for S-RMAC (session response MAC key)
    private const byte DerivationDataSeparator = 0x00; // Separator byte in derivation data
    private const byte DerivationDataCounter = 0x01; // Counter byte in derivation data
    private const short SessionKeyLengthBits = 128; // Session key length in bits (128-bit AES)
    private const int ContextLength = 16; // Context length in bytes
    private const int DerivationDataPrefixLength = 11; // Number of zero bytes at start of derivation data

    private static readonly byte[] DefaultKeyBytes =
        [0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F];

    private byte[]? _dek;
    private bool _disposed;

    private byte[]? _enc;
    private byte[]? _mac;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StaticKeys" /> class.
    /// </summary>
    /// <param name="enc">The static encryption key (must be 16 bytes).</param>
    /// <param name="mac">The static MAC key (must be 16 bytes).</param>
    /// <param name="dek">The optional static DEK (must be 16 bytes if provided).</param>
    /// <exception cref="ArgumentException">Thrown if any key is not 16 bytes.</exception>
    public StaticKeys(ReadOnlySpan<byte> enc, ReadOnlySpan<byte> mac, ReadOnlySpan<byte> dek = default)
    {
        if (enc.Length != 16) throw new ArgumentException("ENC key must be 16 bytes", nameof(enc));
        if (mac.Length != 16) throw new ArgumentException("MAC key must be 16 bytes", nameof(mac));
        if (dek.Length != 0 && dek.Length != 16)
            throw new ArgumentException("DEK must be 16 bytes if provided", nameof(dek));

        _enc = enc.ToArray();
        _mac = mac.ToArray();
        _dek = dek.Length == 16 ? dek.ToArray() : null;
    }

    /// <summary>
    ///     Gets the static encryption key.
    /// </summary>
    internal ReadOnlySpan<byte> Enc => _enc;

    /// <summary>
    ///     Gets the static MAC key.
    /// </summary>
    internal ReadOnlySpan<byte> Mac => _mac;

    /// <summary>
    ///     Gets the static DEK (Data Encryption Key), if available.
    /// </summary>
    internal ReadOnlySpan<byte> Dek => _dek;

    #region IDisposable Members

    /// <summary>
    ///     Releases the resources used by this instance and securely zeroes all key material.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        if (_enc != null)
        {
            CryptographicOperations.ZeroMemory(_enc);
            _enc = null;
        }

        if (_mac != null)
        {
            CryptographicOperations.ZeroMemory(_mac);
            _mac = null;
        }

        if (_dek != null)
        {
            CryptographicOperations.ZeroMemory(_dek);
            _dek = null;
        }

        _disposed = true;
    }

    #endregion

    /// <summary>
    ///     Returns the default SCP03 key set.
    /// </summary>
    /// <returns>A <see cref="StaticKeys" /> instance with default keys.</returns>
    public static StaticKeys GetDefaultKeys() => new(DefaultKeyBytes, DefaultKeyBytes, DefaultKeyBytes);

    /// <summary>
    ///     Derives session keys from the static keys using the provided context.
    /// </summary>
    /// <param name="context">The 16-byte context (host challenge || card challenge).</param>
    /// <returns>The derived session keys.</returns>
    /// <exception cref="ArgumentException">Thrown if context is not 16 bytes.</exception>
    internal SessionKeys Derive(ReadOnlySpan<byte> context)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (context.Length != ContextLength)
            throw new ArgumentException($"Context must be {ContextLength} bytes", nameof(context));

        Span<byte> senc = stackalloc byte[ContextLength];
        Span<byte> smac = stackalloc byte[ContextLength];
        Span<byte> srmac = stackalloc byte[ContextLength];

        // Derive S-ENC from ENC
        DeriveKey(_enc, DerivationTypeSEnc, context, SessionKeyLengthBits, senc);

        // Derive S-MAC and S-RMAC from MAC
        DeriveKey(_mac, DerivationTypeSMac, context, SessionKeyLengthBits, smac);
        DeriveKey(_mac, DerivationTypeSRMac, context, SessionKeyLengthBits, srmac);

        // DEK is NOT derived - it's passed through as-is (used for PUTKEY operations)
        // This matches the Java implementation

        try
        {
            return _dek is not null
                ? new SessionKeys(senc, smac, srmac, _dek)
                : new SessionKeys(senc, smac, srmac);
        }
        finally
        {
            // Zero the stack-allocated key material
            CryptographicOperations.ZeroMemory(senc);
            CryptographicOperations.ZeroMemory(smac);
            CryptographicOperations.ZeroMemory(srmac);
        }
    }

    /// <summary>
    ///     Derives a key using AES-CMAC according to SCP03 specification.
    /// </summary>
    /// <param name="key">The base key.</param>
    /// <param name="derivationType">The derivation type byte.</param>
    /// <param name="context">The derivation context.</param>
    /// <param name="lengthBits">The desired key length in bits.</param>
    /// <param name="output">The output buffer (must be at least lengthBits/8 bytes).</param>
    internal static void DeriveKey(ReadOnlySpan<byte> key, byte derivationType, ReadOnlySpan<byte> context,
        short lengthBits, Span<byte> output)
    {
        if (output.Length < lengthBits / 8) throw new ArgumentException("Output buffer too small", nameof(output));

        // Build derivation data: 11 zero bytes || derivationType || 0x00 || length(2 bytes BE) || 0x01 || context
        // This matches the Java implementation structure exactly
        Span<byte> derivationData = stackalloc byte[DerivationDataPrefixLength + 1 + 1 + 2 + 1 + context.Length];
        derivationData[..DerivationDataPrefixLength].Clear(); // Zero bytes prefix
        derivationData[DerivationDataPrefixLength] = derivationType;
        derivationData[DerivationDataPrefixLength + 1] = DerivationDataSeparator;
        BinaryPrimitives.WriteInt16BigEndian(
            derivationData[(DerivationDataPrefixLength + 2)..(DerivationDataPrefixLength + 4)], lengthBits);
        derivationData[DerivationDataPrefixLength + 4] = DerivationDataCounter;
        context.CopyTo(derivationData[(DerivationDataPrefixLength + 5)..]);

        Console.WriteLine($"[DEBUG] DeriveKey type=0x{derivationType:X2}, length=0x{lengthBits:X4}");
        Console.WriteLine($"[DEBUG] DeriveKey key: {Convert.ToHexString(key)}");
        Console.WriteLine($"[DEBUG] DeriveKey data: {Convert.ToHexString(derivationData)}");

        using var cmac = new AesCmac(key);
        cmac.AppendData(derivationData);
        var mac = cmac.GetHashAndReset();

        Console.WriteLine($"[DEBUG] DeriveKey MAC (full 16 bytes): {Convert.ToHexString(mac)}");
        Console.WriteLine(
            $"[DEBUG] DeriveKey output ({lengthBits / 8} bytes): {Convert.ToHexString(mac.AsSpan(0, lengthBits / 8))}");

        try
        {
            mac.AsSpan(0, lengthBits / 8).CopyTo(output);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mac);
        }
    }
}