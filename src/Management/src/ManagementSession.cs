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
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management.Backend;

namespace Yubico.YubiKit.Management;

/// <summary>Provides device-information reads and device-level configuration over SmartCard, FIDO HID, or OTP HID.</summary>
/// <remarks>
///     Create an initialized session with <see cref="CreateAsync" /> for a connection you own, or use
///     <c>IYubiKey.CreateManagementSessionAsync</c> to let the session own the connection it opens.
///     Dispose the session when finished and await each operation before starting another.
///     See <see cref="IManagementSession" /> for sequencing and transport-specific cancellation boundaries.
/// </remarks>
public sealed class ManagementSession : ApplicationSession, IManagementSession
{
    private static readonly Feature FeatureDeviceInfo =
        new("Device Info", 4, 1, 0);

    private static readonly Feature FeatureSetConfig =
        new("Set Config", 5, 0, 0);

    private static readonly Feature FeatureNfcRestriction =
        new("NFC Restriction", 5, 7, 0);

    private static readonly Feature FeatureDeviceReset =
        new("Device Reset", 5, 6, 0);

    private readonly ILogger _logger;

    private IManagementBackend _backend = null!;

    private FirmwareVersion? _version;
    private readonly ProtocolConfiguration? _configuration;
    private readonly FirmwareVersion? _firmwareVersionOverride;
    private readonly ScpKeyParameters? _scpKeyParameters;
    private bool _endingAfterRefreshFailure;

    private ManagementSession(IConnection connection, SessionCreationOptions? options)
        : base(EnsureSupportedConnection(connection))
    {
        ArgumentNullException.ThrowIfNull(connection);

        _logger = Logger;
        _configuration = options?.ProtocolConfiguration;
        _firmwareVersionOverride = options?.FirmwareVersionOverride;
        _scpKeyParameters = options?.ScpKeyParameters;
    }

    private static IConnection EnsureSupportedConnection(IConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection is ISmartCardConnection or IFidoHidConnection or IOtpHidConnection
            ? connection
            : throw new NotSupportedException(
                $"The connection type {connection.GetType().Name} is not supported by ManagementSession. " +
                "Supported types: ISmartCardConnection, IFidoHidConnection, IOtpHidConnection.");
    }

    /// <summary>Creates and initializes a Management session that borrows an open connection.</summary>
    /// <remarks>
    ///     The caller must dispose the connection on both success and failure. Disposing this session
    ///     releases its protocol and session guard, but does not dispose the borrowed connection.
    ///     Only one live session may use a connection; do not perform raw I/O alongside the session.
    ///     Initialization prepares the protocol, probes the firmware, and attempts a device-information
    ///     read before establishing optional SCP. Non-cancellation failure of that read falls back to
    ///     the transport's firmware probe. If neither source yields a version, firmware gates are optimistic.
    ///     Mandatory transport setup failures and cancellation still propagate. Initialization metadata is not all SCP-protected.
    ///     A firmware override configures the protocol and feature checks; it neither skips discovery
    ///     nor replaces the hardware metadata returned by later reads.
    ///     Creation settings are not retained as an options object. SCP parameters remain caller-owned
    ///     and borrowed for initialization and any post-reset refresh. Management does not retain, invoke, or dispose the optional
    ///     user-presence prompt. Communication, SCP, and response errors propagate from their layers.
    ///     Successful SCP setup sets IsAuthenticated but performs no subsequent protected Management
    ///     command during creation. That flag is setup history, not channel-health or configuration-lock
    ///     authorization. Await complete public operations before teardown; native completion and release
    ///     remain responsibilities of the selected connection implementation.
    /// </remarks>
    /// <param name="connection">An open SmartCard, FIDO HID, or OTP HID connection, retained by the caller.</param>
    /// <param name="options">
    ///     Optional protocol, firmware-override, and SCP settings. A preferred connection type must match
    ///     the supplied connection: it validates the choice, rather than selecting another transport.
    ///     SCP requires SmartCard and is rejected on HID before initialization I/O.
    /// </param>
    /// <param name="cancellationToken">A token requesting cancellation of initialization at transport-supported boundaries.</param>
    /// <returns>An initialized session that the caller must dispose, preferably with <c>await using</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection" /> is null.</exception>
    /// <exception cref="ArgumentException">The preferred connection type is invalid or does not match the connection.</exception>
    /// <exception cref="NotSupportedException">The connection type is unsupported, or SCP is requested over HID.</exception>
    /// <exception cref="ConnectionInUseException">The connection already has a live session.</exception>
    public static Task<ManagementSession> CreateAsync(
        IConnection connection,
        SessionCreationOptions? options = null,
        CancellationToken cancellationToken = default) =>
        CreateWithCachedDeviceInfoAsync(connection, options, null, cancellationToken);

