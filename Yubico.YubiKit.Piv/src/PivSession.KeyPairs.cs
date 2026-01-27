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
using Yubico.YubiKit.Core.Utils;
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
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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
    /// <param name="slot">The slot where the key should be imported.</param>
    /// <param name="privateKey">The private key to import.</param>
    /// <param name="pinPolicy">The PIN policy for using the imported key.</param>
    /// <param name="touchPolicy">The touch policy for using the imported key.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The algorithm of the imported key.</returns>
    /// <exception cref="InvalidOperationException">If session is not authenticated.</exception>
    /// <exception cref="NotSupportedException">If the key type is not supported.</exception>
    public async Task<PivAlgorithm> ImportKeyAsync(
        PivSlot slot,
        IPrivateKey privateKey,
        PivPinPolicy pinPolicy = PivPinPolicy.Default,
        PivTouchPolicy touchPolicy = PivTouchPolicy.Default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        
        Logger.LogDebug("PIV: Importing key into slot 0x{Slot:X2}, key type {KeyType}", (byte)slot, privateKey.KeyType);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Management key authentication required to import keys");
        }

        // Determine algorithm from key type
        var algorithm = privateKey.KeyType switch
        {
            KeyType.RSA1024 => PivAlgorithm.Rsa1024,
            KeyType.RSA2048 => PivAlgorithm.Rsa2048,
            KeyType.RSA3072 => PivAlgorithm.Rsa3072,
            KeyType.RSA4096 => PivAlgorithm.Rsa4096,
            KeyType.ECP256 => PivAlgorithm.EccP256,
            KeyType.ECP384 => PivAlgorithm.EccP384,
            KeyType.Ed25519 => PivAlgorithm.Ed25519,
            KeyType.X25519 => PivAlgorithm.X25519,
            _ => throw new NotSupportedException($"Key type {privateKey.KeyType} is not supported for PIV import")
        };

        // Build TLV-encoded key data based on algorithm
        byte[]? keyData = null;
        try
        {
            keyData = privateKey switch
            {
                RSAPrivateKey rsaKey => EncodeRsaPrivateKey(rsaKey),
                ECPrivateKey ecKey => EncodeEcPrivateKey(ecKey, algorithm),
                Curve25519PrivateKey curveKey => EncodeCurve25519PrivateKey(curveKey),
                _ => throw new NotSupportedException($"Private key type {privateKey.GetType().Name} is not supported")
            };

            // Add PIN policy TLV if not default
            if (pinPolicy != PivPinPolicy.Default)
            {
                using var pinPolicyTlv = new Tlv(0xAA, [(byte)pinPolicy]);
                keyData = [.. keyData, .. pinPolicyTlv.AsSpan().ToArray()];
            }

            // Add touch policy TLV if not default
            if (touchPolicy != PivTouchPolicy.Default)
            {
                using var touchPolicyTlv = new Tlv(0xAB, [(byte)touchPolicy]);
                keyData = [.. keyData, .. touchPolicyTlv.AsSpan().ToArray()];
            }

            // Send IMPORT KEY command: INS 0xFE, P1 = algorithm, P2 = slot
            var command = new ApduCommand(0x00, 0xFE, (byte)algorithm, (byte)slot, keyData);
            var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

            if (!response.IsOK())
            {
                throw ApduException.FromStatusWord(response.SW, $"Key import failed for slot 0x{(byte)slot:X2}");
            }

            Logger.LogDebug("PIV: Key imported successfully into slot 0x{Slot:X2}, algorithm {Algorithm}", (byte)slot, algorithm);
            return algorithm;
        }
        finally
        {
            // Zero sensitive key data
            if (keyData is not null)
            {
                CryptographicOperations.ZeroMemory(keyData);
            }
        }
    }

    /// <summary>
    /// Encodes RSA private key components as TLV for PIV import.
    /// RSA format: TLV(0x01, P) + TLV(0x02, Q) + TLV(0x03, DP) + TLV(0x04, DQ) + TLV(0x05, InverseQ)
    /// </summary>
    private static byte[] EncodeRsaPrivateKey(RSAPrivateKey rsaKey)
    {
        var parameters = rsaKey.Parameters;
        
        // Validate RSA exponent is 65537 (standard requirement)
        if (parameters.Exponent is null || 
            !(parameters.Exponent.Length == 3 && 
              parameters.Exponent[0] == 0x01 && 
              parameters.Exponent[1] == 0x00 && 
              parameters.Exponent[2] == 0x01))
        {
            throw new NotSupportedException("RSA exponent must be 65537 (0x010001)");
        }

        if (parameters.P is null || parameters.Q is null || 
            parameters.DP is null || parameters.DQ is null || parameters.InverseQ is null)
        {
            throw new ArgumentException("RSA private key is missing required CRT parameters (P, Q, DP, DQ, InverseQ)");
        }

        // All components should be half the modulus length
        var componentLength = parameters.P.Length;

        // Create TLVs for each component, padding to correct length
        using var pTlv = new Tlv(0x01, PadToLength(parameters.P, componentLength));
        using var qTlv = new Tlv(0x02, PadToLength(parameters.Q, componentLength));
        using var dpTlv = new Tlv(0x03, PadToLength(parameters.DP, componentLength));
        using var dqTlv = new Tlv(0x04, PadToLength(parameters.DQ, componentLength));
        using var iqTlv = new Tlv(0x05, PadToLength(parameters.InverseQ, componentLength));

        // Concatenate all TLVs
        return [.. pTlv.AsSpan().ToArray(), 
                .. qTlv.AsSpan().ToArray(), 
                .. dpTlv.AsSpan().ToArray(), 
                .. dqTlv.AsSpan().ToArray(), 
                .. iqTlv.AsSpan().ToArray()];
    }

    /// <summary>
    /// Encodes EC private key D value as TLV for PIV import.
    /// ECC format: TLV(0x06, D)
    /// </summary>
    private static byte[] EncodeEcPrivateKey(ECPrivateKey ecKey, PivAlgorithm algorithm)
    {
        var parameters = ecKey.Parameters;
        
        if (parameters.D is null)
        {
            throw new ArgumentException("EC private key is missing D value");
        }

        // D should be padded to the curve size
        var curveSize = algorithm == PivAlgorithm.EccP256 ? 32 : 48;
        using var dTlv = new Tlv(0x06, PadToLength(parameters.D, curveSize));
        
        return dTlv.AsSpan().ToArray();
    }

    /// <summary>
    /// Encodes Curve25519 private key as TLV for PIV import.
    /// Ed25519 format: TLV(0x07, privateKey)
    /// X25519 format: TLV(0x08, privateKey)
    /// </summary>
    private static byte[] EncodeCurve25519PrivateKey(Curve25519PrivateKey curveKey)
    {
        var tag = curveKey.KeyType == KeyType.Ed25519 ? 0x07 : 0x08;
        using var tlv = new Tlv(tag, curveKey.PrivateKey.Span);
        
        return tlv.AsSpan().ToArray();
    }

    /// <summary>
    /// Pads byte array to target length with leading zeros if needed.
    /// </summary>
    private static byte[] PadToLength(byte[] data, int targetLength)
    {
        if (data.Length >= targetLength)
        {
            return data;
        }

        var result = new byte[targetLength];
        var padding = targetLength - data.Length;
        Buffer.BlockCopy(data, 0, result, padding, data.Length);
        return result;
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

        // MoveKey is supported on firmware 5.7.0+. Rather than check version (which uses PIV app
        // version, not firmware version), we try the command and let it fail with appropriate SW.

        if (sourceSlot == PivSlot.Attestation)
        {
            throw new InvalidOperationException("Cannot move attestation key");
        }

        // INS 0xF6, P1 = destination slot, P2 = source slot, NO DATA
        var command = new ApduCommand(0x00, 0xF6, (byte)destinationSlot, (byte)sourceSlot);
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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

        // Delete key is supported on firmware 5.7.0+. Rather than check version,
        // we let the command fail with appropriate SW.

        // INS 0xF6, P1 = 0xFF (delete), P2 = slot to delete, NO DATA
        var command = new ApduCommand(0x00, 0xF6, 0xFF, (byte)slot);
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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

        // Key attestation is supported on firmware 4.3.0+. Rather than check version (which uses PIV app
        // version, not firmware version), we try the command and let it fail with appropriate SW.

        // INS 0xF9 (ATTEST), P1 = slot, P2 = 0, NO DATA, no explicit Le
        // The formatter adds a trailing 00 byte for Case 1 commands (no data, no Le)
        var command = new ApduCommand(0x00, 0xF9, (byte)slot, 0x00, null, 0);
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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
        // NOTE: Algorithm support is determined by firmware version, but PIV GET VERSION returns
        // the PIV application version (typically 0.0.1), not the firmware version.
        // Rather than incorrectly gate features, we let the commands fail with appropriate SW codes
        // if the algorithm is not supported. The device will return SW 0x6A86 (incorrect parameters)
        // or 0x6D00 (instruction not supported) for unsupported algorithms.
    }

    private IPublicKey ParsePublicKey(ReadOnlyMemory<byte> data, PivAlgorithm algorithm)
    {
        // Parse 0x7F49 (Public key template) - Tlv handles 2-byte tags
        var template = Tlv.Create(data.Span);
        if (template.Tag != 0x7F49)
        {
            throw new ApduException("Invalid public key response format");
        }

        var keyData = template.Value.Span;

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
        var tlvDict = TlvHelper.DecodeDictionary(data);
        
        if (!tlvDict.TryGetValue(0x81, out var modulusMemory))
        {
            throw new ApduException("Invalid RSA public key format: missing modulus");
        }
        
        if (!tlvDict.TryGetValue(0x82, out var exponentMemory))
        {
            throw new ApduException("Invalid RSA public key format: missing exponent");
        }

        var parameters = new RSAParameters
        {
            Modulus = modulusMemory.ToArray(),
            Exponent = exponentMemory.ToArray()
        };

        return RSAPublicKey.CreateFromParameters(parameters);
    }
}
