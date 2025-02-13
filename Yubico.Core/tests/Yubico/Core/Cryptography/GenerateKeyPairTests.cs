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
using Xunit;

namespace Yubico.Core.Cryptography
{
    public class GenerateKeyPairTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Generate_PrivateValueSet(int curveNum)
        {
            IEcdhPrimitives ecdhObject = EcdhPrimitives.Create();
            ECCurve ecCurve = CryptoSupport.GetNamedCurve(curveNum);
            ECParameters keyPair = ecdhObject.GenerateKeyPair(ecCurve);

            Assert.NotNull(keyPair.D);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Generate_XCoordinateSet(int curveNum)
        {
            IEcdhPrimitives ecdhObject = EcdhPrimitives.Create();
            ECCurve ecCurve = CryptoSupport.GetNamedCurve(curveNum);
            ECParameters keyPair = ecdhObject.GenerateKeyPair(ecCurve);

            Assert.NotNull(keyPair.Q.X);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Generate_YCoordinateSet(int curveNum)
        {
            IEcdhPrimitives ecdhObject = EcdhPrimitives.Create();
            ECCurve ecCurve = CryptoSupport.GetNamedCurve(curveNum);
            ECParameters keyPair = ecdhObject.GenerateKeyPair(ecCurve);

            Assert.NotNull(keyPair.Q.Y);
        }
    }
}