    internal static Task<ManagementSession> CreateWithCachedDeviceInfoAsync(
        IConnection connection, SessionCreationOptions? options, DeviceInfo? cachedDeviceInfo,
        CancellationToken cancellationToken) =>
        ConstructInitializedAsync(
            connection,
            options,
            () => new ManagementSession(connection, options),
            (session, token) => session.InitializeAsync(token, cachedDeviceInfo),
            cancellationToken);

    private async Task InitializeAsync(
        CancellationToken cancellationToken, DeviceInfo? cachedDeviceInfo = null, bool afterReset = false)
    {
        if (IsInitialized)
            return;

        var protocol = await PrepareProtocolAsync(_scpKeyParameters, cancellationToken).ConfigureAwait(false);
        var fallbackVersion = protocol switch
        {
            ISmartCardProtocol smartCard =>
                await SmartCardBackend.SelectApplicationAsync(smartCard, cancellationToken).ConfigureAwait(false),
            IOtpHidProtocol otp =>
                await OtpBackend.ReadStatusVersionAsync(otp, cancellationToken).ConfigureAwait(false),
            IFidoHidProtocol fido => fido.FirmwareVersion,
            _ => throw new NotSupportedException("Unsupported Management protocol.")
        };

        var liveInfo = afterReset
            ? await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false)
            : await TryReadInitialDeviceInfoAsync(protocol, cancellationToken).ConfigureAwait(false);
        _version = liveInfo?.FirmwareVersion ?? fallbackVersion;
        var featureFirmware = GetFeatureFirmware(cachedDeviceInfo ?? liveInfo, fallbackVersion);
        var effectiveProtocol = await ConfigureProtocolAsync(
                protocol,
                GetTransportFirmware(cachedDeviceInfo),
                _configuration,
                _scpKeyParameters,
                cancellationToken,
                scpEligibilityFirmwareVersion: featureFirmware)
            .ConfigureAwait(false);

        FirmwareVersion = featureFirmware;
        _backend = CreateBackend(effectiveProtocol);

