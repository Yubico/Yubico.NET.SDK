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
using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.Fido2.UnitTests.Extensions;

/// <summary>
/// Unit tests for the CTAP2 extension types.
/// </summary>
public class ExtensionTypesTests
{
    #region CredProtectPolicy Tests
    
    [Theory]
    [InlineData(CredProtectPolicy.UserVerificationOptional, 1)]
    [InlineData(CredProtectPolicy.UserVerificationOptionalWithCredentialIdList, 2)]
    [InlineData(CredProtectPolicy.UserVerificationRequired, 3)]
    public void CredProtectPolicy_HasCorrectValues(CredProtectPolicy policy, int expected)
    {
        Assert.Equal(expected, (int)policy);
    }
    
    #endregion
    
    #region ExtensionIdentifiers Tests
    
    [Fact]
    public void ExtensionIdentifiers_HaveCorrectValues()
    {
        Assert.Equal("hmac-secret", ExtensionIdentifiers.HmacSecret);
        Assert.Equal("hmac-secret-mc", ExtensionIdentifiers.HmacSecretMakeCredential);
        Assert.Equal("credProtect", ExtensionIdentifiers.CredProtect);
        Assert.Equal("credBlob", ExtensionIdentifiers.CredBlob);
        Assert.Equal("largeBlob", ExtensionIdentifiers.LargeBlob);
        Assert.Equal("largeBlobKey", ExtensionIdentifiers.LargeBlobKey);
        Assert.Equal("minPinLength", ExtensionIdentifiers.MinPinLength);
        Assert.Equal("prf", ExtensionIdentifiers.Prf);
    }
    
    #endregion
    
    #region HmacSecretInput Tests
    
    [Fact]
    public void HmacSecretInput_EncodesCorrectly()
    {
        // Arrange
        var keyAgreement = new Dictionary<int, object?>
        {
            { 1, 2 },  // kty = EC2
            { 3, -25 }, // alg = ECDH-ES+HKDF-256
            { -1, 1 }, // crv = P-256
            { -2, new byte[32] }, // x
            { -3, new byte[32] }  // y
        };
        
        var saltEnc = new byte[48]; // 16 IV + 32 encrypted
        var saltAuth = new byte[32];
        
        var input = new HmacSecretInput
        {
            KeyAgreement = keyAgreement,
            SaltEnc = saltEnc,
            SaltAuth = saltAuth,
            PinUvAuthProtocol = 2
        };
        
        // Act
        var encoded = input.Encode();
        
        // Assert
        Assert.NotEmpty(encoded);
        
        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        var mapCount = reader.ReadStartMap();
        Assert.Equal(4, mapCount);
    }
    
    [Fact]
    public void HmacSecretOutput_DecodesCorrectly()
    {
        // Arrange - Create CBOR byte string output
        var outputData = new byte[48]; // 16 IV + 32 encrypted output
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteByteString(outputData);
        var encoded = writer.Encode();
        
        // Act
        var output = HmacSecretOutput.Decode(encoded);
        
        // Assert
        Assert.Equal(48, output.Output.Length);
    }
    
    #endregion
    
    #region LargeBlobInput Tests
    
    [Fact]
    public void LargeBlobInput_EncodesPreferredCorrectly()
    {
        // Arrange
        var input = new LargeBlobInput { Support = LargeBlobSupport.Preferred };
        
        // Act
        var encoded = input.Encode();
        
        // Assert
        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        var count = reader.ReadStartMap();
        Assert.Equal(1, count);
        Assert.Equal("support", reader.ReadTextString());
        Assert.Equal("preferred", reader.ReadTextString());
    }
    
    [Fact]
    public void LargeBlobInput_EncodesRequiredCorrectly()
    {
        // Arrange
        var input = new LargeBlobInput { Support = LargeBlobSupport.Required };
        
        // Act
        var encoded = input.Encode();
        
        // Assert
        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        reader.ReadStartMap();
        reader.ReadTextString();
        Assert.Equal("required", reader.ReadTextString());
    }
    
    [Fact]
    public void LargeBlobAssertionInput_EncodesReadCorrectly()
    {
        // Arrange
        var input = new LargeBlobAssertionInput { Read = true };
        
        // Act
        var encoded = input.Encode();
        
        // Assert
        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("read", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
    }
    
    [Fact]
    public void LargeBlobAssertionInput_EncodesWriteCorrectly()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var input = new LargeBlobAssertionInput { Write = data };
        
        // Act
        var encoded = input.Encode();
        
        // Assert
        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("write", reader.ReadTextString());
        Assert.Equal(data, reader.ReadByteString());
    }
    
