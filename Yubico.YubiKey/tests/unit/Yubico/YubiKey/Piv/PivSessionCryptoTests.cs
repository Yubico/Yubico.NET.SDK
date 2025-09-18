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
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

public class PivSessionCryptoTests : PivSessionUnitTestBase
{
    [Fact]
    public void Sign_InvalidSlot_Exception()
    {
        var dataToSign = new byte[128];
        using var random = RandomObjectUtility.GetRandomObject(null);
        random.GetBytes(dataToSign);
        dataToSign[0] &= 0x7F;

        _ = Assert.Throws<ArgumentException>(() => PivSessionMock.Sign(0x81, dataToSign));
    }

    [Fact]
    public void Sign_InvalidDataLength_Exception()
    {
        var dataToSign = new byte[127];
        using var random = RandomObjectUtility.GetRandomObject(null);
        random.GetBytes(dataToSign);

        _ = Assert.Throws<ArgumentException>(() => PivSessionMock.Sign(0x9a, dataToSign));
    }

    [Fact]
    public void Decrypt_InvalidSlot_Exception()
    {
        var dataToDecrypt = new byte[256];
        using var random = RandomObjectUtility.GetRandomObject(null);
        random.GetBytes(dataToDecrypt);
        dataToDecrypt[0] &= 0x7F;

        _ = Assert.Throws<ArgumentException>(() => PivSessionMock.Decrypt(0xf9, dataToDecrypt));
    }

    [Fact]
    public void Decrypt_InvalidDataLength_Exception()
    {
        var dataToDecrypt = new byte[255];
        using var random = RandomObjectUtility.GetRandomObject(null);
        random.GetBytes(dataToDecrypt);

        _ = Assert.Throws<ArgumentException>(() => PivSessionMock.Decrypt(0x9a, dataToDecrypt));
    }

    [Fact]
    public void KeyAgree_NullPublicKey_Exception()
    {
        _ = Assert.Throws<ArgumentNullException>(() => PivSessionMock.KeyAgree(0x9a, (IPublicKey)null!));
    }

    [Fact]
    public void KeyAgree_EmptyPublicKey_Exception()
    {
        var publicKey = new EmptyPublicKey();
        _ = Assert.Throws<ArgumentException>(() => PivSessionMock.KeyAgree(0x9a, publicKey));
    }

    [Fact]
    public void KeyAgree_InvalidPublicKey_Exception()
    {
        var testPublicKey = TestKeys.GetTestPublicKey(KeyType.RSA1024); // Cant be used for key agreement
        var publicKey = testPublicKey.AsPublicKey();

        _ = Assert.Throws<ArgumentException>(() => PivSessionMock.KeyAgree(0x9a, publicKey));
    }
}
