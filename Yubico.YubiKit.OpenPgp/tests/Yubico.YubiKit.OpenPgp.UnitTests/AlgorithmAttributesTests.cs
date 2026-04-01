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
    [InlineData(CurveOid.Secp256R1, KeyRef.Sig, 0x13)]
    [InlineData(CurveOid.Secp384R1, KeyRef.Aut, 0x13)]
    [InlineData(CurveOid.Secp256R1, KeyRef.Dec, 0x12)]
    [InlineData(CurveOid.Ed25519, KeyRef.Sig, 0x16)]
    [InlineData(CurveOid.Ed25519, KeyRef.Aut, 0x16)]
    public void EcAttributes_Create_SetsCorrectAlgorithmId(CurveOid oid, KeyRef keyRef, int expectedAlgId)
    {
        var attrs = EcAttributes.Create(keyRef, oid);

        Assert.Equal(expectedAlgId, attrs.AlgorithmId);
        Assert.Equal(oid, attrs.Oid);
    }

    [Theory]
    [InlineData(CurveOid.Secp256R1)]
    [InlineData(CurveOid.Secp384R1)]
    [InlineData(CurveOid.Secp521R1)]
    [InlineData(CurveOid.BrainpoolP256R1)]
    [InlineData(CurveOid.Ed25519)]
    [InlineData(CurveOid.X25519)]
    public void EcAttributes_RoundTrip_PreservesOid(CurveOid oid)
    {
        var keyRef = oid == CurveOid.X25519 ? KeyRef.Dec : KeyRef.Sig;
        var attrs = EcAttributes.Create(keyRef, oid);

        var bytes = attrs.ToBytes();
        var parsed = AlgorithmAttributes.Parse(bytes);

        var ec = Assert.IsType<EcAttributes>(parsed);
        Assert.Equal(oid, ec.Oid);
    }

    [Fact]
    public void EcAttributes_WithPubkeyFormat_RoundTrips()
    {
        // Build raw bytes: algorithmId=0x13, OID bytes for Secp256R1, import format 0xFF
        var oidBytes = CurveOid.Secp256R1.GetOidBytes();
        var raw = new byte[1 + oidBytes.Length + 1];
        raw[0] = 0x13; // ECDSA
        oidBytes.CopyTo(raw.AsSpan(1));
        raw[^1] = (byte)EcImportFormat.StandardWithPubkey;
        var attrs = AlgorithmAttributes.Parse(raw);

        var bytes = attrs.ToBytes();
        var parsed = AlgorithmAttributes.Parse(bytes);

        var ec = Assert.IsType<EcAttributes>(parsed);
        Assert.Equal(CurveOid.Secp256R1, ec.Oid);
        Assert.Equal(EcImportFormat.StandardWithPubkey, ec.ImportFormat);
    }

    [Fact]
    public void Parse_UnknownAlgorithmId_Throws()
    {
        byte[] wire = [0xFF, 0x00, 0x00];
        Assert.Throws<ArgumentException>(() => AlgorithmAttributes.Parse(wire));
    }
}
