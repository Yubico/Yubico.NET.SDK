// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

public class Curve25519PrivateKeyTests
{
    // RFC 7748 section 6.1, Alice's X25519 private key, encoded as PKCS#8 by
    // the OpenSSL evppkey_ecx.txt test vector.
    private const string Rfc7748AlicePrivateKeyHex =
        "77076D0A7318A57D3C16C17251B26645DF4C2F87EBC0992AB177FBA51DB92C2A";

    private const string Rfc7748AlicePkcs8Base64 =
        "MC4CAQAwBQYDK2VuBCIEIHcHbQpzGKV9PBbBclGyZkXfTC+H68CZKrF3+6UduSwq";

    [Fact]
    public void CreateFromValue_Rfc7748AliceKey_PreservesRawBytes()
    {
        var rawPrivateKey = Convert.FromHexString(Rfc7748AlicePrivateKeyHex);

        using var key = Curve25519PrivateKey.CreateFromValue(rawPrivateKey, KeyType.X25519);

        Assert.Equal(rawPrivateKey, key.PrivateKey.ToArray());
    }

    [Fact]
    public void CreateFromPkcs8_Rfc7748AliceKey_PreservesRawBytes()
    {
        var pkcs8 = Convert.FromBase64String(Rfc7748AlicePkcs8Base64);

        using var key = Curve25519PrivateKey.CreateFromPkcs8(pkcs8);

        Assert.Equal(KeyType.X25519, key.KeyType);
        Assert.Equal(Convert.FromHexString(Rfc7748AlicePrivateKeyHex), key.PrivateKey.ToArray());
    }

    [Fact]
    public void ExportPkcs8PrivateKey_Rfc7748AliceKey_PreservesExactEncoding()
    {
        var rawPrivateKey = Convert.FromHexString(Rfc7748AlicePrivateKeyHex);
        using var key = Curve25519PrivateKey.CreateFromValue(rawPrivateKey, KeyType.X25519);

        var encoded = key.ExportPkcs8PrivateKey();

        Assert.Equal(Convert.FromBase64String(Rfc7748AlicePkcs8Base64), encoded);
    }

    [Theory]
    [InlineData(KeyType.X25519, 31)]
    [InlineData(KeyType.X25519, 33)]
    [InlineData(KeyType.Ed25519, 31)]
    [InlineData(KeyType.Ed25519, 33)]
    public void CreateFromValue_WrongLength_ThrowsArgumentExceptionWithPrivateKeyParamName(KeyType keyType, int length)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Curve25519PrivateKey.CreateFromValue(new byte[length], keyType));

        Assert.Equal("privateKey", exception.ParamName);
    }

    [Fact]
    public void CreateFromValue_IncompatibleKeyType_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Curve25519PrivateKey.CreateFromValue(new byte[32], KeyType.ECP256));

        Assert.Equal("keyType", exception.ParamName);
    }

    [Fact]
    public void CreateFromValue_CopiesCallerOwnedBytes()
    {
        var rawPrivateKey = Convert.FromHexString(Rfc7748AlicePrivateKeyHex);
        var expected = rawPrivateKey.ToArray();
        using var key = Curve25519PrivateKey.CreateFromValue(rawPrivateKey, KeyType.X25519);

        CryptographicOperations.ZeroMemory(rawPrivateKey);

        Assert.Equal(expected, key.PrivateKey.ToArray());
    }

    [Fact]
    public void Dispose_ZeroesOwnedBytesAndPreventsExport()
    {
        var rawPrivateKey = Convert.FromHexString(Rfc7748AlicePrivateKeyHex);
        var key = Curve25519PrivateKey.CreateFromValue(rawPrivateKey, KeyType.X25519);
        var ownedBytes = key.PrivateKey;

        key.Dispose();

        Assert.All(ownedBytes.ToArray(), value => Assert.Equal(0, value));
        Assert.Throws<ObjectDisposedException>(() => _ = key.PrivateKey);
        Assert.Throws<ObjectDisposedException>(() => key.ExportPkcs8PrivateKey());
    }
}