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
using System.Text.Json;
using Xunit;
using Yubico.Core.Buffers;


/// <summary>
/// Test data is gathered from production YubiKey devices. Parse test hex data using http://cbor.me/.
/// </summary>
namespace Yubico.YubiKey.Fido2.Serialization
{
    public class Ctap2CborSerializerTests
    {
        [Theory]
        [InlineData("A20183665532465F5632684649444F5F325F306C4649444F5F325F315F50524503502FC0579F811347EAB116BB5A8DB9202A")]
        public void Deserialize_GivenMissingOptionalFields_ParsesCorrectly(string cborDataHex)
        {
            DeviceInfo deviceInfo = Ctap2CborSerializer.Deserialize<DeviceInfo>(Hex.HexToBytes(cborDataHex));
            Assert.Equal(deviceInfo.Versions, new string[] { "U2F_V2", "FIDO_2_0", "FIDO_2_1_PRE" });
            Assert.Null(deviceInfo.Extensions);
        }

        [Theory]
        [InlineData("AA0183665532465F5632684649444F5F325F306C4649444F5F325F315F50524502826B6372656450726F746563746B686D61632D73656372657403502FC0579F811347EAB116BB5A8DB9202A04A562726BF5627570F564706C6174F469636C69656E7450696EF47563726564656E7469616C4D676D7450726576696577F5051904B006810107080818800982636E6663637573620A82A263616C672664747970656A7075626C69632D6B6579A263616C672764747970656A7075626C69632D6B6579")]
        public void Deserialize_GivenProvidedOptionalFields_ParsesCorrectly(string cborDataHex)
        {
            DeviceInfo deviceInfo = Ctap2CborSerializer.Deserialize<DeviceInfo>(Hex.HexToBytes(cborDataHex));
            Assert.NotNull(deviceInfo.Options);
            Assert.True(deviceInfo.Options?["rk"]);
            Assert.False(deviceInfo.Options?["plat"]);
            Assert.Equal(deviceInfo.Versions, new string[] { "U2F_V2", "FIDO_2_0", "FIDO_2_1_PRE" });
        }

        public static IEnumerable<object[]> GetTestDeviceInfo()
        {
            yield return new object[] { new DeviceInfo() {
                Versions = new string[] { "U2F_V2", "FIDO_2_0", "FIDO_2_1_PRE" },
                Extensions = new string[] { "credProtect", "hmac-secret"},
                AAGuid = Hex.HexToBytes("2FC0579F811347EAB116BB5A8DB9202A"),
                Options = new Dictionary<string, bool>()
                {
                    { "rk", true },
                    { "up", true },
                    { "plat", false },
                    { "clientPin", false },
                    { "credentialMgmtPreview", true }
                },
                MaxMessageSize = 1200,
                PinUserVerificationAuthenticatorProtocols = new int[] { 1 }
            }, "A60183665532465F5632684649444F5F325F306C4649444F5F325F315F50524502826B6372656450726F746563746B686D61632D73656372657403502FC0579F811347EAB116BB5A8DB9202A04A562726BF5627570F564706C6174F469636C69656E7450696EF47563726564656E7469616C4D676D7450726576696577F5051904B0068101"
            };
        }

        [Theory]
        [MemberData(nameof(GetTestDeviceInfo))]
        internal void Serialize_GivenProvidedOptionalFields_OutputsCorrectly(DeviceInfo ctap2DeviceInfo, string correctCborDataHex)
        {
            byte[] cborEncoded = Ctap2CborSerializer.Serialize(ctap2DeviceInfo);

            Assert.Equal(correctCborDataHex, Hex.BytesToHex(cborEncoded));
        }

        public static IEnumerable<object[]> GetMakeCredentialTestData()
        {
            yield return new object[]
            {
                new MakeCredentialInput()
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
                }, "A5015820687134968222EC17202E42505F8ED2B16AE22F16BB05B88C25DB9E602645F14102A26269646B6578616D706C652E636F6D646E616D656441636D6503A462696458203082019330820138A0030201023082019330820138A0030201023082019330826469636F6E782B68747470733A2F2F706963732E6578616D706C652E636F6D2F30302F702F61426A6A6A707150622E706E67646E616D65766A6F686E70736D697468406578616D706C652E636F6D6B646973706C61794E616D656D4A6F686E20502E20536D6974680482A263616C672664747970656A7075626C69632D6B6579A263616C6739010064747970656A7075626C69632D6B657907A162726BF5"
            };
        }

