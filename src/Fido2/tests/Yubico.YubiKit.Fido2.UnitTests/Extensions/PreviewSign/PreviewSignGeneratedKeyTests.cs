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

using System.Text;
using Xunit;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.Fido2.UnitTests.Extensions;

/// <summary>
/// Tests for <see cref="PreviewSignGeneratedKey"/> ARKG derivation.
/// </summary>
/// <remarks>
/// WARNING -- EXPERIMENTAL -- test only: ARKG previewSign derivation is not ready for production use and must not
/// be treated as production cryptographic guidance.
/// </remarks>
public class PreviewSignGeneratedKeyTests
{
    // Reuse Phase B KAT vectors from Core
    private static readonly byte[] PkBl = HexToBytes(
        "046d3bdf31d0db48988f16d47048fdd24123cd286e42d0512daa9f726b4ecf18df" +
        "65ed42169c69675f936ff7de5f9bd93adbc8ea73036b16e8d90adbfabdaddba7");

    private static readonly byte[] PkKem = HexToBytes(
        "04c38bbdd7286196733fa177e43b73cfd3d6d72cd11cc0bb2c9236cf85a42dcff5" +
        "dfa339c1e07dfcdfda8d7be2a5a3c7382991f387dfe332b1dd8da6e0622cfb35");

    private static readonly byte[] IkmA = HexToBytes(
        "404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f");
    private static readonly byte[] CtxA = Encoding.ASCII.GetBytes("ARKG-P256.test vectors");
    private static readonly byte[] ExpectedDerivedA = HexToBytes(
        "04572a111ce5cfd2a67d56a0f7c684184b16ccd212490dc9c5b579df749647d107" +
        "dac2a1b197cc10d2376559ad6df6bc107318d5cfb90def9f4a1f5347e086c2cd");

    [Fact]
    public void DerivePublicKey_WithKnownKATVector_ProducesExpectedDerivedKey()
    {
        // Arrange
        var generatedKey = new PreviewSignGeneratedKey(
            keyHandle: new byte[32], // arbitrary
            blindingPublicKey: PkBl,
            kemPublicKey: PkKem,
            derivedKeyAlgorithm: CoseAlgorithm.ArkgP256);

        // Act
        var derived = generatedKey.DerivePublicKey(IkmA, CtxA);

        // Assert
        Assert.Equal(ExpectedDerivedA, derived.PublicKey.ToArray());
        Assert.NotEmpty(derived.ArkgKeyHandle.ToArray()); // Should be 16+65 bytes
        Assert.Equal(generatedKey.KeyHandle.ToArray(), derived.DeviceKeyHandle.ToArray());
        Assert.Equal(CtxA, derived.Context.ToArray());
    }

    [Fact]
    public void DerivePublicKey_DifferentIkm_ProducesDifferentDerivedKeys()
    {
        // Arrange
        var generatedKey = new PreviewSignGeneratedKey(
            keyHandle: new byte[32],
            blindingPublicKey: PkBl,
            kemPublicKey: PkKem,
            derivedKeyAlgorithm: CoseAlgorithm.ArkgP256);

        var ikmB = HexToBytes("606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f");

        // Act
        var derivedA = generatedKey.DerivePublicKey(IkmA, CtxA);
        var derivedB = generatedKey.DerivePublicKey(ikmB, CtxA);

        // Assert
        Assert.NotEqual(derivedA.PublicKey.ToArray(), derivedB.PublicKey.ToArray());
        Assert.NotEqual(derivedA.ArkgKeyHandle.ToArray(), derivedB.ArkgKeyHandle.ToArray());
    }

    [Fact]
    public void DerivePublicKey_DifferentContext_ProducesDifferentDerivedKeys()
    {
        // Arrange
        var generatedKey = new PreviewSignGeneratedKey(
            keyHandle: new byte[32],
            blindingPublicKey: PkBl,
            kemPublicKey: PkKem,
            derivedKeyAlgorithm: CoseAlgorithm.ArkgP256);

        var ctxB = Encoding.ASCII.GetBytes("ARKG-P256.alt context");

        // Act
        var derivedA = generatedKey.DerivePublicKey(IkmA, CtxA);
        var derivedC = generatedKey.DerivePublicKey(IkmA, ctxB);

        // Assert
        Assert.NotEqual(derivedA.PublicKey.ToArray(), derivedC.PublicKey.ToArray());
        // ArkgKeyHandle also differs because it includes context-derived material
        Assert.NotEqual(derivedA.ArkgKeyHandle.ToArray(), derivedC.ArkgKeyHandle.ToArray());
    }

    private static byte[] HexToBytes(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return bytes;
    }
}
