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

using System;
using Xunit;

namespace Yubico.Core.Cryptography;

public class ComputeSharedSecretTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ComputeSecret_Matches(
        int curveNum)
    {
        var ecdhObject = EcdhPrimitives.Create();
        var ecCurve = CryptoSupport.GetNamedCurve(curveNum);
        var keyPairA = ecdhObject.GenerateKeyPair(ecCurve);
        var keyPairB = ecdhObject.GenerateKeyPair(ecCurve);

        var secretA = ecdhObject.ComputeSharedSecret(keyPairB, keyPairA.D);
        var secretB = ecdhObject.ComputeSharedSecret(keyPairA, keyPairB.D);

        var isValid = secretA.AsSpan().SequenceEqual(secretB.AsSpan());

        Assert.True(isValid);
    }
}
