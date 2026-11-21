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
/// or setMinPINLength as appropriate. Except for reading alwaysUv before toggling it,
/// the helper does not query or enforce capabilities or firmware thresholds. The SDK's firmware feature metadata is not qualified support
/// for every setting on every model. Read GetInfo again after success to inspect results.
/// </para>
/// <para>
/// The helper borrows the session, protocol and token memory; unlike ClientPin it does
/// not dispose the protocol. Keep both objects alive and token bytes unchanged through
/// awaited calls, then clear the caller-owned token with CryptographicOperations.ZeroMemory,
/// including on failure. No token acquisition, refresh, permission or expiry validation is
/// performed locally. This helper clears the temporary request and authentication buffers it owns.
/// Device changes persist beyond the helper's lifetime; failure or cancellation is not rollback,
/// and the helper performs no verification read or restoration.
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
        var request = BuildRequest(ConfigSubCommand.EnableEnterpriseAttestation);
        await SendConfigCommandAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the alwaysUv setting on the authenticator, toggling only when needed.
    /// </summary>
    /// <param name="enabled">The requested alwaysUv state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// <para>
    /// alwaysUv concerns credential creation and authentication, not every protocol
    /// operation or metadata query (CTAP 2.1, 2022-06-21 errata, Section 7.2.3). Its
    /// presence indicates support and its Boolean value is the current enabled state.
    /// </para>
    /// <para>
    /// Reads fresh GetInfo before each attempt and sends a toggle only if the current
    /// value differs. Read GetInfo again to verify success if needed. The device can refuse disabling with
    /// OperationDenied (Section 6.11.2); support does not guarantee both directions work.
    /// Enabling can also disable makeCredUvNotRqd. If a sent command's outcome is unknown,
    /// another call reads the current state again. No automatic retry or rollback is provided.
    /// </para>
    /// </remarks>
    /// <exception cref="CtapException">Thrown when the operation fails.</exception>
    /// <exception cref="NotSupportedException">GetInfo does not advertise alwaysUv.</exception>
    public async Task SetAlwaysUvAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        var info = await _session.GetInfoAsync(cancellationToken).ConfigureAwait(false);
        if (!info.Options.TryGetValue("alwaysUv", out bool current))
        {
            throw new NotSupportedException("The authenticator does not advertise alwaysUv support.");
        }

        if (current == enabled)
        {
            return;
        }

        var request = BuildRequest(ConfigSubCommand.ToggleAlwaysUv);
        await SendConfigCommandAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the minimum PIN length required by the authenticator.
    /// </summary>
    /// <param name="newMinPinLength">The new ClientPIN minimum in Unicode code points.
    /// Pass null to leave the minimum unchanged. This helper accepts 4-63; device policy and
    /// UTF-8 PIN-byte limits apply separately.</param>
    /// <param name="rpIds">Optional replacement mutable list of RP IDs allowed to receive
    /// minPinLength extension output. Null omits the field; an empty list is encoded as an empty
    /// array, not a revocation request. CTAP 2.1/2.3 §6.11.4 updates the stored list only when
    /// the array contains at least one string.</param>
    /// <param name="forceChangePin">If true, request a PIN change before PIN-token acquisition.
    /// Null omits the field. False is transmitted, but does not clear an already-set flag.</param>
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
    /// limits still apply. This helper does not change RP account policy
    /// or verify extension output; read fresh GetInfo after success to inspect minimum/force state.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="newMinPinLength"/> is less than 4 or greater than 63.
    /// </exception>
    /// <exception cref="CtapException">Thrown when the operation fails.</exception>
    public async Task SetMinPinLengthAsync(
        int? newMinPinLength = null,
        IReadOnlyList<string>? rpIds = null,
        bool? forceChangePin = null,
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

        var request = BuildSetMinPinLengthRequest(newMinPinLength, rpIds, forceChangePin);
        await SendConfigCommandAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendConfigCommandAsync(byte[] request, CancellationToken cancellationToken)
    {
        try
        {
            await _session.SendCborRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(request);
        }
    }

    private byte[] BuildRequest(byte subCommand, byte[]? parameters = null)
    {
        byte[]? tag = null;
        try
        {
            // CTAP 2.1 §6.11: authenticate(32*0xff || 0x0D || subCommand || parameters).
            Span<byte> transcript = parameters is null
                ? stackalloc byte[34]
                : new byte[34 + parameters.Length];
            try
            {
                transcript[..32].Fill(0xff);
                transcript[32] = CtapCommand.Config;
                transcript[33] = subCommand;
                if (parameters is not null) parameters.AsSpan().CopyTo(transcript[34..]);
                tag = _protocol.Authenticate(_pinUvAuthToken.Span, transcript);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(transcript);
            }

            using var request = CtapRequestBuilder.Create(CtapCommand.Config)
                .WithInt(1, subCommand)
                .WithInt(3, _protocol.Version)
                .WithBytes(4, tag.AsSpan());
            if (parameters is not null)
                request.WithEncodedValue(2, parameters);
            return request.Build();
        }
        finally
        {
            if (parameters is not null) CryptographicOperations.ZeroMemory(parameters);
            if (tag is not null) CryptographicOperations.ZeroMemory(tag);
        }
    }

    private byte[] BuildSetMinPinLengthRequest(
        int? newMinPinLength,
        IReadOnlyList<string>? rpIds,
        bool? forceChangePin)
    {
        // Build the subCommandParams map first
        var paramsWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);

        var paramsCount = newMinPinLength.HasValue ? 1 : 0;
        if (rpIds is not null) paramsCount++;
        if (forceChangePin.HasValue) paramsCount++;

        paramsWriter.WriteStartMap(paramsCount);

        // 0x01: newMinPINLength
        if (newMinPinLength is int length)
        {
            paramsWriter.WriteInt32(1);
            paramsWriter.WriteInt32(length);
        }

        // 0x02: minPinLengthRPIDs; an empty list is distinct from omission.
        if (rpIds is not null)
        {
            paramsWriter.WriteInt32(2);
            paramsWriter.WriteStartArray(rpIds.Count);
            foreach (var rpId in rpIds)
            {
                paramsWriter.WriteTextString(rpId);
            }
            paramsWriter.WriteEndArray();
        }

        // 0x03: forceChangePin; explicit false is distinct from omission.
        if (forceChangePin is bool force)
        {
            paramsWriter.WriteInt32(3);
            paramsWriter.WriteBoolean(force);
        }

        paramsWriter.WriteEndMap();
        return BuildRequest(ConfigSubCommand.SetMinPinLength, paramsWriter.Encode());
    }
}