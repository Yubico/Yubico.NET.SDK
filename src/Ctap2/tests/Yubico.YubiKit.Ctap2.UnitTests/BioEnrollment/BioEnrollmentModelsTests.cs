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
using Yubico.YubiKit.Ctap2.BioEnrollment;

namespace Yubico.YubiKit.Ctap2.UnitTests.BioEnrollment;

/// <summary>
/// Tests for BioEnrollment model parsing.
/// </summary>
/// <remarks>
/// Synthetic fixtures check decoded values and selected raw-copy behavior, not sensor
/// enrollment. Flat template maps below reflect the current decoder, not CTAP 2.1's
/// key-7 template array with nested keys 1/2. Status-value assertions pin the wire
/// mappings, including the python-fido2 status at 0x0C. No test
/// here establishes rejection of missing sample fields or independent enrollment completion.
/// </remarks>
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
    public void FingerprintSensorInfo_Decode_PreservesUnmodeledKeyFourWithoutInterpretingIt()
    {
        // Key 4 is not a sensor-info field; it must not be interpreted as an integer capacity.
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteInt32(2); // fingerprintKind key
        writer.WriteInt32(1); // Touch
        writer.WriteInt32(3); // maxCaptureSamplesRequiredForEnroll key
        writer.WriteInt32(5);
        writer.WriteInt32(4);
        writer.WriteTextString("unmodeled");
        writer.WriteEndMap();
        var cbor = writer.Encode();

        // Act
        var info = FingerprintSensorInfo.Decode(cbor);

        // Assert
        Assert.Equal(FingerprintKind.Touch, info.FingerprintKind);
        Assert.Equal(5, info.MaxCaptureSamplesRequiredForEnroll);
        Assert.Equal(cbor, info.RawData.ToArray());
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
        byte[] cbor = writer.Encode();
        byte[] expectedRawData = cbor.ToArray();

        // Act
        var info = FingerprintSensorInfo.Decode(cbor);
        cbor.AsSpan().Clear();

        // Assert
        Assert.Equal(FingerprintKind.Touch, info.FingerprintKind);
        Assert.Equal(5, info.MaxCaptureSamplesRequiredForEnroll);
        Assert.Equal(expectedRawData, info.RawData.ToArray());
    }

    [Fact]
    public void EnrollmentBeginResult_Decode_WithUnknownField_PreservesOwnedRawData()
    {
        // Arrange - CBOR response from enrollBegin
        var templateId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);
        writer.WriteInt32(4); // templateId key
        writer.WriteByteString(templateId);
        writer.WriteInt32(5); // lastEnrollSampleStatus key
        writer.WriteInt32(0); // Good
        writer.WriteInt32(6); // remainingSamples key
        writer.WriteInt32(4);
        writer.WriteInt32(99);
        writer.WriteStartArray(1);
        writer.WriteBoolean(true);
        writer.WriteEndArray();
        writer.WriteEndMap();
        byte[] cbor = writer.Encode();
        byte[] expectedRawData = cbor.ToArray();

        // Act
        var result = EnrollmentBeginResult.Decode(cbor);
        cbor.AsSpan().Clear();

        // Assert
        Assert.Equal(templateId, result.TemplateId.ToArray());
        Assert.Equal(FingerprintSampleStatus.Good, result.LastSampleStatus);
        Assert.Equal(4, result.RemainingSamples);
        Assert.False(result.IsComplete);
        Assert.Equal(expectedRawData, result.RawData.ToArray());
    }

    [Theory]
    [InlineData("A205000600", true)] // missing ID
    [InlineData("A20441010600", true)] // missing status
    [InlineData("A20441010500", true)] // missing remaining
    [InlineData("A10600", false)] // missing status
    [InlineData("A10500", false)] // missing remaining
    [InlineData("A205000620", false)] // negative remaining
    [InlineData("A20500060000", false)] // trailing value
    public void EnrollmentDecode_RejectsMalformedCapture(string encoded, bool begin)
    {
        byte[] response = Convert.FromHexString(encoded);
        Assert.Throws<CborContentException>(() => begin
            ? EnrollmentBeginResult.Decode(response)
            : EnrollmentSampleResult.Decode(response));
    }

    [Theory]
    [InlineData("A10305")]
    [InlineData("A10201")]
    public void FingerprintSensorInfo_RejectsMissingRequiredField(string encoded) =>
        Assert.Throws<CborContentException>(() => FingerprintSensorInfo.Decode(Convert.FromHexString(encoded)));

    [Fact]
    public void EnrollmentSampleResult_Decode_ParsesCompletedEnrollment()
    {
        // Arrange - supplied zero count exercises IsComplete equality, not actual enrolled-template persistence.
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
        writer.WriteInt32(3); // Supplied count; this single fixture does not compare successive samples.
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
    [InlineData(FingerprintSampleStatus.Exists, 11)]
    [InlineData(FingerprintSampleStatus.DatabaseFull, 12)]
    [InlineData(FingerprintSampleStatus.NoUserActivity, 13)]
    [InlineData(FingerprintSampleStatus.NoUpTransition, 14)]
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
        writer.WriteInt32(1); // nested templateId key
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
        writer.WriteInt32(1); // nested templateId key
        writer.WriteByteString(templateId);
        writer.WriteInt32(2); // nested templateFriendlyName key
        writer.WriteTextString(friendlyName);
        writer.WriteEndMap();
        var cbor = writer.Encode();

        // Act
        var info = TemplateInfo.Decode(cbor);

        // Assert
        Assert.Equal(templateId, info.TemplateId.ToArray());
        Assert.Equal(friendlyName, info.FriendlyName);
    }

    [Theory]
    [InlineData("A1044101")] // Legacy flat key 4 is not the nested ID.
    [InlineData("A10140")] // Empty ID.
    [InlineData("A101410100")] // Trailing bytes.
    public void TemplateInfo_Decode_RejectsMalformedEntry(string hex) =>
        Assert.Throws<CborContentException>(() => TemplateInfo.Decode(Convert.FromHexString(hex)));

    [Fact]
    public void EnrollmentEnumerationResult_Decode_ParsesAllTemplatesWithOptionalNames()
    {
        // {7: [{1: h'01', 2: "Left"}, {1: h'02'}], 8: 32}
        var result = EnrollmentEnumerationResult.Decode(Convert.FromHexString("A20782A201410102644C656674A1014102081820"));

        Assert.Collection(result.Templates, first =>
        {
            Assert.Equal(new byte[] { 1 }, first.TemplateId.ToArray());
            Assert.Equal("Left", first.FriendlyName);
            Assert.Null(first.SampleCount);
        }, second =>
        {
            Assert.Equal(new byte[] { 2 }, second.TemplateId.ToArray());
            Assert.Null(second.FriendlyName);
        });
    }

    [Theory]
    [InlineData("A0")] // Missing templateInfos.
    [InlineData("A10781A1026141")] // Template name without ID.
    [InlineData("A1078000")] // Trailing bytes.
    public void EnrollmentEnumerationResult_Decode_RejectsMalformedInventory(string hex) =>
        Assert.Throws<CborContentException>(() => EnrollmentEnumerationResult.Decode(Convert.FromHexString(hex)));

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