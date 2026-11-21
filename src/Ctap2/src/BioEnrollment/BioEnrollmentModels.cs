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
/// Fingerprint capture feedback.
/// </summary>
/// <remarks>
/// The values follow CTAP 2.1 (2022-06-21 errata), Section 6.7, except for DatabaseFull.
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
    /// Fingerprint already exists (0x0B).
    /// </summary>
    Exists = 0x0B,

    /// <summary>
    /// Fingerprint database full (0x0C).
    /// </summary>
    /// <remarks>Matches python-fido2 FP_DATABASE_FULL; CTAP 2.1 leaves 0x0C unused.</remarks>
    DatabaseFull = 0x0C,

    /// <summary>
    /// No touch or swipe activity (0x0D).
    /// </summary>
    NoUserActivity = 0x0D,

    /// <summary>
    /// No user-presence transition, such as lifting the finger (0x0E).
    /// </summary>
    NoUpTransition = 0x0E
}

/// <summary>
/// Information about the fingerprint sensor capabilities.
/// </summary>
/// <remarks>
/// Models selected sensor response fields. CTAP 2.1 (2022-06-21 errata), Section 6.7.3,
/// also supplies maxTemplateFriendlyName at key 8, which is available only through RawData
/// here. A decoded object does not establish physical sensor interoperability.
/// </remarks>
public sealed class FingerprintSensorInfo
{
    /// <summary>
    /// Gets the complete original CBOR-encoded authenticator response.
    /// </summary>
    public ReadOnlyMemory<byte> RawData { get; private init; }

    /// <summary>
    /// Gets the decoded fingerprint kind.
    /// </summary>
    public FingerprintKind FingerprintKind { get; private init; }

    /// <summary>
    /// Gets the maximum number of samples required for enrollment.
    /// </summary>
    /// <remarks>
    /// This reports the maximum good samples, not a limit on total capture attempts:
    /// rejected captures may not count.
    /// </remarks>
    public int MaxCaptureSamplesRequiredForEnroll { get; private init; }

    /// <summary>
    /// Decodes a FingerprintSensorInfo from a CBOR response.
    /// </summary>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded sensor info.</returns>
    public static FingerprintSensorInfo Decode(ReadOnlyMemory<byte> data)
    {
        ReadOnlyMemory<byte> rawData = data.ToArray();
        var reader = new CborReader(rawData, CborConformanceMode.Ctap2Canonical);

        FingerprintKind? fingerprintKind = null;
        int? maxSamples = null;

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

                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        if (reader.BytesRemaining != 0 || fingerprintKind is null || maxSamples is null or < 0)
            throw new CborContentException("Incomplete or invalid fingerprint sensor information.");

        return new FingerprintSensorInfo
        {
            RawData = rawData,
            FingerprintKind = fingerprintKind.Value,
            MaxCaptureSamplesRequiredForEnroll = maxSamples.Value
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

/// <summary>Feedback from a fingerprint enrollment capture.</summary>
public class EnrollmentSampleResult
{
    internal EnrollmentSampleResult(ReadOnlyMemory<byte> rawData, FingerprintSampleStatus status, int remainingSamples)
    {
        RawData = rawData;
        LastSampleStatus = status;
        RemainingSamples = remainingSamples;
    }

    /// <summary>Gets the complete original CBOR-encoded authenticator response.</summary>
    public ReadOnlyMemory<byte> RawData { get; }

    /// <summary>Gets the authenticator's sample feedback.</summary>
    public FingerprintSampleStatus LastSampleStatus { get; }

    /// <summary>Gets the authenticator's remaining sample count.</summary>
    public int RemainingSamples { get; }

    /// <summary>Gets whether the authenticator reports zero remaining samples.</summary>
    public bool IsComplete => RemainingSamples == 0;

    /// <summary>Decodes a continuation response, which does not require the begin template ID.</summary>
    /// <param name="data">The borrowed CBOR response.</param>
    /// <returns>Sample feedback with an owned copy of the response bytes.</returns>
    /// <exception cref="CborContentException">The response lacks valid status or remaining-count fields.</exception>
    public static EnrollmentSampleResult Decode(ReadOnlyMemory<byte> data) => DecodeSample(data, begin: false);

    internal static EnrollmentSampleResult DecodeSample(ReadOnlyMemory<byte> data, bool begin)
    {
        ReadOnlyMemory<byte> rawData = data.ToArray();
        var reader = new CborReader(rawData, CborConformanceMode.Ctap2Canonical);
        byte[]? templateId = null;
        int? status = null;
        int? remaining = null;

        int count = reader.ReadStartMap() ?? 0;
        for (int i = 0; i < count; i++)
        {
            int key = reader.ReadInt32();
            if (key == 4 && begin) templateId = reader.ReadByteString();
            else if (key == 5) status = reader.ReadInt32();
            else if (key == 6) remaining = reader.ReadInt32();
            else reader.SkipValue();
        }
        reader.ReadEndMap();
        if (reader.BytesRemaining != 0 || status is null or < 0 || remaining is null or < 0)
            throw new CborContentException("Incomplete or invalid fingerprint capture response.");

        return begin
            ? new EnrollmentBeginResult(rawData, (FingerprintSampleStatus)status.Value, remaining.Value,
                templateId is { Length: > 0 } ? templateId : throw new CborContentException("Missing enrollment template ID."))
            : new EnrollmentSampleResult(rawData, (FingerprintSampleStatus)status.Value, remaining.Value);
    }
}

/// <summary>Initial capture feedback and the template ID assigned by the authenticator.</summary>
public sealed class EnrollmentBeginResult : EnrollmentSampleResult
{
    internal EnrollmentBeginResult(ReadOnlyMemory<byte> rawData, FingerprintSampleStatus status,
        int remainingSamples, ReadOnlyMemory<byte> templateId) : base(rawData, status, remainingSamples)
    {
        TemplateId = templateId;
    }

    /// <summary>Gets the template ID to pass to every continuation request.</summary>
    public ReadOnlyMemory<byte> TemplateId { get; }

    /// <summary>Decodes a begin response, including its required template ID.</summary>
    /// <param name="data">The borrowed CBOR response.</param>
    /// <returns>Sample feedback, template ID and an owned copy of the response bytes.</returns>
    /// <exception cref="CborContentException">The response lacks a valid template ID, status or remaining count.</exception>
    public new static EnrollmentBeginResult Decode(ReadOnlyMemory<byte> data) =>
        (EnrollmentBeginResult)DecodeSample(data, begin: true);
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