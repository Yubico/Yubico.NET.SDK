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
using Yubico.YubiKit.Fido2.Cbor;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.Config;

/// <summary>
/// Provides operations for configuring FIDO2 authenticator settings.
/// </summary>
/// <remarks>
/// <para>
/// Authenticator configuration allows modifying authenticator behavior such as
/// enabling enterprise attestation, toggling the always-UV requirement, and
/// setting minimum PIN length policies.
/// </para>
/// <para>
/// All configuration operations require a PIN/UV auth token with the
/// <see cref="PinUvAuthTokenPermissions.AuthenticatorConfig"/> permission.
/// </para>
/// <para>
/// Requires YubiKey firmware 5.4 or later.
/// </para>
/// </remarks>
public sealed class AuthenticatorConfig
{
    private readonly FidoSession _session;
    private readonly IPinUvAuthProtocol _protocol;
    private readonly ReadOnlyMemory<byte> _pinUvAuthToken;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticatorConfig"/> class.
    /// </summary>
    /// <param name="session">The FIDO session to use for communication.</param>
    /// <param name="protocol">The PIN/UV auth protocol to use.</param>
    /// <param name="pinUvAuthToken">The PIN/UV auth token with authenticator config permission.</param>
    /// <exception cref="ArgumentNullException">Thrown when session or protocol is null.</exception>
    public AuthenticatorConfig(
        FidoSession session,
        IPinUvAuthProtocol protocol,
        ReadOnlyMemory<byte> pinUvAuthToken)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        _pinUvAuthToken = pinUvAuthToken;
    }
    
    /// <summary>
    /// Enables enterprise attestation on the authenticator.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// <para>
    /// Enterprise attestation allows the authenticator to return a uniquely
    /// identifying attestation certificate during credential creation when
    /// the relying party requests enterprise attestation.
    /// </para>
    /// <para>
    /// This operation requires the authenticator to support the ep option.
    /// Once enabled, enterprise attestation cannot be disabled without a
    /// factory reset.
    /// </para>
    /// </remarks>
    /// <exception cref="CtapException">Thrown when the operation fails.</exception>
    public async Task EnableEnterpriseAttestationAsync(CancellationToken cancellationToken = default)
    {
        var payload = BuildCommandPayload(ConfigSubCommand.EnableEnterpriseAttestation);
        await SendConfigCommandAsync(payload, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Toggles the alwaysUv setting on the authenticator.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// <para>
    /// When alwaysUv is enabled, user verification is always performed
    /// regardless of the uv option specified in the request. This provides
    /// additional security by ensuring every operation requires user
    /// verification (typically PIN entry or biometric).
    /// </para>
    /// <para>
    /// This toggles the current state - if alwaysUv is disabled, it becomes
    /// enabled, and vice versa.
    /// </para>
    /// </remarks>
    /// <exception cref="CtapException">Thrown when the operation fails.</exception>
    public async Task ToggleAlwaysUvAsync(CancellationToken cancellationToken = default)
    {
        var payload = BuildCommandPayload(ConfigSubCommand.ToggleAlwaysUv);
        await SendConfigCommandAsync(payload, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Sets the minimum PIN length required by the authenticator.
    /// </summary>
    /// <param name="newMinPinLength">The new minimum PIN length (4-63 characters).</param>
    /// <param name="rpIds">Optional list of RP IDs that can see the current minPinLength via minPinLength extension.</param>
    /// <param name="forceChangePin">If true, require PIN change before the next use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// <para>
    /// The new minimum PIN length must be greater than or equal to the
    /// current minimum PIN length. The authenticator enforces this during
    /// PIN set and PIN change operations.
    /// </para>
    /// <para>
    /// If <paramref name="forceChangePin"/> is true, the current PIN will
    /// be invalidated and the user must change their PIN before the next
    /// operation that requires PIN authentication.
    /// </para>
    /// <para>
    /// The <paramref name="rpIds"/> parameter allows specifying relying
    /// parties that can observe the minimum PIN length via the minPinLength
    /// extension. This requires setMinPINLength option support.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="newMinPinLength"/> is less than 4 or greater than 63.
    /// </exception>
    /// <exception cref="CtapException">Thrown when the operation fails.</exception>
    public async Task SetMinPinLengthAsync(
        int newMinPinLength,
        IReadOnlyList<string>? rpIds = null,
        bool forceChangePin = false,
        CancellationToken cancellationToken = default)
    {
        if (newMinPinLength < 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newMinPinLength),
                newMinPinLength,
                "Minimum PIN length must be at least 4 characters.");
        }
        
        if (newMinPinLength > 63)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newMinPinLength),
                newMinPinLength,
                "Minimum PIN length cannot exceed 63 characters.");
        }
        
        var payload = BuildSetMinPinLengthPayload(newMinPinLength, rpIds, forceChangePin);
        await SendConfigCommandAsync(payload, cancellationToken).ConfigureAwait(false);
    }
    
    private async Task SendConfigCommandAsync(
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        await _session.SendCborAsync(CtapCommand.Config, payload, cancellationToken)
            .ConfigureAwait(false);
    }
    
    private ReadOnlyMemory<byte> BuildCommandPayload(byte subCommand)
    {
        // Build PIN/UV auth param over just the subcommand (0xff || subCommand)
        Span<byte> message = stackalloc byte[2];
        message[0] = 0xff; // Magic prefix for config command auth
        message[1] = subCommand;
        var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, message);
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        
        // 0x01: subCommand
        writer.WriteInt32(1);
        writer.WriteInt32(subCommand);
        
        // 0x03: pinUvAuthProtocol
        writer.WriteInt32(3);
        writer.WriteInt32(_protocol.Version);
        
        // 0x04: pinUvAuthParam
        writer.WriteInt32(4);
        writer.WriteByteString(pinUvAuthParam);
        
        writer.WriteEndMap();
        
        return writer.Encode();
    }
    
    private ReadOnlyMemory<byte> BuildSetMinPinLengthPayload(
        int newMinPinLength,
        IReadOnlyList<string>? rpIds,
        bool forceChangePin)
    {
        // Build the subCommandParams map first
        var paramsWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        
        var paramsCount = 1; // always have newMinPINLength
        if (rpIds is { Count: > 0 }) paramsCount++;
        if (forceChangePin) paramsCount++;
        
        paramsWriter.WriteStartMap(paramsCount);
        
        // 0x01: newMinPINLength
        paramsWriter.WriteInt32(1);
        paramsWriter.WriteInt32(newMinPinLength);
        
        // 0x02: minPinLengthRPIDs (optional)
        if (rpIds is { Count: > 0 })
        {
            paramsWriter.WriteInt32(2);
            paramsWriter.WriteStartArray(rpIds.Count);
            foreach (var rpId in rpIds)
            {
                paramsWriter.WriteTextString(rpId);
            }
            paramsWriter.WriteEndArray();
        }
        
        // 0x03: forceChangePin (optional)
        if (forceChangePin)
        {
            paramsWriter.WriteInt32(3);
            paramsWriter.WriteBoolean(true);
        }
        
        paramsWriter.WriteEndMap();
        var subCommandParams = paramsWriter.Encode();
        
        // Build PIN/UV auth param over (0xff || subCommand || params)
        var subCommand = ConfigSubCommand.SetMinPinLength;
        var messageLength = 2 + subCommandParams.Length;
        var message = new byte[messageLength];
        message[0] = 0xff;
        message[1] = subCommand;
        subCommandParams.CopyTo(message.AsMemory(2));
        
        var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, message);
        
        // Build main payload
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);
        
        // 0x01: subCommand
        writer.WriteInt32(1);
        writer.WriteInt32(subCommand);
        
        // 0x02: subCommandParams
        writer.WriteInt32(2);
        writer.WriteEncodedValue(subCommandParams);
        
        // 0x03: pinUvAuthProtocol
        writer.WriteInt32(3);
        writer.WriteInt32(_protocol.Version);
        
        // 0x04: pinUvAuthParam
        writer.WriteInt32(4);
        writer.WriteByteString(pinUvAuthParam);
        
        writer.WriteEndMap();
        
        return writer.Encode();
    }
}
