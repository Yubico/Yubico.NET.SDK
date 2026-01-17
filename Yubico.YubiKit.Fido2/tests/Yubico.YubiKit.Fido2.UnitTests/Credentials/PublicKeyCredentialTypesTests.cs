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
using Yubico.YubiKit.Fido2.Credentials;
using Xunit;

namespace Yubico.YubiKit.Fido2.UnitTests.Credentials;

/// <summary>
/// Unit tests for PublicKeyCredentialDescriptor, RpEntity, and UserEntity.
/// </summary>
public class PublicKeyCredentialTypesTests
{
    [Fact]
    public void PublicKeyCredentialDescriptor_FromCredentialId_CreatesDescriptor()
    {
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        
        var descriptor = PublicKeyCredentialDescriptor.FromCredentialId(credentialId);
        
        Assert.Equal("public-key", descriptor.Type);
        Assert.Equal(credentialId, descriptor.Id.ToArray());
        Assert.Null(descriptor.Transports);
    }
    
    [Fact]
    public void PublicKeyCredentialDescriptor_WithTransports_StoresTransports()
    {
        var credentialId = new byte[] { 0x01, 0x02, 0x03 };
        var transports = new List<string> { "usb", "nfc" };
        
        var descriptor = new PublicKeyCredentialDescriptor(credentialId, "public-key", transports);
        
        Assert.Equal(transports, descriptor.Transports);
    }
    
