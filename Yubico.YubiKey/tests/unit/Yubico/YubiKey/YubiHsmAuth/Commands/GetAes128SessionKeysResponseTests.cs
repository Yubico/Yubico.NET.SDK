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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands;

public class GetAes128SessionKeysResponseTests
{
    private static readonly byte[] _encKey =
        new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

    private static readonly byte[] _macKey =
        new byte[16] { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 };

    private static readonly byte[] _rmacKey =
        new byte[16] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31 };

    private byte[] _data()
    {
        var data = new byte[48];
        _encKey.CopyTo(data, 0);
        _macKey.CopyTo(data, 16);
        _rmacKey.CopyTo(data, 32);

        return data;
    }

    [Fact]
    public void GetData_NotSuccess_ThrowsInvalidOperationException()
    {
        var apdu = new ResponseApdu(new byte[0], SWConstants.AuthenticationMethodBlocked);

        var response = new GetAes128SessionKeysResponse(apdu);

        Action action = () => response.GetData();

        _ = Assert.Throws<InvalidOperationException>(action);
    }

    [Theory]
    [InlineData(48 - 1)]
    [InlineData(48 + 1)]
    public void GetData_InvalidDataLength_ThrowsMalformedResponseException(
        int dataLength)
    {
        var apdu = new ResponseApdu(new byte[dataLength], SWConstants.Success);

        var response = new GetAes128SessionKeysResponse(apdu);

        Action action = () => response.GetData();

        _ = Assert.Throws<MalformedYubiKeyResponseException>(action);
    }

    [Fact]
    public void GetData_Success_ReturnsExpectedEncryptionSessionKey()
    {
        var apdu = new ResponseApdu(_data(), SWConstants.Success);

        var response = new GetAes128SessionKeysResponse(apdu);
        var sessionKeys = response.GetData();

        Assert.Equal(_encKey, sessionKeys.EncryptionKey.ToArray());
    }

    [Fact]
    public void GetData_Success_ReturnsExpectedMacSessionKey()
    {
        var apdu = new ResponseApdu(_data(), SWConstants.Success);

        var response = new GetAes128SessionKeysResponse(apdu);
        var sessionKeys = response.GetData();

        Assert.Equal(_macKey, sessionKeys.MacKey.ToArray());
    }

    [Fact]
    public void GetData_Success_ReturnsExpectedRmacSessionKey()
    {
        var apdu = new ResponseApdu(_data(), SWConstants.Success);

        var response = new GetAes128SessionKeysResponse(apdu);
        var sessionKeys = response.GetData();

        Assert.Equal(_rmacKey, sessionKeys.RmacKey.ToArray());
    }
}
