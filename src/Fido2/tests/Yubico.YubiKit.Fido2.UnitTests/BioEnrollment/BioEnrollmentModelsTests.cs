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
using Yubico.YubiKit.Fido2.BioEnrollment;

namespace Yubico.YubiKit.Fido2.UnitTests.BioEnrollment;

/// <summary>
/// Tests for BioEnrollment model parsing.
/// </summary>
public class BioEnrollmentModelsTests
{
    [Fact]
    public void FingerprintSensorInfo_Decode_ParsesTouchSensor()
    {
        // Arrange - CBOR map with fingerprintKind=touch, maxSamples=5
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(2); // fingerprintKind key
        writer.WriteInt32(1); // Touch = 0x01
        writer.WriteInt32(3); // maxCaptureSamplesRequiredForEnroll key
        writer.WriteInt32(5);
        writer.WriteEndMap();
        var cbor = writer.Encode();
        
        // Act
        var info = FingerprintSensorInfo.Decode(cbor);
        
        // Assert
        Assert.Equal(FingerprintKind.Touch, info.FingerprintKind);
        Assert.Equal(5, info.MaxCaptureSamplesRequiredForEnroll);
        Assert.Null(info.MaxTemplateCount);
    }
    
    [Fact]
    public void FingerprintSensorInfo_Decode_ParsesSwipeSensor()
    {
        // Arrange - CBOR map with fingerprintKind=swipe, maxSamples=12
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(2); // fingerprintKind key
        writer.WriteInt32(2); // Swipe = 0x02
        writer.WriteInt32(3); // maxCaptureSamplesRequiredForEnroll key
        writer.WriteInt32(12);
        writer.WriteEndMap();
        var cbor = writer.Encode();
        
        // Act
        var info = FingerprintSensorInfo.Decode(cbor);
        
        // Assert
        Assert.Equal(FingerprintKind.Swipe, info.FingerprintKind);
        Assert.Equal(12, info.MaxCaptureSamplesRequiredForEnroll);
    }
    
    [Fact]
    public void FingerprintSensorInfo_Decode_ParsesMaxTemplateCount()
    {
        // Arrange - CBOR map with vendor extension for max templates
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteInt32(2); // fingerprintKind key
        writer.WriteInt32(1); // Touch
        writer.WriteInt32(3); // maxCaptureSamplesRequiredForEnroll key
        writer.WriteInt32(5);
        writer.WriteInt32(4); // maxTemplateCount key (vendor extension)
        writer.WriteInt32(3);
        writer.WriteEndMap();
        var cbor = writer.Encode();
        
        // Act
        var info = FingerprintSensorInfo.Decode(cbor);
        
        // Assert
        Assert.Equal(FingerprintKind.Touch, info.FingerprintKind);
        Assert.Equal(5, info.MaxCaptureSamplesRequiredForEnroll);
        Assert.Equal(3, info.MaxTemplateCount);
    }
    
    [Fact]
    public void FingerprintSensorInfo_Decode_IgnoresUnknownKeys()
    {
        // Arrange - CBOR map with unknown keys
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);
        writer.WriteInt32(2); // fingerprintKind key
        writer.WriteInt32(1); // Touch
        writer.WriteInt32(3); // maxCaptureSamplesRequiredForEnroll key
        writer.WriteInt32(5);
        writer.WriteInt32(99); // Unknown key
        writer.WriteTextString("ignored");
        writer.WriteInt32(100); // Another unknown key
        writer.WriteInt32(42);
        writer.WriteEndMap();
        var cbor = writer.Encode();
        
        // Act
        var info = FingerprintSensorInfo.Decode(cbor);
        
