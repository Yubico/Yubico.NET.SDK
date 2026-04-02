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

using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.Credentials;
using Xunit;

namespace Yubico.YubiKit.Fido2.UnitTests.Credentials;

/// <summary>
/// Unit tests for MakeCredentialResponse and GetAssertionResponse decoding.
/// </summary>
public class CredentialResponseTests
{
    /// <summary>
    /// Creates a minimal MakeCredential CBOR response for testing.
    /// </summary>
    private static byte[] CreateMakeCredentialResponse(string format = "packed")
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        
        writer.WriteStartMap(3);
        
        // 0x01: fmt
        writer.WriteInt32(1);
        writer.WriteTextString(format);
        
        // 0x02: authData (minimal: 37 bytes with AT flag)
        writer.WriteInt32(2);
        var authData = CreateAuthDataWithAttestedCredentialData();
        writer.WriteByteString(authData);
        
        // 0x03: attStmt (empty for "none" format)
        writer.WriteInt32(3);
        writer.WriteStartMap(0);
        writer.WriteEndMap();
        
        writer.WriteEndMap();
        
        return writer.Encode();
    }
    
    private static byte[] CreateAuthDataWithAttestedCredentialData()
    {
        // rpIdHash (32) + flags (1) + signCount (4) + AAGUID (16) + credIdLen (2) + credId + publicKey
        var data = new List<byte>();
        
        // rpIdHash (32 bytes)
        var rpIdHash = new byte[32];
        SHA256.HashData("example.com"u8, rpIdHash);
        data.AddRange(rpIdHash);
        
        // flags = UP | AT (0x41)
        data.Add(0x41);
        
        // signCount (4 bytes, big-endian)
        data.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x01 });
        
        // AAGUID (16 bytes, all zeros for this test)
        data.AddRange(new byte[16]);
        
        // Credential ID length (2 bytes, big-endian) = 32
        data.AddRange(new byte[] { 0x00, 0x20 });
        
        // Credential ID (32 bytes)
        var credId = new byte[32];
        new Random(42).NextBytes(credId);
        data.AddRange(credId);
        
        // COSE public key (minimal EC2 key in CBOR)
        var keyWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        keyWriter.WriteStartMap(5);
        
        // kty = 2 (EC2)
        keyWriter.WriteInt32(1);
        keyWriter.WriteInt32(2);
        
        // alg = -7 (ES256)
        keyWriter.WriteInt32(3);
        keyWriter.WriteInt32(-7);
        
        // crv = 1 (P-256)
        keyWriter.WriteInt32(-1);
        keyWriter.WriteInt32(1);
        
        // x coordinate (32 bytes)
        keyWriter.WriteInt32(-2);
        keyWriter.WriteByteString(new byte[32]);
        
        // y coordinate (32 bytes)
        keyWriter.WriteInt32(-3);
        keyWriter.WriteByteString(new byte[32]);
        
        keyWriter.WriteEndMap();
        data.AddRange(keyWriter.Encode());
        
        return data.ToArray();
    }
    
    /// <summary>
    /// Creates a minimal GetAssertion CBOR response for testing.
    /// </summary>
    private static byte[] CreateGetAssertionResponse(bool includeCredential = false, bool includeUser = false)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        
        var mapCount = 2; // authData + signature
        if (includeCredential) mapCount++;
        if (includeUser) mapCount++;
        
        writer.WriteStartMap(mapCount);
        
        // 0x01: credential (optional)
        if (includeCredential)
        {
            writer.WriteInt32(1);
            writer.WriteStartMap(2);
            writer.WriteTextString("type");
            writer.WriteTextString("public-key");
            writer.WriteTextString("id");
            writer.WriteByteString(new byte[] { 0x01, 0x02, 0x03, 0x04 });
            writer.WriteEndMap();
        }
        
        // 0x02: authData (minimal: 37 bytes)
        writer.WriteInt32(2);
        var authData = CreateMinimalAuthData();
        writer.WriteByteString(authData);
        
        // 0x03: signature
        writer.WriteInt32(3);
        writer.WriteByteString(new byte[] { 0x30, 0x44 }); // Minimal signature
        
        // 0x04: user (optional)
        if (includeUser)
        {
            writer.WriteInt32(4);
            writer.WriteStartMap(3);
            writer.WriteTextString("id");
            writer.WriteByteString(new byte[] { 0xAA, 0xBB });
            writer.WriteTextString("name");
            writer.WriteTextString("testuser");
            writer.WriteTextString("displayName");
            writer.WriteTextString("Test User");
            writer.WriteEndMap();
        }
        
        writer.WriteEndMap();
        
        return writer.Encode();
    }
    
    private static byte[] CreateMinimalAuthData()
    {
        var data = new byte[37];
        SHA256.HashData("example.com"u8, data.AsSpan(0, 32));
        data[32] = 0x05; // UP + UV
        data[33] = 0x00;
        data[34] = 0x00;
        data[35] = 0x00;
        data[36] = 0x05;
        return data;
    }
    
    [Fact]
    public void MakeCredentialResponse_Decode_ParsesFormat()
    {
        var cbor = CreateMakeCredentialResponse("packed");
        
        var response = MakeCredentialResponse.Decode(cbor);
        
        Assert.Equal("packed", response.Format);
    }
    
    [Fact]
    public void MakeCredentialResponse_Decode_ParsesAuthenticatorData()
    {
        var cbor = CreateMakeCredentialResponse();
        
        var response = MakeCredentialResponse.Decode(cbor);
        
        Assert.NotNull(response.AuthenticatorData);
        Assert.True(response.AuthenticatorData.UserPresent);
        Assert.True(response.AuthenticatorData.HasAttestedCredentialData);
    }
    
    [Fact]
    public void MakeCredentialResponse_GetCredentialId_ReturnsId()
    {
        var cbor = CreateMakeCredentialResponse();
        
        var response = MakeCredentialResponse.Decode(cbor);
        
        var credId = response.GetCredentialId();
        Assert.Equal(32, credId.Length);
    }
    
    [Fact]
    public void MakeCredentialResponse_GetCredentialPublicKey_ReturnsCoseKey()
    {
        var cbor = CreateMakeCredentialResponse();
        
        var response = MakeCredentialResponse.Decode(cbor);
        
        var publicKey = response.GetCredentialPublicKey();
        Assert.False(publicKey.IsEmpty);
        
        // Verify it's valid CBOR
        var reader = new CborReader(publicKey);
        var mapLength = reader.ReadStartMap();
        Assert.True(mapLength > 0);
    }
    
    [Fact]
    public void MakeCredentialResponse_AttestationStatement_HasIsNone()
    {
        // Create response with "none" format and empty attStmt
        var cbor = CreateMakeCredentialResponse("none");
        
        var response = MakeCredentialResponse.Decode(cbor);
        
        // "none" format has empty attStmt (no sig, no x5c)
        Assert.Equal("none", response.Format);
        
        // Debug: check actual values
        var attStmt = response.AttestationStatement;
        Assert.Null(attStmt.Signature);
        Assert.Null(attStmt.X5c);
        Assert.True(attStmt.IsNone, 
            $"Expected IsNone=true, but got: Signature={attStmt.Signature}, X5c={attStmt.X5c}");
    }
    
    [Fact]
    public void GetAssertionResponse_Decode_ParsesSignature()
    {
        var cbor = CreateGetAssertionResponse();
        
        var response = GetAssertionResponse.Decode(cbor);
        
        Assert.False(response.Signature.IsEmpty);
    }
    
    [Fact]
    public void GetAssertionResponse_Decode_ParsesAuthenticatorData()
    {
        var cbor = CreateGetAssertionResponse();
        
        var response = GetAssertionResponse.Decode(cbor);
        
        Assert.NotNull(response.AuthenticatorData);
        Assert.True(response.AuthenticatorData.UserPresent);
        Assert.True(response.AuthenticatorData.UserVerified);
        Assert.Equal(5u, response.AuthenticatorData.SignCount);
    }
    
    [Fact]
    public void GetAssertionResponse_WithCredential_ParsesCredentialDescriptor()
    {
        var cbor = CreateGetAssertionResponse(includeCredential: true);
        
        var response = GetAssertionResponse.Decode(cbor);
        
        Assert.NotNull(response.Credential);
        Assert.Equal("public-key", response.Credential.Type);
        Assert.Equal(4, response.Credential.Id.Length);
    }
    
    [Fact]
    public void GetAssertionResponse_WithUser_ParsesUserEntity()
    {
        var cbor = CreateGetAssertionResponse(includeCredential: false, includeUser: true);
        
        var response = GetAssertionResponse.Decode(cbor);
        
        Assert.NotNull(response.User);
        Assert.Equal("testuser", response.User.Name);
        Assert.Equal("Test User", response.User.DisplayName);
    }
    
    [Fact]
    public void GetAssertionResponse_GetCredentialId_ReturnsIdOrEmpty()
    {
        var cbor = CreateGetAssertionResponse(includeCredential: true);
        var response = GetAssertionResponse.Decode(cbor);
        
        var credId = response.GetCredentialId();
        Assert.False(credId.IsEmpty);
        
        // Without credential
        var cborNoCredential = CreateGetAssertionResponse(includeCredential: false);
        var responseNoCredential = GetAssertionResponse.Decode(cborNoCredential);
        
        Assert.True(responseNoCredential.GetCredentialId().IsEmpty);
    }
    
    [Fact]
    public void GetAssertionResponse_GetUserHandle_ReturnsHandleOrEmpty()
    {
        var cbor = CreateGetAssertionResponse(includeUser: true);
        var response = GetAssertionResponse.Decode(cbor);
        
        var userHandle = response.GetUserHandle();
        Assert.False(userHandle.IsEmpty);
        
        // Without user
        var cborNoUser = CreateGetAssertionResponse(includeUser: false);
        var responseNoUser = GetAssertionResponse.Decode(cborNoUser);
        
        Assert.True(responseNoUser.GetUserHandle().IsEmpty);
    }
}
