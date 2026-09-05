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
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management.Backend;

namespace Yubico.YubiKit.Management;

public sealed class ManagementSession : ApplicationSession, IManagementSession
{
    private static readonly Feature FeatureDeviceInfo =
        new("Device Info", 4, 1, 0);

    private static readonly Feature FeatureSetConfig =
        new("Set Config", 5, 0, 0);

    private static readonly Feature FeatureDeviceReset =
        new("Device Reset", 5, 6, 0);

    private readonly ILogger _logger;
    private readonly ScpKeyParameters? _scpKeyParams;

    private IProtocol _protocol = null!;
    private IManagementBackend _backend = null!;

    private FirmwareVersion? _version;

    private ManagementSession(
        IConnection connection,
        ScpKeyParameters? scpKeyParams = null)
        : base(EnsureSupportedConnection(connection))
    {
        ArgumentNullException.ThrowIfNull(connection);

        _scpKeyParams = scpKeyParams;
        _logger = Logger;

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

    public static async Task<ManagementSession> CreateAsync(
        IConnection connection,
        SessionCreationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var configuration = options?.ProtocolConfiguration;
        var scpKeyParams = options?.ScpKeyParameters;
        var firmwareVersionOverride = options?.FirmwareVersionOverride;

        ValidatePreferredConnectionType(connection, options);

        // A session that fails to initialize must not keep its claim on the connection: the connection
        // outlives it, and the next session over it would otherwise be refused forever.
        var session = Construct(connection, () => new ManagementSession(connection, scpKeyParams));
        try
        {
            await session.InitializeAsync(configuration, firmwareVersionOverride, cancellationToken).ConfigureAwait(false);
            return session;
        }
        catch
        {
            session.DisposeAfterInitializationFailure();
            throw;
        }
    }

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        FirmwareVersion? firmwareVersionOverride,
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        var protocol = ProtocolFactory.Create(Connection);
        Protocol = protocol;
        var backend = CreateBackend(protocol);
        _protocol = protocol;
        _backend = backend;

        _version = await ResolveFirmwareVersionAsync(backend, protocol, cancellationToken).ConfigureAwait(false);
        var effectiveFirmwareVersion = firmwareVersionOverride ?? _version;

        var effectiveProtocol = await InitializeProtocolAsync(
                protocol,
                effectiveFirmwareVersion,
                configuration,
                _scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        if (!ReferenceEquals(protocol, effectiveProtocol))
        {
            backend = CreateBackend(effectiveProtocol);
        }

        _protocol = effectiveProtocol;
        _backend = backend;

        _logger.LogDebug("Management session initialized with protocol {ProtocolType}", _protocol.GetType().Name);
    }

    public Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return DeviceInfoReader.ReadAsync(_protocol, _version, cancellationToken);
    }

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
                if (!configBytes.IsEmpty)
                    CryptographicOperations.ZeroMemory(configBytes.Span);
            }
        }
    }

    public Task ResetDeviceAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureDeviceReset);
        return _backend.DeviceResetAsync(cancellationToken).AsTask();
    }

    private async Task<FirmwareVersion> ResolveFirmwareVersionAsync(
        IManagementBackend backend,
        IProtocol protocol,
        CancellationToken cancellationToken)
    {
        var probedVersion = await backend.InitializeAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var deviceInfo = await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
            return deviceInfo.FirmwareVersion;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogDebug(e,
                "Could not get version from DeviceInfo, fallback to versionHeader in Management.Select");
        }

        return probedVersion
            ?? throw new InvalidOperationException("Could not determine firmware version from device");
    }

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