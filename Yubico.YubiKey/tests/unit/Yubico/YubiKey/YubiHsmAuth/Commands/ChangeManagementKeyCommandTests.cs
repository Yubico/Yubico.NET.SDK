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
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth.Commands;

public class ChangeManagementKeyCommandTests
{
    private static readonly byte[] _currentMgmtKey =
        new byte[16] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7 };

    private static readonly byte[] _newMgmtKey =
        new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    private ChangeManagementKeyCommand _command => new(_currentMgmtKey, _newMgmtKey);

    [Fact]
    public void Application_Get_ReturnsYubiHsmAuth()
    {
        var command = _command;

        Assert.Equal(YubiKeyApplication.YubiHsmAuth, command.Application);
    }

    [Fact]
    public void CreateCommandApdu_Cla0()
    {
        var command = _command;
        var apdu = command.CreateCommandApdu();

        Assert.Equal(0, apdu.Cla);
    }

    [Fact]
    public void CreateCommandApdu_Ins0x08()
    {
        var command = _command;
        var apdu = command.CreateCommandApdu();

        Assert.Equal(0x08, apdu.Ins);
    }

    [Fact]
    public void CreateCommandApdu_P1Is0()
    {
        var command = _command;
        var apdu = command.CreateCommandApdu();

        Assert.Equal(0, apdu.P1);
    }

    [Fact]
    public void CreateCommandApdu_P2Is0()
    {
        var command = _command;
        var apdu = command.CreateCommandApdu();

        Assert.Equal(0, apdu.P2);
    }

    [Fact]
    public void CreateCommandApdu_DataContainsTwoMgmtKeyTags()
    {
        var command = _command;
        var apdu = command.CreateCommandApdu();

        var reader = new TlvReader(apdu.Data);

        var mgmtKeyTagCount = 0;

        while (reader.HasData)
        {
            var tag = reader.PeekTag();
            if (tag == 0x7b)
            {
                mgmtKeyTagCount++;
            }

            _ = reader.ReadValue(tag);
        }

        Assert.Equal(2, mgmtKeyTagCount);
    }

    [Fact]
    public void CreateCommandApdu_DataContainsCurrentMgmtKeyValue()
    {
        var command = _command;
        var apdu = command.CreateCommandApdu();

        var reader = new TlvReader(apdu.Data);

        var mgmtKeyTagCount = 0;
        var value = Array.Empty<byte>();

        while (reader.HasData && mgmtKeyTagCount < 1)
        {
            var tag = reader.PeekTag();
            if (tag == 0x7b)
            {
                mgmtKeyTagCount++;
            }

            value = reader.ReadValue(tag).ToArray();
        }

        Assert.Equal(_currentMgmtKey, value);
    }

    [Fact]
    public void CreateCommandApdu_DataContainsNewMgmtKeyValue()
    {
        var command = _command;
        var apdu = command.CreateCommandApdu();

        var reader = new TlvReader(apdu.Data);

        var mgmtKeyTagCount = 0;
        var value = Array.Empty<byte>();

        while (reader.HasData && mgmtKeyTagCount < 2)
        {
            var tag = reader.PeekTag();
            if (tag == 0x7b)
            {
                mgmtKeyTagCount++;
            }

            value = reader.ReadValue(tag).ToArray();
        }

        Assert.Equal(_newMgmtKey, value);
    }
}
