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

using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public class OpenPgpKeyInfoTests
{
    // Independent OpenPGP card 3.4 / RFC 6637 / RFC 5639 OID VALUE octets, not full DER TLVs.
    [Theory]
    [InlineData(KeyType.P256, "1.2.840.10045.3.1.7", "2A8648CE3D030107")]
    [InlineData(KeyType.P384, "1.3.132.0.34", "2B81040022")]
    [InlineData(KeyType.P521, "1.3.132.0.35", "2B81040023")]
    [InlineData(KeyType.Secp256k1, "1.3.132.0.10", "2B8104000A")]
    [InlineData(KeyType.BrainpoolP256r1, "1.3.36.3.3.2.8.1.1.7", "2B2403030208010107")]
    [InlineData(KeyType.BrainpoolP384r1, "1.3.36.3.3.2.8.1.1.11", "2B240303020801010B")]
    [InlineData(KeyType.BrainpoolP512r1, "1.3.36.3.3.2.8.1.1.13", "2B240303020801010D")]
    [InlineData(KeyType.X25519, "1.3.6.1.4.1.3029.1.5.1", "2B060104019755010501")]
    [InlineData(KeyType.Ed25519, "1.3.6.1.4.1.11591.15.1", "2B06010401DA470F01")]
    public void CurveRepresentation_UsesExactProtocolOid(KeyType keyType, string openPgpCurveOid, string valueHex)
    {
        var oidValueBytes = Convert.FromHexString(valueHex);
        var info = Assert.IsType<OpenPgpKeyInfo>(keyType.GetKeyInfo().GetOpenPgpInfo());
        Assert.Equal(openPgpCurveOid, info.CurveOid);
        Assert.Equal(oidValueBytes, OpenPgpCurveOids.GetOidValueBytes(keyType).ToArray());
        Assert.True(OpenPgpCurveOids.TryFromOidValueBytes(oidValueBytes, out var decodedKeyType));
        Assert.Equal(keyType, decodedKeyType);
        Assert.Equal(keyType, OpenPgpCurveOids.FromOidValueBytes(oidValueBytes));
        var attributes = EcAttributes.Create(KeyRef.Sig, keyType);
        Assert.Equal(oidValueBytes, attributes.ToBytes()[1..]);
        Assert.Equal(keyType, Assert.IsType<EcAttributes>(AlgorithmAttributes.Parse(attributes.ToBytes())).KeyType);
    }

    [Fact]
    public void UnknownOid_IsNotMistakenForDefaultP256()
    {
        Assert.False(OpenPgpCurveOids.TryFromOidValueBytes([0x2A, 0x03, 0x04], out var keyType));
        Assert.Equal(KeyType.None, keyType);
        Assert.Throws<ArgumentException>(() => AlgorithmAttributes.Parse([0x13, 0x2A, 0x03, 0x04]));
    }

    [Fact]
    public void Metadata_IsOptionalAndDoesNotAdvertiseEveryCoreRsaSize()
    {
        Assert.Null(CryptographicDefinitions.Keys.Rsa1024.GetOpenPgpInfo());
        Assert.Equal(RsaSize.Rsa2048, CryptographicDefinitions.Keys.Rsa2048.GetOpenPgpInfo()?.RsaSize);
        Assert.Equal(RsaSize.Rsa3072, CryptographicDefinitions.Keys.Rsa3072.GetOpenPgpInfo()?.RsaSize);
        Assert.Equal(RsaSize.Rsa4096, CryptographicDefinitions.Keys.Rsa4096.GetOpenPgpInfo()?.RsaSize);
        Assert.Null(CryptographicDefinitions.Keys.Aes256.GetOpenPgpInfo());
        Assert.Null(CryptographicDefinitions.Keys.TripleDes.GetOpenPgpInfo());
        Assert.All(typeof(OpenPgpKeyInfo).GetProperties(), property => Assert.False(property.CanWrite));
    }

    [Theory]
    [InlineData(KeyType.X25519, "1.3.101.110", "1.3.6.1.4.1.3029.1.5.1")]
    [InlineData(KeyType.Ed25519, "1.3.101.112", "1.3.6.1.4.1.11591.15.1")]
    public void Curve25519_OpenPgpOidIsNotAsn1KeyAlgorithm(KeyType keyType, string keyAlgorithmOid, string openPgpCurveOid)
    {
        var keyInfo = keyType.GetKeyInfo();
        Assert.Equal(keyAlgorithmOid, keyInfo.Asn1?.AlgorithmOid);
        Assert.Equal(openPgpCurveOid, keyInfo.GetOpenPgpInfo()?.CurveOid);
        Assert.Null(keyInfo.Asn1?.NamedCurveOid);
    }
}