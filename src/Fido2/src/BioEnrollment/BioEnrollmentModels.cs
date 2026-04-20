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

namespace Yubico.YubiKit.Fido2.BioEnrollment;

/// <summary>
/// Fingerprint modality constants per CTAP2.1 specification.
/// </summary>
public static class FingerprintModality
{
    /// <summary>
    /// Fingerprint sensor type (0x01).
    /// </summary>
    public const int Fingerprint = 0x01;
}

/// <summary>
/// Fingerprint capture sample status codes per CTAP2.1 specification.
/// </summary>
public enum FingerprintSampleStatus
{
    /// <summary>
    /// Good sample captured (0x00).
    /// </summary>
    Good = 0x00,
    
    /// <summary>
    /// Sample too high on sensor (0x01).
    /// </summary>
    TooHigh = 0x01,
    
    /// <summary>
    /// Sample too low on sensor (0x02).
    /// </summary>
    TooLow = 0x02,
    
    /// <summary>
    /// Sample too left on sensor (0x03).
    /// </summary>
    TooLeft = 0x03,
    
    /// <summary>
    /// Sample too right on sensor (0x04).
    /// </summary>
    TooRight = 0x04,
    
    /// <summary>
    /// Finger moved too fast (0x05).
    /// </summary>
    TooFast = 0x05,
    
    /// <summary>
    /// Finger moved too slow (0x06).
    /// </summary>
    TooSlow = 0x06,
    
    /// <summary>
    /// Poor quality sample (0x07).
    /// </summary>
    PoorQuality = 0x07,
    
    /// <summary>
    /// Sample too skewed (0x08).
    /// </summary>
    TooSkewed = 0x08,
    
    /// <summary>
    /// Sample too short (0x09).
    /// </summary>
    TooShort = 0x09,
    
    /// <summary>
    /// Merge failure (0x0A).
    /// </summary>
    MergeFailure = 0x0A,
    
    /// <summary>
    /// Data storage full (0x0B).
    /// </summary>
    StorageFull = 0x0B,
    
    /// <summary>
    /// No user activity (timeout) (0x0C).
    /// </summary>
    NoUserActivity = 0x0C,
    
    /// <summary>
    /// No user presence detected (0x0D).
    /// </summary>
    NoUserPresence = 0x0D
}

/// <summary>
/// Information about the fingerprint sensor capabilities.
/// </summary>
/// <remarks>
/// Returned by authenticatorBioEnrollment getFingerprintSensorInfo sub-command.
/// </remarks>
public sealed class FingerprintSensorInfo
{
    /// <summary>
    /// Gets the fingerprint kind (touch or swipe).
    /// </summary>
    public FingerprintKind FingerprintKind { get; private init; }
    
    /// <summary>
    /// Gets the maximum number of samples required for enrollment.
    /// </summary>
    /// <remarks>
    /// This is a hint to the platform about the expected number of captures.
    /// Actual enrollment may complete with fewer samples.
    /// </remarks>
    public int MaxCaptureSamplesRequiredForEnroll { get; private init; }
    
    /// <summary>
    /// Gets the maximum number of templates that can be stored.
    /// </summary>
    public int? MaxTemplateCount { get; private init; }
    
    /// <summary>
    /// Decodes a FingerprintSensorInfo from a CBOR response.
    /// </summary>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded sensor info.</returns>
    public static FingerprintSensorInfo Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Lax);
        
        var fingerprintKind = FingerprintKind.Touch;
        var maxSamples = 0;
        int? maxTemplates = null;
        
        var mapCount = reader.ReadStartMap() ?? 0;
        
        for (var i = 0; i < mapCount; i++)
        {
            var key = reader.ReadInt32();
            
            switch (key)
            {
                case 2: // fingerprintKind (0x02)
                    fingerprintKind = (FingerprintKind)reader.ReadInt32();
                    break;
                    
                case 3: // maxCaptureSamplesRequiredForEnroll (0x03)
                    maxSamples = reader.ReadInt32();
                    break;
                    
                case 4: // maxTemplateCount (0x04) - vendor extension
                    maxTemplates = reader.ReadInt32();
                    break;
                    
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        
        return new FingerprintSensorInfo
        {
            FingerprintKind = fingerprintKind,
            MaxCaptureSamplesRequiredForEnroll = maxSamples,
            MaxTemplateCount = maxTemplates
        };
    }
}

/// <summary>
/// Fingerprint sensor kind.
/// </summary>
public enum FingerprintKind
{
    /// <summary>
    /// Touch sensor - finger placed on sensor (0x01).
    /// </summary>
    Touch = 0x01,
    
    /// <summary>
    /// Swipe sensor - finger swiped across sensor (0x02).
    /// </summary>
    Swipe = 0x02
}

