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

using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public class AlgorithmAttributesTests
{
    [Theory]
    [InlineData(RsaSize.Rsa2048, 2048)]
    [InlineData(RsaSize.Rsa3072, 3072)]
    [InlineData(RsaSize.Rsa4096, 4096)]
    public void RsaAttributes_RoundTrip_PreservesValues(RsaSize keySize, int expectedNLen)
    {
        var attrs = RsaAttributes.Create(keySize);

        var bytes = attrs.ToBytes();
        var parsed = AlgorithmAttributes.Parse(bytes);

        var rsa = Assert.IsType<RsaAttributes>(parsed);
        Assert.Equal(0x01, rsa.AlgorithmId);
        Assert.Equal(expectedNLen, rsa.NLen);
        Assert.Equal(17, rsa.ELen);
        Assert.Equal(RsaImportFormat.Standard, rsa.ImportFormat);
    }

    [Fact]
    public void RsaAttributes_WithCrtFormat_RoundTrips()
    {
        var attrs = RsaAttributes.Create(RsaSize.Rsa2048, RsaImportFormat.Crt);

        var bytes = attrs.ToBytes();
        var parsed = AlgorithmAttributes.Parse(bytes);

        var rsa = Assert.IsType<RsaAttributes>(parsed);
        Assert.Equal(RsaImportFormat.Crt, rsa.ImportFormat);
    }

    [Fact]
    public void RsaAttributes_ToBytes_ProducesCorrectWireFormat()
    {
        var attrs = RsaAttributes.Create(RsaSize.Rsa2048);

        var bytes = attrs.ToBytes();

        // Algorithm=0x01, NLen=0x0800 (2048), ELen=0x0011 (17), ImportFormat=0x00
        Assert.Equal([0x01, 0x08, 0x00, 0x00, 0x11, 0x00], bytes);
    }

    [Fact]
    public void RsaAttributes_Parse_FromWireFormat()
    {
        // RSA 4096, e=17, CRT format
        byte[] wire = [0x01, 0x10, 0x00, 0x00, 0x11, 0x02];
        var parsed = AlgorithmAttributes.Parse(wire);

        var rsa = Assert.IsType<RsaAttributes>(parsed);
        Assert.Equal(4096, rsa.NLen);
        Assert.Equal(17, rsa.ELen);
        Assert.Equal(RsaImportFormat.Crt, rsa.ImportFormat);
    }

    [Theory]
    [InlineData(KeyType.P256, KeyRef.Sig, 0x13)]
    [InlineData(KeyType.P384, KeyRef.Aut, 0x13)]
    [InlineData(KeyType.P256, KeyRef.Dec, 0x12)]
    [InlineData(KeyType.Ed25519, KeyRef.Sig, 0x16)]
    [InlineData(KeyType.Ed25519, KeyRef.Aut, 0x16)]
    [InlineData(KeyType.X25519, KeyRef.Aut, 0x12)]
    [InlineData(KeyType.Secp256k1, KeyRef.Dec, 0x12)]
    [InlineData(KeyType.BrainpoolP512r1, KeyRef.Sig, 0x13)]
    public void EcAttributes_Create_SetsCorrectAlgorithmId(KeyType keyType, KeyRef keyRef, int expectedAlgorithmId)
    {
        var attributes = EcAttributes.Create(keyRef, keyType);

        Assert.Equal(expectedAlgorithmId, attributes.AlgorithmId);
        Assert.Equal(keyType, attributes.KeyType);
    }

    [Theory]
    [InlineData(KeyType.P256)]
    [InlineData(KeyType.P384)]
    [InlineData(KeyType.P521)]
    [InlineData(KeyType.BrainpoolP256r1)]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void EcAttributes_RoundTrip_PreservesConcreteKeyType(KeyType keyType)
    {
        var keyRef = keyType == KeyType.X25519 ? KeyRef.Dec : KeyRef.Sig;
        var attributes = EcAttributes.Create(keyRef, keyType);

        var bytes = attributes.ToBytes();
        var parsed = AlgorithmAttributes.Parse(bytes);

        var ec = Assert.IsType<EcAttributes>(parsed);
        Assert.Equal(keyType, ec.KeyType);
    }

    [Fact]
    public void EcAttributes_WithPubkeyFormat_RoundTrips()
    {
        byte[] raw = Convert.FromHexString("132A8648CE3D030107FF");
        var attrs = AlgorithmAttributes.Parse(raw);

        var bytes = attrs.ToBytes();
        var parsed = AlgorithmAttributes.Parse(bytes);

        var ec = Assert.IsType<EcAttributes>(parsed);
        Assert.Equal(KeyType.P256, ec.KeyType);
        Assert.Equal(EcImportFormat.StandardWithPubkey, ec.ImportFormat);
    }

    [Fact]
    public void Parse_UnknownAlgorithmId_Throws()
    {
        byte[] wire = [0xFF, 0x00, 0x00];
        Assert.Throws<ArgumentException>(() => AlgorithmAttributes.Parse(wire));
    }

    [Theory]
    [InlineData("122A8648CE3D030107FF", 0x12)]
    [InlineData("132A8648CE3D030107FF", 0x13)]
    [InlineData("162A8648CE3D030107FF", 0x16)]
    public void EcAttributes_Parse_PreservesReceivedAlgorithmAndFormat(string wireHex, int algorithmId)
    {
        var wire = Convert.FromHexString(wireHex);
        var attributes = Assert.IsType<EcAttributes>(AlgorithmAttributes.Parse(wire));
        Assert.Equal(KeyType.P256, attributes.KeyType);
        Assert.Equal(algorithmId, attributes.AlgorithmId);
        Assert.Equal(EcImportFormat.StandardWithPubkey, attributes.ImportFormat);
        Assert.Equal(wire, attributes.ToBytes());
    }

    [Theory]
    [InlineData(KeyType.Rsa2048)]
    [InlineData(KeyType.Aes256)]
    [InlineData(KeyType.TripleDes)]
    [InlineData(KeyType.None)]
    public void EcAttributes_NonEcKey_NeverFallsBackToEcdsa(KeyType keyType) =>
        Assert.Throws<ArgumentException>(() => EcAttributes.Create(KeyRef.Sig, keyType));
}