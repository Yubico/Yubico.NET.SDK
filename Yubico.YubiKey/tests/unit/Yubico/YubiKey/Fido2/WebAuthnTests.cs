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
using System.Security.Cryptography;
using System.Text;
using Xunit;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2;

[Trait(TraitTypes.Category, TestCategories.Elevated)]
public class WebAuthnTests
{
    #region ClientData Hash Tests

    [Fact]
    public void ClientData_ComputeHash_WithStringParameters_ReturnsValidHash()
    {
        const string type = "webauthn.create";
        const string challenge = "Y2hhbGxlbmdl";
        const string origin = "https://example.com";
        const bool crossOrigin = false;

        var clientData = ClientData.Create(type, challenge, origin, crossOrigin);
        byte[] hash = clientData.ComputeHash();

        Assert.NotNull(hash);
        Assert.Equal(32, hash.Length);
        Assert.All(hash, b => Assert.True(b >= 0));
    }

    [Fact]
    public void ClientData_ComputeHash_WithClientDataObject_ReturnsValidHash()
    {
        var clientData = new ClientData("webauthn.create", "Y2hhbGxlbmdl", "https://example.com", false);

        byte[] hash = clientData.ComputeHash();

        Assert.NotNull(hash);
        Assert.Equal(32, hash.Length);
    }

