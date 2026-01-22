// Copyright 2026 Yubico AB
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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Piv;

public sealed partial class PivSession
{
    /// <summary>
    /// Generates a new key pair in the specified slot.
    /// </summary>
    /// <param name="slot">The slot where the key should be generated.</param>
    /// <param name="algorithm">The algorithm to use for key generation.</param>
    /// <param name="pinPolicy">The PIN policy for using the generated key.</param>
    /// <param name="touchPolicy">The touch policy for using the generated key.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The generated public key.</returns>
    /// <exception cref="InvalidOperationException">If session is not authenticated.</exception>
    /// <exception cref="NotSupportedException">If the algorithm is not supported on this firmware version.</exception>
    public async Task<IPublicKey> GenerateKeyAsync(
        PivSlot slot,
        PivAlgorithm algorithm,
        PivPinPolicy pinPolicy = PivPinPolicy.Default,
        PivTouchPolicy touchPolicy = PivTouchPolicy.Default,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Generating key in slot 0x{Slot:X2}, algorithm {Algorithm}", (byte)slot, algorithm);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Management key authentication required to generate keys");
        }

        // Check version requirements
        CheckAlgorithmSupport(algorithm);

        // Build the command data: TAG 0xAC [ TAG 0x80 (algorithm) + optional policies ]
        var dataList = new List<byte>();
        
        // TAG 0xAC (Template)
        var templateStart = dataList.Count;
        dataList.Add(0xAC);
        dataList.Add(0x00); // Length placeholder

        // TAG 0x80 (Algorithm)
        dataList.Add(0x80);
        dataList.Add(0x01);
        dataList.Add((byte)algorithm);

        // TAG 0xAA (PIN policy) - only if not default
        if (pinPolicy != PivPinPolicy.Default)
        {
            dataList.Add(0xAA);
            dataList.Add(0x01);
            dataList.Add((byte)pinPolicy);
        }

        // TAG 0xAB (Touch policy) - only if not default
        if (touchPolicy != PivTouchPolicy.Default)
        {
            dataList.Add(0xAB);
            dataList.Add(0x01);
            dataList.Add((byte)touchPolicy);
        }

        // Update template length
        dataList[templateStart + 1] = (byte)(dataList.Count - templateStart - 2);

        var command = new ApduCommand(0x00, 0x47, 0x00, (byte)slot, dataList.ToArray());
        var response = await _protocol.TransmitAsync(command, cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, $"Key generation failed for slot 0x{(byte)slot:X2}");
        }

        // Parse public key from response (TAG 0x7F49)
        return ParsePublicKey(response.Data, algorithm);
    }

    /// <summary>
    /// Imports a private key into the specified slot.
    /// </summary>
    public async Task<PivAlgorithm> ImportKeyAsync(
        PivSlot slot,
        IPrivateKey privateKey,
        PivPinPolicy pinPolicy = PivPinPolicy.Default,
        PivTouchPolicy touchPolicy = PivTouchPolicy.Default,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Importing key into slot 0x{Slot:X2}", (byte)slot);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Management key authentication required to import keys");
        }

        // TODO: Implement key import with proper TLV encoding
        // This requires encoding the private key components based on algorithm type
        throw new NotImplementedException("ImportKeyAsync is not yet implemented");
    }

    /// <summary>
    /// Moves a key from one slot to another.
    /// </summary>
    /// <param name="sourceSlot">The slot containing the key to move.</param>
    /// <param name="destinationSlot">The slot where the key should be moved.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="NotSupportedException">If firmware version is less than 5.7.0.</exception>
    public async Task MoveKeyAsync(
        PivSlot sourceSlot,
        PivSlot destinationSlot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Moving key from slot 0x{Source:X2} to slot 0x{Dest:X2}", 
            (byte)sourceSlot, (byte)destinationSlot);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Management key authentication required to move keys");
        }

        if (FirmwareVersion < new FirmwareVersion(5, 7, 0))
        {
            throw new NotSupportedException("Move key requires firmware 5.7.0 or later");
        }

        if (sourceSlot == PivSlot.Attestation)
        {
            throw new InvalidOperationException("Cannot move attestation key");
        }

        // INS 0xF6, P1 = 0xFF (MOVE), P2 = dest slot, DATA = source slot
        var command = new ApduCommand(0x00, 0xF6, 0xFF, (byte)destinationSlot, new[] { (byte)sourceSlot });
        var response = await _protocol.TransmitAsync(command, cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, 
                $"Failed to move key from slot 0x{(byte)sourceSlot:X2} to 0x{(byte)destinationSlot:X2}");
        }
    }

    /// <summary>
    /// Deletes a key from the specified slot.
    /// </summary>
    public async Task DeleteKeyAsync(
        PivSlot slot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Deleting key from slot 0x{Slot:X2}", (byte)slot);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Management key authentication required to delete keys");
        }

        if (FirmwareVersion < new FirmwareVersion(5, 7, 0))
        {
            throw new NotSupportedException("Delete key requires firmware 5.7.0 or later");
        }

        // INS 0xF6, P1 = 0xFF (MOVE), P2 = 0xFF (delete), DATA = source slot
        var command = new ApduCommand(0x00, 0xF6, 0xFF, 0xFF, new[] { (byte)slot });
        var response = await _protocol.TransmitAsync(command, cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, 
                $"Failed to delete key from slot 0x{(byte)slot:X2}");
        }
    }

    /// <summary>
    /// Generates an attestation certificate for a key in the specified slot.
    /// </summary>
    public async Task<X509Certificate2> AttestKeyAsync(
        PivSlot slot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Attesting key in slot 0x{Slot:X2}", (byte)slot);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (FirmwareVersion < new FirmwareVersion(4, 3, 0))
        {
            throw new NotSupportedException("Key attestation requires firmware 4.3.0 or later");
        }

        // INS 0xF9, P2 = slot
        var command = new ApduCommand(0x00, 0xF9, 0x00, (byte)slot, ReadOnlyMemory<byte>.Empty);
        var response = await _protocol.TransmitAsync(command, cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, 
                $"Failed to attest key in slot 0x{(byte)slot:X2}");
        }

        // Response is a DER-encoded X.509 certificate
