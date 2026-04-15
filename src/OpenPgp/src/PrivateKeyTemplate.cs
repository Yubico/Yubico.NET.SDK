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
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Base class for private key import templates used with PUT_DATA_ODD (INS 0xDB).
/// </summary>
/// <remarks>
///     Wire format:
///     <code>
///     TLV(0x4D,
///         CRT bytes
///         + TLV(0x7F48, concatenated tag+length headers for each component)
///         + TLV(0x5F48, concatenated raw values for each component)
///     )
///     </code>
/// </remarks>
public abstract class PrivateKeyTemplate
{
    /// <summary>
    ///     The Control Reference Template identifying the target key slot.
    /// </summary>
    public ReadOnlyMemory<byte> CrtBytes { get; }

    /// <summary>
    ///     Creates a new private key template for the specified key slot.
    /// </summary>
    protected PrivateKeyTemplate(KeyRef keyRef)
    {
        CrtBytes = keyRef.GetCrt();
    }

    /// <summary>
    ///     Gets the list of TLV components that make up the key data.
    ///     Each TLV has a tag identifying the component and the raw key bytes as value.
    /// </summary>
    protected abstract Tlv[] GetComponents();

    /// <summary>
    ///     Serializes the private key template to its wire format for PUT_DATA_ODD.
    /// </summary>
    public byte[] ToBytes()
    {
        var components = GetComponents();
        try
        {
            // Build the header list: tag+length for each component (no value)
            var headerSize = 0;
            foreach (var c in components)
            {
                headerSize += c.TotalLength - c.Length;
            }

            var headers = new byte[headerSize];
            var headerOffset = 0;
            foreach (var c in components)
            {
                var fullBytes = c.AsSpan();
                var headerLen = c.TotalLength - c.Length;
                fullBytes[..headerLen].CopyTo(headers.AsSpan(headerOffset));
                headerOffset += headerLen;
            }

            // Build the concatenated values
            var valueSize = 0;
            foreach (var c in components)
            {
                valueSize += c.Length;
            }

            var values = new byte[valueSize];
            byte[]? inner = null;
            try
            {
                var valueOffset = 0;
                foreach (var c in components)
                {
                    c.Value.Span.CopyTo(values.AsSpan(valueOffset));
                    valueOffset += c.Length;
                }

                // Build the final structure: CRT + TLV(0x7F48, headers) + TLV(0x5F48, values)
                using var headerTlv = new Tlv(0x7F48, headers);
                using var valueTlv = new Tlv(0x5F48, values);

                var innerSize = CrtBytes.Length + headerTlv.TotalLength + valueTlv.TotalLength;
                inner = new byte[innerSize];
                CrtBytes.Span.CopyTo(inner);
                headerTlv.AsSpan().CopyTo(inner.AsSpan(CrtBytes.Length));
                valueTlv.AsSpan().CopyTo(inner.AsSpan(CrtBytes.Length + headerTlv.TotalLength));

                using var outerTlv = new Tlv(0x4D, inner);
                return outerTlv.AsMemory().ToArray();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(values);
                if (inner is not null)
                {
                    CryptographicOperations.ZeroMemory(inner);
                }
            }
        }
        finally
        {
            foreach (var c in components)
            {
                c.Dispose();
            }
        }
    }
}

/// <summary>
///     RSA private key template in standard format (e, p, q).
/// </summary>
public class RsaKeyTemplate : PrivateKeyTemplate
{
    /// <summary>
    ///     The public exponent (e).
    /// </summary>
    public ReadOnlyMemory<byte> E { get; }

    /// <summary>
    ///     The first prime factor (p).
    /// </summary>
    public ReadOnlyMemory<byte> P { get; }

    /// <summary>
    ///     The second prime factor (q).
    /// </summary>
    public ReadOnlyMemory<byte> Q { get; }

    public RsaKeyTemplate(KeyRef keyRef, ReadOnlyMemory<byte> e, ReadOnlyMemory<byte> p, ReadOnlyMemory<byte> q)
        : base(keyRef)
    {
        E = e;
        P = p;
        Q = q;
    }

    /// <inheritdoc />
    protected override Tlv[] GetComponents() =>
    [
        new Tlv(0x91, E.Span),
        new Tlv(0x92, P.Span),
        new Tlv(0x93, Q.Span),
    ];
}

/// <summary>
///     RSA private key template in CRT (Chinese Remainder Theorem) format
///     (e, p, q, iqmp, dmp1, dmq1, n).
/// </summary>
public sealed class RsaCrtKeyTemplate : RsaKeyTemplate
{
    /// <summary>
    ///     The CRT coefficient (q^-1 mod p).
    /// </summary>
    public ReadOnlyMemory<byte> Iqmp { get; }

    /// <summary>
    ///     The first CRT exponent (d mod (p-1)).
    /// </summary>
    public ReadOnlyMemory<byte> Dmp1 { get; }

    /// <summary>
    ///     The second CRT exponent (d mod (q-1)).
    /// </summary>
    public ReadOnlyMemory<byte> Dmq1 { get; }

    /// <summary>
    ///     The modulus (n = p * q).
    /// </summary>
    public ReadOnlyMemory<byte> N { get; }

    public RsaCrtKeyTemplate(
        KeyRef keyRef,
        ReadOnlyMemory<byte> e,
        ReadOnlyMemory<byte> p,
        ReadOnlyMemory<byte> q,
        ReadOnlyMemory<byte> iqmp,
        ReadOnlyMemory<byte> dmp1,
        ReadOnlyMemory<byte> dmq1,
        ReadOnlyMemory<byte> n)
        : base(keyRef, e, p, q)
    {
        Iqmp = iqmp;
        Dmp1 = dmp1;
        Dmq1 = dmq1;
        N = n;
    }

    /// <inheritdoc />
    protected override Tlv[] GetComponents() =>
    [
        .. base.GetComponents(),
        new Tlv(0x94, Iqmp.Span),
        new Tlv(0x95, Dmp1.Span),
        new Tlv(0x96, Dmq1.Span),
        new Tlv(0x97, N.Span),
    ];
}

/// <summary>
///     EC private key template containing the private scalar and optional public key.
/// </summary>
public sealed class EcKeyTemplate : PrivateKeyTemplate
{
    /// <summary>
    ///     The EC private key scalar.
    /// </summary>
    public ReadOnlyMemory<byte> PrivateKey { get; }

    /// <summary>
    ///     The EC public key point (uncompressed). Null if not included.
    /// </summary>
    public ReadOnlyMemory<byte>? PublicKey { get; }

    public EcKeyTemplate(KeyRef keyRef, ReadOnlyMemory<byte> privateKey, ReadOnlyMemory<byte>? publicKey = null)
        : base(keyRef)
    {
        PrivateKey = privateKey;
        PublicKey = publicKey;
    }

    /// <inheritdoc />
    protected override Tlv[] GetComponents()
    {
        if (PublicKey is { } pubKey)
        {
            return
            [
                new Tlv(0x92, PrivateKey.Span),
                new Tlv(0x99, pubKey.Span),
            ];
        }

        return [new Tlv(0x92, PrivateKey.Span)];
    }
}