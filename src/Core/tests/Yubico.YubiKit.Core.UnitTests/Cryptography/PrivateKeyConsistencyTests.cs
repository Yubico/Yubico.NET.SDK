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

using System.Numerics;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

/// <summary>
///     Audit #27 (YESDK-1635): public and private components must belong together on every creation
///     path, not only PKCS#8. RFC 8017 §3.1-3.2 and Appendix A.1.2 for RSA; Q = d·G for EC.
/// </summary>
public class PrivateKeyConsistencyTests
{
    // ---- Known-answer vectors (external references) -------------------------------------------

    /// <summary>RFC 7517 Appendix A.2: a complete, consistent 2048-bit key must load.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1635")]
    public void Rsa_Rfc7517Vector_Loads()
    {
        using var key = RSAPrivateKey.CreateFromParameters(KnownPrivateKeyVectors.Rfc7517Rsa2048());

        Assert.Equal(KeyType.RSA2048, key.KeyType);
    }

    /// <summary>
    ///     RFC 7517 Appendix A.2 with exactly one component disturbed: each RFC 8017 relation is checked
    ///     against a reference key, not merely against itself.
    /// </summary>
    [Theory]
    [Trait("Audit", "YESDK-1635")]
    [InlineData("Modulus")] // n = p·q (RFC 8017 §3.2)
    [InlineData("Exponent")] // e·d ≡ 1 mod λ(n) (§3.1, §3.2)
    [InlineData("D")]
    [InlineData("P")]
    [InlineData("Q")]
    [InlineData("DP")] // dP = d mod (p−1) (Appendix A.1.2)
    [InlineData("DQ")] // dQ = d mod (q−1)
    [InlineData("InverseQ")] // qInv·q ≡ 1 mod p
    public void Rsa_Rfc7517Vector_OneComponentOff_IsRejected(string component)
    {
        var v = KnownPrivateKeyVectors.Rfc7517Rsa2048();
        var perturbed = component switch
        {
            "Modulus" => v with { Modulus = AddOne(v.Modulus!) },
            "Exponent" => v with { Exponent = AddOne(v.Exponent!) },
            "D" => v with { D = AddOne(v.D!) },
            "P" => v with { P = AddOne(v.P!) },
            "Q" => v with { Q = AddOne(v.Q!) },
            "DP" => v with { DP = AddOne(v.DP!) },
            "DQ" => v with { DQ = AddOne(v.DQ!) },
            _ => v with { InverseQ = AddOne(v.InverseQ!) },
        };

        Assert.Throws<CryptographicException>(() => RSAPrivateKey.CreateFromParameters(perturbed));
    }

    /// <summary>RFC 6979 Appendix A.2.5 (P-256) and A.2.6 (P-384): x with its (Ux, Uy) must load.</summary>
    [Theory]
    [Trait("Audit", "YESDK-1635")]
    [InlineData("nistP256", KnownPrivateKeyVectors.Rfc6979P256D, KnownPrivateKeyVectors.Rfc6979P256X, KnownPrivateKeyVectors.Rfc6979P256Y, KeyType.ECP256)]
    [InlineData("nistP384", KnownPrivateKeyVectors.Rfc6979P384D, KnownPrivateKeyVectors.Rfc6979P384X, KnownPrivateKeyVectors.Rfc6979P384Y, KeyType.ECP384)]
    public void Ec_Rfc6979Vector_Loads(string curve, string d, string x, string y, KeyType expected)
    {
        using var key = ECPrivateKey.CreateFromParameters(EcParameters(curve, d, x, y));

        Assert.Equal(expected, key.KeyType);
    }

    /// <summary>RFC 6979 P-256 x with the P-384 vector's point is a mismatch; so is Ux with Uy ± 1 (off curve).</summary>
    [Fact]
    [Trait("Audit", "YESDK-1635")]
    public void Ec_Rfc6979Vector_WrongOrOffCurvePoint_IsRejected()
    {
        var swapped = EcParameters("nistP256", KnownPrivateKeyVectors.Rfc6979P256D,
            KnownPrivateKeyVectors.Rfc6979P384X[..64], KnownPrivateKeyVectors.Rfc6979P384Y[..64]);
        Assert.Throws<CryptographicException>(() => ECPrivateKey.CreateFromParameters(swapped));

        var offCurve = EcParameters("nistP256", KnownPrivateKeyVectors.Rfc6979P256D,
            KnownPrivateKeyVectors.Rfc6979P256X, KnownPrivateKeyVectors.Rfc6979P256Y[..^2] + "9A");
        Assert.Throws<CryptographicException>(() => ECPrivateKey.CreateFromParameters(offCurve));
    }

