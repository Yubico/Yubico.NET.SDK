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

namespace Yubico.YubiKey.U2f.Commands;

public class AuthenticateCommandTests
{
    [Fact]
    public void Constructor_CorrectApplication()
    {
        var cmd = new AuthenticateCommand(
            U2fAuthenticationType.EnforceUserPresence,
            RegistrationDataTests.GetAppIdArray(true),
            RegistrationDataTests.GetClientDataHashArray(true),
            RegistrationDataTests.GetKeyHandleArray(true, out _));
        Assert.Equal(YubiKeyApplication.FidoU2f, cmd.Application);
    }

    [Theory]
    [InlineData(U2fAuthenticationType.CheckOnly)]
    [InlineData(U2fAuthenticationType.EnforceUserPresence)]
    [InlineData(U2fAuthenticationType.DontEnforceUserPresence)]
    public void Constructor_CorrectControlByte(
        U2fAuthenticationType controlByte)
    {
        var cmd = new AuthenticateCommand(
            controlByte,
            RegistrationDataTests.GetAppIdArray(true),
            RegistrationDataTests.GetClientDataHashArray(true),
            RegistrationDataTests.GetKeyHandleArray(true, out _));
        Assert.Equal(controlByte, cmd.ControlByte);
    }

    [Theory]
    [InlineData(U2fAuthenticationType.CheckOnly)]
    [InlineData(U2fAuthenticationType.EnforceUserPresence)]
    [InlineData(U2fAuthenticationType.DontEnforceUserPresence)]
    public void Constructor_ChangeControlByte_Correct(
        U2fAuthenticationType controlByte)
    {
        var newByte = controlByte switch
        {
            U2fAuthenticationType.CheckOnly => U2fAuthenticationType.EnforceUserPresence,
            U2fAuthenticationType.EnforceUserPresence => U2fAuthenticationType.DontEnforceUserPresence,
            U2fAuthenticationType.DontEnforceUserPresence => U2fAuthenticationType.CheckOnly,
            _ => U2fAuthenticationType.Unknown
        };

#pragma warning disable IDE0017 // Testing specific behavior
        var cmd = new AuthenticateCommand(
            controlByte,
            RegistrationDataTests.GetAppIdArray(true),
            RegistrationDataTests.GetClientDataHashArray(true),
            RegistrationDataTests.GetKeyHandleArray(true, out _));
        cmd.ControlByte = newByte;
#pragma warning restore IDE0017
        Assert.Equal(newByte, cmd.ControlByte);
    }

    [Fact]
    public void InvalidClientDataLength_Throws()
    {
        _ = Assert.Throws<ArgumentException>(() => new AuthenticateCommand(
            U2fAuthenticationType.EnforceUserPresence,
            RegistrationDataTests.GetKeyHandleArray(true, out _),
            RegistrationDataTests.GetAppIdArray(true),
            RegistrationDataTests.GetKeyHandleArray(true, out _)));
    }

    [Fact]
    public void Reset_InvalidClientDataLength_Throws()
    {
        var cmd = new AuthenticateCommand(
            U2fAuthenticationType.EnforceUserPresence,
            RegistrationDataTests.GetAppIdArray(true),
            RegistrationDataTests.GetClientDataHashArray(true),
            RegistrationDataTests.GetKeyHandleArray(true, out _));

        _ = Assert.Throws<ArgumentException>(() =>
            cmd.ClientDataHash = RegistrationDataTests.GetKeyHandleArray(true, out _));
    }

    [Fact]
    public void InvalidAppIdLength_Throws()
    {
        _ = Assert.Throws<ArgumentException>(() => new AuthenticateCommand(
            U2fAuthenticationType.EnforceUserPresence,
            RegistrationDataTests.GetClientDataHashArray(true),
            RegistrationDataTests.GetKeyHandleArray(true, out _),
            RegistrationDataTests.GetKeyHandleArray(true, out _)));
    }

    [Fact]
    public void Reset_InvalidAppIdLength_Throws()
    {
        var cmd = new AuthenticateCommand(
            U2fAuthenticationType.EnforceUserPresence,
            RegistrationDataTests.GetAppIdArray(true),
            RegistrationDataTests.GetClientDataHashArray(true),
            RegistrationDataTests.GetKeyHandleArray(true, out _));

        _ = Assert.Throws<ArgumentException>(() =>
            cmd.ApplicationId = RegistrationDataTests.GetKeyHandleArray(true, out _));
    }

    [Fact]
    public void InvalidKeyHandleLength_Throws()
    {
        _ = Assert.Throws<ArgumentException>(() => new AuthenticateCommand(
            U2fAuthenticationType.EnforceUserPresence,
            RegistrationDataTests.GetAppIdArray(true),
            RegistrationDataTests.GetClientDataHashArray(true),
            RegistrationDataTests.GetAppIdArray(true)));
    }

    [Fact]
    public void Reset_InvalidKeyHandleLength_Throws()
    {
        var cmd = new AuthenticateCommand(
            U2fAuthenticationType.EnforceUserPresence,
            RegistrationDataTests.GetAppIdArray(true),
            RegistrationDataTests.GetClientDataHashArray(true),
            RegistrationDataTests.GetKeyHandleArray(true, out _));

        _ = Assert.Throws<ArgumentException>(() =>
            cmd.KeyHandle = RegistrationDataTests.GetClientDataHashArray(true));
    }

    [Fact]
    public void CommandApdu_ClaProperty_Zero()
    {
        var cmd = new AuthenticateCommand(
            U2fAuthenticationType.EnforceUserPresence,
            RegistrationDataTests.GetAppIdArray(true),
            RegistrationDataTests.GetClientDataHashArray(true),
            RegistrationDataTests.GetKeyHandleArray(true, out _));
        var cmdApdu = cmd.CreateCommandApdu();

        Assert.Equal(0, cmdApdu.Cla);
    }

    [Fact]
    public void CommandApdu_InsProperty_Three()
    {
        var cmd = new AuthenticateCommand(
            U2fAuthenticationType.EnforceUserPresence,
            RegistrationDataTests.GetAppIdArray(true),
            RegistrationDataTests.GetClientDataHashArray(true),
            RegistrationDataTests.GetKeyHandleArray(true, out _));
        var cmdApdu = cmd.CreateCommandApdu();

        Assert.Equal(3, cmdApdu.Ins);
    }
}
