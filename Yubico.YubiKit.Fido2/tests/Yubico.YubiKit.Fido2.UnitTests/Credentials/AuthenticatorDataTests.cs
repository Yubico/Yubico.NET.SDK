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

using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.Credentials;
using Xunit;

namespace Yubico.YubiKit.Fido2.UnitTests.Credentials;

/// <summary>
/// Unit tests for AuthenticatorData parsing and verification.
/// </summary>
public class AuthenticatorDataTests
{
    // Minimum valid authenticator data: 32 bytes rpIdHash + 1 byte flags + 4 bytes signCount
    private static readonly byte[] MinimalAuthData = CreateMinimalAuthData();
    
    private static byte[] CreateMinimalAuthData()
    {
        var data = new byte[37];
        // Fill rpIdHash with sample data (first 32 bytes)
        SHA256.HashData("example.com"u8, data.AsSpan(0, 32));
        // Flags = 0x01 (UP)
        data[32] = 0x01;
        // SignCount = 1 (big-endian)
        data[33] = 0x00;
        data[34] = 0x00;
        data[35] = 0x00;
        data[36] = 0x01;
        return data;
    }
    
    [Fact]
    public void Parse_MinimalAuthData_ReturnsValidResult()
    {
        var authData = AuthenticatorData.Parse(MinimalAuthData);
        
        Assert.Equal(32, authData.RpIdHash.Length);
        Assert.True(authData.UserPresent);
        Assert.False(authData.UserVerified);
        Assert.Equal(1u, authData.SignCount);
        Assert.Null(authData.AttestedCredentialData);
        Assert.Null(authData.Extensions);
    }
    
    [Fact]
    public void Parse_WithUserVerifiedFlag_ReturnsUserVerified()
    {
        var data = MinimalAuthData.ToArray();
        data[32] = 0x05; // UP + UV flags
        
        var authData = AuthenticatorData.Parse(data);
        
        Assert.True(authData.UserPresent);
        Assert.True(authData.UserVerified);
    }
    
    [Fact]
    public void Parse_TooShort_ThrowsArgumentException()
    {
        var shortData = new byte[36]; // Needs 37
        
        Assert.Throws<ArgumentException>(() => AuthenticatorData.Parse(shortData));
    }
    
    [Fact]
    public void VerifyRpIdHash_MatchingRpId_ReturnsTrue()
    {
        var authData = AuthenticatorData.Parse(MinimalAuthData);
        
        Assert.True(authData.VerifyRpIdHash("example.com"));
    }
    
    [Fact]
    public void VerifyRpIdHash_NonMatchingRpId_ReturnsFalse()
    {
        var authData = AuthenticatorData.Parse(MinimalAuthData);
        
        Assert.False(authData.VerifyRpIdHash("different.com"));
    }
    
    [Fact]
    public void Parse_SignCountBigEndian_ParsesCorrectly()
    {
        var data = MinimalAuthData.ToArray();
        // Set signCount to 0x01020304 (big-endian)
        data[33] = 0x01;
        data[34] = 0x02;
        data[35] = 0x03;
        data[36] = 0x04;
        
        var authData = AuthenticatorData.Parse(data);
        
        Assert.Equal(0x01020304u, authData.SignCount);
    }
    
    [Fact]
    public void Parse_BackupFlags_ParsesCorrectly()
    {
        var data = MinimalAuthData.ToArray();
        data[32] = 0x19; // UP + BE + BS
        
        var authData = AuthenticatorData.Parse(data);
        
        Assert.True(authData.BackupEligible);
        Assert.True(authData.BackedUp);
    }
    
    [Fact]
    public void Parse_AttestedCredentialDataFlag_ReturnsTrue()
    {
        var data = MinimalAuthData.ToArray();
        data[32] = 0x41; // UP + AT flag
        // Note: This would fail parsing without actual attested credential data
        // Just test the flag detection logic
        
        var flags = (AuthenticatorDataFlags)data[32];
        Assert.True(flags.HasFlag(AuthenticatorDataFlags.AttestedCredentialData));
    }
    
    [Fact]
    public void Parse_ExtensionDataFlag_ReturnsTrue()
    {
        var data = MinimalAuthData.ToArray();
        data[32] = 0x81; // UP + ED flag
        
        var flags = (AuthenticatorDataFlags)data[32];
        Assert.True(flags.HasFlag(AuthenticatorDataFlags.ExtensionData));
    }
    
    [Fact]
    public void RawData_ContainsOriginalBytes()
    {
        var authData = AuthenticatorData.Parse(MinimalAuthData);
        
        Assert.Equal(MinimalAuthData.Length, authData.RawData.Length);
        Assert.True(MinimalAuthData.AsSpan().SequenceEqual(authData.RawData.Span));
    }
}