#pragma warning disable SYSLIB0057 // X509Certificate2(byte[]) is obsolete
        return new X509Certificate2(response.Data.ToArray());
#pragma warning restore SYSLIB0057
    }

    private void CheckAlgorithmSupport(PivAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case PivAlgorithm.EccP384:
                if (FirmwareVersion < new FirmwareVersion(4, 0, 0))
                {
                    throw new NotSupportedException("ECC P-384 requires firmware 4.0.0 or later");
                }
                break;

            case PivAlgorithm.Ed25519:
            case PivAlgorithm.X25519:
                if (FirmwareVersion < new FirmwareVersion(5, 7, 0))
                {
                    throw new NotSupportedException("Curve25519 algorithms require firmware 5.7.0 or later");
                }
                break;

            case PivAlgorithm.Rsa3072:
            case PivAlgorithm.Rsa4096:
                if (FirmwareVersion < new FirmwareVersion(5, 7, 0))
                {
                    throw new NotSupportedException("RSA 3072/4096 requires firmware 5.7.0 or later");
                }
                break;

            case PivAlgorithm.Rsa1024:
            case PivAlgorithm.Rsa2048:
                // Check for ROCA vulnerability (4.2.0-4.3.4)
                if (FirmwareVersion >= new FirmwareVersion(4, 2, 0) && FirmwareVersion <= new FirmwareVersion(4, 3, 4))
                {
                    throw new NotSupportedException(
                        "RSA key generation is disabled on firmware 4.2.0-4.3.4 due to ROCA vulnerability. " +
                        "Please upgrade to firmware 4.3.5 or later.");
                }
                break;
        }
    }

    private IPublicKey ParsePublicKey(ReadOnlyMemory<byte> data, PivAlgorithm algorithm)
    {
        var span = data.Span;
        
        // Expect TAG 0x7F49 (Public key template)
        if (span.Length < 2 || span[0] != 0x7F || span[1] != 0x49)
        {
            throw new ApduException("Invalid public key response format");
        }

        // Parse length (can be 1 or 2 bytes)
        int offset = 2;
        int length;
        if (span[offset] <= 0x7F)
        {
            length = span[offset];
            offset++;
        }
        else if (span[offset] == 0x81)
        {
            length = span[offset + 1];
            offset += 2;
        }
        else if (span[offset] == 0x82)
        {
            length = (span[offset + 1] << 8) | span[offset + 2];
            offset += 3;
        }
        else
        {
            throw new ApduException("Invalid TLV length encoding");
        }

        var keyData = span.Slice(offset, length);

        // Parse based on algorithm
        return algorithm switch
        {
            PivAlgorithm.EccP256 or PivAlgorithm.EccP384 => ParseEccPublicKey(keyData, algorithm),
            PivAlgorithm.Ed25519 or PivAlgorithm.X25519 => ParseCurve25519PublicKey(keyData, algorithm),
            PivAlgorithm.Rsa1024 or PivAlgorithm.Rsa2048 or PivAlgorithm.Rsa3072 or PivAlgorithm.Rsa4096 
                => ParseRsaPublicKey(keyData),
            _ => throw new NotSupportedException($"Unsupported algorithm: {algorithm}")
        };
    }

    private IPublicKey ParseEccPublicKey(ReadOnlySpan<byte> data, PivAlgorithm algorithm)
    {
        // ECC public key format: TAG 0x86 (EC point)
        if (data.Length < 2 || data[0] != 0x86)
        {
            throw new ApduException("Invalid ECC public key format");
        }

        int offset = 1;
        int length = data[offset];
        offset++;

        var point = data.Slice(offset, length);

        // Convert to ECPublicKey using the Core cryptography types
        var curve = algorithm == PivAlgorithm.EccP256 
            ? ECCurve.NamedCurves.nistP256 
            : ECCurve.NamedCurves.nistP384;

        var parameters = new ECParameters
        {
            Curve = curve,
            Q = new ECPoint
            {
                X = point.Slice(1, (point.Length - 1) / 2).ToArray(),
                Y = point.Slice(1 + (point.Length - 1) / 2).ToArray()
            }
        };

        return ECPublicKey.CreateFromParameters(parameters);
    }

    private IPublicKey ParseCurve25519PublicKey(ReadOnlySpan<byte> data, PivAlgorithm algorithm)
    {
        // Curve25519 public key format: TAG 0x86 (point)
        if (data.Length < 2 || data[0] != 0x86)
        {
            throw new ApduException("Invalid Curve25519 public key format");
        }

        int offset = 1;
        int length = data[offset];
        offset++;

        var point = data.Slice(offset, length);
        
        var keyType = algorithm == PivAlgorithm.Ed25519 ? KeyType.Ed25519 : KeyType.X25519;
        return Curve25519PublicKey.CreateFromValue(point.ToArray(), keyType);
    }

    private IPublicKey ParseRsaPublicKey(ReadOnlySpan<byte> data)
    {
        // RSA public key format: TAG 0x81 (modulus) + TAG 0x82 (public exponent)
        int offset = 0;
        byte[] modulus = null!;
        byte[] exponent = null!;

        while (offset < data.Length)
        {
            byte tag = data[offset++];
            int length = data[offset++];

            if (tag == 0x81)
            {
                modulus = data.Slice(offset, length).ToArray();
            }
            else if (tag == 0x82)
            {
                exponent = data.Slice(offset, length).ToArray();
            }

            offset += length;
        }

        if (modulus == null || exponent == null)
        {
            throw new ApduException("Invalid RSA public key format");
        }

        var parameters = new RSAParameters
        {
            Modulus = modulus,
            Exponent = exponent
        };

        return RSAPublicKey.CreateFromParameters(parameters);
    }
}
