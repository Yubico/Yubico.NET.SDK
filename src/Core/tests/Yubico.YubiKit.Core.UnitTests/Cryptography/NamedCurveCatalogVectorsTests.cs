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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

public class NamedCurveCatalogVectorsTests
{
    // d=1, Q=G from the retrieved SEC 2 v2.0 §2.4.1 copy (with Draft page headers) and
    // RFC 5639 §§3.4, 3.6, 3.7. These published points are distinct from the assembled DER SPKI
    // prefixes. Provider-backed assertions below are conditional on RequireHostCurve.
    [Theory]
    [InlineData(KeyType.Secp256k1, "1.3.132.0.10", 32, "3056301006072A8648CE3D020106052B8104000A034200",
        "0479BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798483ADA7726A3C4655DA4FBFC0E1108A8FD17B448A68554199C47D08FFB10D4B8")]
    [InlineData(KeyType.BrainpoolP256r1, "1.3.36.3.3.2.8.1.1.7", 32, "305A301406072A8648CE3D020106092B2403030208010107034200",
        "048BD2AEB9CB7E57CB2C4B482FFC81B7AFB9DE27E1E3BD23C23A4453BD9ACE3262547EF835C3DAC4FD97F8461A14611DC9C27745132DED8E545C1D54C72F046997")]
    [InlineData(KeyType.BrainpoolP384r1, "1.3.36.3.3.2.8.1.1.11", 48, "307A301406072A8648CE3D020106092B240303020801010B036200",
        "041D1C64F068CF45FFA2A63A81B7C13F6B8847A3E77EF14FE3DB7FCAFE0CBD10E8E826E03436D646AAEF87B2E247D4AF1E8ABE1D7520F9C2A45CB1EB8E95CFD55262B70B29FEEC5864E19C054FF99129280E4646217791811142820341263C5315")]
    [InlineData(KeyType.BrainpoolP512r1, "1.3.36.3.3.2.8.1.1.13", 64, "30819B301406072A8648CE3D020106092B240303020801010D03818200",
        "0481AEE4BDD82ED9645A21322E9C4C6A9385ED9F70B5D916C1B43B62EEF4D0098EFF3B1F78E2D0D48D50D1687B93B97D5F7C6D5047406A5E688B352209BCB9F8227DDE385D566332ECC0EABFA9CF7822FDF209F70024A57B1AA000C55B881F8111B2DCDE494A5F485E5BCA4BD88A2763AED1CA2B2FA8F0540678CD1E0F3AD80892")]
    public void NamedCurve_GeneratorKeyEncodesDecodesAndRetainsConsistencyChecks(
        KeyType keyType, string curveOid, int coordinateLengthBytes, string spkiPrefixHex, string pointHex)
    {
        var point = Convert.FromHexString(pointHex);
        var expectedSpki = Convert.FromHexString(spkiPrefixHex + pointHex);
        var keyInfo = keyType.GetKeyInfo();
        Assert.Equal(curveOid, keyInfo.Curve?.NamedCurveOid);
        Assert.Equal(coordinateLengthBytes, keyInfo.Curve?.CoordinateLengthBytes);
        Assert.Equal(expectedSpki, AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(point, keyType));
        var publicKey = Assert.IsType<ECPublicKey>(AsnPublicKeyDecoder.CreatePublicKey(expectedSpki));
        Assert.Equal(keyType, publicKey.KeyType);
        Assert.Equal(point, publicKey.PublicPoint.ToArray());
        Assert.Equal(expectedSpki, publicKey.ExportSubjectPublicKeyInfo());

        RequireHostCurve(curveOid);
        using var scalar = new Yubico.YubiKit.Core.Utilities.DisposableBufferHandle(new byte[coordinateLengthBytes]);
        scalar.Data.Span[^1] = 1;
        using var privateKey = ECPrivateKey.CreateFromValue(scalar.Data, keyType);
        Assert.Equal(point[1..(1 + coordinateLengthBytes)], privateKey.Parameters.Q.X);
        Assert.Equal(point[(1 + coordinateLengthBytes)..], privateKey.Parameters.Q.Y);
        using var pkcs8 = new Yubico.YubiKit.Core.Utilities.DisposableBufferHandle(privateKey.ExportPkcs8PrivateKey());
        using var decoded = Assert.IsType<ECPrivateKey>(AsnPrivateKeyDecoder.CreatePrivateKey(pkcs8.Data));
        Assert.Equal(keyType, decoded.KeyType);
        Assert.Equal(privateKey.Parameters.Q.X, decoded.Parameters.Q.X);
        Assert.True(CryptographicOperations.FixedTimeEquals(scalar.Data.Span, decoded.Parameters.D));
        using var provider = ECDsa.Create();
        provider.ImportPkcs8PrivateKey(pkcs8.Data.Span, out var bytesRead);
        Assert.Equal(pkcs8.Data.Length, bytesRead);
        Assert.Equal(expectedSpki, provider.ExportSubjectPublicKeyInfo());

        var inconsistent = privateKey.Parameters.DeepCopy();
        try
        {
            Assert.NotNull(inconsistent.D);
            inconsistent.D[^1] = 2; // Q remains G, not 2G; must still fail the provider-backed check.
            Assert.Throws<CryptographicException>(() => ECPrivateKey.CreateFromParameters(inconsistent));
        }
        finally { CryptographicOperations.ZeroMemory(inconsistent.D); }
    }

    private static void RequireHostCurve(string curveOid)
    {
        try { using var provider = ECDsa.Create(ECCurve.CreateFromValue(curveOid)); }
        catch (PlatformNotSupportedException) { Assert.Skip($"Host provider does not implement curve {curveOid}."); }
        catch (CryptographicException) { Assert.Skip($"Host provider cannot generate curve {curveOid}."); }
    }
}