// Copyright 2021 Yubico AB
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

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public class PivAlgorithmTests
    {
        [Theory]
        [InlineData(PivAlgorithm.Rsa1024, true)]
        [InlineData(PivAlgorithm.Rsa2048, true)]
        [InlineData(PivAlgorithm.Rsa3072, true)]
        [InlineData(PivAlgorithm.Rsa4096, true)]
        [InlineData(PivAlgorithm.EccP256, true)]
        [InlineData(PivAlgorithm.EccP384, true)]
        [InlineData(PivAlgorithm.None, false)]
        [InlineData(PivAlgorithm.TripleDes, false)]
        [InlineData(PivAlgorithm.Pin, false)]
        public void IsValidAlg_ReturnsCorrect(PivAlgorithm algorithm, bool expectedResult)
        {
            bool result = algorithm.IsValidAlgorithmForGenerate();

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024, true)]
        [InlineData(PivAlgorithm.Rsa2048, true)]
        [InlineData(PivAlgorithm.Rsa3072, true)]
        [InlineData(PivAlgorithm.Rsa4096, true)]
        [InlineData(PivAlgorithm.EccP256, false)]
        [InlineData(PivAlgorithm.EccP384, false)]
        [InlineData(PivAlgorithm.None, false)]
        [InlineData(PivAlgorithm.TripleDes, false)]
        [InlineData(PivAlgorithm.Pin, false)]
        public void IsRsa_ReturnsCorrect(PivAlgorithm algorithm, bool expectedResult)
        {
            bool result = algorithm.IsRsa();

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024, false)]
        [InlineData(PivAlgorithm.Rsa2048, false)]
        [InlineData(PivAlgorithm.Rsa3072, false)]
        [InlineData(PivAlgorithm.Rsa4096, false)]
        [InlineData(PivAlgorithm.EccP256, true)]
        [InlineData(PivAlgorithm.EccP384, true)]
        [InlineData(PivAlgorithm.None, false)]
        [InlineData(PivAlgorithm.TripleDes, false)]
        [InlineData(PivAlgorithm.Pin, false)]
        public void IsEcc_ReturnsCorrect(PivAlgorithm algorithm, bool expectedResult)
        {
            bool result = algorithm.IsEcc();

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024, 1024)]
        [InlineData(PivAlgorithm.Rsa2048, 2048)]
        [InlineData(PivAlgorithm.Rsa3072, 3072)]
        [InlineData(PivAlgorithm.Rsa4096, 4096)]
        [InlineData(PivAlgorithm.EccP256, 256)]
        [InlineData(PivAlgorithm.EccP384, 384)]
        [InlineData(PivAlgorithm.None, 0)]
        [InlineData(PivAlgorithm.TripleDes, 192)]
        [InlineData(PivAlgorithm.Pin, 64)]
        public void KeySizeBits_ReturnsCorrect(PivAlgorithm algorithm, int expectedResult)
        {
            int result = algorithm.KeySizeBits();

            Assert.Equal(expectedResult, result);
        }
    }
}
