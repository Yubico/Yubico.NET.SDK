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

using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands;

public class GetPagedDeviceInfoCommandTests
{
    [Fact]
    public void CreateCommandApdu_GetClaProperty_ReturnsZero()
    {
        var command = new GetPagedDeviceInfoCommand();

        Assert.Equal(0, command.CreateCommandApdu().Cla);
    }

    [Fact]
    public void CreateCommandApdu_GetInsProperty_Returns0xC2()
    {
        var command = new GetPagedDeviceInfoCommand();

        Assert.Equal(0xC2, command.CreateCommandApdu().Ins);
    }

    [Fact]
    public void CreateCommandApdu_GetP1Property_ReturnsZero()
    {
        var command = new GetPagedDeviceInfoCommand();

        Assert.Equal(0, command.CreateCommandApdu().P1);
    }

    [Fact]
    public void CreateCommandApdu_GetP2Property_ReturnsZero()
    {
        var command = new GetPagedDeviceInfoCommand();

        Assert.Equal(0, command.CreateCommandApdu().P2);
    }

    [Fact]
    public void CreateCommandApdu_GetNc_WithNewCommand_ReturnsCorrectLengthOfOnlyOne()
    {
        var command = new GetPagedDeviceInfoCommand();

        Assert.Equal(1, command.CreateCommandApdu().Nc);
    }

    [Fact]
    public void CreateCommandApdu_GetData_WithNewCommand_ReturnsLengthOfOnlyOne()
    {
        var command = new GetPagedDeviceInfoCommand();

        Assert.Equal(1, command.CreateCommandApdu().Data.Length);
    }

    [Fact]
    public void CreateResponseApdu_ReturnsCorrectType()
    {
        var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
        var command = new GetPagedDeviceInfoCommand();
        var response = command.CreateResponseForApdu(responseApdu);

        Assert.NotNull(response);
    }
}
