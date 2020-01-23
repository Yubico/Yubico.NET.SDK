// Copyright 2021 Yubico AB
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

using System.Collections.Generic;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.Core.Buffers;
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class GetAssertionCommandTests
    {
        [Fact]
        public void Constructor_GivenBadGetAssertionInput_ThrowsCtap2DataException()
        {
            var getAssertionInput = new GetAssertionInput();

            _ = Assert.Throws<Ctap2DataException>(() => new GetAssertionCommand(getAssertionInput));
        }

        private static GetAssertionInput GetValidGetAssertionInput() => new GetAssertionInput()
        {
            ClientDataHash = Hex.HexToBytes("687134968222EC17202E42505F8ED2B16AE22F16BB05B88C25DB9E602645F141"),
            RelyingPartyId = "example.com",
            AllowList = new PublicKeyCredentialDescriptor[]
                    {
                        new PublicKeyCredentialDescriptor()
                        {
                            Id = Hex.HexToBytes("F22006DE4F905AF68A43942F024F2A5ECE603D9C6D4B3DF8BE08ED01FC442646D034858AC75BED3FD580BF9808D94FCBEE82B9B2EF6677AF0ADCC35852EA6B9E"),
                            Type = "public-key"
                        },
                        new PublicKeyCredentialDescriptor()
                        {
                            Id = Hex.HexToBytes("0303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303"),
                            Type = "public-key"
                        }
                    },
            Options = new Dictionary<string, bool>()
                    {
                        { "uv", true }
                    }
        };

        [Fact]
        public void Constructor_GivenValidGetAssertionInput_Succeeds()
        {
            GetAssertionInput getAssertionInput = GetValidGetAssertionInput();

            _ = new GetAssertionCommand(getAssertionInput);
        }

        [Fact]
        public void CreateCommandApdu_GivenValidSetup_Succeeds()
        {
            GetAssertionInput getAssertionInput = GetValidGetAssertionInput();

            var GetAssertionCommand = new GetAssertionCommand(getAssertionInput);

            _ = GetAssertionCommand.CreateCommandApdu();
        }

        [Fact]
        public void CreateCommandApdu_GivenValidSetup_SetsInsToHex10()
        {
            GetAssertionInput getAssertionInput = GetValidGetAssertionInput();

            var GetAssertionCommand = new GetAssertionCommand(getAssertionInput);

            CommandApdu commandApdu = GetAssertionCommand.CreateCommandApdu();

            Assert.Equal(0x10, commandApdu.Ins);
        }

        [Fact]
        public void CreateCommandApdu_GivenValidSetup_SetsDataByte0ToHex02()
        {
            GetAssertionInput getAssertionInput = GetValidGetAssertionInput();

            var GetAssertionCommand = new GetAssertionCommand(getAssertionInput);

            CommandApdu commandApdu = GetAssertionCommand.CreateCommandApdu();

            Assert.Equal(0x02, commandApdu.Data.Span[0]);
        }

        [Fact]
        public void CreateCommandApdu_GivenValidSetup_SetsDataAfterByte1ToCbor()
        {
            GetAssertionInput getAssertionInput = GetValidGetAssertionInput();

            var GetAssertionCommand = new GetAssertionCommand(getAssertionInput);

            CommandApdu commandApdu = GetAssertionCommand.CreateCommandApdu();

            Assert.Equal(Ctap2CborSerializer.Serialize(getAssertionInput), commandApdu.Data.ToArray()[1..]);
        }
    }
}
