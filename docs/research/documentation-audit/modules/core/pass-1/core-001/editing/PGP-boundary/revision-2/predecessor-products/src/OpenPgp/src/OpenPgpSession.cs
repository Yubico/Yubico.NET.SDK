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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.OpenPgp.Backend;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Provides access to the OpenPGP application on a YubiKey device.
/// </summary>
/// <remarks>
///     <para>
///         Use the static <see cref="CreateAsync" /> factory method to create and initialize a session.
///         Initialization selects the applet, configures the protocol and parses application data before return.
///     </para>
///     <para>
///         This is a partial class split across multiple files by operation domain:
///         <c>OpenPgpSession.Pin.cs</c>, <c>OpenPgpSession.Keys.cs</c>, <c>OpenPgpSession.Certificates.cs</c>,
///         <c>OpenPgpSession.Config.cs</c>, <c>OpenPgpSession.Crypto.cs</c>, <c>OpenPgpSession.Reset.cs</c>.
///     </para>
/// </remarks>
public sealed partial class OpenPgpSession : ApplicationSession, IOpenPgpSession
{
    // ── Feature Constants ─────────────────────────────────────────────

    /// <summary>Current SDK version threshold for application reset (1.0.6).</summary>
    public static readonly Feature FeatureReset = new("OpenPGP Reset", 1, 0, 6);

    /// <summary>Current SDK UIF threshold (4.2.0); individual policies/slots need additional card support.</summary>
    public static readonly Feature FeatureUif = new("OpenPGP UIF", 4, 2, 0);

    /// <summary>Current SDK EC generation/import threshold (5.2.0); consult advertised algorithms.</summary>
    public static readonly Feature FeatureEc = new("OpenPGP EC Keys", 5, 2, 0);

    /// <summary>Current SDK threshold for ordinary per-slot certificate access (5.2.0).</summary>
    public static readonly Feature FeatureCertificates = new("OpenPGP Certificates", 5, 2, 0);

    /// <summary>Current SDK target-key attestation threshold (5.2.0).</summary>
    public static readonly Feature FeatureAttestation = new("OpenPGP Key Attestation", 5, 2, 0);

    /// <summary>Current SDK algorithm-information query threshold (5.2.0).</summary>
    public static readonly Feature FeatureAlgorithmInfo = new("OpenPGP Algorithm Info", 5, 2, 0);

    /// <summary>SDK threshold for corrected SELECT DATA framing (5.4.4); earlier paths add a length prefix.</summary>
    public static readonly Feature FeatureSelectDataFix = new("OpenPGP SELECT_DATA Fix", 5, 4, 4);

    /// <summary>Current SDK User PIN unverification threshold (5.6.0).</summary>
    public static readonly Feature FeatureUnverify = new("OpenPGP Unverify PIN", 5, 6, 0);

    // ── Fields ────────────────────────────────────────────────────────

    private readonly ILogger _logger;
    private readonly Dictionary<KeyRef, Uif> _updatedUifs = [];
    private IOpenPgpBackend? _backend;
    private ApplicationRelatedData _appData = null!;
    private Kdf? _kdf;

    // ── Constructor (private — use CreateAsync) ───────────────────────

    private OpenPgpSession(
        ISmartCardConnection connection,
        IUserPresencePrompt? userPresencePrompt = null)
        : base(connection, userPresencePrompt)
    {
        _logger = Logger;
    }

    // ── Factory ───────────────────────────────────────────────────────

    /// <summary>
    ///     Creates and initializes an OpenPGP session on the given SmartCard connection.
    /// </summary>
    /// <param name="connection">An open SmartCard connection to a YubiKey.</param>
    /// <param name="options">Optional cross-cutting session creation settings.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An initialized <see cref="OpenPgpSession" />.</returns>
    /// <remarks>
    ///     The caller retains the connection and disposes it after session teardown. Use the IYubiKey extension
    ///     to acquire an owned connection automatically. Parsed mandatory algorithm fields must be understood
    ///     for initialization to succeed. Version checks use session configuration; preserve applet/device
    ///     version provenance when diagnosing development-device feature checks.
    ///     The session binds to the connection before device I/O; failed initialization disposes and detaches
    ///     the session while leaving this caller-owned connection open. Selection and version discovery precede
    ///     optional SCP setup; application-data parsing uses the configured protocol before readiness is published.
    ///     FirmwareVersionOverride selects the effective framing/feature version without skipping the version probe
    ///     or consulting physical-device metadata. Keep borrowed SCP inputs alive through factory completion.
    /// </remarks>
    public static Task<OpenPgpSession> CreateAsync(
        ISmartCardConnection connection,
        SessionCreationOptions? options = null,
        CancellationToken cancellationToken = default) =>
        ConstructInitializedAsync(
            connection,
            options,
            () => new OpenPgpSession(connection, options?.UserPresencePrompt),
            (session, token) => session.InitializeAsync(
                options?.ProtocolConfiguration, options?.ScpKeyParameters, options?.FirmwareVersionOverride, token),
            cancellationToken);

