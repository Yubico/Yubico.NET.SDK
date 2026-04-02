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

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public class CurveOidTests
{
    [Theory]
    [InlineData(CurveOid.Secp256R1)]
    [InlineData(CurveOid.Secp256K1)]
    [InlineData(CurveOid.Secp384R1)]
    [InlineData(CurveOid.Secp521R1)]
    [InlineData(CurveOid.BrainpoolP256R1)]
    [InlineData(CurveOid.BrainpoolP384R1)]
    [InlineData(CurveOid.BrainpoolP512R1)]
    [InlineData(CurveOid.X25519)]
    [InlineData(CurveOid.Ed25519)]
    public void GetOidBytes_ThenFromOidBytes_RoundTrips(CurveOid oid)
    {
        var oidBytes = oid.GetOidBytes();
        var parsed = CurveOidExtensions.FromOidBytes(oidBytes);

        Assert.Equal(oid, parsed);
    }

    [Theory]
    [InlineData(CurveOid.Secp256R1, "1.2.840.10045.3.1.7")]
    [InlineData(CurveOid.Secp384R1, "1.3.132.0.34")]
    [InlineData(CurveOid.Ed25519, "1.3.6.1.4.1.11591.15.1")]
    public void ToDottedString_ReturnsExpected(CurveOid oid, string expected)
    {
        Assert.Equal(expected, oid.ToDottedString());
    }

    [Theory]
    [InlineData(CurveOid.Secp256R1)]
    [InlineData(CurveOid.Secp384R1)]
    [InlineData(CurveOid.Secp521R1)]
    public void ToEcCurve_NistCurves_ReturnsValidCurve(CurveOid oid)
    {
        var curve = oid.ToEcCurve();
        Assert.NotNull(curve.Oid);
    }

    [Fact]
    public void ToEcCurve_X25519_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() => CurveOid.X25519.ToEcCurve());
    }

    [Fact]
    public void ToEcCurve_Ed25519_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() => CurveOid.Ed25519.ToEcCurve());
    }

    [Fact]
    public void FromOidBytes_UnknownOid_Throws()
    {
        byte[] unknown = [0xFF, 0xFF, 0xFF];
        Assert.Throws<ArgumentException>(() => CurveOidExtensions.FromOidBytes(unknown));
    }

    [Fact]
    public void TryFromOidBytes_UnknownOid_ReturnsFalse()
    {
        byte[] unknown = [0xFF, 0xFF, 0xFF];
        Assert.False(CurveOidExtensions.TryFromOidBytes(unknown, out _));
    }

    [Fact]
    public void GetOidBytes_Secp256R1_MatchesExpected()
    {
        byte[] expected = [0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x03, 0x01, 0x07];
        Assert.Equal(expected, CurveOid.Secp256R1.GetOidBytes().ToArray());
    }
}
