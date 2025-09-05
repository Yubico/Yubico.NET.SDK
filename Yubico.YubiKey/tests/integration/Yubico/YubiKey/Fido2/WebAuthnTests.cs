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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2;

[Trait(TraitTypes.Category, TestCategories.Elevated)]
public class WebAuthnTests : FidoSessionIntegrationTestBase
{
    [SkippableFact(typeof(DeviceNotFoundException))]
    [Trait(TraitTypes.Category, TestCategories.RequiresTouch)]
    public void MakeCredential_WithValidParameters_ReturnsCredentialData()
    {
        var device = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Any);
        const string rpId = "example.com";
        const string rpName = "Example Corp";
        byte[] userId = { 1, 2, 3, 4, 5 };
        const string userName = "testuser";
        const string userDisplayName = "Test User";
        var challenge = "Y2hhbGxlbmdl"; // pretend base64 encoded challenge

        var clientData = ClientData.Create(WebAuthn.Create, challenge, "https://example.com");
        var clientDataHash = clientData.ComputeHash();
        var protocol = new PinUvAuthProtocolTwo();
        var makeParams = WebAuthn.CreateMakeCredentialParameters(
            protocol, rpId, rpName, userId, userName, userDisplayName, clientDataHash);

        var credentialData = WebAuthn.MakeCredential(device, clientDataHash, makeParams);

        Assert.NotNull(credentialData);
        Assert.True(credentialData.AuthenticatorData.UserPresence);
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    [Trait(TraitTypes.Category, TestCategories.RequiresTouch)]
    public void MakeCredential_WithValidParameters_GeneratedChallenge_ReturnsCredentialData()
    {
        var device = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Any);
        const string rpId = "example.com";
        const string rpName = "Example Corp";
        byte[] userId = { 1, 2, 3, 4, 5 };
        const string userName = "testuser";
        const string userDisplayName = "Test User";

        var (clientData, challenge) = ClientData.CreateWithRandomChallenge(WebAuthn.Create, "https://example.com");
       
        Assert.NotNull(challenge);
        Assert.Equal(32, challenge.Length);

        var clientDataHash = clientData.ComputeHash();
        var protocol = new PinUvAuthProtocolTwo();
        var makeParams = WebAuthn.CreateMakeCredentialParameters(
            protocol, rpId, rpName, userId, userName, userDisplayName, clientDataHash);

        var credentialData = WebAuthn.MakeCredential(device, clientDataHash, makeParams);

        Assert.NotNull(credentialData);
        Assert.True(credentialData.AuthenticatorData.UserPresence);
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void MakeCredential_UnsupportedProtocol_ThrowsArgumentException()
    {
        var device = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Any);
        byte[] clientDataHash = new byte[32];
        var makeParams = new MakeCredentialParameters(Rp, UserEntity)
        {
            ClientDataHash = clientDataHash,
            Protocol = (PinUvAuthProtocol)99
        };

        Assert.Throws<ArgumentException>(() =>
            WebAuthn.MakeCredential(device, clientDataHash, makeParams));
    }
}