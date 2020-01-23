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
using Yubico.Core.Buffers;

namespace Yubico.YubiKey.Fido2
{
    public class Ctap2ValidationTests
    {
        public static IEnumerable<object[]> GetInvalidObjects()
        {
            object[][] invalidObjects = new object[][] {
                new[] { new MakeCredentialInput() { ClientDataHash = new byte[19] } },
                new[] { new PublicKeyCredentialParameter() { Type = "not-public-key" } },
                new[] { new PublicKeyCredentialDescriptor() { Type = "not-public-key" } },
                new[] { new GetAssertionInput() { ClientDataHash = new byte[GetAssertionInput.ExpectedClientDataHashLength], AllowList = Array.Empty<PublicKeyCredentialDescriptor>() } },
            };

            foreach (object[] args in invalidObjects)
            {
                yield return args;
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidObjects))]
        internal void Validate_GivenInvalidData_ThrowsCtap2DataException(IValidatable v)
        {
            _ = Assert.Throws<Ctap2DataException>(() => v.Validate());
        }

        public static IEnumerable<object[]> GetMissingDataObjects()
        {
            object[][] missingDataObjects = new object[][] {
                new[] { new MakeCredentialInput() {} },
                new[] { new RelyingParty() {} },
                new[] { new PublicKeyCredentialUserEntity() {} },
                new[] { new PublicKeyCredentialParameter() {} },
                new[] { new PublicKeyCredentialDescriptor() { Type = "public-key" } },
                new[] { new GetAssertionInput() {} },
                new[] { new RelyingParty() { Name = "" } },
                new[] { new PublicKeyCredentialUserEntity() { Id = Array.Empty<byte>() } },
            };

            foreach (object[] args in missingDataObjects)
            {
                yield return args;
            }
        }

        [Theory]
        [MemberData(nameof(GetMissingDataObjects))]
        internal void Validate_GivenMissingData_ThrowsCtap2DataException(IValidatable v)
        {
            _ = Assert.Throws<Ctap2DataException>(() => v.Validate());
        }

        public static IEnumerable<object[]> GetValidObjects()
        {

            var Rp = new RelyingParty()
            {
                Name = "Acme",
                Id = "example.com"
            };

            var PubKeyCredParameters = new PublicKeyCredentialParameter[]
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
            };

            var User = new PublicKeyCredentialUserEntity()
            {
                Id = Hex.HexToBytes("3082019330820138a0030201023082019330820138a003020102308201933082"),
                Icon = new Uri("https://pics.example.com/00/p/aBjjjpqPb.png"),
                Name = "johnpsmith@example.com",
                DisplayName = "John P. Smith"
            };

            byte[] ClientDataHash = Hex.HexToBytes("687134968222ec17202e42505f8ed2b16ae22f16bb05b88c25db9e602645f141");

            var Options = new Dictionary<string, bool>()
            {
                { "rk", true }
            };

            object[][] validObjects = new object[][] {
                new[] { new MakeCredentialInput() {
                    ClientDataHash = ClientDataHash,
                    User = User,
                    PublicKeyCredentialParameters = PubKeyCredParameters,
                    RelyingParty = Rp,
                    Options = Options
                } },
                new[] { User },
                new[] { PubKeyCredParameters[0] },
                new[] { Rp },
            };

            foreach (object[] args in validObjects)
            {
                yield return args;
            }
        }

        [Theory]
        [MemberData(nameof(GetValidObjects))]
        internal void Validate_GivenValidData_Succeeds(IValidatable v)
        {
            if (v is null) 
            {
                throw new ArgumentNullException(nameof(v));
            }

            v.Validate();
        }
    }
}
