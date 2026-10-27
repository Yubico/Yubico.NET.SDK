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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Derives what an OpenPGP private key import needs from the key itself: the algorithm attributes
///     for the slot and the private key template. Owns the padded or reordered copies it makes and
///     clears them on dispose.
/// </summary>
internal sealed class PrivateKeyImport : IDisposable
{
    private static readonly byte[] PublicExponent65537 = [0x01, 0x00, 0x01];
    private readonly List<byte[]> _ownedBuffers = [];

    private PrivateKeyImport(AlgorithmAttributes attributes)
    {
        Attributes = attributes;
    }

    public AlgorithmAttributes Attributes { get; }

    public PrivateKeyTemplate Template { get; private set; } = null!;

    /// <summary>
    ///     Builds the import for <paramref name="privateKey" /> into <paramref name="keyRef" /> on a card
    ///     running <paramref name="firmwareVersion" />.
    /// </summary>
    /// <remarks>
    ///     RSA uses the standard (e, p, q) form on firmware 4 and later; firmware 1 through 3 took only the
    ///     CRT form with modulus. The slot picks ECDH or ECDSA for NIST curves; Ed25519 is always EdDSA and
    ///     X25519 always ECDH. The X25519 scalar is sent big-endian, the reverse of its RFC 7748 encoding.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    ///     The key type, RSA size or public exponent is not one the card imports.
    /// </exception>
    public static PrivateKeyImport Create(IPrivateKey privateKey, KeyRef keyRef, FirmwareVersion firmwareVersion)
    {
        var import = new PrivateKeyImport(GetAttributes(privateKey, keyRef, firmwareVersion));
        try
        {
            import.Template = privateKey switch
            {
                RSAPrivateKey rsa => import.RsaTemplate(rsa, useCrt: firmwareVersion.Major is > 0 and < 4),
                ECPrivateKey ec => import.EcTemplate(ec),
                Curve25519PrivateKey curve => import.Curve25519Template(curve),
                _ => throw new NotSupportedException($"Private key type {privateKey.GetType().Name} is not supported for OpenPGP import."),
            };
            return import;
        }
        catch
        {
            import.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        foreach (var buffer in _ownedBuffers)
        {
            CryptographicOperations.ZeroMemory(buffer);
        }

        _ownedBuffers.Clear();
    }

    private static AlgorithmAttributes GetAttributes(IPrivateKey privateKey, KeyRef keyRef, FirmwareVersion firmwareVersion)
    {
        switch (privateKey)
        {
            case RSAPrivateKey rsa:
                if (!PublicExponent65537.AsSpan().SequenceEqual(TrimLeadingZeros(rsa.Parameters.Exponent)))
                {
                    throw new NotSupportedException("RSA keys with a public exponent other than 65537 are not supported.");
                }

                var size = (RsaSize)rsa.KeyDefinition.LengthInBits;
                if (!Enum.IsDefined(size))
                {
                    throw new NotSupportedException($"RSA {rsa.KeyDefinition.LengthInBits} is not a size the OpenPGP application imports.");
                }

                return RsaAttributes.Create(
                    size,
                    firmwareVersion.Major is > 0 and < 4 ? RsaImportFormat.Crt : RsaImportFormat.Standard);

            case ECPrivateKey ec:
                return EcAttributes.Create(keyRef, ec.KeyType switch
                {
                    KeyType.ECP256 => CurveOid.Secp256R1,
                    KeyType.ECP384 => CurveOid.Secp384R1,
                    KeyType.ECP521 => CurveOid.Secp521R1,
                    _ => throw new NotSupportedException($"Curve {ec.KeyType} is not supported for OpenPGP import."),
                });

            case Curve25519PrivateKey curve:
                return EcAttributes.Create(keyRef, curve.KeyType == KeyType.Ed25519 ? CurveOid.Ed25519 : CurveOid.X25519);

            default:
                throw new NotSupportedException($"Private key type {privateKey.GetType().Name} is not supported for OpenPGP import.");
        }
    }

    private PrivateKeyTemplate RsaTemplate(RSAPrivateKey rsa, bool useCrt)
    {
        var parameters = rsa.Parameters;
        if (parameters.P is null || parameters.Q is null)
        {
            throw new ArgumentException("The RSA private key has no prime factors.", nameof(rsa));
        }

        var componentLength = rsa.KeyDefinition.LengthInBytes / 2;
        var p = FixedLength(parameters.P, componentLength);
        var q = FixedLength(parameters.Q, componentLength);
        if (!useCrt)
        {
            return new RsaKeyTemplate(PublicExponent65537, p, q);
        }

        if (parameters.InverseQ is null || parameters.DP is null || parameters.DQ is null || parameters.Modulus is null)
        {
            throw new ArgumentException("The RSA private key has no CRT parameters.", nameof(rsa));
        }

        return new RsaCrtKeyTemplate(
            PublicExponent65537, p, q,
            FixedLength(parameters.InverseQ, componentLength),
            FixedLength(parameters.DP, componentLength),
            FixedLength(parameters.DQ, componentLength),
            FixedLength(parameters.Modulus, componentLength * 2));
    }

    private PrivateKeyTemplate EcTemplate(ECPrivateKey ec)
    {
        var d = ec.Parameters.D ?? throw new ArgumentException("The EC private key has no scalar.", nameof(ec));
        return new EcKeyTemplate(FixedLength(d, ec.KeyDefinition.LengthInBytes));
    }

    private PrivateKeyTemplate Curve25519Template(Curve25519PrivateKey curve)
    {
        if (curve.KeyType == KeyType.Ed25519)
        {
            return new EcKeyTemplate(curve.PrivateKey);
        }

        var reversed = curve.PrivateKey.ToArray();
        Array.Reverse(reversed);
        _ownedBuffers.Add(reversed);
        return new EcKeyTemplate(reversed);
    }

    /// <summary>A copy of <paramref name="value" /> as a big-endian integer in exactly <paramref name="length" /> bytes.</summary>
    private byte[] FixedLength(byte[] value, int length)
    {
        var trimmed = TrimLeadingZeros(value);
        if (trimmed.Length > length)
        {
            throw new ArgumentException($"Key component of {trimmed.Length} bytes does not fit in {length} bytes.");
        }

        var result = new byte[length];
        trimmed.CopyTo(result.AsSpan(length - trimmed.Length));
        _ownedBuffers.Add(result);
        return result;
    }

    private static ReadOnlySpan<byte> TrimLeadingZeros(byte[]? value)
    {
        if (value is null)
        {
            return [];
        }

        var span = value.AsSpan();
        while (span.Length > 1 && span[0] == 0)
        {
            span = span[1..];
        }

        return span;
    }
}