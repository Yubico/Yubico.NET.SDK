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

namespace Yubico.YubiKey.Fido2.Commands
{
    public class GetPinRetriesCommandTests
    {
        [Fact]
        public void Constructor_Succeeds()
        {
            var command = new GetPinRetriesCommand();

            Assert.NotNull(command);
        }

        [Fact]
        public void CreateCommandApdu_CreatesCorrectApdu()
        {
            var command = new GetPinRetriesCommand();
            CommandApdu apdu = command.CreateCommandApdu();

            byte[] expectedData = new byte[]
            {
                0x06, // authenticatorClientPin (0x06)
                0xA1, // map (1 entry)
                0x02, 0x01 // subcommand = 1
            };

            Assert.Equal(0, apdu.Cla);
            Assert.Equal(0x10, apdu.Ins);
            Assert.Equal(0, apdu.P1);
            Assert.Equal(0, apdu.P2);
            Assert.True(apdu.Data.Span.SequenceEqual(expectedData));
        }
    }
}
