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
using Yubico.YubiKit.Ctap2.Pin;

namespace Yubico.YubiKit.Ctap2.BioEnrollment;

/// <summary>
/// Provides operations for biometric (fingerprint) enrollment on a CTAP2 authenticator.
/// </summary>
/// <remarks>
/// <para>
/// Standard enrollment requires fingerprint hardware, bioEnroll option presence and a
/// token with <see cref="PinUvAuthTokenPermissions.BioEnrollment"/> permission. A false
/// bioEnroll value means supported with no templates, not unsupported. Acquiring this
/// permission through built-in UV additionally requires uvBioEnroll support. The helper
/// does not negotiate command/protocol support, obtain tokens or check their permissions/expiry.
/// </para>
/// <para>
/// The current standard-command path does not match CTAP 2.1 (2022-06-21 errata),
/// Section 6.7: its enrollment/administration subcommands are shifted by one, its sensor
/// request omits subCommand=7, and authenticated messages omit the modality byte. Explicit
/// timeouts are encoded at top-level key 6 instead of parameter-map key 3. The inventory
/// decoder also expects a flat template rather than the standard template array. These
/// limitations prevent treating this helper as a complete interoperable standard enrollment
/// workflow. Selecting the prototype command is not an established workaround for these defects.
/// </para>
/// <para>
/// Capture requires the user to place or swipe a finger on the sensor; inventory, rename,
/// removal and enrollment cancellation do not themselves require a capture. Calls route
/// through generic CBOR exchange and do not provide operation-specific SDK presence
/// notifications. No local interaction deadline or automatic enrollment cancellation is
/// implemented. The SDK's firmware threshold alone does not establish hardware or wire support.
/// </para>
/// <para>
/// This helper borrows the session, protocol and token memory. Keep both objects alive
/// and the token unchanged through awaited calls, then clear caller-owned token bytes
/// with CryptographicOperations.ZeroMemory, including on failure. Unlike ClientPin, this
/// helper does not dispose the protocol. It does not comprehensively clear encoded
/// requests or authentication-tag copies. Cancellation or failure is not device rollback.
/// </para>
/// <para>
/// For a valid enrollment response, retain the template ID from begin independently
/// of later results. Continue captures using that same ID until the device reports zero
/// remaining samples, an unrecoverable error occurs or the user cancels. The current decoder
/// defaults missing fields, so IsComplete alone is not proof of successful enrollment.
/// </para>
/// </remarks>
public sealed class FingerprintBioEnrollment
{
    private readonly ICtap2Session _session;
    private readonly IPinUvAuthProtocol _protocol;
    private readonly ReadOnlyMemory<byte> _pinUvAuthToken;
    private readonly byte _command;

    /// <summary>
    /// Initializes a new instance of the <see cref="FingerprintBioEnrollment"/> class.
    /// </summary>
    /// <param name="session">The borrowed CTAP2 session to use for communication.</param>
    /// <param name="protocol">The borrowed, authenticator-supported PIN/UV auth protocol.</param>
    /// <param name="pinUvAuthToken">Borrowed token bytes with bio enrollment permission for
    /// the standard command; the caller owns their lifetime and clearing.</param>
    /// <param name="usePreviewCommand">
    /// Selects prototype command 0x40 when true, standard 0x09 otherwise, without negotiation.
    /// CTAP 2.1 (2022-06-21 errata), Section 6.12, forbids prototype use when bioEnroll is
    /// present and specifies FIDO_2_1_PRE/preview feature detection. Preview authorization
    /// differs from permission-based standard authorization; this switch does not validate
    /// the prototype payload format or make the standard-path limitations disappear.
    /// </param>
    public FingerprintBioEnrollment(
        ICtap2Session session,
        IPinUvAuthProtocol protocol,
        ReadOnlyMemory<byte> pinUvAuthToken,
        bool usePreviewCommand = false)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(protocol);

