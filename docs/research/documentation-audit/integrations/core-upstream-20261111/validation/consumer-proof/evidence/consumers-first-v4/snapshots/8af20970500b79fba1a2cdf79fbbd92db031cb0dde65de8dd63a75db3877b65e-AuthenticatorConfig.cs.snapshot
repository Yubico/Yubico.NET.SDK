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

namespace Yubico.YubiKit.Ctap2.Config;

/// <summary>
/// Provides operations for configuring CTAP2 authenticator settings.
/// </summary>
/// <remarks>
/// <para>
/// Authenticator configuration allows modifying authenticator behavior such as
/// enabling enterprise attestation, toggling the always-UV requirement, and
/// setting minimum PIN length policies.
/// </para>
/// <para>
/// This helper always sends authentication using the supplied protocol and token with
/// <see cref="PinUvAuthTokenPermissions.AuthenticatorConfig"/> permission. CTAP 2.1
/// (2022-06-21 errata), Section 6.11, also permits configuration without authorization
/// in specified unprotected-device states; this helper does not expose that route.
/// Acquiring the permission through built-in UV requires configured UV, pinUvAuthToken
/// and uvAcfg support, not just an authenticatorConfig capability advertisement.
/// </para>
/// <para>
/// Read fresh GetInfo before choosing an operation: authnrCfg=true advertises command
/// support, while individual subcommands can still be unsupported. Check ep, alwaysUv
/// or setMinPINLength as appropriate. The helper does not query or enforce capabilities
/// or firmware thresholds. The SDK's firmware feature metadata is not qualified support
/// for every setting on every model. Read GetInfo again after success to inspect results.
/// </para>
/// <para>
/// The helper borrows the session, protocol and token memory; unlike ClientPin it does
/// not dispose the protocol. Keep both objects alive and token bytes unchanged through
/// awaited calls, then clear the caller-owned token with CryptographicOperations.ZeroMemory,
/// including on failure. No token acquisition, refresh, permission or expiry validation is
/// performed locally. Request and authentication-tag copies are not comprehensively cleared
/// by this helper. Device changes persist beyond the helper's lifetime; failure or
/// cancellation is not rollback, and the helper performs no verification read or restoration.
/// </para>
/// </remarks>
public sealed class AuthenticatorConfig
{
    private readonly ICtap2Session _session;
    private readonly IPinUvAuthProtocol _protocol;
    private readonly ReadOnlyMemory<byte> _pinUvAuthToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticatorConfig"/> class.
    /// </summary>
    /// <param name="session">The borrowed CTAP2 session to use for communication.</param>
    /// <param name="protocol">The borrowed, authenticator-supported PIN/UV auth protocol.</param>
    /// <param name="pinUvAuthToken">Borrowed token bytes with authenticator config permission;
    /// the caller owns their lifetime and clearing.</param>
    /// <exception cref="ArgumentNullException">Thrown when session or protocol is null.</exception>
    public AuthenticatorConfig(
        ICtap2Session session,
        IPinUvAuthProtocol protocol,
        ReadOnlyMemory<byte> pinUvAuthToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(protocol);
        _session = session;
        _protocol = protocol;
        _pinUvAuthToken = pinUvAuthToken;
    }

