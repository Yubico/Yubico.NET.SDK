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

namespace Yubico.YubiKey.Otp.Commands;

public class SwapSlotsCommandTests
{
    [Fact]
    public void CreateCommandApdu_GetClaProperty_ReturnsZero()
    {
        var command = new SwapSlotsCommand();

        var cla = command.CreateCommandApdu().Cla;

        Assert.Equal(0, cla);
    }

    [Fact]
    public void CreateCommandApdu_GetInsProperty_ReturnsHex01()
    {
        var command = new SwapSlotsCommand();

        var ins = command.CreateCommandApdu().Ins;

        Assert.Equal(1, ins);
    }

    [Fact]
    public void CreateCommandApdu_GetP1Property_ReturnsHex06()
    {
        var command = new SwapSlotsCommand();

        var p1 = command.CreateCommandApdu().P1;

        Assert.Equal(0x06, p1);
    }

    [Fact]
    public void CreateCommandApdu_GetP2Property_ReturnsZero()
    {
        var command = new SwapSlotsCommand();

        var p2 = command.CreateCommandApdu().P2;

        Assert.Equal(0, p2);
    }

    [Fact]
    public void CreateCommandApdu_GetData_ReturnsEmpty()
    {
        var command = new SwapSlotsCommand();

        var data = command.CreateCommandApdu().Data;

        Assert.True(data.IsEmpty);
    }

    [Fact]
    public void CreateCommandApdu_GetNc_ReturnsZero()
    {
        var command = new SwapSlotsCommand();

        var nc = command.CreateCommandApdu().Nc;

        Assert.Equal(0, nc);
    }

    [Fact]
    public void CreateCommandApdu_GetNe_ReturnsZero()
    {
        var command = new SwapSlotsCommand();

        var ne = command.CreateCommandApdu().Ne;

        Assert.Equal(0, ne);
    }

    [Fact]
    public void CreateResponseApdu_ReturnsCorrectType()
    {
        var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
        var command = new SwapSlotsCommand();

        IYubiKeyResponse response = command.CreateResponseForApdu(responseApdu);

        Assert.True(response is ReadStatusResponse);
    }
}