    [Fact]
    public void ClientData_ComputeHash_SameInput_ReturnsSameHash()
    {
        const string type = "webauthn.create";
        const string challenge = "Y2hhbGxlbmdl";
        const string origin = "https://example.com";

        var clientData1 = ClientData.Create(type, challenge, origin);
        var clientData2 = ClientData.Create(type, challenge, origin);
        byte[] hash1 = clientData1.ComputeHash();
        byte[] hash2 = clientData2.ComputeHash();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ClientData_ComputeHash_DifferentInput_ReturnsDifferentHash()
    {
        const string challenge1 = "Y2hhbGxlbmdl";
        const string challenge2 = "Y2hhbGxlbmdlMg";
        const string origin = "https://example.com";

        var clientData1 = ClientData.Create("webauthn.create", challenge1, origin);
        var clientData2 = ClientData.Create("webauthn.create", challenge2, origin);
        byte[] hash1 = clientData1.ComputeHash();
        byte[] hash2 = clientData2.ComputeHash();

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ClientData_ComputeHash_CrossOriginTrue_ReturnsDifferentHash()
    {
        const string type = "webauthn.create";
        const string challenge = "Y2hhbGxlbmdl";
        const string origin = "https://example.com";

        var clientDataFalse = ClientData.Create(type, challenge, origin, false);
        var clientDataTrue = ClientData.Create(type, challenge, origin, true);
        byte[] hashFalse = clientDataFalse.ComputeHash();
        byte[] hashTrue = clientDataTrue.ComputeHash();

        Assert.NotEqual(hashFalse, hashTrue);
    }

    [Fact]
    public void ClientData_CreateWithRandomChallenge_ReturnsValidHashAndChallenge()
    {
        const string type = "webauthn.create";
        const string origin = "https://example.com";

        var (clientData, challenge) = ClientData.CreateWithRandomChallenge(type, origin);
        byte[] hash = clientData.ComputeHash();

        Assert.NotNull(hash);
        Assert.Equal(32, hash.Length);
        Assert.NotNull(challenge);
        Assert.Equal(32, challenge.Length);
        Assert.All(challenge, b => Assert.True(b >= 0));
    }

    [Fact]
    public void ClientData_CreateWithRandomChallenge_CrossOriginTrue_ReturnsValidHash()
    {
        const string type = "webauthn.create";
        const string origin = "https://example.com";

        var (clientData, challenge) = ClientData.CreateWithRandomChallenge(type, origin, true);
        byte[] hash = clientData.ComputeHash();

        Assert.NotNull(hash);
        Assert.Equal(32, hash.Length);
        Assert.NotNull(challenge);
        Assert.Equal(32, challenge.Length);
    }

    [Fact]
    public void ClientData_CreateWithRandomChallenge_MultipleCallsReturnDifferentChallenges()
    {
        const string type = "webauthn.create";
        const string origin = "https://example.com";

        var (clientData1, challenge1) = ClientData.CreateWithRandomChallenge(type, origin);
        var (clientData2, challenge2) = ClientData.CreateWithRandomChallenge(type, origin);
        byte[] hash1 = clientData1.ComputeHash();
        byte[] hash2 = clientData2.ComputeHash();

        Assert.NotEqual(challenge1, challenge2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ClientData_CreateWithRandomChallenge_CrossOriginDifference_ReturnsDifferentHashes()
    {
        const string type = "webauthn.create";
        const string origin = "https://example.com";
        
        var (clientDataFalse, _) = ClientData.CreateWithRandomChallenge(type, origin, false);
        var (clientDataTrue, _) = ClientData.CreateWithRandomChallenge(type, origin, true);
        byte[] hashFalse = clientDataFalse.ComputeHash();
        byte[] hashTrue = clientDataTrue.ComputeHash();

        Assert.NotEqual(hashFalse, hashTrue);
    }

    [Fact]
    public void ClientData_CreateWithRandomChallenge_ProducesBase64UrlEncodedChallenge()
    {
        const string type = "webauthn.create";
        const string origin = "https://example.com";

        var (clientData, challenge) = ClientData.CreateWithRandomChallenge(type, origin);

        string challengeBase64 = ClientData.Base64(challenge);
        string challengeBase64Url = ClientData.UrlEncode(challengeBase64);

        var expectedClientData = ClientData.Create(type, challengeBase64Url, origin, false);
        byte[] expectedHash = expectedClientData.ComputeHash();
        byte[] actualHash = clientData.ComputeHash();

        Assert.Equal(expectedHash, actualHash);
        Assert.Equal(challengeBase64Url, clientData.Challenge);
    }

    #endregion

    #region CreateMakeCredentialParameters Tests

    [Fact]
    public void CreateMakeCredentialParameters_ValidInputs_ReturnsValidParameters()
    {
        var protocol = new PinUvAuthProtocolOne();
        const string rpId = "example.com";
        const string rpName = "Example Corp";
        byte[] userId = { 1, 2, 3, 4, 5 };
        const string userName = "testuser";
        const string userDisplayName = "Test User";
        byte[] clientDataHash = new byte[32];
        Array.Fill<byte>(clientDataHash, 0x42);

        var parameters = WebAuthn.CreateMakeCredentialParameters(
            protocol, rpId, rpName, userId, userName, userDisplayName, clientDataHash);

        Assert.NotNull(parameters);
        Assert.Equal(rpId, parameters.RelyingParty.Id);
        Assert.Equal(rpName, parameters.RelyingParty.Name);
        Assert.Equal(userId, parameters.UserEntity.Id.ToArray());
        Assert.Equal(userName, parameters.UserEntity.Name);
        Assert.Equal(userDisplayName, parameters.UserEntity.DisplayName);
        Assert.Equal(clientDataHash, parameters.ClientDataHash);
        Assert.Equal(protocol.Protocol, parameters.Protocol);
        Assert.True(parameters.Options.ContainsKey(AuthenticatorOptions.rk));
        Assert.True((bool)parameters.Options[AuthenticatorOptions.rk]);
    }

    [Fact]
    public void CreateMakeCredentialParameters_ProtocolTwo_SetsCorrectProtocol()
    {
        var protocol = new PinUvAuthProtocolTwo();
        const string rpId = "example.com";
        const string rpName = "Example Corp";
        byte[] userId = { 1, 2, 3, 4, 5 };
        const string userName = "testuser";
        const string userDisplayName = "Test User";
        byte[] clientDataHash = new byte[32];

        var parameters = WebAuthn.CreateMakeCredentialParameters(
            protocol, rpId, rpName, userId, userName, userDisplayName, clientDataHash);

        Assert.Equal(PinUvAuthProtocol.ProtocolTwo, parameters.Protocol);
    }

    #endregion

    #region ClientData Tests

    [Fact]
    public void ClientData_Constructor_SetsPropertiesCorrectly()
    {
        const string type = "webauthn.create";
        const string challenge = "Y2hhbGxlbmdl";
        const string origin = "https://example.com";
        const bool crossOrigin = true;

        var clientData = new ClientData(type, challenge, origin, crossOrigin);

        Assert.Equal(type, clientData.Type);
        Assert.Equal(challenge, clientData.Challenge);
        Assert.Equal(origin, clientData.Origin);
        Assert.Equal(crossOrigin, clientData.CrossOrigin);
    }

    [Fact]
    public void ClientData_StaticProperties_ReturnCorrectValues()
    {
        Assert.Equal("webauthn.create", WebAuthn.Create);
        Assert.Equal("webauthn.get", WebAuthn.Get);
    }

    [Fact]
    public void ClientData_ToString_ReturnsFormattedString()
    {
        var clientData = new ClientData("webauthn.create", "Y2hhbGxlbmdl", "https://example.com", false);
        string result = clientData.ToString();

        Assert.Contains("webauthn.create", result);
        Assert.Contains("Y2hhbGxlbmdl", result);
        Assert.Contains("https://example.com", result);
        Assert.Contains("False", result);
    }

    [Fact]
    public void ClientData_DefaultCrossOrigin_IsFalse()
    {
        var clientData = new ClientData("webauthn.create", "Y2hhbGxlbmdl", "https://example.com");

        Assert.False(clientData.CrossOrigin);
    }

    [Fact]
    public void ClientData_Create_WithByteChallenge_CreatesCorrectClientData()
    {
        const string type = "webauthn.create";
        const string origin = "https://example.com";
        byte[] challengeBytes = { 0x63, 0x68, 0x61, 0x6c, 0x6c, 0x65, 0x6e, 0x67, 0x65 }; // "challenge" in UTF-8
        
        var clientData = ClientData.Create(type, challengeBytes, origin, true);
        
        Assert.Equal(type, clientData.Type);
        Assert.Equal(origin, clientData.Origin);
        Assert.True(clientData.CrossOrigin);
        
        // Verify the challenge is base64url encoded
        string expectedBase64 = ClientData.Base64(challengeBytes);
        string expectedBase64Url = ClientData.UrlEncode(expectedBase64);
        Assert.Equal(expectedBase64Url, clientData.Challenge);
    }
    
    [Fact]
    public void ClientData_Create_StaticFactoryMethods_WorkCorrectly()
    {
        const string type = "webauthn.create";
        const string challenge = "Y2hhbGxlbmdl";
        const string origin = "https://example.com";
        
        var clientData1 = ClientData.Create(type, challenge, origin, false);
        var clientData2 = new ClientData(type, challenge, origin, false);
        
        Assert.Equal(clientData1.Type, clientData2.Type);
        Assert.Equal(clientData1.Challenge, clientData2.Challenge);
        Assert.Equal(clientData1.Origin, clientData2.Origin);
        Assert.Equal(clientData1.CrossOrigin, clientData2.CrossOrigin);
        
        byte[] hash1 = clientData1.ComputeHash();
        byte[] hash2 = clientData2.ComputeHash();
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ClientData_Base64AndUrlEncode_WorkCorrectly()
    {
        byte[] testData = { 0x48, 0x65, 0x6c, 0x6c, 0x6f }; // "Hello"
        
        string base64 = ClientData.Base64(testData);
        Assert.Equal("SGVsbG8=", base64);
        
        string urlEncoded = ClientData.UrlEncode(base64);
        Assert.Equal("SGVsbG8", urlEncoded); // padding removed, no + or / in this case
    }
    
    [Fact]
    public void ClientData_Base64UrlEncode_HandlesSpecialCharacters()
    {
        // Create data that will produce + and / in base64
        byte[] testData = { 0x3f, 0x3f, 0x3f }; // Will produce "Pz8/" in base64
        
        string base64 = ClientData.Base64(testData);
        string urlEncoded = ClientData.UrlEncode(base64);
        
        Assert.DoesNotContain("+", urlEncoded);
        Assert.DoesNotContain("/", urlEncoded);
        Assert.DoesNotContain("=", urlEncoded);
        Assert.Contains("-", urlEncoded); // + should be replaced with -
        Assert.Contains("_", urlEncoded); // / should be replaced with _
    }

    #endregion

    #region Integration Tests with Real Hash Validation

    [Fact]
    public void ClientData_ComputeHash_MatchesManualSha256()
    {
        var clientData = new ClientData("webauthn.create", "Y2hhbGxlbmdl", "https://example.com", false);
        byte[] clientDataHash = clientData.ComputeHash();

        // Manual JSON creation to match the explicit ordering in ComputeHash
        string json = "{\"type\":\"webauthn.create\",\"challenge\":\"Y2hhbGxlbmdl\",\"origin\":\"https://example.com\"}";
        using var sha256 = SHA256.Create();
        byte[] manualHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));

        Assert.Equal(manualHash, clientDataHash);
    }
    
    [Fact]
    public void ClientData_ComputeHash_WithCrossOriginTrue_MatchesManualSha256()
    {
        var clientData = new ClientData("webauthn.create", "Y2hhbGxlbmdl", "https://example.com", true);
        byte[] clientDataHash = clientData.ComputeHash();

        // Manual JSON creation to match the explicit ordering in ComputeHash with crossOrigin=true
        string json = "{\"type\":\"webauthn.create\",\"challenge\":\"Y2hhbGxlbmdl\",\"origin\":\"https://example.com\",\"crossOrigin\":true}";
        using var sha256 = SHA256.Create();
        byte[] manualHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));

        Assert.Equal(manualHash, clientDataHash);
    }

    [Fact]
    public void CreateMakeCredentialParameters_WithDifferentUserIds_CreatesDistinctParameters()
    {
        var protocol = new PinUvAuthProtocolOne();
        byte[] userId1 = { 1, 2, 3, 4, 5 };
        byte[] userId2 = { 6, 7, 8, 9, 10 };
        byte[] clientDataHash = new byte[32];

        var params1 = WebAuthn.CreateMakeCredentialParameters(
            protocol, "example.com", "Example", userId1, "user1", "User 1", clientDataHash);
        var params2 = WebAuthn.CreateMakeCredentialParameters(
            protocol, "example.com", "Example", userId2, "user2", "User 2", clientDataHash);

        Assert.NotEqual(params1.UserEntity.Id.ToArray(), params2.UserEntity.Id.ToArray());
        Assert.NotEqual(params1.UserEntity.Name, params2.UserEntity.Name);
        Assert.NotEqual(params1.UserEntity.DisplayName, params2.UserEntity.DisplayName);
    }

    #endregion
}