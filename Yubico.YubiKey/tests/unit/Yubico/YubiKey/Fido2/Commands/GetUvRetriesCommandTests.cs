// Copyright 2022 Yubico AB
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

namespace Yubico.YubiKey.Fido2.Commands
{
    public class GetUvRetriesCommandTests
    {
        [Fact]
        public void Constructor_Succeeds()
        {
            var command = new GetUvRetriesCommand();

            Assert.NotNull(command);
        }

        [Fact]
        public void CreateCommandApdu_CreatesCorrectApdu()
        {
            var command = new GetUvRetriesCommand();
            var apdu = command.CreateCommandApdu();

            byte[] expectedData =
            {
                0x06, // authenticatorClientPin (0x06)
                0xA1, // map (1 entry)
                0x02, 0x07 // subcommand = 7
            };

            Assert.Equal(expected: 0, apdu.Cla);
            Assert.Equal(expected: 0x10, apdu.Ins);
            Assert.Equal(expected: 0, apdu.P1);
            Assert.Equal(expected: 0, apdu.P2);
            Assert.True(apdu.Data.Span.SequenceEqual(expectedData));
        }
    }
}
