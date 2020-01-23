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

using System;
using System.Collections.Generic;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.Core.Buffers;
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class MakeCredentialCommandTests
    {
        [Fact]
        public void Constructor_GivenBadMakeCredentialInput_ThrowsCtap2DataException()
        {
            var makeCredentialInput = new MakeCredentialInput();

            _ = Assert.Throws<Ctap2DataException>(() => new MakeCredentialCommand(makeCredentialInput));
        }

        private static MakeCredentialInput GetValidMakeCredentialInput() => new MakeCredentialInput()
        {
            ClientDataHash = Hex.HexToBytes("687134968222EC17202E42505F8ED2B16AE22F16BB05B88C25DB9E602645F141"),
            RelyingParty = new RelyingParty()
            {
                Name = "Acme",
                Id = "example.com"
            },
            User = new PublicKeyCredentialUserEntity()
            {
                Id = Hex.HexToBytes("3082019330820138A0030201023082019330820138A003020102308201933082"),
                Icon = new Uri("https://pics.example.com/00/p/aBjjjpqPb.png"),
                Name = "johnpsmith@example.com",
                DisplayName = "John P. Smith"
            },
            PublicKeyCredentialParameters = new PublicKeyCredentialParameter[]
                    {
                        new PublicKeyCredentialParameter()
                        {
                            Algorithm = CoseAlgorithmIdentifier.ES256,
                            Type = "public-key"
                        },
                        new PublicKeyCredentialParameter()
                        {
                            Algorithm = CoseAlgorithmIdentifier.RS256,
                            Type = "public-key"
                        }
                    },
            Options = new Dictionary<string, bool>()
                    {
                        { "rk", true }
                    }
        };

        [Fact]
        public void Constructor_GivenValidMakeCredentialInput_Succeeds()
        {
            MakeCredentialInput makeCredentialInput = GetValidMakeCredentialInput();

           _ = new MakeCredentialCommand(makeCredentialInput);
        }

        [Fact]
        public void CreateCommandApdu_GivenValidSetup_Succeeds()
        {
            MakeCredentialInput makeCredentialInput = GetValidMakeCredentialInput();

            var makeCredentialCommand = new MakeCredentialCommand(makeCredentialInput);

            _ = makeCredentialCommand.CreateCommandApdu();
        }

        [Fact]
        public void CreateCommandApdu_GivenValidSetup_SetsInsToHex10()
        {
            MakeCredentialInput makeCredentialInput = GetValidMakeCredentialInput();

            var makeCredentialCommand = new MakeCredentialCommand(makeCredentialInput);

            CommandApdu commandApdu = makeCredentialCommand.CreateCommandApdu();

            Assert.Equal(0x10, commandApdu.Ins);
        }

        [Fact]
        public void CreateCommandApdu_GivenValidSetup_SetsDataByte0ToHex01()
        {
            MakeCredentialInput makeCredentialInput = GetValidMakeCredentialInput();

            var makeCredentialCommand = new MakeCredentialCommand(makeCredentialInput);

            CommandApdu commandApdu = makeCredentialCommand.CreateCommandApdu();

            Assert.Equal(0x01, commandApdu.Data.Span[0]);
        }

        [Fact]
        public void CreateCommandApdu_GivenValidSetup_SetsDataAfterByte1ToCbor()
        {
            MakeCredentialInput makeCredentialInput = GetValidMakeCredentialInput();

            var makeCredentialCommand = new MakeCredentialCommand(makeCredentialInput);

            CommandApdu commandApdu = makeCredentialCommand.CreateCommandApdu();

            Assert.Equal(Ctap2CborSerializer.Serialize(makeCredentialInput), commandApdu.Data.ToArray()[1..]);
        }
    }
}