    // ── Initialization ────────────────────────────────────────────────

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        ScpKeyParameters? scpKeyParams,
        FirmwareVersion? firmwareVersionOverride,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (IsInitialized)
            return;

        var protocol = (ISmartCardProtocol)await PrepareProtocolAsync(scpKeyParams, cancellationToken)
            .ConfigureAwait(false);
        IOpenPgpBackend backend = new OpenPgpBackend(protocol);

        var initialization = await backend.ReadInitialStateAsync(cancellationToken).ConfigureAwait(false);
        var firmwareVersion = initialization.FirmwareVersion;

        var effectiveProtocol = (ISmartCardProtocol)await ConfigureProtocolAsync(
                protocol,
                firmwareVersionOverride ?? firmwareVersion,
                configuration,
                scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        if (!ReferenceEquals(protocol, effectiveProtocol))
        {
            backend = new OpenPgpBackend(effectiveProtocol);
        }

        _backend = backend;

        // Cache capabilities, algorithms and UIF. F9/KDF is loaded separately on first use.
        _appData = await GetApplicationRelatedDataCoreAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "OpenPGP session initialized (firmware {Version}, serial {Serial})",
            firmwareVersion, _appData.Aid.Serial);
    }

    // ── Data Access ───────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ApplicationRelatedData> GetApplicationRelatedDataAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _appData = await GetApplicationRelatedDataCoreAsync(cancellationToken)
            .ConfigureAwait(false);
        _updatedUifs.Clear();
        return _appData;
    }

    private async Task<ApplicationRelatedData> GetApplicationRelatedDataCoreAsync(
        CancellationToken cancellationToken)
    {
        var data = await GetDataCoreAsync(DataObject.ApplicationRelatedData, cancellationToken)
            .ConfigureAwait(false);
        return ApplicationRelatedData.Parse(data.Span);
    }

    /// <inheritdoc />
    public Task<ReadOnlyMemory<byte>> GetDataAsync(
        DataObject dataObject,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return GetDataCoreAsync(dataObject, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PutDataAsync(
        DataObject dataObject,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var tag = (int)dataObject;
        var command = new ApduCommand
        {
            Cla = 0x00,
            Ins = (byte)Ins.PutData,
            P1 = (byte)(tag >> 8),
            P2 = (byte)(tag & 0xFF),
            Data = data,
        };

        _logger.LogDebug("PUT DATA for DO 0x{Tag:X4}", tag);
        await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> GetChallengeAsync(
        int length,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

        var command = new ApduCommand
        {
            Cla = 0x00,
            Ins = (byte)Ins.GetChallenge,
            P1 = 0x00,
            P2 = 0x00,
            Le = length,
        };

        _logger.LogDebug("GET CHALLENGE ({Length} bytes)", length);
        var response = await TransmitWithResponseAsync(command, cancellationToken)
            .ConfigureAwait(false);
        return response.Data;
    }

    /// <inheritdoc />
    public async Task<int> GetSignatureCounterAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var data = await GetDataCoreAsync(DataObject.SecuritySupportTemplate, cancellationToken)
            .ConfigureAwait(false);
        var sst = SecuritySupportTemplate.Parse(data.Span);
        return sst.SignatureCounter;
    }

    /// <inheritdoc />
    public async Task<PwStatus> GetPinStatusAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var data = await GetDataCoreAsync(DataObject.PwStatusBytes, cancellationToken)
            .ConfigureAwait(false);
        return PwStatus.Parse(data.Span);
    }

    // ── Internal Helpers ──────────────────────────────────────────────

    private async Task<ReadOnlyMemory<byte>> GetDataCoreAsync(
        DataObject dataObject,
        CancellationToken cancellationToken)
    {
        var tag = (int)dataObject;
        var command = new ApduCommand
        {
            Cla = 0x00,
            Ins = (byte)Ins.GetData,
            P1 = (byte)(tag >> 8),
            P2 = (byte)(tag & 0xFF),
        };

        _logger.LogDebug("GET DATA for DO 0x{Tag:X4}", tag);
        var response = await TransmitWithResponseAsync(command, cancellationToken)
            .ConfigureAwait(false);
        return response.Data;
    }

    /// <summary>
    ///     Transmits an APDU command and throws on non-success status.
    /// </summary>
    private async Task TransmitAsync(
        ApduCommand command,
        CancellationToken cancellationToken)
    {
        EnsureInitializedBackend();
        await _backend!.SendAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Transmits an APDU command and returns the response. Throws on non-success status.
    /// </summary>
    private async Task<ApduResponse> TransmitWithResponseAsync(
        ApduCommand command,
        CancellationToken cancellationToken)
    {
        EnsureInitializedBackend();
        return await _backend!.SendAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Transmits an APDU command without throwing on error, returning the raw response.
    /// </summary>
    private async Task<ApduResponse> TransmitNoThrowAsync(
        ApduCommand command,
        CancellationToken cancellationToken)
    {
        EnsureInitializedBackend();
        return await _backend!.SendAsync(
                command, throwOnError: false, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private void EnsureInitializedBackend()
    {
        ThrowIfDisposed();
        if (_backend is null)
            throw new InvalidOperationException("Session is not initialized.");
    }

    private async Task<UserPresenceContext?> GetUserPresenceContextAsync(
        KeyRef keyRef,
        string operation,
        CancellationToken cancellationToken)
    {
        if (!IsUserPresenceNotificationEnabled || !IsSupported(FeatureUif))
            return null;

        UserPresenceBasis basis;
        try
        {
            Uif? cachedUif = _updatedUifs.TryGetValue(keyRef, out Uif updatedUif)
                ? updatedUif
                : keyRef switch
                {
                    KeyRef.Sig => _appData.Discretionary.UifSig,
                    KeyRef.Dec => _appData.Discretionary.UifDec,
                    KeyRef.Aut => _appData.Discretionary.UifAut,
                    KeyRef.Att => _appData.Discretionary.UifAtt,
                    _ => null
                };
            Uif uif = cachedUif ?? await GetUifAsync(keyRef, cancellationToken).ConfigureAwait(false);
            if (uif == Uif.Off)
                return null;

            basis = uif switch
            {
                Uif.On or Uif.Fixed => UserPresenceBasis.PolicyRequires,
                Uif.Cached or Uif.CachedFixed => UserPresenceBasis.PolicyMayRequire,
                _ => UserPresenceBasis.PolicyMayRequire
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // UIF is advisory metadata. A failed read must not replace the cryptographic operation.
            _logger.LogDebug(ex, "Could not read UIF for {Slot}; user presence may be required", keyRef);
            basis = UserPresenceBasis.PolicyMayRequire;
        }

        return new UserPresenceContext
        {
            Application = UserPresenceApplications.OpenPgp,
            Operation = operation,
            Scope = keyRef.ToString(),
            Basis = basis
        };
    }

    /// <summary>
    ///     Loads and caches the KDF configuration from the card.
    /// </summary>
    private async Task<Kdf> GetOrLoadKdfAsync(CancellationToken cancellationToken)
    {
        if (_kdf is not null)
            return _kdf;

        try
        {
            var data = await GetDataCoreAsync(DataObject.Kdf, cancellationToken)
                .ConfigureAwait(false);
            _kdf = Kdf.Parse(data.Span);
        }
        catch (ApduException)
        {
            // Any F9 APDU error currently falls back to KdfNone, including errors beyond absence.
            _kdf = new KdfNone();
        }

        return _kdf;
    }

    // ── Disposal ──────────────────────────────────────────────────────

    // Synchronous disposal reaches this cleanup before base protocol drain. Asynchronous disposal awaits
    // Core protocol/owned-connection teardown first, then invokes this override in finally, even on failure.
    // Neither path waits for an entire multi-exchange public workflow: callers must await it before disposal.
    protected override void Dispose(bool disposing)
    {
        Kdf? kdf = null;
        try
        {
            if (disposing)
            {
                kdf = _kdf;
                _kdf = null;
                kdf?.Dispose();
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }
}
