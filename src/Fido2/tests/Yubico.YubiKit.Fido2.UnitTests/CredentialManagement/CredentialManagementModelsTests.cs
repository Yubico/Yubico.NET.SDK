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
using Xunit;
using Yubico.YubiKit.Fido2.CredentialManagement;

namespace Yubico.YubiKit.Fido2.UnitTests.CredentialManagement;

/// <summary>
/// Tests for credential management data model decoding.
/// </summary>
public class CredentialManagementModelsTests
{
    [Fact]
    public void CredentialMetadata_Decode_ParsesAllFields()
    {
        // Create CBOR for credential metadata response
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        
        // 0x01: existingResidentCredentialsCount
        writer.WriteInt32(1);
        writer.WriteInt32(5);
        
        // 0x02: maxPossibleRemainingResidentCredentialsCount
        writer.WriteInt32(2);
        writer.WriteInt32(20);
        
        writer.WriteEndMap();
        
        var metadata = CredentialMetadata.Decode(writer.Encode());
        
        Assert.Equal(5, metadata.ExistingResidentCredentialsCount);
        Assert.Equal(20, metadata.MaxPossibleRemainingResidentCredentialsCount);
    }
    
    [Fact]
    public void CredentialMetadata_Decode_HandlesZeroCounts()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(1);
        writer.WriteInt32(0);
        writer.WriteInt32(2);
        writer.WriteInt32(25);
        writer.WriteEndMap();
        
        var metadata = CredentialMetadata.Decode(writer.Encode());
        