        _logger.LogDebug("Management session initialized with protocol {ProtocolType}", effectiveProtocol.GetType().Name);
    }

    /// <inheritdoc cref="IManagementSession.GetDeviceInfoAsync" />
    public Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureDeviceInfo);
        var protocol = Protocol ?? throw new InvalidOperationException("Management session is not initialized.");
        // Query current fields while retaining initialization's firmware default; non-final qualifier
        // precedence belongs to DeviceInfo.CreateFromTlvs. This read does not publish discovery metadata.
        return DeviceInfoReader.ReadAsync(protocol, _version, cancellationToken);
    }

    /// <inheritdoc cref="IManagementSession.SetDeviceConfigAsync" />
    public Task SetDeviceConfigAsync(
        DeviceConfig config,
        SetDeviceConfigOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Before the feature gate: a disposed session should say so, not report the firmware verdict of a
        // session that no longer exists.
        ThrowIfDisposed();
        EnsureSupports(FeatureSetConfig);
        ArgumentNullException.ThrowIfNull(config);
        if (config.NfcRestricted == true)
            EnsureSupports(FeatureNfcRestriction);

        bool reboot = options?.Reboot ?? false;
        ReadOnlyMemory<byte>? currentLockCode = options?.CurrentLockCode;
        ReadOnlyMemory<byte>? newLockCode = options?.NewLockCode;

        const int lockCodeLength = 16;
        if (currentLockCode is { } current && current.Length != lockCodeLength)
            throw new ArgumentException("options.CurrentLockCode must be 16 bytes", nameof(options));

        if (newLockCode is { } replacement && replacement.Length != lockCodeLength)
            throw new ArgumentException("options.NewLockCode must be 16 bytes", nameof(options));

        var configBytes = config.GetBytes(options);
        return WriteConfigAndZeroAsync(_backend, configBytes, cancellationToken);

        static async Task WriteConfigAndZeroAsync(
            IManagementBackend backend,
            Memory<byte> configBytes,
            CancellationToken cancellationToken)
        {
            try
            {
                await backend.WriteConfigAsync(configBytes, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(configBytes.Span);
            }
        }
    }

    /// <inheritdoc cref="IManagementSession.ResetDeviceAsync" />
    public async Task ResetDeviceAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureDeviceReset);
        if (_backend is not SmartCardBackend smartCard)
            throw new NotSupportedException("Device reset is only available over SmartCard (CCID) connections.");

        await smartCard.DeviceResetAsync(cancellationToken).ConfigureAwait(false);

        IsInitialized = false;
        IsAuthenticated = false;
        FirmwareVersion = new FirmwareVersion();
        _version = null;

        try
        {
            var oldProtocol = Protocol;
            Protocol = null;
            if (oldProtocol is IAsyncDisposable asyncProtocol)
                await asyncProtocol.DisposeAsync().ConfigureAwait(false);
            else
                oldProtocol?.Dispose();

            // After confirmation, caller cancellation cannot undo the reset or strand the refresh.
            await InitializeSessionAsync(token => InitializeAsync(token, afterReset: true), CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _endingAfterRefreshFailure = true;
            await DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException("Management reset was confirmed, but session refresh failed and the session ended.", exception);
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
        catch (Exception exception) when (_endingAfterRefreshFailure)
        {
            try
            {
                _logger.LogDebug(exception, "Management cleanup after a failed reset refresh also failed.");
            }
            catch
            {
                // Keep the disposal gate from replaying a secondary failure over the reset refresh error.
            }
        }
    }

    private async Task<DeviceInfo?> TryReadInitialDeviceInfoAsync(
        IProtocol protocol,
        CancellationToken cancellationToken)
    {
        try
        {
            return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogDebug(e, "Could not read Management initialization metadata.");
            return null;
        }
    }

    // APDU framing needs the numeric version, not the sentinel used for optimistic feature gates.
    private FirmwareVersion GetTransportFirmware(DeviceInfo? cachedDeviceInfo) =>
        _firmwareVersionOverride ??
        (_version is { Major: > 0 } ? _version : cachedDeviceInfo?.FirmwareVersion ?? _version) ??
        new FirmwareVersion();

    private FirmwareVersion GetFeatureFirmware(DeviceInfo? info, FirmwareVersion? fallback) =>
        _firmwareVersionOverride ??
        (info?.VersionQualifier is { Type: not VersionQualifierType.Final }
            ? new FirmwareVersion()
            : info?.FirmwareVersion ?? fallback ?? new FirmwareVersion());

    private static IManagementBackend CreateBackend(IProtocol protocol) =>
        protocol switch
        {
            ISmartCardProtocol smartCard => new SmartCardBackend(smartCard),
            IFidoHidProtocol fidoHid => new FidoHidBackend(fidoHid),
            IOtpHidProtocol otpHid => new OtpBackend(otpHid),
            _ => throw new NotSupportedException(
                $"The protocol type {protocol.GetType().Name} is not supported by ManagementSession.")
        };

}