        // Assert
        Assert.Equal(FingerprintKind.Touch, info.FingerprintKind);
        Assert.Equal(5, info.MaxCaptureSamplesRequiredForEnroll);
    }
    
    [Fact]
    public void EnrollmentSampleResult_Decode_ParsesFirstSample()
    {
        // Arrange - CBOR response from enrollBegin
        var templateId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteInt32(4); // templateId key
        writer.WriteByteString(templateId);
        writer.WriteInt32(5); // lastEnrollSampleStatus key
        writer.WriteInt32(0); // Good
        writer.WriteInt32(6); // remainingSamples key
        writer.WriteInt32(4);
        writer.WriteEndMap();
        var cbor = writer.Encode();
        
        // Act
        var result = EnrollmentSampleResult.Decode(cbor);
        
        // Assert
        Assert.Equal(templateId, result.TemplateId.ToArray());
        Assert.Equal(FingerprintSampleStatus.Good, result.LastSampleStatus);
        Assert.Equal(4, result.RemainingSamples);
        Assert.False(result.IsComplete);
    }
    
    [Fact]
    public void EnrollmentSampleResult_Decode_ParsesCompletedEnrollment()
    {
        // Arrange - CBOR response with remainingSamples = 0
        var templateId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteInt32(4); // templateId key
        writer.WriteByteString(templateId);
        writer.WriteInt32(5); // lastEnrollSampleStatus key
        writer.WriteInt32(0); // Good
        writer.WriteInt32(6); // remainingSamples key
        writer.WriteInt32(0);
        writer.WriteEndMap();
        var cbor = writer.Encode();
        
        // Act
        var result = EnrollmentSampleResult.Decode(cbor);
        
        // Assert
        Assert.Equal(0, result.RemainingSamples);
        Assert.True(result.IsComplete);
    }
    
    [Fact]
    public void EnrollmentSampleResult_Decode_ParsesPoorQualitySample()
    {
        // Arrange - CBOR response with poor quality status
        var templateId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteInt32(4); // templateId key
        writer.WriteByteString(templateId);
        writer.WriteInt32(5); // lastEnrollSampleStatus key
        writer.WriteInt32(7); // PoorQuality = 0x07
        writer.WriteInt32(6); // remainingSamples key
        writer.WriteInt32(3); // May not decrement on poor quality
        writer.WriteEndMap();
        var cbor = writer.Encode();
        
        // Act
        var result = EnrollmentSampleResult.Decode(cbor);
        
        // Assert
        Assert.Equal(FingerprintSampleStatus.PoorQuality, result.LastSampleStatus);
        Assert.Equal(3, result.RemainingSamples);
        Assert.False(result.IsComplete);
    }
    
    [Theory]
    [InlineData(FingerprintSampleStatus.TooHigh, 1)]
    [InlineData(FingerprintSampleStatus.TooLow, 2)]
    [InlineData(FingerprintSampleStatus.TooLeft, 3)]
    [InlineData(FingerprintSampleStatus.TooRight, 4)]
    [InlineData(FingerprintSampleStatus.TooFast, 5)]
    [InlineData(FingerprintSampleStatus.TooSlow, 6)]
    [InlineData(FingerprintSampleStatus.TooSkewed, 8)]
    [InlineData(FingerprintSampleStatus.TooShort, 9)]
    [InlineData(FingerprintSampleStatus.MergeFailure, 10)]
    [InlineData(FingerprintSampleStatus.StorageFull, 11)]
    [InlineData(FingerprintSampleStatus.NoUserActivity, 12)]
    [InlineData(FingerprintSampleStatus.NoUserPresence, 13)]
    public void EnrollmentSampleResult_Decode_ParsesAllStatusCodes(
        FingerprintSampleStatus expectedStatus,
        int cborValue)
    {
        // Arrange
        var templateId = new byte[] { 0x01 };
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteInt32(4);
        writer.WriteByteString(templateId);
        writer.WriteInt32(5);
        writer.WriteInt32(cborValue);
        writer.WriteInt32(6);
        writer.WriteInt32(2);
        writer.WriteEndMap();
        var cbor = writer.Encode();
        
        // Act
        var result = EnrollmentSampleResult.Decode(cbor);
        
        // Assert
        Assert.Equal(expectedStatus, result.LastSampleStatus);
    }
    
    [Fact]
    public void TemplateInfo_Decode_ParsesBasicTemplate()
    {
        // Arrange
        var templateId = new byte[] { 0xAB, 0xCD, 0xEF };
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteInt32(4); // templateId key
        writer.WriteByteString(templateId);
        writer.WriteEndMap();
        var cbor = writer.Encode();
        
        // Act
        var info = TemplateInfo.Decode(cbor);
        
        // Assert
        Assert.Equal(templateId, info.TemplateId.ToArray());
        Assert.Null(info.FriendlyName);
        Assert.Null(info.SampleCount);
    }
    
    [Fact]
    public void TemplateInfo_Decode_ParsesWithFriendlyName()
    {
        // Arrange
        var templateId = new byte[] { 0xAB, 0xCD, 0xEF };
        var friendlyName = "Right Index";
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(4); // templateId key
        writer.WriteByteString(templateId);
        writer.WriteInt32(7); // templateFriendlyName key
        writer.WriteTextString(friendlyName);
        writer.WriteEndMap();
        var cbor = writer.Encode();
        
        // Act
        var info = TemplateInfo.Decode(cbor);
        
        // Assert
        Assert.Equal(templateId, info.TemplateId.ToArray());
        Assert.Equal(friendlyName, info.FriendlyName);
    }
    
    [Fact]
    public void TemplateInfo_Decode_ParsesWithSampleCount()
    {
        // Arrange
        var templateId = new byte[] { 0xAB, 0xCD, 0xEF };
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteInt32(4); // templateId key
        writer.WriteByteString(templateId);
        writer.WriteInt32(7); // templateFriendlyName key
        writer.WriteTextString("Left Thumb");
        writer.WriteInt32(8); // sampleCount key (vendor extension)
        writer.WriteInt32(5);
        writer.WriteEndMap();
        var cbor = writer.Encode();
        
        // Act
        var info = TemplateInfo.Decode(cbor);
        
        // Assert
        Assert.Equal(templateId, info.TemplateId.ToArray());
        Assert.Equal("Left Thumb", info.FriendlyName);
        Assert.Equal(5, info.SampleCount);
    }
    
    [Fact]
    public void EnrollmentEnumerationResult_Decode_ParsesSingleTemplate()
    {
        // Arrange
        var templateId = new byte[] { 0x01, 0x02, 0x03 };
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(4); // templateId key
        writer.WriteByteString(templateId);
        writer.WriteInt32(7); // templateFriendlyName key
        writer.WriteTextString("My Finger");
        writer.WriteEndMap();
        var cbor = writer.Encode();
        
        // Act
        var result = EnrollmentEnumerationResult.Decode(cbor);
        
        // Assert
        Assert.Single(result.Templates);
        Assert.Equal(templateId, result.Templates[0].TemplateId.ToArray());
        Assert.Equal("My Finger", result.Templates[0].FriendlyName);
    }
    
    [Fact]
    public void EnrollmentEnumerationResult_FromTemplates_CreatesResult()
    {
        // Arrange
        var templates = new List<TemplateInfo>
        {
            new() { TemplateId = new byte[] { 1, 2, 3 }, FriendlyName = "Finger 1" },
            new() { TemplateId = new byte[] { 4, 5, 6 }, FriendlyName = "Finger 2" }
        };
        
        // Act
        var result = EnrollmentEnumerationResult.FromTemplates(templates);
        
        // Assert
        Assert.Equal(2, result.Templates.Count);
        Assert.Equal("Finger 1", result.Templates[0].FriendlyName);
        Assert.Equal("Finger 2", result.Templates[1].FriendlyName);
    }
    
    [Fact]
    public void FingerprintKind_HasCorrectValues()
    {
        // Assert
        Assert.Equal(1, (int)FingerprintKind.Touch);
        Assert.Equal(2, (int)FingerprintKind.Swipe);
    }
    
    [Fact]
    public void FingerprintModality_HasCorrectValue()
    {
        // Assert
        Assert.Equal(1, FingerprintModality.Fingerprint);
    }
}
