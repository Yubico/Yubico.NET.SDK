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

namespace Yubico.YubiKit.Ctap2.BioEnrollment;

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
/// Fingerprint capture feedback as currently modeled by the SDK.
/// </summary>
/// <remarks>
/// Values 0x00-0x0A match CTAP 2.1 (2022-06-21 errata), Section 6.7.
/// The final three member names do not match that standard table; consult their remarks
/// rather than treating their names as standardized device feedback.
/// </remarks>
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
    /// Local member for feedback value 0x0B.
    /// </summary>
    /// <remarks>The standard means fingerprint already exists, not storage full.</remarks>
    StorageFull = 0x0B,

    /// <summary>
    /// Local member for feedback value 0x0C.
    /// </summary>
    /// <remarks>The cited standard leaves 0x0C unused; no-user-activity is 0x0D.</remarks>
    NoUserActivity = 0x0C,

    /// <summary>
    /// Local member for feedback value 0x0D.
    /// </summary>
    /// <remarks>The standard means no touch/swipe activity. No finger-lift transition is 0x0E.</remarks>
    NoUserPresence = 0x0D
}

/// <summary>
/// Information about the fingerprint sensor capabilities.
/// </summary>
/// <remarks>
/// Models selected sensor response fields. CTAP 2.1 (2022-06-21 errata), Section 6.7.3,
/// also supplies maxTemplateFriendlyName at key 8, which is available only through RawData
/// here. The current helper's sensor request omits the required subcommand. A decoded
/// object does not establish complete or valid standard sensor-info exchange.
/// </remarks>
public sealed class FingerprintSensorInfo
{
    /// <summary>
    /// Gets the complete original CBOR-encoded authenticator response.
    /// </summary>
    public ReadOnlyMemory<byte> RawData { get; private init; }

    /// <summary>
    /// Gets the decoded fingerprint kind, defaulting to Touch when key 2 is missing.
    /// </summary>
    public FingerprintKind FingerprintKind { get; private init; }

    /// <summary>
    /// Gets the maximum number of samples required for enrollment.
    /// </summary>
    /// <remarks>
    /// This reports the maximum good samples, not a limit on total capture attempts:
    /// rejected captures may not count. Missing key 3 currently defaults to zero, so zero
    /// is not evidence that no samples are required. This decoder does not validate field presence.
    /// </remarks>
    public int MaxCaptureSamplesRequiredForEnroll { get; private init; }

    /// <summary>
    /// Gets the optional integer this decoder interprets as maximum template count at key 4.
    /// </summary>
    /// <remarks>CTAP 2.1 Section 6.7.3 does not define this sensor field. No original vendor
    /// definition establishes its meaning or availability; it is not qualified capacity information.</remarks>
    public int? MaxTemplateCount { get; private init; }

    /// <summary>
    /// Decodes a FingerprintSensorInfo from a CBOR response.
    /// </summary>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded sensor info.</returns>
    public static FingerprintSensorInfo Decode(ReadOnlyMemory<byte> data)
    {
        ReadOnlyMemory<byte> rawData = data.ToArray();
        var reader = new CborReader(rawData, CborConformanceMode.Lax);

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

                case 4: // Locally modeled maxTemplateCount; not a defined standard sensor-info field.
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
            RawData = rawData,
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
    /// Gets the complete original CBOR-encoded authenticator response.
    /// </summary>
    public ReadOnlyMemory<byte> RawData { get; private init; }

    /// <summary>
    /// Gets the template ID decoded from key 4, or empty memory if it is absent.
    /// </summary>
    /// <remarks>Retain the begin-response ID separately. CTAP 2.1 Section 6.7.4 does not
    /// require next-sample responses to repeat it; this decoder does not preserve earlier IDs.</remarks>
    public ReadOnlyMemory<byte> TemplateId { get; internal init; }

    /// <summary>
    /// Gets decoded sample feedback, defaulting to Good when key 5 is missing.
    /// </summary>
    public FingerprintSampleStatus LastSampleStatus { get; internal init; }

    /// <summary>
    /// Gets the number of remaining samples needed to complete enrollment.
    /// </summary>
    /// <remarks>
    /// In a valid sample response, zero reports completion. This decoder also defaults
    /// missing key 6 to zero and does not validate operation-specific required fields.
    /// The value is device feedback, not a verified persistence result or an SDK sample budget.
    /// </remarks>
    public int RemainingSamples { get; internal init; }

    /// <summary>
    /// Gets whether RemainingSamples equals zero, including its missing-field default.
    /// </summary>
    /// <remarks>An empty map currently produces true; this property alone is not proof of successful enrollment.</remarks>
    public bool IsComplete => RemainingSamples == 0;

    /// <summary>
    /// Decodes an EnrollmentSampleResult from a CBOR response.
    /// </summary>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded enrollment result.</returns>
    /// <remarks>
    /// Copies the complete borrowed response and reads keys 4/5/6. Missing fields retain
    /// empty ID, Good status and zero remaining count; required-field validation is absent.
    /// These defaults must not be treated as valid standard operation responses.
    /// </remarks>
    public static EnrollmentSampleResult Decode(ReadOnlyMemory<byte> data)
    {
        ReadOnlyMemory<byte> rawData = data.ToArray();
        var reader = new CborReader(rawData, CborConformanceMode.Lax);

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
            RawData = rawData,
            TemplateId = templateId,
            LastSampleStatus = lastStatus,
            RemainingSamples = remaining
        };
    }
}

/// <summary>
/// Metadata identifying and naming an enrolled fingerprint, not biometric template sample bytes.
/// </summary>
/// <remarks>
/// The current decoder reads a flat map with ID/name at keys 4/7. CTAP 2.1 (2022-06-21 errata),
/// Section 6.7, instead defines nested TemplateInfo with required ID key 1 and optional name
/// key 2, inside the inventory's key-7 array. This decoder does not implement that schema.
/// </remarks>
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
    /// Gets the optional integer locally interpreted as a sample count at key 8.
    /// </summary>
    /// <remarks>
    /// No original vendor definition establishes this interpretation. Standard envelope
    /// key 8 is maxTemplateFriendlyName, not sample count, and nested TemplateInfo has no such field.
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

                case 7: // Local flat-name field, not the standard nested TemplateInfo key 2.
                    friendlyName = reader.ReadTextString();
                    break;

                case 8: // Local sampleCount interpretation; no established vendor definition.
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
    /// <remarks>
    /// Wraps one flat decoded TemplateInfo. This does not parse the all-template array
    /// required by CTAP 2.1 Section 6.7.6, and is not a complete standard inventory decoder.
    /// </remarks>
    public static EnrollmentEnumerationResult Decode(ReadOnlyMemory<byte> data)
    {
        // Current flat-template wrapper, not the standard key-7 array of nested TemplateInfo maps.
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