/// <summary>
/// Result of a fingerprint enrollment capture sample operation.
/// </summary>
public sealed class EnrollmentSampleResult
{
    /// <summary>
    /// Gets the current template ID for this enrollment.
    /// </summary>
    public ReadOnlyMemory<byte> TemplateId { get; internal init; }
    
    /// <summary>
    /// Gets the status of the last sample capture.
    /// </summary>
    public FingerprintSampleStatus LastSampleStatus { get; internal init; }
    
    /// <summary>
    /// Gets the number of remaining samples needed to complete enrollment.
    /// </summary>
    /// <remarks>
    /// A value of 0 indicates enrollment is complete.
    /// </remarks>
    public int RemainingSamples { get; internal init; }
    
    /// <summary>
    /// Gets whether the enrollment is complete.
    /// </summary>
    public bool IsComplete => RemainingSamples == 0;
    
    /// <summary>
    /// Decodes an EnrollmentSampleResult from a CBOR response.
    /// </summary>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded enrollment result.</returns>
    public static EnrollmentSampleResult Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Lax);
        
        ReadOnlyMemory<byte> templateId = default;
        var lastStatus = FingerprintSampleStatus.Good;
        var remaining = 0;
        
        var mapCount = reader.ReadStartMap() ?? 0;
        
        for (var i = 0; i < mapCount; i++)
        {
            var key = reader.ReadInt32();
            
            switch (key)
            {
                case 4: // templateId (0x04)
                    templateId = reader.ReadByteString();
                    break;
                    
                case 5: // lastEnrollSampleStatus (0x05)
                    lastStatus = (FingerprintSampleStatus)reader.ReadInt32();
                    break;
                    
                case 6: // remainingSamples (0x06)
                    remaining = reader.ReadInt32();
                    break;
                    
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        
        return new EnrollmentSampleResult
        {
            TemplateId = templateId,
            LastSampleStatus = lastStatus,
            RemainingSamples = remaining
        };
    }
}

/// <summary>
/// Information about an enrolled fingerprint template.
/// </summary>
public sealed class TemplateInfo
{
    /// <summary>
    /// Gets the unique identifier of this template.
    /// </summary>
    public ReadOnlyMemory<byte> TemplateId { get; internal init; }
    
    /// <summary>
    /// Gets the friendly name of this template, if set.
    /// </summary>
    public string? FriendlyName { get; internal init; }
    
    /// <summary>
    /// Gets the number of samples captured for this template.
    /// </summary>
    /// <remarks>
    /// This is a vendor extension and may not be available on all authenticators.
    /// </remarks>
    public int? SampleCount { get; internal init; }
    
    /// <summary>
    /// Decodes a TemplateInfo from a CBOR response.
    /// </summary>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded template info.</returns>
    public static TemplateInfo Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Lax);
        return DecodeFromReader(reader);
    }
    
    /// <summary>
    /// Decodes a TemplateInfo from an existing CBOR reader.
    /// </summary>
    internal static TemplateInfo DecodeFromReader(CborReader reader)
    {
        ReadOnlyMemory<byte> templateId = default;
        string? friendlyName = null;
        int? sampleCount = null;
        
        var mapCount = reader.ReadStartMap() ?? 0;
        
        for (var i = 0; i < mapCount; i++)
        {
            var key = reader.ReadInt32();
            
            switch (key)
            {
                case 4: // templateId (0x04)
                    templateId = reader.ReadByteString();
                    break;
                    
                case 7: // templateFriendlyName (0x07)
                    friendlyName = reader.ReadTextString();
                    break;
                    
                case 8: // sampleCount (0x08) - vendor extension
                    sampleCount = reader.ReadInt32();
                    break;
                    
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        
        return new TemplateInfo
        {
            TemplateId = templateId,
            FriendlyName = friendlyName,
            SampleCount = sampleCount
        };
    }
}

/// <summary>
/// Result from enumerating enrolled fingerprint templates.
/// </summary>
public sealed class EnrollmentEnumerationResult
{
    /// <summary>
    /// Gets the list of enrolled templates.
    /// </summary>
    public IReadOnlyList<TemplateInfo> Templates { get; private init; } = [];
    
    /// <summary>
    /// Decodes an EnrollmentEnumerationResult from a CBOR response.
    /// </summary>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded enumeration result containing the first template.</returns>
    public static EnrollmentEnumerationResult Decode(ReadOnlyMemory<byte> data)
    {
        // The response contains a single template info
        var template = TemplateInfo.Decode(data);
        
        return new EnrollmentEnumerationResult
        {
            Templates = [template]
        };
    }
    
    /// <summary>
    /// Creates an enumeration result from a list of templates.
    /// </summary>
    internal static EnrollmentEnumerationResult FromTemplates(IReadOnlyList<TemplateInfo> templates)
    {
        return new EnrollmentEnumerationResult { Templates = templates };
    }
}
