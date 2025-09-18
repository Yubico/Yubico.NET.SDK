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

public class PivResponseTests
{
    [Fact]
    public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullException()
    {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
        _ = Assert.Throws<ArgumentNullException>(() => new PivResponse(null));
#pragma warning restore CS8625
    }

    [Fact]
    public void StatusWord_GivenResponseApdu_EqualsSWField()
    {
        var responseApdu = new ResponseApdu(new byte[] { 24, 73 });

        var pivResponse = new PivResponse(responseApdu);

        Assert.Equal(responseApdu.SW, pivResponse.StatusWord);
    }

    [Fact]
    public void Status_GivenSuccessfulResponseApdu_ReturnsSuccess()
    {
        var responseApdu = new ResponseApdu(new byte[] { SW1Constants.Success, 0x00 });

        var pivResponse = new PivResponse(responseApdu);

        Assert.Equal(ResponseStatus.Success, pivResponse.Status);
    }

    [Fact]
    public void Status_GivenFailedResponseApdu_ReturnsFailed()
    {
        var responseApdu = new ResponseApdu(new byte[] { SW1Constants.CommandNotAllowed, 0x00 });

        var pivResponse = new PivResponse(responseApdu);

        Assert.Equal(ResponseStatus.Failed, pivResponse.Status);
    }
}
