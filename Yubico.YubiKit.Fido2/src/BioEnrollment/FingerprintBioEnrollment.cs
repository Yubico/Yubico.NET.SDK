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
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.BioEnrollment;

/// <summary>
/// Provides operations for biometric (fingerprint) enrollment on a FIDO2 authenticator.
/// </summary>
/// <remarks>
/// <para>
/// Bio enrollment requires authenticator support (bioEnroll option) and a
/// PIN/UV auth token with the <see cref="PinUvAuthTokenPermissions.BioEnrollment"/>
/// permission.
/// </para>
/// <para>
/// Requires YubiKey firmware 5.2 or later with bio-enabled hardware.
/// </para>
/// <para>
/// IMPORTANT: Fingerprint enrollment operations require physical user interaction
/// (placing finger on sensor). These operations will timeout if no user activity
/// is detected.
/// </para>
/// </remarks>
public sealed class FingerprintBioEnrollment
{
    private readonly IFidoSession _session;
    private readonly IPinUvAuthProtocol _protocol;
    private readonly ReadOnlyMemory<byte> _pinUvAuthToken;
    private readonly byte _command;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FingerprintBioEnrollment"/> class.
    /// </summary>
    /// <param name="session">The FIDO session to use for communication.</param>
    /// <param name="protocol">The PIN/UV auth protocol to use.</param>
    /// <param name="pinUvAuthToken">The PIN/UV auth token with bio enrollment permission.</param>
    /// <param name="usePreviewCommand">
    /// If true, uses the prototype bio enrollment command (0x40) for older authenticators.
    /// Default is false to use the standard command (0x09).
    /// </param>
    public FingerprintBioEnrollment(
        IFidoSession session,
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
    /// This command does not require user interaction.
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
    /// Optional timeout in milliseconds for user interaction.
    /// Default is 10000ms (10 seconds).
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the first enrollment capture.</returns>
    /// <remarks>
    /// <para>
    /// This begins a new fingerprint enrollment. The user must place their finger
    /// on the sensor when this method is called. The returned result contains:
    /// - TemplateId: The ID of the new template being enrolled
    /// - RemainingSamples: Number of additional samples needed
    /// - LastSampleStatus: Status of the first capture
    /// </para>
    /// <para>
    /// After calling this method, continue calling <see cref="EnrollCaptureNextSampleAsync"/>
    /// until <see cref="EnrollmentSampleResult.IsComplete"/> is true.
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
    /// Optional timeout in milliseconds for user interaction.
    /// Default is 10000ms (10 seconds).
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of this capture, including remaining samples.</returns>
    /// <remarks>
    /// <para>
    /// Continue calling this method until <see cref="EnrollmentSampleResult.IsComplete"/>
    /// is true. Each call requires the user to place their finger on the sensor.
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
    /// Call this method if enrollment needs to be aborted before completion.
    /// This does not require user interaction.
    /// </remarks>
    public async Task EnrollCancelAsync(CancellationToken cancellationToken = default)
    {
        var payload = BuildEnrollCancelPayload();
        await SendBioEnrollmentCommandAsync(payload, cancellationToken)
            .ConfigureAwait(false);
    }
    
    /// <summary>
    /// Enumerates all enrolled fingerprint templates.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of all enrolled templates.</returns>
    /// <remarks>
    /// This does not require user interaction.
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
            // No templates enrolled - return empty list
            return [];
        }
        
        var results = new List<TemplateInfo>();
        var reader = new CborReader(response, CborConformanceMode.Lax);
        
        // Parse the response which may contain a list or single template
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
                    
                case 7: // templateFriendlyName
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
    /// <param name="templateId">The template ID to rename.</param>
    /// <param name="friendlyName">The friendly name to set.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// This does not require user interaction.
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
    /// <param name="templateId">The template ID to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// This does not require user interaction.
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
        // Get sensor info uses modality parameter but no auth
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
        
        // Build message to authenticate (subCommand only for enrollBegin)
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
        
        // 0x06: timeout (optional)
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
        
        // Build message to authenticate: subCommand || subCommandParams
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
        
        // 0x06: timeout (optional)
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
        
        // Build PIN/UV auth param over just the subcommand
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
        
        // Build message to authenticate: subCommand || subCommandParams
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
        
        // Build message to authenticate: subCommand || subCommandParams
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

/// <summary>
/// Represents an internal TemplateInfo that can be modified during parsing.
/// </summary>
file sealed class MutableTemplateInfo
{
    public ReadOnlyMemory<byte> TemplateId { get; set; }
    public string? FriendlyName { get; set; }
    public int? SampleCount { get; set; }
    
    public TemplateInfo ToImmutable() => new()
    {
        TemplateId = TemplateId,
        FriendlyName = FriendlyName,
        SampleCount = SampleCount
    };
}

// Extension to support internal init for TemplateInfo
file static class TemplateInfoExtensions
{
    public static TemplateInfo Create(
        ReadOnlyMemory<byte> templateId,
        string? friendlyName = null,
        int? sampleCount = null)
    {
        return new TemplateInfo
        {
            TemplateId = templateId,
            FriendlyName = friendlyName,
            SampleCount = sampleCount
        };
    }
}