    /// <summary>
    /// Enables enterprise attestation on the authenticator.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// <para>
    /// Enables the device feature; it does not itself create a credential or return a
    /// certificate. An eligible MakeCredential request must separately request enterprise
    /// attestation, with the appropriate platform/RP policy and device provisioning.
    /// </para>
    /// <para>
    /// Presence of ep advertises support; false means supported but disabled. Enabling is
    /// idempotent under CTAP 2.1 (2022-06-21 errata), Section 6.11.1. Read fresh GetInfo after
    /// success to check ep. This helper has no disable method. The standard FIDO reset
    /// disables enterprise attestation (Section 7.1.3), but also invalidates credentials;
    /// do not use it as routine configuration rollback.
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
    /// alwaysUv concerns credential creation and authentication, not every protocol
    /// operation or metadata query (CTAP 2.1, 2022-06-21 errata, Section 7.2.3). Its
    /// presence indicates support and its Boolean value is the current enabled state.
    /// </para>
    /// <para>
    /// This toggles state rather than setting a requested value. Read fresh GetInfo before
    /// deciding to toggle and again after success. The device can refuse disabling with
    /// OperationDenied (Section 6.11.2); support does not guarantee both directions work.
    /// Enabling can also disable makeCredUvNotRqd. If a sent command's outcome is unknown,
    /// reconcile the current state before another toggle: blindly repeating it can invert
    /// the intended result. No automatic retry or rollback is provided.
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
    /// <param name="newMinPinLength">The new ClientPIN minimum in Unicode code points.
    /// This helper accepts 4-63; device policy and UTF-8 PIN-byte limits apply separately.</param>
    /// <param name="rpIds">Optional replacement mutable list of RP IDs allowed to receive
    /// minPinLength extension output. Null and empty lists are omitted, not requests to clear the list.</param>
    /// <param name="forceChangePin">If true, request a PIN change before PIN-token acquisition.
    /// False is omitted, not an explicit request to clear forcePINChange.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// <para>
    /// The new minimum PIN length must be greater than or equal to the
    /// current minimum PIN length. The authenticator enforces this during
    /// PIN set and PIN change operations.
    /// </para>
    /// <para>
    /// Under CTAP 2.1 (2022-06-21 errata), Section 6.11.4, forceChangePin=true, or an
    /// existing PIN shorter than the new minimum, sets forcePINChange. This does not
    /// erase the current PIN: use it for ChangePin to satisfy policy. PIN-token acquisition
    /// is blocked until a successful change. In this revision, legacy getPinToken (0x05)
    /// returns PinInvalid (Section 6.5.5.7.1); permission-based PIN-token acquisition (0x09)
    /// returns PinPolicyViolation (Section 6.5.5.7.2). Forcing a change
    /// when no PIN is set produces PinNotSet. A false argument does not prevent a higher
    /// minimum from forcing a change.
    /// </para>
    /// <para>
    /// Check setMinPINLength support, and minPinLength extension support before supplying
    /// RP IDs. A nonempty list replaces previously added mutable recipients and is combined
    /// with any immutable preconfigured list; it is not an append-only grant. Device storage
    /// limits still apply. This helper always sends a numeric minimum and cannot express
    /// a policy-only request with the length omitted. It does not change RP account policy
    /// or verify extension output; read fresh GetInfo after success to inspect minimum/force state.
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
        // Build full request: command byte + payload. This request and auth-tag copies are not cleared here.
        var request = new byte[1 + payload.Length];
        request[0] = CtapCommand.Config;
        payload.CopyTo(request.AsMemory(1));

        await _session.SendCborRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
    }

    private ReadOnlyMemory<byte> BuildCommandPayload(byte subCommand)
    {
        // Build PIN/UV auth param: authenticate(pinUvAuthToken, 32*0xff || 0x0D || subCommand)
        // CTAP 2.1 (2022-06-21 errata), Section 6.11; not the credential-management transcript.
        Span<byte> message = stackalloc byte[32 + 1 + 1];
        message[..32].Fill(0xff);
        message[32] = CtapCommand.Config; // 0x0D
        message[33] = subCommand;
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

        // 0x02: minPinLengthRPIDs; null/empty omit the field, not a list-clearing request.
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

        // 0x03: forceChangePin; false omits the field rather than clearing the device flag.
        if (forceChangePin)
        {
            paramsWriter.WriteInt32(3);
            paramsWriter.WriteBoolean(true);
        }

        paramsWriter.WriteEndMap();
        var subCommandParams = paramsWriter.Encode();

        // Build PIN/UV auth param: authenticate(pinUvAuthToken, 32*0xff || 0x0D || subCommand || subCommandParams)
        // CTAP 2.1 (2022-06-21 errata), Section 6.11; parameters are part of the authenticated bytes.
        var subCommand = ConfigSubCommand.SetMinPinLength;
        var messageLength = 32 + 1 + 1 + subCommandParams.Length;
        var message = new byte[messageLength];
        message.AsSpan(0, 32).Fill(0xff);
        message[32] = CtapCommand.Config; // 0x0D
        message[33] = subCommand;
        subCommandParams.CopyTo(message.AsMemory(34));

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