    // ---- Freshly generated keys ---------------------------------------------------------------

    [Fact]
    [Trait("Audit", "YESDK-1635")]
    public void Rsa_ConsistentParameters_Load()
    {
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(true);

        using var key = RSAPrivateKey.CreateFromParameters(parameters);

        Assert.Equal(KeyType.RSA2048, key.KeyType);
    }

    [Theory]
    [Trait("Audit", "YESDK-1635")]
    [InlineData("DP")]
    [InlineData("DQ")]
    [InlineData("InverseQ")]
    [InlineData("D")]
    public void Rsa_PerturbedPrivateComponent_IsRejected(string component)
    {
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(true);
        var perturbed = component switch
        {
            "DP" => parameters with { DP = AddOne(parameters.DP!) },
            "DQ" => parameters with { DQ = AddOne(parameters.DQ!) },
            "InverseQ" => parameters with { InverseQ = AddOne(parameters.InverseQ!) },
            _ => parameters with { D = AddOne(parameters.D!) },
        };

        Assert.Throws<CryptographicException>(() => RSAPrivateKey.CreateFromParameters(perturbed));
    }

    [Fact]
    [Trait("Audit", "YESDK-1635")]
    public void Rsa_WrongPublicExponent_IsRejected()
    {
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(true) with { Exponent = [0x03] };

        Assert.Throws<CryptographicException>(() => RSAPrivateKey.CreateFromParameters(parameters));
    }

    [Fact]
    [Trait("Audit", "YESDK-1635")]
    public void Rsa_ModulusFromAnotherKey_IsRejectedViaCreateFromParameters()
    {
        using var a = RSA.Create(2048);
        using var b = RSA.Create(2048);
        var parameters = a.ExportParameters(true) with { Modulus = b.ExportParameters(false).Modulus };

        Assert.Throws<CryptographicException>(() => RSAPrivateKey.CreateFromParameters(parameters));
    }

    [Fact]
    [Trait("Audit", "YESDK-1635")]
    public void Ec_ConsistentParameters_Load_AndOptionalQMayBeOmitted()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = ecdsa.ExportParameters(true);

        using var withQ = ECPrivateKey.CreateFromParameters(parameters);
        using var withoutQ = ECPrivateKey.CreateFromParameters(new ECParameters { Curve = parameters.Curve, D = parameters.D });

        Assert.Equal(KeyType.ECP256, withQ.KeyType);
        Assert.Equal(KeyType.ECP256, withoutQ.KeyType);
    }

    [Fact]
    [Trait("Audit", "YESDK-1635")]
    public void Ec_PointFromAnotherScalar_IsRejectedViaCreateFromParameters()
    {
        using var a = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using var b = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var parameters = a.ExportParameters(true) with { Q = b.ExportParameters(false).Q };

        Assert.Throws<CryptographicException>(() => ECPrivateKey.CreateFromParameters(parameters));
    }

    [Fact]
    [Trait("Audit", "YESDK-1635")]
    public void Ec_PointNotOnCurve_IsRejected()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = ecdsa.ExportParameters(true);
        var y = (byte[])parameters.Q.Y!.Clone();
        y[^1] ^= 0x01;
        var offCurve = parameters with { Q = new ECPoint { X = parameters.Q.X, Y = y } };

        Assert.Throws<CryptographicException>(() => ECPrivateKey.CreateFromParameters(offCurve));
    }

    private static ECParameters EcParameters(string curve, string d, string x, string y) => new()
    {
        Curve = ECCurve.CreateFromFriendlyName(curve),
        D = Convert.FromHexString(d),
        Q = new ECPoint { X = Convert.FromHexString(x), Y = Convert.FromHexString(y) },
    };

    private static byte[] AddOne(byte[] value) =>
        (new BigInteger(value, isUnsigned: true, isBigEndian: true) + 1)
        .ToByteArray(isUnsigned: true, isBigEndian: true);
}