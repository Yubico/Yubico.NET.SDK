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

namespace Yubico.YubiKit.SecurityDomain.UnitTests;

/// <summary>
///     Validates <see cref="Scp03HandshakeFakeConnection" />'s hand-rolled AES-128-CMAC against the
///     official RFC 4493 test vectors. This exists so the fake SCP03 device used by
///     <see cref="SecureChannelExceptionTests" /> is checked against an independent, published
///     reference rather than trusted on inspection alone.
/// </summary>
public class Scp03HandshakeFakeConnectionTests
{
    // RFC 4493 Section 4: "Test Vectors" (AES-128).
    private static readonly byte[] Key =
        Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");

    private static readonly byte[] Message =
        Convert.FromHexString(
            "6bc1bee22e409f96e93d7e117393172a" +
            "ae2d8a571e03ac9c9eb76fac45af8e51" +
            "30c81c46a35ce411e5fbc1191a0a52ef" +
            "f69f2445df4f9b17ad2b417be66c3710");

    [Theory]
    [InlineData(0, "bb1d6929e95937287fa37d129b756746")]
    [InlineData(16, "070a16b46b4d4144f79bdd9dd04a287c")]
    [InlineData(40, "dfa66747de9ae63030ca32611497c827")]
    [InlineData(64, "51f0bebf7e3b9d92fc49741779363cfe")]
    public void AesCmac_Rfc4493OfficialVectors_MatchExpectedMac(int messageLength, string expectedMacHex)
    {
        var mac = Scp03HandshakeFakeConnection.AesCmac(Key, Message[..messageLength]);

        Assert.Equal(expectedMacHex, Convert.ToHexString(mac).ToLowerInvariant());
    }
}