        public static IEnumerable<object[]> GetGetAssertionTestData()
        {
            yield return new object[]
            {
                new GetAssertionInput()
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
                }, "A4016B6578616D706C652E636F6D025820687134968222EC17202E42505F8ED2B16AE22F16BB05B88C25DB9E602645F1410382A26269645840F22006DE4F905AF68A43942F024F2A5ECE603D9C6D4B3DF8BE08ED01FC442646D034858AC75BED3FD580BF9808D94FCBEE82B9B2EF6677AF0ADCC35852EA6B9E64747970656A7075626C69632D6B6579A26269645832030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030364747970656A7075626C69632D6B657905A1627576F5"
            };
        }

        [Theory]
        [MemberData(nameof(GetMakeCredentialTestData))]
        internal void Serialize_GivenMakeCredential_OutputsCorrectly(MakeCredentialInput amci, string correctCborDataHex)
        {
            byte[] cborEncoded = Ctap2CborSerializer.Serialize(amci);

            Assert.Equal(correctCborDataHex, Hex.BytesToHex(cborEncoded));
        }

        [Theory]
        [MemberData(nameof(GetMakeCredentialTestData))]
        internal void Deserialize_GivenMakeCredential_OutputsCorrectly(MakeCredentialInput correctAmci, string cborDataHex) {
            MakeCredentialInput amci = Ctap2CborSerializer.Deserialize<MakeCredentialInput>(Hex.HexToBytes(cborDataHex));

            Assert.Equal(JsonSerializer.Serialize(correctAmci), JsonSerializer.Serialize(amci));
        }

        [Theory]
        [MemberData(nameof(GetGetAssertionTestData))]
        internal void Serialize_GivenGetAssertion_OutputsCorrectly(GetAssertionInput amci, string correctCborDataHex)
        {
            byte[] cborEncoded = Ctap2CborSerializer.Serialize(amci);

            Assert.Equal(correctCborDataHex, Hex.BytesToHex(cborEncoded));
        }

        [Theory]
        [MemberData(nameof(GetGetAssertionTestData))]
        internal void Deserialize_GivenGetAssertion_OutputsCorrectly(GetAssertionInput correctAmci, string cborDataHex)
        {
            GetAssertionInput amci = Ctap2CborSerializer.Deserialize<GetAssertionInput>(Hex.HexToBytes(cborDataHex));

            Assert.Equal(JsonSerializer.Serialize(correctAmci), JsonSerializer.Serialize(amci));
        }

        [Theory]
        [InlineData("A263616C672664747970656A7075626C69632D6B6579")]
        public void Deserialize_GivenEnumFields_ParsesCorrectly(string cborDataHex)
        {
            PublicKeyCredentialParameter publicKeyCredentialParameter = Ctap2CborSerializer.Deserialize<PublicKeyCredentialParameter>(Hex.HexToBytes(cborDataHex));
            Assert.Equal(CoseAlgorithmIdentifier.ES256, publicKeyCredentialParameter.Algorithm);
        }

        public static IEnumerable<object[]> GetEnumTestData()
        {
            yield return new object[]
            {
                new PublicKeyCredentialParameter()
                {
                    Algorithm = CoseAlgorithmIdentifier.ES256,
                    Type = "public-key"
                },
                "A263616C672664747970656A7075626C69632D6B6579"
            };
        }

        [Theory]
        [MemberData(nameof(GetEnumTestData))]
        internal void Serialize_GivenEnumFields_SerializesCorrectly(PublicKeyCredentialParameter publicKeyCredentialParameter, string correctCborDataHex)
        {
            byte[] cborEncoded = Ctap2CborSerializer.Serialize(publicKeyCredentialParameter);

            Assert.Equal(correctCborDataHex, Hex.BytesToHex(cborEncoded));
        }
    }
}
