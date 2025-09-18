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
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2;

public class MakeCredentialParametersTests
{
    [Fact]
    public void Constructor_Succeeds()
    {
        var clientDataHash = new byte[]
        {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
        };
        var credId = new byte[]
        {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
        };
        var pinUvAuth = new byte[]
        {
            0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, 0x70,
            0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, 0x70
        };

        var protocol = PinUvAuthProtocol.ProtocolOne;
        var authData = new ReadOnlyMemory<byte>(pinUvAuth);
        if (protocol == PinUvAuthProtocol.ProtocolOne)
        {
            authData = authData[..16];
        }

        var rp = new RelyingParty("someRpId")
        {
            Name = "SomeRpName"
        };
        var user = new UserEntity(new byte[] { 0x11, 0x22, 0x33, 0x44 })
        {
            Name = "SomeUserName",
            DisplayName = "User"
        };
        var credentialId = new CredentialId
        {
            Id = credId
        };
        credentialId.AddTransport(AuthenticatorTransports.Usb);
        credentialId.AddTransport(AuthenticatorTransports.Nfc);

        var makeParams = new MakeCredentialParameters(rp, user)
        {
            ClientDataHash = clientDataHash,
            Protocol = protocol,
            EnterpriseAttestation = EnterpriseAttestation.VendorFacilitated,
            PinUvAuthParam = authData
        };
        makeParams.ExcludeCredential(credentialId);
        makeParams.AddExtension("fakeExtension", false);
        makeParams.AddThirdPartyPaymentExtension();
        makeParams.AddOption("up", true);

        // makeParams
        Assert.NotNull(makeParams.ExcludeList);
        if (makeParams.ExcludeList is null)
        {
            return;
        }

        Assert.NotEmpty(makeParams.ExcludeList);

        Assert.Contains(makeParams.Extensions, e => e is { Key: "fakeExtension", Value: [0xF4] });
        Assert.Contains(makeParams.Extensions, e => e is { Key: "thirdPartyPayment", Value: [0xF5] });
        var encodedParams = makeParams.CborEncode();
        Assert.Equal(0xAA, encodedParams[0]);
    }
}
