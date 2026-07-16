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
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
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

    private YubiKeyProtocol _protocol;
    private IManagementBackend _backend;

    private FirmwareVersion? _version;

    private ManagementSession(
        IConnection connection,
        ScpKeyParameters? scpKeyParams = null)
    {
        _scpKeyParams = scpKeyParams;
        _logger = Logger;

        _protocol = YubiKeyProtocol.Create(connection);
        _backend = CreateBackend(_protocol);

        Protocol = _protocol.Inner;
    }

    public static async Task<ManagementSession> CreateAsync(
        IConnection connection,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        var session = new ManagementSession(connection, scpKeyParams);
        await session.InitializeAsync(configuration, cancellationToken).ConfigureAwait(false);
        return session;
    }

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        _version = await ResolveFirmwareVersionAsync(cancellationToken).ConfigureAwait(false);

        _protocol = await InitializeCoreAsync(
                _protocol,
                _version,
                configuration,
                _scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        if (IsAuthenticated)
        {
            // Recreate backend with SCP-wrapped protocol
            _backend = CreateBackend(_protocol);
        }

        _logger.LogDebug("Management session initialized with protocol {ProtocolType}", _protocol.Inner.GetType().Name);
    }

    public Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default) =>
        DeviceInfoReader.ReadAsync(_protocol.Inner, _version, cancellationToken);

    public Task SetDeviceConfigAsync(
        DeviceConfig config,
        bool reboot,
        byte[]? currentLockCode = null,
        byte[]? newLockCode = null,
        CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureSetConfig);
        ArgumentNullException.ThrowIfNull(config);

        const int lockCodeLength = 16;
        if (currentLockCode is { Length: not lockCodeLength })
            throw new ArgumentException("Current lock code must be 16 bytes", nameof(currentLockCode));

        if (newLockCode is { Length: not lockCodeLength })
            throw new ArgumentException("New lock code must be 16 bytes", nameof(newLockCode));

        var configBytes = config.GetBytes(reboot, currentLockCode, newLockCode);
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
        EnsureSupports(FeatureDeviceReset);
        return _backend.DeviceResetAsync(cancellationToken).AsTask();
    }

    private async Task<FirmwareVersion> ResolveFirmwareVersionAsync(CancellationToken cancellationToken)
    {
        var probedVersion = await _backend.InitializeAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var deviceInfo = await GetDeviceInfoAsync(cancellationToken).ConfigureAwait(false);
            return deviceInfo.FirmwareVersion;
        }
        catch (Exception e)
        {
            _logger.LogDebug(e,
                "Could not get version from DeviceInfo, fallback to versionHeader in Management.Select");
        }

        return probedVersion
            ?? throw new InvalidOperationException("Could not determine firmware version from device");
    }

    private static IManagementBackend CreateBackend(YubiKeyProtocol protocol) =>
        protocol switch
        {
            YubiKeyProtocol.SmartCard sc => new SmartCardBackend(sc.Protocol),
            YubiKeyProtocol.FidoHid fido => new FidoHidBackend(fido.Protocol),
            YubiKeyProtocol.OtpHid otp => new OtpBackend(otp.Protocol),
            _ => throw new NotSupportedException(
                $"The protocol type {protocol.GetType().Name} is not supported by ManagementSession.")
        };

}
