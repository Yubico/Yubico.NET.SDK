﻿// Copyright 2022 Yubico AB
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

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    public class ResetApplicationCommandTests
    {
        [Fact]
        public void Application_Get_ReturnsYubiHsmAuth()
        {
            ResetApplicationCommand command = new ResetApplicationCommand();

            Assert.Equal(YubiKeyApplication.YubiHsmAuth, command.Application);
        }

        [Fact]
        public void Constructor_ReturnsObject()
        {
            ResetApplicationCommand command = new ResetApplicationCommand();

            Assert.NotNull(command);
        }

        [Fact]
        public void CreateCommandApdu_Cla0()
        {
            ResetApplicationCommand command = new ResetApplicationCommand();
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0, apdu.Cla);
        }

        [Fact]
        public void CreateCommandApdu_Ins0x06()
        {
            ResetApplicationCommand command = new ResetApplicationCommand();
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0x06, apdu.Ins);
        }

        [Fact]
        public void CreateCommandApdu_P1Is0xde()
        {
            ResetApplicationCommand command = new ResetApplicationCommand();
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0xde, apdu.P1);
        }

        [Fact]
        public void CreateCommandApdu_P2Is0xad()
        {
            ResetApplicationCommand command = new ResetApplicationCommand();
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0xad, apdu.P2);
        }

        [Fact]
        public void CreateCommandApdu_DataLength0()
        {
            ResetApplicationCommand command = new ResetApplicationCommand();
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0, apdu.Data.Length);
        }
    }
}
