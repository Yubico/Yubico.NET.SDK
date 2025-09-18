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

namespace Yubico.YubiKey.Otp.Commands;

public class SelectNdefDataCommandTests
{
    [Fact]
    public void Application_Get_AlwaysEqualsOtpNdef()
    {
        var command = new SelectNdefDataCommand();

        var application = command.Application;

        Assert.Equal(YubiKeyApplication.OtpNdef, application);
    }

    [Fact]
    public void CreateCommandApdu_GetClaProperty_ReturnsZero()
    {
        var command = new SelectNdefDataCommand();

        var cla = command.CreateCommandApdu().Cla;

        Assert.Equal(0, cla);
    }

    [Fact]
    public void CreateCommandApdu_GetInsProperty_ReturnsHexA4()
    {
        var command = new SelectNdefDataCommand();

        var ins = command.CreateCommandApdu().Ins;

        Assert.Equal(0xA4, ins);
    }

    [Fact]
    public void CreateCommandApdu_GetP1Property_ReturnsZero()
    {
        var command = new SelectNdefDataCommand();

        var p1 = command.CreateCommandApdu().P1;

        Assert.Equal(0, p1);
    }

    [Fact]
    public void CreateCommandApdu_GetP2Property_ReturnsHex0C()
    {
        var command = new SelectNdefDataCommand();

        var p2 = command.CreateCommandApdu().P2;

        Assert.Equal(0x0C, p2);
    }

    [Fact]
    public void CreateCommandApdu_GetData_ReturnsCorrectBuffer()
    {
        byte[] expectedBuffer = { 0xE1, 0x04 };
        var command = new SelectNdefDataCommand();

        var data = command.CreateCommandApdu().Data;

        Assert.True(data.Span.SequenceEqual(expectedBuffer));
    }

    [Fact]
    public void CreateCommandApdu_GetNc_ReturnsTwo()
    {
        var command = new SelectNdefDataCommand();

        var nc = command.CreateCommandApdu().Nc;

        Assert.Equal(2, nc);
    }

    [Fact]
    public void CreateCommandApdu_GetNe_ReturnsZero()
    {
        var command = new SelectNdefDataCommand();

        var ne = command.CreateCommandApdu().Ne;

        Assert.Equal(0, ne);
    }

    [Fact]
    public void CreateResponseForApdu_ReturnsCorrectType()
    {
        var responseApdu = new ResponseApdu(new byte[] { 0x90, 00 });
        var command = new SelectNdefDataCommand();

        IYubiKeyResponse response = command.CreateResponseForApdu(responseApdu);

        _ = Assert.IsAssignableFrom<OtpResponse>(response);
    }
}
