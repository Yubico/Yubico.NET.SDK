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
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2.Cbor;
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
/// The standard path uses the CTAP 2.1 Section 6.7 request format. The opt-in prototype
/// path retains its previous request format; the SDK does not establish prototype interoperability.
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
/// helper does not dispose the protocol. It clears the named temporary request and
/// authentication buffers it owns. Cancellation or failure is not device rollback.
/// </para>
/// <para>
/// For a valid enrollment response, retain the template ID from begin independently
/// of later results. Continue captures using that same ID until the device reports zero
/// remaining samples, an unrecoverable error occurs or the user cancels.
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
    /// the prototype payload format.
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
    /// No capture or token authentication is requested.
    /// </remarks>
    public async Task<FingerprintSensorInfo> GetFingerprintSensorInfoAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await SendBioEnrollmentCommandAsync(BioEnrollmentSubCommand.GetFingerprintSensorInfo, cancellationToken)
            .ConfigureAwait(false);

        return FingerprintSensorInfo.Decode(response);
    }

    /// <summary>
    /// Begins a new fingerprint enrollment.
    /// </summary>
    /// <param name="timeout">
    /// Optional interaction timeout in milliseconds. Null omits it; no SDK ten-second
    /// default or host deadline is set. The standard command sends it in subCommandParams key 3.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the first enrollment capture.</returns>
    /// <remarks>
    /// <para>
    /// The standard begin operation captures a sample and cancels any unfinished enrollment
    /// (CTAP 2.1 Section 6.7.4).
    /// </para>
    /// <para>
    /// Retain the begin TemplateId for every <see cref="EnrollCaptureNextSampleAsync"/> call.
    /// Standard next responses need not repeat it. Use sample status and remaining count
    /// as device feedback, not a fixed SDK capture budget. Stop on unrecoverable error or
    /// user cancellation. Responses missing required fields fail decoding.
    /// </para>
    /// </remarks>
    public async Task<EnrollmentBeginResult> EnrollBeginAsync(
        int? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var response = await SendBioEnrollmentCommandAsync(BioEnrollmentSubCommand.EnrollBegin, cancellationToken, timeout: timeout)
            .ConfigureAwait(false);

        return EnrollmentBeginResult.Decode(response);
    }

    /// <summary>
    /// Captures the next sample for an ongoing fingerprint enrollment.
    /// </summary>
    /// <param name="templateId">The template ID from the EnrollBegin response.</param>
    /// <param name="timeout">
    /// Optional interaction timeout in milliseconds. Null omits it; the standard command
    /// sends it in subCommandParams key 3.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of this capture, including remaining samples.</returns>
    /// <remarks>
    /// <para>
    /// Retain and reuse the ID from begin; this result contains only sample feedback.
    /// A standard capture requires sensor interaction and reports status/remaining count
    /// (CTAP 2.1 Section 6.7.4). IsComplete is only a decoded-count projection,
    /// not independently verified persistence.
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
        var response = await SendBioEnrollmentCommandAsync(BioEnrollmentSubCommand.EnrollCaptureNextSample, cancellationToken, templateId, timeout: timeout)
            .ConfigureAwait(false);

        return EnrollmentSampleResult.Decode(response);
    }

    /// <summary>
    /// Cancels an ongoing fingerprint enrollment.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// Sends cancelCurrentEnrollment without sensor capture or authentication.
    /// This is distinct from cancelling a .NET wait or CTAPHID exchange. Cancellation tokens
    /// do not automatically call this method, and its completion is not a rollback guarantee.
    /// </remarks>
    public async Task EnrollCancelAsync(CancellationToken cancellationToken = default)
    {
        await SendBioEnrollmentCommandAsync(BioEnrollmentSubCommand.EnrollCancel, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Requests enrollment inventory.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The returned templates, or an empty list for InvalidOption (no enrollments).</returns>
    /// <remarks>
    /// No sensor capture is needed. CTAP 2.1 Section 6.7.6 returns a key-7 array
    /// of nested templates (required ID key 1, optional name key 2). Other CTAP statuses propagate.
    /// </remarks>
    public async Task<IReadOnlyList<TemplateInfo>> EnumerateEnrollmentsAsync(
        CancellationToken cancellationToken = default)
    {
        ReadOnlyMemory<byte> response;

        try
        {
            response = await SendBioEnrollmentCommandAsync(BioEnrollmentSubCommand.EnumerateEnrollments, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CtapException ex) when (ex.Status == CtapStatus.InvalidOption)
        {
            return [];
        }

        return ReadTemplateInventory(response);
    }

    private static IReadOnlyList<TemplateInfo> ReadTemplateInventory(ReadOnlyMemory<byte> response)
    {
        var reader = new CborReader(response, CborConformanceMode.Ctap2Canonical);
        List<TemplateInfo>? results = null;
        var mapCount = reader.ReadStartMap() ?? 0;
        for (var i = 0; i < mapCount; i++)
        {
            if (reader.ReadInt32() != 7)
            {
                reader.SkipValue();
                continue;
            }

            int count = reader.ReadStartArray() ?? throw new CborContentException("Template array must have a definite length.");
            results = [];
            for (int j = 0; j < count; j++)
                results.Add(ReadInventoryEntry(reader));
            reader.ReadEndArray();
        }

        reader.ReadEndMap();
        if (reader.BytesRemaining != 0 || results is null)
            throw new CborContentException("Expected a complete template inventory response with templateInfos.");
        return results;
    }

    private static TemplateInfo ReadInventoryEntry(CborReader reader)
    {
        byte[]? id = null;
        string? name = null;
        int fields = reader.ReadStartMap() ?? 0;
        for (int i = 0; i < fields; i++)
        {
            int key = reader.ReadInt32();
            if (key == 1) id = reader.ReadByteString();
            else if (key == 2) name = reader.ReadTextString();
            else reader.SkipValue();
        }
        reader.ReadEndMap();
        if (id is null || id.Length == 0)
            throw new CborContentException("Template inventory entry lacks an ID.");
        return new TemplateInfo { TemplateId = id, FriendlyName = name };
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
    /// No sensor capture is needed.
    /// </remarks>
    public async Task SetFriendlyNameAsync(
        ReadOnlyMemory<byte> templateId,
        string friendlyName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(friendlyName);

        await SendBioEnrollmentCommandAsync(BioEnrollmentSubCommand.SetFriendlyName, cancellationToken, templateId, friendlyName)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes an enrolled fingerprint template.
    /// </summary>
    /// <param name="templateId">The opaque template ID to remove, not fingerprint template bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// No sensor capture is needed. The standard operation permanently removes the template
    /// and returns InvalidOption for an unknown ID (CTAP 2.1 Section 6.7.8).
    /// A failed call does not establish unchanged device state.
    /// </remarks>
    public async Task RemoveEnrollmentAsync(
        ReadOnlyMemory<byte> templateId,
        CancellationToken cancellationToken = default)
    {
        await SendBioEnrollmentCommandAsync(BioEnrollmentSubCommand.RemoveEnrollment, cancellationToken, templateId)
            .ConfigureAwait(false);
    }

    private async Task<ReadOnlyMemory<byte>> SendBioEnrollmentCommandAsync(
        byte subCommand,
        CancellationToken cancellationToken,
        ReadOnlyMemory<byte>? templateId = null,
        string? friendlyName = null,
        int? timeout = null)
    {
        var request = _command == CtapCommand.PrototypeBioEnrollment
            ? BuildPreviewRequest(subCommand, templateId, friendlyName, timeout)
            : BuildRequest(CtapCommand.BioEnrollment, subCommand, templateId, friendlyName, timeout);
        try
        {
            return await _session.SendCborRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(request);
        }
    }

    // Preserve the existing opt-in prototype wire profile; do not treat 0x40 as a standard fallback.
    private byte[] BuildPreviewRequest(
        byte subCommand, ReadOnlyMemory<byte>? templateId, string? friendlyName, int? timeout) =>
        BuildRequest(CtapCommand.PrototypeBioEnrollment, (byte)(subCommand + 1), templateId, friendlyName,
            timeout: null, previewTimeout: timeout);

    private byte[] BuildRequest(
        byte command, byte subCommand, ReadOnlyMemory<byte>? templateId, string? friendlyName,
        int? timeout, int? previewTimeout = null)
    {
        using var request = CtapRequestBuilder.Create(command).WithInt(1, FingerprintModality.Fingerprint);
        if (command == CtapCommand.PrototypeBioEnrollment && subCommand == 8)
            return request.Build(); // Prototype sensor info previously used only modality.

        request.WithInt(2, subCommand);
        if (previewTimeout is int milliseconds)
            request.WithInt(6, milliseconds);

        bool authenticated = command == CtapCommand.BioEnrollment
            ? subCommand is not (BioEnrollmentSubCommand.EnrollCancel or
                BioEnrollmentSubCommand.GetFingerprintSensorInfo)
            : subCommand != 4;
        return authenticated
            ? BuildAuthenticatedRequest(request, command, subCommand, templateId, friendlyName, timeout)
            : request.Build();
    }

    private byte[] BuildAuthenticatedRequest(
        CtapRequestBuilder request, byte command, byte subCommand, ReadOnlyMemory<byte>? templateId,
        string? friendlyName, int? timeout)
    {
        byte[] parameters = BuildParameters(templateId, friendlyName, timeout);
        byte[]? message = null;
        byte[]? tag = null;
        try
        {
            bool standard = command == CtapCommand.BioEnrollment;
            int prefixLength = standard ? 2 : 1;
            message = new byte[prefixLength + parameters.Length];
            if (standard) message[0] = FingerprintModality.Fingerprint;
            message[prefixLength - 1] = subCommand;
            parameters.CopyTo(message.AsSpan(prefixLength));
            tag = _protocol.Authenticate(_pinUvAuthToken.Span, message);

            if (parameters.Length != 0)
                request.WithEncodedValue(3, parameters);
            request.WithInt(4, _protocol.Version).WithBytes(5, tag.AsSpan());
            return request.Build();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(parameters);
            if (message is not null) CryptographicOperations.ZeroMemory(message);
            if (tag is not null) CryptographicOperations.ZeroMemory(tag);
        }
    }

    private static byte[] BuildParameters(ReadOnlyMemory<byte>? templateId, string? friendlyName, int? timeout)
    {
        if (templateId is null && timeout is null)
            return [];

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        int count = (templateId.HasValue ? 1 : 0) + (friendlyName is not null ? 1 : 0) + (timeout.HasValue ? 1 : 0);
        writer.WriteStartMap(count);
        if (templateId is { } id)
        {
            writer.WriteInt32(1);
            writer.WriteByteString(id.Span);
        }
        if (friendlyName is not null)
        {
            writer.WriteInt32(2);
            writer.WriteTextString(friendlyName);
        }
        if (timeout is int milliseconds)
        {
            writer.WriteInt32(3);
            writer.WriteInt32(milliseconds);
        }
        writer.WriteEndMap();
        return writer.Encode();
    }
}