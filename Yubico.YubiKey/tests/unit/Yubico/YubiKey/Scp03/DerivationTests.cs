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
using Xunit;
using Yubico.Core.Buffers;

namespace Yubico.YubiKey.Scp03
{
    public class DerivationTests
    {
        //Derive(byte dataDerivationConstant, byte outputLen, byte[] kdfKey, byte[] hostChallenge, byte[] cardChallenge)
        private static byte[] GetBadKey() => Hex.HexToBytes("4041424344454647");
        private static byte[] GetBadChallenge() => Hex.HexToBytes("41424344454647");
        private static byte[] GetHostChallenge() => Hex.HexToBytes("360CB43F4301B894");
        private static byte[] GetCardChallenge() => Hex.HexToBytes("CAAFA4DAC615236A");
        private static byte[] GetKey() => Hex.HexToBytes("FC90AA67CDC5DABFD5051663045DFA23");
        private static byte[] GetCorrectDeriveOutput() => Hex.HexToBytes("45330AB30BB1A079");

        [Fact]
        public void Derive_GivenBadChallengeLen_ThrowsSecureChannelException()
        {
            _ = Assert.Throws<SecureChannelException>(() => Derivation.Derive(Derivation.DDC_HOST_CRYPTOGRAM, 0x40, GetKey(), GetBadChallenge(), GetCardChallenge()));
        }

        [Fact]
        public void Derive_GivenBadOutputLen_ThrowsSecureChannelException()
        {
            _ = Assert.Throws<SecureChannelException>(() => Derivation.Derive(Derivation.DDC_HOST_CRYPTOGRAM, 0xC0, GetKey(), GetHostChallenge(), GetCardChallenge()));
        }

        [Fact]
        public void Derive_GivenBadKey_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => Derivation.Derive(Derivation.DDC_HOST_CRYPTOGRAM,0x40,  GetBadKey(), GetHostChallenge(), GetCardChallenge()));
        }

        [Fact]
        public void Derive_GivenCorrectVals_ReturnsCorrectHostCryptogram()
        {
            byte[] hostCryptogram = Derivation.Derive(Derivation.DDC_HOST_CRYPTOGRAM, 0x40, GetKey(), GetHostChallenge(), GetCardChallenge());
            Assert.Equal(GetCorrectDeriveOutput(), hostCryptogram);
        }
    }
}
