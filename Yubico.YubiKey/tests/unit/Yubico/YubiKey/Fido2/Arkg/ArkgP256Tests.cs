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

using Yubico.Core.Cryptography;
using Yubico.YubiKey.TestUtilities.Fido2;
using Xunit;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Regression tests for ARKG-P256 derivation.
    /// </summary>
    /// <remarks>
    /// WARNING: This code is for testing purposes only and is not intended to be a
    /// secure or complete implementation of ARKG.
    /// </remarks>
    public class ArkgP256Tests
    {
        public static TheoryData<byte[], byte[], byte[]> AdditionalRegressionVectors => new()
        {
            {
                ArkgP256RegressionVectors.IkmA,
                ArkgP256RegressionVectors.CtxA,
                ArkgP256RegressionVectors.ExpectedDerivedA
            },
            {
                ArkgP256RegressionVectors.IkmAdditionalB,
                ArkgP256RegressionVectors.CtxA,
                ArkgP256RegressionVectors.ExpectedDerivedAdditionalB
            },
            {
                ArkgP256RegressionVectors.IkmA,
                ArkgP256RegressionVectors.CtxAdditionalC,
                ArkgP256RegressionVectors.ExpectedDerivedAdditionalC
            },
        };

        [Fact]
        public void DerivePublicKey_AgainstRegressionVector_ProducesExpectedPublicKey()
        {
            (byte[] derivedPk, byte[] arkgKeyHandle) = ArkgPrimitives.Create().DerivePublicKey(
                ArkgP256RegressionVectors.BlindingPublicKey,
                ArkgP256RegressionVectors.KemPublicKey,
                ArkgP256RegressionVectors.IkmA,
                ArkgP256RegressionVectors.CtxA);

            Assert.Equal(ArkgP256RegressionVectors.ExpectedDerivedA, derivedPk);
            Assert.Equal(ArkgP256RegressionVectors.ExpectedHandleA, arkgKeyHandle);
        }

        [Fact]
        public void DerivePublicKey_DifferentIkm_ProducesDifferentKeys()
        {
            (byte[] derivedA, _) = ArkgPrimitives.Create().DerivePublicKey(
                ArkgP256RegressionVectors.BlindingPublicKey,
                ArkgP256RegressionVectors.KemPublicKey,
                ArkgP256RegressionVectors.IkmA,
                ArkgP256RegressionVectors.CtxA);
            (byte[] derivedB, _) = ArkgPrimitives.Create().DerivePublicKey(
                ArkgP256RegressionVectors.BlindingPublicKey,
                ArkgP256RegressionVectors.KemPublicKey,
                ArkgP256RegressionVectors.IkmB,
                ArkgP256RegressionVectors.CtxA);

            Assert.NotEqual(derivedA, derivedB);
            Assert.Equal(ArkgP256RegressionVectors.ExpectedDerivedA, derivedA);
            Assert.Equal(ArkgP256RegressionVectors.ExpectedDerivedB, derivedB);
        }

        [Fact]
        public void DerivePublicKey_DifferentCtx_ProducesDifferentKeys()
        {
            (byte[] derivedA, _) = ArkgPrimitives.Create().DerivePublicKey(
                ArkgP256RegressionVectors.BlindingPublicKey,
                ArkgP256RegressionVectors.KemPublicKey,
                ArkgP256RegressionVectors.IkmA,
                ArkgP256RegressionVectors.CtxA);
            (byte[] derivedC, _) = ArkgPrimitives.Create().DerivePublicKey(
                ArkgP256RegressionVectors.BlindingPublicKey,
                ArkgP256RegressionVectors.KemPublicKey,
                ArkgP256RegressionVectors.IkmA,
                ArkgP256RegressionVectors.CtxC);

            Assert.NotEqual(derivedA, derivedC);
            Assert.Equal(ArkgP256RegressionVectors.ExpectedDerivedC, derivedC);
        }

        [Theory]
        [MemberData(nameof(AdditionalRegressionVectors))]
        public void DerivePublicKey_AgainstAdditionalRegressionVector_ProducesExpectedPublicKey(
            byte[] inputKeyingMaterial,
            byte[] context,
            byte[] expectedDerivedPublicKey)
        {
            (byte[] derivedPublicKey, _) = ArkgPrimitives.Create().DerivePublicKey(
                ArkgP256RegressionVectors.BlindingPublicKey,
                ArkgP256RegressionVectors.KemPublicKey,
                inputKeyingMaterial,
                context);

            Assert.Equal(expectedDerivedPublicKey, derivedPublicKey);
        }
    }
}