    [Fact]
    public void PublicKeyCredentialDescriptor_EmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => 
            new PublicKeyCredentialDescriptor(ReadOnlyMemory<byte>.Empty));
    }
    
    [Fact]
    public void PublicKeyCredentialDescriptor_Encode_ProducesCbor()
    {
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var descriptor = new PublicKeyCredentialDescriptor(credentialId);
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        descriptor.Encode(writer);
        var cbor = writer.Encode();
        
        // Decode and verify
        var reader = new CborReader(cbor);
        var map = reader.ReadStartMap();
        Assert.Equal(2, map); // type + id
    }
    
    [Fact]
    public void PublicKeyCredentialDescriptor_RoundTrip_PreservesData()
    {
        var credentialId = new byte[] { 0xAA, 0xBB, 0xCC };
        var transports = new List<string> { "internal", "hybrid" };
        var original = new PublicKeyCredentialDescriptor(credentialId, "public-key", transports);
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        original.Encode(writer);
        var cbor = writer.Encode();
        
        var reader = new CborReader(cbor);
        var parsed = PublicKeyCredentialDescriptor.Parse(reader);
        
        Assert.Equal(original.Type, parsed.Type);
        Assert.Equal(original.Id.ToArray(), parsed.Id.ToArray());
        Assert.Equal(original.Transports, parsed.Transports);
    }
    
    [Fact]
    public void PublicKeyCredentialRpEntity_WithNameAndId_CreatesEntity()
    {
        var rp = new PublicKeyCredentialRpEntity("example.com", "Example Corp");
        
        Assert.Equal("example.com", rp.Id);
        Assert.Equal("Example Corp", rp.Name);
    }
    
    [Fact]
    public void PublicKeyCredentialRpEntity_IdOnly_AllowsNullName()
    {
        var rp = new PublicKeyCredentialRpEntity("example.com");
        
        Assert.Equal("example.com", rp.Id);
        Assert.Null(rp.Name);
    }
    
    [Fact]
    public void PublicKeyCredentialRpEntity_NullOrEmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new PublicKeyCredentialRpEntity(""));
        Assert.Throws<ArgumentNullException>(() => new PublicKeyCredentialRpEntity(null!));
    }
    
    [Fact]
    public void PublicKeyCredentialRpEntity_Encode_ProducesCbor()
    {
        var rp = new PublicKeyCredentialRpEntity("example.com", "Test");
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        rp.Encode(writer);
        var cbor = writer.Encode();
        
        var reader = new CborReader(cbor);
        var map = reader.ReadStartMap();
        Assert.Equal(2, map); // id + name
    }
    
    [Fact]
    public void PublicKeyCredentialRpEntity_RoundTrip_PreservesData()
    {
        var original = new PublicKeyCredentialRpEntity("example.org", "Example Org");
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        original.Encode(writer);
        var cbor = writer.Encode();
        
        var reader = new CborReader(cbor);
        var parsed = PublicKeyCredentialRpEntity.Parse(reader);
        
        Assert.Equal(original.Id, parsed.Id);
        Assert.Equal(original.Name, parsed.Name);
    }
    
    [Fact]
    public void PublicKeyCredentialUserEntity_ValidData_CreatesEntity()
    {
        var userId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var user = new PublicKeyCredentialUserEntity(userId, "testuser", "Test User");
        
        Assert.Equal(userId, user.Id.ToArray());
        Assert.Equal("testuser", user.Name);
        Assert.Equal("Test User", user.DisplayName);
    }
    
    [Fact]
    public void PublicKeyCredentialUserEntity_EmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => 
            new PublicKeyCredentialUserEntity(ReadOnlyMemory<byte>.Empty, "name", "display"));
    }
    
    [Fact]
    public void PublicKeyCredentialUserEntity_IdTooLong_ThrowsArgumentException()
    {
        var longId = new byte[65]; // Max is 64
        
        Assert.Throws<ArgumentException>(() => 
            new PublicKeyCredentialUserEntity(longId, "name", "display"));
    }
    
    [Fact]
    public void PublicKeyCredentialUserEntity_MaxLengthId_Succeeds()
    {
        var maxId = new byte[64]; // Exactly 64 is allowed
        
        var user = new PublicKeyCredentialUserEntity(maxId, "name", "display");
        
        Assert.Equal(64, user.Id.Length);
    }
    
    [Fact]
    public void PublicKeyCredentialUserEntity_RoundTrip_PreservesData()
    {
        var userId = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var original = new PublicKeyCredentialUserEntity(userId, "alice", "Alice Smith");
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        original.Encode(writer);
        var cbor = writer.Encode();
        
        var reader = new CborReader(cbor);
        var parsed = PublicKeyCredentialUserEntity.Parse(reader);
        
        Assert.Equal(original.Id.ToArray(), parsed.Id.ToArray());
        Assert.Equal(original.Name, parsed.Name);
        Assert.Equal(original.DisplayName, parsed.DisplayName);
    }
    
    [Fact]
    public void MakeCredentialOptions_WithResidentKey_SetsFlag()
    {
        var options = new MakeCredentialOptions()
            .WithResidentKey(true);
        
        Assert.True(options.ResidentKey);
    }
    
    [Fact]
    public void MakeCredentialOptions_WithUserVerification_SetsFlag()
    {
        var options = new MakeCredentialOptions()
            .WithUserVerification(true);
        
        Assert.True(options.UserVerification);
    }
    
    [Fact]
    public void MakeCredentialOptions_WithPinUvAuth_SetsParams()
    {
        var authParam = new byte[] { 0x01, 0x02, 0x03 };
        
        var options = new MakeCredentialOptions()
            .WithPinUvAuth(authParam, 2);
        
        Assert.Equal(authParam, options.PinUvAuthParam!.Value.ToArray());
        Assert.Equal(2, options.PinUvAuthProtocol);
    }
    
    [Fact]
    public void GetAssertionOptions_WithAllowList_SetsCredentials()
    {
        var credId1 = new byte[] { 0x01, 0x02 };
        var credId2 = new byte[] { 0x03, 0x04 };
        
        var options = new GetAssertionOptions()
            .WithAllowList(credId1, credId2);
        
        Assert.NotNull(options.AllowList);
        Assert.Equal(2, options.AllowList.Count);
        Assert.Equal(credId1, options.AllowList[0].Id.ToArray());
        Assert.Equal(credId2, options.AllowList[1].Id.ToArray());
    }
    
    [Fact]
    public void GetAssertionOptions_FluentApi_ChainsCorrectly()
    {
        var authParam = new byte[] { 0xAB, 0xCD };
        
        var options = new GetAssertionOptions()
            .WithUserVerification(true)
            .WithPinUvAuth(authParam, 1);
        
        Assert.True(options.UserVerification);
        Assert.Equal(authParam, options.PinUvAuthParam!.Value.ToArray());
        Assert.Equal(1, options.PinUvAuthProtocol);
    }
}