        Assert.Equal(0, metadata.ExistingResidentCredentialsCount);
        Assert.Equal(25, metadata.MaxPossibleRemainingResidentCredentialsCount);
    }
    
    [Fact]
    public void RelyingPartyInfo_Decode_ParsesAllFields()
    {
        var rpIdHash = new byte[32];
        new Random(42).NextBytes(rpIdHash);
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        
        // 0x03: rp
        writer.WriteInt32(3);
        writer.WriteStartMap(2);
        writer.WriteTextString("id");
        writer.WriteTextString("example.com");
        writer.WriteTextString("name");
        writer.WriteTextString("Example Corp");
        writer.WriteEndMap();
        
        // 0x04: rpIDHash
        writer.WriteInt32(4);
        writer.WriteByteString(rpIdHash);
        
        // 0x05: totalRPs
        writer.WriteInt32(5);
        writer.WriteInt32(3);
        
        writer.WriteEndMap();
        
        var rpInfo = RelyingPartyInfo.Decode(writer.Encode());
        
        Assert.Equal("example.com", rpInfo.RelyingParty.Id);
        Assert.Equal("Example Corp", rpInfo.RelyingParty.Name);
        Assert.Equal(rpIdHash, rpInfo.RpIdHash.ToArray());
        Assert.Equal(3, rpInfo.TotalRpCount);
    }
    
    [Fact]
    public void RelyingPartyInfo_Decode_WithoutTotalRpCount()
    {
        var rpIdHash = new byte[32];
        new Random(42).NextBytes(rpIdHash);
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        
        // 0x03: rp
        writer.WriteInt32(3);
        writer.WriteStartMap(1);
        writer.WriteTextString("id");
        writer.WriteTextString("test.org");
        writer.WriteEndMap();
        
        // 0x04: rpIDHash
        writer.WriteInt32(4);
        writer.WriteByteString(rpIdHash);
        
        writer.WriteEndMap();
        
        var rpInfo = RelyingPartyInfo.Decode(writer.Encode());
        
        Assert.Equal("test.org", rpInfo.RelyingParty.Id);
        Assert.Null(rpInfo.RelyingParty.Name);
        Assert.Null(rpInfo.TotalRpCount);
    }
    
    [Fact]
    public void RelyingPartyInfo_Decode_ThrowsOnMissingRp()
    {
        var rpIdHash = new byte[32];
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteInt32(4);
        writer.WriteByteString(rpIdHash);
        writer.WriteEndMap();
        
        Assert.Throws<InvalidOperationException>(() => 
            RelyingPartyInfo.Decode(writer.Encode()));
    }
    
    [Fact]
    public void StoredCredentialInfo_Decode_ParsesBasicFields()
    {
        var credId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var userId = new byte[] { 0x10, 0x20, 0x30 };
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);
        
        // 0x06: user
        writer.WriteInt32(6);
        writer.WriteStartMap(3);
        writer.WriteTextString("displayName");
        writer.WriteTextString("John Doe");
        writer.WriteTextString("id");
        writer.WriteByteString(userId);
        writer.WriteTextString("name");
        writer.WriteTextString("john@example.com");
        writer.WriteEndMap();
        
        // 0x07: credentialID
        writer.WriteInt32(7);
        writer.WriteStartMap(2);
        writer.WriteTextString("id");
        writer.WriteByteString(credId);
        writer.WriteTextString("type");
        writer.WriteTextString("public-key");
        writer.WriteEndMap();
        
        // 0x08: publicKey (minimal COSE key)
        writer.WriteInt32(8);
        var keyWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        keyWriter.WriteStartMap(1);
        keyWriter.WriteInt32(1);
        keyWriter.WriteInt32(2);
        keyWriter.WriteEndMap();
        writer.WriteEncodedValue(keyWriter.Encode());
        
        // 0x09: totalCredentials
        writer.WriteInt32(9);
        writer.WriteInt32(5);
        
        writer.WriteEndMap();
        
        var credInfo = StoredCredentialInfo.Decode(writer.Encode());
        
        Assert.Equal("john@example.com", credInfo.User.Name);
        Assert.Equal("John Doe", credInfo.User.DisplayName);
        Assert.Equal(userId, credInfo.User.Id.ToArray());
        Assert.Equal("public-key", credInfo.CredentialId.Type);
        Assert.Equal(credId, credInfo.CredentialId.Id.ToArray());
        Assert.Equal(5, credInfo.TotalCredentials);
    }
    
    [Fact]
    public void StoredCredentialInfo_Decode_ParsesCredProtect()
    {
        var credId = new byte[] { 0x01, 0x02 };
        var userId = new byte[] { 0x10 };
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);
        
        // 0x06: user
        writer.WriteInt32(6);
        writer.WriteStartMap(3);
        writer.WriteTextString("displayName");
        writer.WriteTextString("Test User");
        writer.WriteTextString("id");
        writer.WriteByteString(userId);
        writer.WriteTextString("name");
        writer.WriteTextString("test@example.com");
        writer.WriteEndMap();
        
        // 0x07: credentialID
        writer.WriteInt32(7);
        writer.WriteStartMap(2);
        writer.WriteTextString("id");
        writer.WriteByteString(credId);
        writer.WriteTextString("type");
        writer.WriteTextString("public-key");
        writer.WriteEndMap();
        
        // 0x08: publicKey
        writer.WriteInt32(8);
        writer.WriteStartMap(0);
        writer.WriteEndMap();
        
        // 0x0A: credProtect
        writer.WriteInt32(10);
        writer.WriteInt32(2); // userVerificationRequired
        
        writer.WriteEndMap();
        
        var credInfo = StoredCredentialInfo.Decode(writer.Encode());
        
        Assert.Equal(2, credInfo.CredProtectPolicy);
    }
    
    [Fact]
    public void StoredCredentialInfo_Decode_ParsesLargeBlobKey()
    {
        var credId = new byte[] { 0x01 };
        var userId = new byte[] { 0x10 };
        var largeBlobKey = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);
        
        // 0x06: user
        writer.WriteInt32(6);
        writer.WriteStartMap(3);
        writer.WriteTextString("displayName");
        writer.WriteTextString("Test User");
        writer.WriteTextString("id");
        writer.WriteByteString(userId);
        writer.WriteTextString("name");
        writer.WriteTextString("test@example.com");
        writer.WriteEndMap();
        
        // 0x07: credentialID
        writer.WriteInt32(7);
        writer.WriteStartMap(2);
        writer.WriteTextString("id");
        writer.WriteByteString(credId);
        writer.WriteTextString("type");
        writer.WriteTextString("public-key");
        writer.WriteEndMap();
        
        // 0x08: publicKey
        writer.WriteInt32(8);
        writer.WriteStartMap(0);
        writer.WriteEndMap();
        
        // 0x0B: largeBlobKey
        writer.WriteInt32(11);
        writer.WriteByteString(largeBlobKey);
        
        writer.WriteEndMap();
        
        var credInfo = StoredCredentialInfo.Decode(writer.Encode());
        
        Assert.NotNull(credInfo.LargeBlobKey);
        Assert.Equal(largeBlobKey, credInfo.LargeBlobKey.Value.ToArray());
    }
    
    [Fact]
    public void StoredCredentialInfo_Decode_ThrowsOnMissingUser()
    {
        var credId = new byte[] { 0x01 };
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        
        // 0x07: credentialID
        writer.WriteInt32(7);
        writer.WriteStartMap(2);
        writer.WriteTextString("id");
        writer.WriteByteString(credId);
        writer.WriteTextString("type");
        writer.WriteTextString("public-key");
        writer.WriteEndMap();
        
        // 0x08: publicKey
        writer.WriteInt32(8);
        writer.WriteStartMap(0);
        writer.WriteEndMap();
        
        writer.WriteEndMap();
        
        Assert.Throws<InvalidOperationException>(() => 
            StoredCredentialInfo.Decode(writer.Encode()));
    }
    
    [Fact]
    public void StoredCredentialInfo_Decode_ParsesThirdPartyPayment()
    {
        var credId = new byte[] { 0x01 };
        var userId = new byte[] { 0x10 };
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);
        
        // 0x06: user
        writer.WriteInt32(6);
        writer.WriteStartMap(3);
        writer.WriteTextString("displayName");
        writer.WriteTextString("Test User");
        writer.WriteTextString("id");
        writer.WriteByteString(userId);
        writer.WriteTextString("name");
        writer.WriteTextString("test@example.com");
        writer.WriteEndMap();
        
        // 0x07: credentialID
        writer.WriteInt32(7);
        writer.WriteStartMap(2);
        writer.WriteTextString("id");
        writer.WriteByteString(credId);
        writer.WriteTextString("type");
        writer.WriteTextString("public-key");
        writer.WriteEndMap();
        
        // 0x08: publicKey
        writer.WriteInt32(8);
        writer.WriteStartMap(0);
        writer.WriteEndMap();
        
        // 0x0C: thirdPartyPayment
        writer.WriteInt32(12);
        writer.WriteBoolean(true);
        
        writer.WriteEndMap();
        
        var credInfo = StoredCredentialInfo.Decode(writer.Encode());
        
        Assert.True(credInfo.ThirdPartyPayment);
    }
    
    [Fact]
    public void StoredCredentialInfo_Decode_WithoutTotalCredentials()
    {
        var credId = new byte[] { 0x01 };
        var userId = new byte[] { 0x10 };
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        
        // 0x06: user
        writer.WriteInt32(6);
        writer.WriteStartMap(3);
        writer.WriteTextString("displayName");
        writer.WriteTextString("Test User");
        writer.WriteTextString("id");
        writer.WriteByteString(userId);
        writer.WriteTextString("name");
        writer.WriteTextString("test@example.com");
        writer.WriteEndMap();
        
        // 0x07: credentialID
        writer.WriteInt32(7);
        writer.WriteStartMap(2);
        writer.WriteTextString("id");
        writer.WriteByteString(credId);
        writer.WriteTextString("type");
        writer.WriteTextString("public-key");
        writer.WriteEndMap();
        
        // 0x08: publicKey
        writer.WriteInt32(8);
        writer.WriteStartMap(0);
        writer.WriteEndMap();
        
        writer.WriteEndMap();
        
        var credInfo = StoredCredentialInfo.Decode(writer.Encode());
        
        Assert.Null(credInfo.TotalCredentials);
        Assert.Null(credInfo.CredProtectPolicy);
        Assert.Null(credInfo.LargeBlobKey);
        Assert.Null(credInfo.ThirdPartyPayment);
    }
}
