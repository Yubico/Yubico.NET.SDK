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

using System.Collections.Frozen;
using System.Formats.Asn1;
using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>Modeled OpenPGP key representation, not a device capability or slot/import-format selection.</summary>
public sealed record OpenPgpKeyInfo
{
    internal OpenPgpKeyInfo(KeyType keyType, string? openPgpCurveOid = null, RsaSize? rsaSize = null)
    {
        KeyType = keyType;
        CurveOid = openPgpCurveOid;
        RsaSize = rsaSize;
    }

    /// <summary>The shared concrete key identity described by this protocol-specific metadata.</summary>
    public KeyType KeyType { get; }
    /// <summary>OpenPGP curve OID, or null for RSA. For 25519 this is not the RFC 8410 ASN.1 key algorithm OID.</summary>
    public string? CurveOid { get; }
    /// <summary>The OpenPGP RSA modulus size in bits, or null for elliptic-curve keys.</summary>
    public RsaSize? RsaSize { get; }
}

/// <summary>OpenPGP-specific metadata navigation. Core has no dependency on this module.</summary>
public static class OpenPgpKeyInfoExtensions
{
    /// <summary>Gets modeled representation metadata, or null. Does not select a slot or imply hardware support.</summary>
    /// <param name="keyInfo">The shared concrete key metadata.</param>
    /// <returns>The protocol representation, or null when it is not modeled (including symmetric keys and RSA-1024).</returns>
    public static OpenPgpKeyInfo? GetOpenPgpInfo(this KeyInfo keyInfo) => OpenPgpCurveOids.GetInfo(keyInfo.KeyType);
}

// OpenPGP algorithm attributes carry DER OID VALUE OCTETS ONLY, not the tag/length wrapper.
// The two legacy 25519 OIDs belong to this protocol, not the shared ASN.1 key catalog.
internal static class OpenPgpCurveOids
{
    private static readonly FrozenDictionary<KeyType, OpenPgpKeyInfo> Infos = CreateInfos();
    private static readonly FrozenDictionary<KeyType, byte[]> EncodedOids = Infos.Values
        .Where(info => info.CurveOid is not null)
        .ToFrozenDictionary(info => info.KeyType, info => EncodeOidValue(info.CurveOid ?? throw new InvalidOperationException()));

    internal static OpenPgpKeyInfo? GetInfo(KeyType keyType) => Infos.GetValueOrDefault(keyType);

    internal static ReadOnlySpan<byte> GetOidValueBytes(KeyType keyType) => EncodedOids.TryGetValue(keyType, out var oidValueBytes)
        ? oidValueBytes : throw new ArgumentException($"Key '{keyType}' has no modeled OpenPGP curve OID.", nameof(keyType));

    internal static bool TryFromOidValueBytes(ReadOnlySpan<byte> oidValueBytes, out KeyType keyType)
    {
        foreach (var entry in EncodedOids)
        {
            if (oidValueBytes.SequenceEqual(entry.Value))
            {
                keyType = entry.Key;
                return true;
            }
        }
        keyType = KeyType.None;
        return false;
    }

    internal static KeyType FromOidValueBytes(ReadOnlySpan<byte> oidValueBytes) => TryFromOidValueBytes(oidValueBytes, out var keyType)
        ? keyType : throw new ArgumentException("Unknown OpenPGP curve OID value octets.", nameof(oidValueBytes));

    private static FrozenDictionary<KeyType, OpenPgpKeyInfo> CreateInfos()
    {
        KeyType[] modeledEcTypes = [KeyType.P256, KeyType.P384, KeyType.P521, KeyType.Secp256k1,
            KeyType.BrainpoolP256r1, KeyType.BrainpoolP384r1, KeyType.BrainpoolP512r1];
        var infos = modeledEcTypes.Select(keyType => new OpenPgpKeyInfo(keyType,
            keyType.GetKeyInfo().Curve?.NamedCurveOid ?? throw new InvalidOperationException("OpenPGP ordinary EC metadata requires a named curve."))).ToList();
        infos.Add(new(KeyType.X25519, "1.3.6.1.4.1.3029.1.5.1"));
        infos.Add(new(KeyType.Ed25519, "1.3.6.1.4.1.11591.15.1"));
        infos.AddRange(CryptographicDefinitions.Keys.All.Where(keyInfo => keyInfo.IsRsa && Enum.IsDefined((RsaSize)keyInfo.KeySizeBits))
            .Select(keyInfo => new OpenPgpKeyInfo(keyInfo.KeyType, rsaSize: (RsaSize)keyInfo.KeySizeBits)));
        return infos.ToFrozenDictionary(info => info.KeyType);
    }

    private static byte[] EncodeOidValue(string openPgpCurveOid)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.WriteObjectIdentifier(openPgpCurveOid);
        var encoded = writer.Encode();
        _ = AsnDecoder.ReadEncodedValue(encoded, AsnEncodingRules.DER, out var contentOffset, out var contentLength, out _);
        return encoded.AsSpan(contentOffset, contentLength).ToArray(); // cached nonsecret metadata escapes this scope
    }
}