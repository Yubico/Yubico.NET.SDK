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

namespace Yubico.YubiKey.Piv.Commands;

public class ImportKeyResponseTests
{
    [Fact]
    public void Constructor_NullResponseApdu_ThrowsException()
    {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
        _ = Assert.Throws<ArgumentNullException>(() => new ImportAsymmetricKeyResponse(null));
#pragma warning restore CS8625
    }

    [Theory]
    [InlineData(SWConstants.Success)]
    [InlineData(SWConstants.SecurityStatusNotSatisfied)]
    [InlineData(SWConstants.FunctionNotSupported)]
    public void Constructor_SetsStatusWordCorrectly(
        short expectedStatusWord)
    {
        var sw1 = unchecked((byte)(expectedStatusWord >> 8));
        var sw2 = unchecked((byte)expectedStatusWord);
        var responseApdu = new ResponseApdu(new[] { sw1, sw2 });
        var response = new ImportAsymmetricKeyResponse(responseApdu);

        var StatusWord = response.StatusWord;

        Assert.Equal(expectedStatusWord, StatusWord);
    }

    [Theory]
    [InlineData(SWConstants.Success, ResponseStatus.Success)]
    [InlineData(SWConstants.SecurityStatusNotSatisfied, ResponseStatus.AuthenticationRequired)]
    [InlineData(SWConstants.FunctionNotSupported, ResponseStatus.Failed)]
    public void Constructor_SetsStatusCorrectly(
        short statusWord,
        ResponseStatus expected)
    {
        var sw1 = unchecked((byte)(statusWord >> 8));
        var sw2 = unchecked((byte)statusWord);
        var responseApdu = new ResponseApdu(new[] { sw1, sw2 });
        var response = new ImportAsymmetricKeyResponse(responseApdu);

        var Status = response.Status;

        Assert.Equal(expected, Status);
    }
}