        _session = session;
        _protocol = protocol;
        _pinUvAuthToken = pinUvAuthToken;
        _command = usePreviewCommand
            ? CtapCommand.PrototypeBioEnrollment
            : CtapCommand.BioEnrollment;
    }

    /// <summary>
    /// Gets information about the fingerprint sensor.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Information about the fingerprint sensor capabilities.</returns>
    /// <remarks>
    /// No capture or token authentication is requested. The current request sends modality
    /// only, omitting subCommand=7 required by CTAP 2.1 Section 6.7.3. Decoded defaults are
    /// not proof that a sensor-info response contained the required fields.
    /// </remarks>
    public async Task<FingerprintSensorInfo> GetFingerprintSensorInfoAsync(
        CancellationToken cancellationToken = default)
    {
        var payload = BuildGetSensorInfoPayload();
        var response = await SendBioEnrollmentCommandAsync(payload, cancellationToken)
            .ConfigureAwait(false);

        return FingerprintSensorInfo.Decode(response);
    }

    /// <summary>
    /// Begins a new fingerprint enrollment.
    /// </summary>
    /// <param name="timeout">
    /// Optional interaction timeout in milliseconds. Null omits it; no SDK ten-second
    /// default or host deadline is set. Explicit values currently use the wrong standard
    /// field (top-level key 6), rather than subCommandParams key 3.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the first enrollment capture.</returns>
    /// <remarks>
    /// <para>
    /// Sends the helper's begin request and decodes the first sample feedback. The
    /// standard begin operation captures a sample and cancels any unfinished enrollment
    /// (CTAP 2.1 Section 6.7.4), but the current subcommand/transcript/timeout limitations
    /// described on this class apply; successful standard enrollment is not guaranteed.
    /// </para>
    /// <para>
    /// Retain the begin TemplateId for every <see cref="EnrollCaptureNextSampleAsync"/> call.
    /// Standard next responses need not repeat it. Use sample status and remaining count
    /// as device feedback, not a fixed SDK capture budget. Stop on unrecoverable error or
    /// user cancellation; missing response fields can currently produce IsComplete=true.
    /// </para>
    /// </remarks>
    public async Task<EnrollmentSampleResult> EnrollBeginAsync(
        int? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildEnrollBeginPayload(timeout);
        var response = await SendBioEnrollmentCommandAsync(payload, cancellationToken)
            .ConfigureAwait(false);

        return EnrollmentSampleResult.Decode(response);
    }

    /// <summary>
    /// Captures the next sample for an ongoing fingerprint enrollment.
    /// </summary>
    /// <param name="templateId">The template ID from the EnrollBegin response.</param>
    /// <param name="timeout">
    /// Optional interaction timeout in milliseconds. Null omits it; explicit values
    /// currently use top-level key 6 instead of the standard parameter-map key 3.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of this capture, including remaining samples.</returns>
    /// <remarks>
    /// <para>
    /// Retain and reuse the ID from begin, not this result's potentially empty TemplateId.
    /// A standard capture requires sensor interaction and reports status/remaining count
    /// (CTAP 2.1 Section 6.7.4). The current subcommand/transcript/timeout limitations apply;
    /// IsComplete is only a decoded-count projection, not independently verified persistence.
    /// </para>
    /// <para>
    /// If the sample quality is poor (LastSampleStatus != Good), the sample may
    /// not count and RemainingSamples may not decrease.
    /// </para>
    /// </remarks>
    public async Task<EnrollmentSampleResult> EnrollCaptureNextSampleAsync(
        ReadOnlyMemory<byte> templateId,
        int? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildEnrollCaptureNextPayload(templateId, timeout);
        var response = await SendBioEnrollmentCommandAsync(payload, cancellationToken)
            .ConfigureAwait(false);

        return EnrollmentSampleResult.Decode(response);
    }

    /// <summary>
    /// Cancels an ongoing fingerprint enrollment.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// Intended to send cancelCurrentEnrollment without sensor capture or authentication.
    /// The current helper sends subcommand 4 rather than standard 3 (CTAP 2.1 Section 6.7.5).
    /// This is distinct from cancelling a .NET wait or CTAPHID exchange. Cancellation tokens
    /// do not automatically call this method, and its completion is not a rollback guarantee.
    /// </remarks>
    public async Task EnrollCancelAsync(CancellationToken cancellationToken = default)
    {
        var payload = BuildEnrollCancelPayload();
        await SendBioEnrollmentCommandAsync(payload, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Requests enrollment inventory and decodes the helper's current flat-template shape.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>At most one flat decoded template, or an empty list for InvalidCommand.</returns>
    /// <remarks>
    /// No sensor capture is needed. CTAP 2.1 Section 6.7.6 instead returns a key-7 array
    /// of nested templates (ID key 1, name key 2); this decoder does not handle that shape.
    /// The standard no-enrollment status is InvalidOption, not InvalidCommand. Because this
    /// helper converts InvalidCommand to an empty list, emptiness is not evidence of an
    /// empty device inventory. The class's request/transcript limitations also apply.
    /// </remarks>
    public async Task<IReadOnlyList<TemplateInfo>> EnumerateEnrollmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var payload = BuildEnumerateEnrollmentsPayload();
        ReadOnlyMemory<byte> response;

        try
        {
            response = await SendBioEnrollmentCommandAsync(payload, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CtapException ex) when (ex.Status == CtapStatus.InvalidCommand)
        {
            // Unsupported command is currently normalized to empty; standard no-enrollment is InvalidOption.
            return [];
        }

        var results = new List<TemplateInfo>();
        var reader = new CborReader(response, CborConformanceMode.Lax);

        // Current flat decoder; Section 6.7.6 instead defines key 7 as an array of nested templates.
        var mapCount = reader.ReadStartMap() ?? 0;
        ReadOnlyMemory<byte>? templateId = null;
        string? friendlyName = null;

        for (var i = 0; i < mapCount; i++)
        {
            var key = reader.ReadInt32();
            switch (key)
            {
                case 4: // templateId
                    templateId = reader.ReadByteString();
                    break;
                case 7: // Locally treated as a name; standard envelope key 7 is templateInfos array.
                    friendlyName = reader.ReadTextString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        if (templateId.HasValue)
        {
            results.Add(new TemplateInfo
            {
                TemplateId = templateId.Value,
                FriendlyName = friendlyName
            });
        }

        return results;
    }

    /// <summary>
    /// Sets a friendly name for an enrolled fingerprint template.
    /// </summary>
    /// <param name="templateId">The opaque template ID to rename, not fingerprint template bytes.</param>
    /// <param name="friendlyName">The nonblank friendly name to set. The device's advertised
    /// UTF-8 byte limit applies; CTAP 2.1 Section 6.7.7 recommends at most the lesser of 64
    /// bytes and maxTemplateFriendlyName. This helper only checks null/blank locally.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// No sensor capture is needed. The current subcommand and missing-modality
    /// authentication transcript do not match CTAP 2.1 Section 6.7.7; see the class limitations.
    /// </remarks>
    public async Task SetFriendlyNameAsync(
        ReadOnlyMemory<byte> templateId,
        string friendlyName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(friendlyName);

        var payload = BuildSetFriendlyNamePayload(templateId, friendlyName);
        await SendBioEnrollmentCommandAsync(payload, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes an enrolled fingerprint template.
    /// </summary>
    /// <param name="templateId">The opaque template ID to remove, not fingerprint template bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// No sensor capture is needed. The standard operation permanently removes the template
    /// and returns InvalidOption for an unknown ID (CTAP 2.1 Section 6.7.8). This helper's
    /// subcommand and authentication transcript do not match that operation; see the class
    /// limitations. A failed call does not establish unchanged device state.
    /// </remarks>
    public async Task RemoveEnrollmentAsync(
        ReadOnlyMemory<byte> templateId,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildRemoveEnrollmentPayload(templateId);
        await SendBioEnrollmentCommandAsync(payload, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ReadOnlyMemory<byte>> SendBioEnrollmentCommandAsync(
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        // Build request with command byte prefix
        var request = new byte[1 + payload.Length];
        request[0] = _command;
        payload.CopyTo(request.AsMemory(1));

        return await _session.SendCborRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
    }

    private ReadOnlyMemory<byte> BuildGetSensorInfoPayload()
    {
        // No auth; the current request omits subCommand=7 required by CTAP 2.1 Section 6.7.3.
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);

        // 0x01: modality (fingerprint = 0x01)
        writer.WriteInt32(1);
        writer.WriteInt32(FingerprintModality.Fingerprint);

        writer.WriteEndMap();

        return writer.Encode();
    }

    private ReadOnlyMemory<byte> BuildEnrollBeginPayload(int? timeout)
    {
        const byte subCommand = BioEnrollmentSubCommand.EnrollBegin;

        // Current transcript omits modality; Section 6.7.4 requires modality || subCommand || parameters.
        var subCommandBytes = new byte[] { subCommand };
        var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, subCommandBytes);

        // Count map entries
        var mapSize = 4; // modality, subCommand, pinUvAuthProtocol, pinUvAuthParam
        if (timeout.HasValue) mapSize++;

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(mapSize);

        // 0x01: modality
        writer.WriteInt32(1);
        writer.WriteInt32(FingerprintModality.Fingerprint);

        // 0x02: subCommand
        writer.WriteInt32(2);
        writer.WriteInt32(subCommand);

        // 0x04: pinUvAuthProtocol
        writer.WriteInt32(4);
        writer.WriteInt32(_protocol.Version);

        // 0x05: pinUvAuthParam
        writer.WriteInt32(5);
        writer.WriteByteString(pinUvAuthParam.AsSpan());

        // Current timeout placement is incorrect: standard key 6 is Boolean getModality, not timeout.
        if (timeout.HasValue)
        {
            writer.WriteInt32(6);
            writer.WriteInt32(timeout.Value);
        }

        writer.WriteEndMap();

        return writer.Encode();
    }

    private ReadOnlyMemory<byte> BuildEnrollCaptureNextPayload(
        ReadOnlyMemory<byte> templateId,
        int? timeout)
    {
        const byte subCommand = BioEnrollmentSubCommand.EnrollCaptureNextSample;

        // Build subCommandParams
        var subCommandParams = BuildTemplateIdParam(templateId);

        // Current transcript: subCommand || parameters; Section 6.7 requires a leading modality byte too.
        var messageToAuth = new byte[1 + subCommandParams.Length];
        messageToAuth[0] = subCommand;
        subCommandParams.Span.CopyTo(messageToAuth.AsSpan(1));

        var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, messageToAuth);

        // Count map entries
        var mapSize = 5; // modality, subCommand, subCommandParams, pinUvAuthProtocol, pinUvAuthParam
        if (timeout.HasValue) mapSize++;

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(mapSize);

        // 0x01: modality
        writer.WriteInt32(1);
        writer.WriteInt32(FingerprintModality.Fingerprint);

        // 0x02: subCommand
        writer.WriteInt32(2);
        writer.WriteInt32(subCommand);

        // 0x03: subCommandParams
        writer.WriteInt32(3);
        writer.WriteStartMap(1);
        writer.WriteInt32(1); // templateId key
        writer.WriteByteString(templateId.Span);
        writer.WriteEndMap();

        // 0x04: pinUvAuthProtocol
        writer.WriteInt32(4);
        writer.WriteInt32(_protocol.Version);

        // 0x05: pinUvAuthParam
        writer.WriteInt32(5);
        writer.WriteByteString(pinUvAuthParam.AsSpan());

        // Current timeout placement is incorrect: Section 6.7 puts it in parameter-map key 3, authenticated too.
        if (timeout.HasValue)
        {
            writer.WriteInt32(6);
            writer.WriteInt32(timeout.Value);
        }

        writer.WriteEndMap();

        return writer.Encode();
    }

    private ReadOnlyMemory<byte> BuildEnrollCancelPayload()
    {
        const byte subCommand = BioEnrollmentSubCommand.EnrollCancel;

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);

        // 0x01: modality
        writer.WriteInt32(1);
        writer.WriteInt32(FingerprintModality.Fingerprint);

        // 0x02: subCommand
        writer.WriteInt32(2);
        writer.WriteInt32(subCommand);

        writer.WriteEndMap();

        return writer.Encode();
    }

    private ReadOnlyMemory<byte> BuildEnumerateEnrollmentsPayload()
    {
        const byte subCommand = BioEnrollmentSubCommand.EnumerateEnrollments;

        // Current transcript omits modality required by CTAP 2.1 Section 6.7.6.
        var subCommandBytes = new byte[] { subCommand };
        var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, subCommandBytes);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);

        // 0x01: modality
        writer.WriteInt32(1);
        writer.WriteInt32(FingerprintModality.Fingerprint);

        // 0x02: subCommand
        writer.WriteInt32(2);
        writer.WriteInt32(subCommand);

        // 0x04: pinUvAuthProtocol
        writer.WriteInt32(4);
        writer.WriteInt32(_protocol.Version);

        // 0x05: pinUvAuthParam
        writer.WriteInt32(5);
        writer.WriteByteString(pinUvAuthParam.AsSpan());

        writer.WriteEndMap();

        return writer.Encode();
    }

    private ReadOnlyMemory<byte> BuildSetFriendlyNamePayload(
        ReadOnlyMemory<byte> templateId,
        string friendlyName)
    {
        const byte subCommand = BioEnrollmentSubCommand.SetFriendlyName;

        // Build subCommandParams
        var subCommandParams = BuildSetNameParams(templateId, friendlyName);

        // Current transcript omits the leading modality byte required by CTAP 2.1 Section 6.7.7.
        var messageToAuth = new byte[1 + subCommandParams.Length];
        messageToAuth[0] = subCommand;
        subCommandParams.Span.CopyTo(messageToAuth.AsSpan(1));

        var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, messageToAuth);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);

        // 0x01: modality
        writer.WriteInt32(1);
        writer.WriteInt32(FingerprintModality.Fingerprint);

        // 0x02: subCommand
        writer.WriteInt32(2);
        writer.WriteInt32(subCommand);

        // 0x03: subCommandParams
        writer.WriteInt32(3);
        writer.WriteStartMap(2);
        writer.WriteInt32(1); // templateId key
        writer.WriteByteString(templateId.Span);
        writer.WriteInt32(2); // templateFriendlyName key
        writer.WriteTextString(friendlyName);
        writer.WriteEndMap();

        // 0x04: pinUvAuthProtocol
        writer.WriteInt32(4);
        writer.WriteInt32(_protocol.Version);

        // 0x05: pinUvAuthParam
        writer.WriteInt32(5);
        writer.WriteByteString(pinUvAuthParam.AsSpan());

        writer.WriteEndMap();

        return writer.Encode();
    }

    private ReadOnlyMemory<byte> BuildRemoveEnrollmentPayload(ReadOnlyMemory<byte> templateId)
    {
        const byte subCommand = BioEnrollmentSubCommand.RemoveEnrollment;

        // Build subCommandParams
        var subCommandParams = BuildTemplateIdParam(templateId);

        // Current transcript omits the leading modality byte required by CTAP 2.1 Section 6.7.8.
        var messageToAuth = new byte[1 + subCommandParams.Length];
        messageToAuth[0] = subCommand;
        subCommandParams.Span.CopyTo(messageToAuth.AsSpan(1));

        var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, messageToAuth);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);

        // 0x01: modality
        writer.WriteInt32(1);
        writer.WriteInt32(FingerprintModality.Fingerprint);

        // 0x02: subCommand
        writer.WriteInt32(2);
        writer.WriteInt32(subCommand);

        // 0x03: subCommandParams
        writer.WriteInt32(3);
        writer.WriteStartMap(1);
        writer.WriteInt32(1); // templateId key
        writer.WriteByteString(templateId.Span);
        writer.WriteEndMap();

        // 0x04: pinUvAuthProtocol
        writer.WriteInt32(4);
        writer.WriteInt32(_protocol.Version);

        // 0x05: pinUvAuthParam
        writer.WriteInt32(5);
        writer.WriteByteString(pinUvAuthParam.AsSpan());

        writer.WriteEndMap();

        return writer.Encode();
    }

    private static ReadOnlyMemory<byte> BuildTemplateIdParam(ReadOnlyMemory<byte> templateId)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteInt32(1); // templateId
        writer.WriteByteString(templateId.Span);
        writer.WriteEndMap();
        return writer.Encode();
    }

    private static ReadOnlyMemory<byte> BuildSetNameParams(
        ReadOnlyMemory<byte> templateId,
        string friendlyName)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(1); // templateId
        writer.WriteByteString(templateId.Span);
        writer.WriteInt32(2); // templateFriendlyName
        writer.WriteTextString(friendlyName);
        writer.WriteEndMap();
        return writer.Encode();
    }
}