    [Fact]
    public void LargeBlobAssertionInput_ThrowsWhenEmpty()
    {
        // Arrange
        var input = new LargeBlobAssertionInput();
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => input.Encode());
    }
    
    [Fact]
    public void LargeBlobOutput_DecodesCorrectly()
    {
        // Arrange
        var key = new byte[32];
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteTextString("largeBlobKey");
        writer.WriteByteString(key);
        writer.WriteTextString("written");
        writer.WriteBoolean(true);
        writer.WriteEndMap();
        var encoded = writer.Encode();
        
        // Act
        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        var output = LargeBlobOutput.Decode(reader);
        
        // Assert
        Assert.NotNull(output.LargeBlobKey);
        Assert.Equal(32, output.LargeBlobKey.Value.Length);
        Assert.True(output.Written);
    }
    
    #endregion
    
    #region CredBlobInput Tests
    
    [Fact]
    public void CredBlobInput_EncodesCorrectly()
    {
        // Arrange
        var blob = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var input = new CredBlobInput { Blob = blob };
        
        // Act
        var encoded = input.Encode();
        
        // Assert
        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        Assert.Equal(blob, reader.ReadByteString());
    }
    
    [Fact]
    public void CredBlobMakeCredentialOutput_DecodesCorrectly()
    {
        // Arrange
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteBoolean(true);
        var encoded = writer.Encode();
        
        // Act
        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        var output = CredBlobMakeCredentialOutput.Decode(reader);
        
        // Assert
        Assert.True(output.Stored);
    }
    
    [Fact]
    public void CredBlobAssertionOutput_DecodesCorrectly()
    {
        // Arrange
        var blob = new byte[] { 10, 20, 30 };
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteByteString(blob);
        var encoded = writer.Encode();
        
        // Act
        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        var output = CredBlobAssertionOutput.Decode(reader);
        
        // Assert
        Assert.Equal(blob, output.Blob.ToArray());
    }
    
    #endregion
    
    #region MinPinLengthInput Tests
    
    [Fact]
    public void MinPinLengthInput_EncodesCorrectly()
    {
        // Arrange
        var input = new MinPinLengthInput { Requested = true };
        
        // Act
        var encoded = input.Encode();
        
        // Assert
        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        Assert.True(reader.ReadBoolean());
    }
    
    [Fact]
    public void MinPinLengthOutput_DecodesCorrectly()
    {
        // Arrange
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteInt32(8);
        var encoded = writer.Encode();
        
        // Act
        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        var output = MinPinLengthOutput.Decode(reader);
        
        // Assert
        Assert.Equal(8, output.MinPinLength);
    }
    
    #endregion
    
    #region PrfInput Tests
    
    [Fact]
    public void PrfInput_ComputesSaltCorrectly()
    {
        // Arrange
        var input = new byte[] { 1, 2, 3, 4, 5 };
        
        // Act
        var salt = PrfInput.ComputeSalt(input);
        
        // Assert
        Assert.Equal(32, salt.Length);
        // Salt should be deterministic
        var salt2 = PrfInput.ComputeSalt(input);
        Assert.Equal(salt, salt2);
    }
    
    [Fact]
    public void PrfInput_DifferentInputsProduceDifferentSalts()
    {
        // Arrange
        var input1 = new byte[] { 1, 2, 3 };
        var input2 = new byte[] { 4, 5, 6 };
        
        // Act
        var salt1 = PrfInput.ComputeSalt(input1);
        var salt2 = PrfInput.ComputeSalt(input2);
        
        // Assert
        Assert.NotEqual(salt1, salt2);
    }
    
    [Fact]
    public void PrfOutput_FromHmacSecretOutput_ParsesSingleOutput()
    {
        // Arrange
        var decrypted = new byte[32];
        Random.Shared.NextBytes(decrypted);
        
        // Act
        var output = PrfOutput.FromHmacSecretOutput(decrypted, hasTwoOutputs: false);
        
        // Assert
        Assert.True(output.Enabled);
        Assert.NotNull(output.First);
        Assert.Equal(32, output.First.Value.Length);
        // When hasTwoOutputs is false, Second should be empty (no second output)
        Assert.True(output.Second is null || output.Second.Value.IsEmpty);
    }
    
    [Fact]
    public void PrfOutput_FromHmacSecretOutput_ParsesTwoOutputs()
    {
        // Arrange
        var decrypted = new byte[64];
        Random.Shared.NextBytes(decrypted);
        
        // Act
        var output = PrfOutput.FromHmacSecretOutput(decrypted, hasTwoOutputs: true);
        
        // Assert
        Assert.True(output.Enabled);
        Assert.NotNull(output.First);
        Assert.NotNull(output.Second);
        Assert.Equal(32, output.First.Value.Length);
        Assert.Equal(32, output.Second.Value.Length);
    }
    
    [Fact]
    public void PrfOutput_FromHmacSecretOutput_ThrowsOnShortData()
    {
        // Arrange
        var decrypted = new byte[16]; // Too short
        
        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => PrfOutput.FromHmacSecretOutput(decrypted));
    }
    
    #endregion
}
