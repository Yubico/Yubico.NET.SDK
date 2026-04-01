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
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Provides access to the OpenPGP application on a YubiKey device.
/// </summary>
/// <remarks>
///     <para>
///         Use the static <see cref="CreateAsync" /> factory method to create and initialize a session.
///         The constructor is private to enforce two-phase initialization (construct + SELECT + version detect).
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

    /// <summary>Factory reset support.</summary>
    public static readonly Feature FeatureReset = new("OpenPGP Reset", 1, 0, 6);

    /// <summary>User Interaction Flag (touch policy) support.</summary>
    public static readonly Feature FeatureUif = new("OpenPGP UIF", 4, 2, 0);

    /// <summary>Elliptic curve key support.</summary>
    public static readonly Feature FeatureEc = new("OpenPGP EC Keys", 5, 2, 0);

    /// <summary>Per-slot certificate storage.</summary>
    public static readonly Feature FeatureCertificates = new("OpenPGP Certificates", 5, 2, 0);

    /// <summary>Key attestation support.</summary>
    public static readonly Feature FeatureAttestation = new("OpenPGP Key Attestation", 5, 2, 0);

    /// <summary>Algorithm information query.</summary>
    public static readonly Feature FeatureAlgorithmInfo = new("OpenPGP Algorithm Info", 5, 2, 0);

    /// <summary>SELECT_DATA fix for multi-certificate access.</summary>
    public static readonly Feature FeatureSelectDataFix = new("OpenPGP SELECT_DATA Fix", 5, 4, 4);

    /// <summary>PIN unverify support.</summary>
    public static readonly Feature FeatureUnverify = new("OpenPGP Unverify PIN", 5, 6, 0);

    // ── Fields ────────────────────────────────────────────────────────

    private readonly ISmartCardConnection _connection;
    private readonly ILogger _logger;
    private ISmartCardProtocol? _protocol;
    private ApplicationRelatedData _appData = null!;
    private Kdf? _kdf;

    // ── Constructor (private — use CreateAsync) ───────────────────────

    private OpenPgpSession(ISmartCardConnection connection)
    {
        _connection = connection;
        _logger = Logger;
    }

    // ── Factory ───────────────────────────────────────────────────────

    /// <summary>
    ///     Creates and initializes an OpenPGP session on the given SmartCard connection.
    /// </summary>
    /// <param name="connection">An open SmartCard connection to a YubiKey.</param>
    /// <param name="configuration">Optional protocol configuration overrides.</param>
    /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An initialized <see cref="OpenPgpSession" />.</returns>
    public static async Task<OpenPgpSession> CreateAsync(
        ISmartCardConnection connection,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        var session = new OpenPgpSession(connection);
        await session.InitializeAsync(configuration, scpKeyParams, cancellationToken)
            .ConfigureAwait(false);
        return session;
    }

    // ── Initialization ────────────────────────────────────────────────

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        ScpKeyParameters? scpKeyParams,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return;

        var smartCardProtocol = PcscProtocolFactory<ISmartCardConnection>
            .Create()
            .Create(_connection);

        // SELECT OpenPGP AID — handle terminated state (0x6285/0x6985)
        try
        {
            await smartCardProtocol
                .SelectAsync(ApplicationIds.OpenPgp, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ApduException ex) when (ex.SW is SWConstants.FileTerminated or SWConstants.ConditionsNotSatisfied)
        {
            _logger.LogDebug("OpenPGP applet in terminated state, sending ACTIVATE");
            await smartCardProtocol.TransmitAndReceiveAsync(
                    new ApduCommand(0x00, (int)Ins.Activate, 0x00, 0x00),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await smartCardProtocol
                .SelectAsync(ApplicationIds.OpenPgp, cancellationToken)
                .ConfigureAwait(false);
        }

        // GET VERSION — BCD decode; fall back to 1.0.0 for very old keys
        var firmwareVersion = await GetVersionAsync(smartCardProtocol, cancellationToken)
            .ConfigureAwait(false);

        await InitializeCoreAsync(
                smartCardProtocol,
                firmwareVersion,
                configuration,
                scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        _protocol = Protocol as ISmartCardProtocol
                    ?? throw new InvalidOperationException("Protocol is not an ISmartCardProtocol.");

        // Cache ApplicationRelatedData for feature detection and KDF state
        _appData = await GetApplicationRelatedDataCoreAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "OpenPGP session initialized (firmware {Version}, serial {Serial})",
            firmwareVersion, _appData.Aid.Serial);
    }

    private static async Task<FirmwareVersion> GetVersionAsync(
        ISmartCardProtocol protocol,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await protocol.TransmitAndReceiveAsync(
                    new ApduCommand(0x00, (int)Ins.GetVersion, 0x00, 0x00),
                    throwOnError: false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (response.IsOK() && response.Data.Length >= 3)
            {
                var data = response.Data.Span;
                return new FirmwareVersion(
                    (byte)BcdHelper.DecodeByte(data[0]),
                    (byte)BcdHelper.DecodeByte(data[1]),
                    (byte)BcdHelper.DecodeByte(data[2]));
            }
        }
        catch (ApduException)
        {
            // CONDITIONS_NOT_SATISFIED on very old firmware
        }

        return new FirmwareVersion(1, 0, 0);
    }

    // ── Data Access ───────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ApplicationRelatedData> GetApplicationRelatedDataAsync(
        CancellationToken cancellationToken = default)
    {
        _appData = await GetApplicationRelatedDataCoreAsync(cancellationToken)
            .ConfigureAwait(false);
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
        CancellationToken cancellationToken = default) =>
        GetDataCoreAsync(dataObject, cancellationToken);

    /// <inheritdoc />
    public async Task PutDataAsync(
        DataObject dataObject,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
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
        var data = await GetDataCoreAsync(DataObject.SecuritySupportTemplate, cancellationToken)
            .ConfigureAwait(false);
        var sst = SecuritySupportTemplate.Parse(data.Span);
        return sst.SignatureCounter;
    }

    /// <inheritdoc />
    public async Task<PwStatus> GetPinStatusAsync(
        CancellationToken cancellationToken = default)
    {
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
        EnsureInitializedProtocol();
        await _protocol!.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Transmits an APDU command and returns the response. Throws on non-success status.
    /// </summary>
    private async Task<ApduResponse> TransmitWithResponseAsync(
        ApduCommand command,
        CancellationToken cancellationToken)
    {
        EnsureInitializedProtocol();
        return await _protocol!.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Transmits an APDU command without throwing on error, returning the raw response.
    /// </summary>
    private async Task<ApduResponse> TransmitNoThrowAsync(
        ApduCommand command,
        CancellationToken cancellationToken)
    {
        EnsureInitializedProtocol();
        return await _protocol!.TransmitAndReceiveAsync(
                command, throwOnError: false, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private void EnsureInitializedProtocol()
    {
        ThrowIfDisposed();
        if (_protocol is null)
            throw new InvalidOperationException("Session is not initialized.");
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
            // KDF not supported or not configured
            _kdf = new KdfNone();
        }

        return _kdf;
    }

    // ── Disposal ──────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _protocol = null;
        _kdf = null;
        base.Dispose(disposing);
    }
}
