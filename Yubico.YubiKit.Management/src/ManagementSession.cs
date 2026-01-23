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
using System.Text;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Hid.Otp;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Management;

public sealed class ManagementSession : ApplicationSession, IManagementSession, IAsyncDisposable
{
    private const int TagMoreDeviceInfo = 0x10;

    private static readonly Feature FeatureDeviceInfo =
        new("Device Info", 4, 1, 0);

    private static readonly Feature FeatureSetConfig =
        new("Set Config", 5, 0, 0);

    private static readonly Feature FeatureDeviceReset =
        new("Device Reset", 5, 6, 0);

    private readonly ILogger _logger;
    private readonly ScpKeyParameters? _scpKeyParams;

    private IProtocol _protocol;
    private IManagementBackend _backend;

    private FirmwareVersion? _version;

    /// <summary>
    /// Disposes the session asynchronously.
    /// </summary>
    /// <returns>A ValueTask representing the asynchronous dispose operation.</returns>
    /// <remarks>
    /// This allows the session to be used with <c>await using</c> syntax for async-friendly cleanup.
    /// </remarks>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private ManagementSession(
        IConnection connection,
        ScpKeyParameters? scpKeyParams = null)
    {
        _scpKeyParams = scpKeyParams;
        _logger = Logger;

        (_protocol, _backend) = connection switch
        {
            ISmartCardConnection sc => CreateSmartCardBackend(sc),
            IFidoHidConnection fido => CreateFidoBackend(fido),
            IOtpHidConnection otp => CreateOtpBackend(otp),
            _ => throw new NotSupportedException(
                $"The connection type {connection.GetType().Name} is not supported by ManagementSession. " +
                $"Supported types: ISmartCardConnection, IFidoHidConnection, IOtpHidConnection.")
        };

        Protocol = _protocol;
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

        _version = await GetVersionAsync(cancellationToken).ConfigureAwait(false);

        await InitializeCoreAsync(
                _protocol,
                _version,
                configuration,
                _scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        _protocol = Protocol ?? throw new InvalidOperationException();

        if (IsAuthenticated)
        {
            // Recreate backend with SCP-wrapped protocol
            _backend = new SmartCardBackend(
                _protocol as ISmartCardProtocol ?? throw new InvalidOperationException());
        }

        _logger.LogDebug("Management session initialized with protocol {ProtocolType}", _protocol.GetType().Name);
    }

    public async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        // TODO Add try catch
        byte page = 0;
        var allPagesTlvs = new List<Tlv>();

        var hasMoreData = true;
        while (hasMoreData)
        {
            var encodedResult = await _backend.ReadConfigAsync(page, cancellationToken).ConfigureAwait(false);

            if (encodedResult.Length < 1)
                throw new BadResponseException($"Empty response for page {page}");
                
            if (encodedResult.Length - 1 != encodedResult[0])
            {
                throw new BadResponseException("Invalid length");
            }

            var pageTlvs = TlvHelper.DecodeList(encodedResult.AsSpan()[1..]);
            allPagesTlvs.AddRange(pageTlvs);

            var moreData = pageTlvs.SingleOrDefault(t => t.Tag == TagMoreDeviceInfo);
            if (moreData is null)
                break;

            var moreDataValue = moreData.Value;
            hasMoreData = moreData?.Length == 1 && moreDataValue.Span[0] == 1;
            ++page;
        }

        using var allTlvs = new DisposableTlvList(allPagesTlvs);
        return DeviceInfo.CreateFromTlvs([.. allTlvs], _version);
    }

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
        return _backend.WriteConfigAsync(configBytes.ToArray(), cancellationToken).AsTask();
    }

    public Task ResetDeviceAsync(CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureDeviceReset);
        return _backend.DeviceResetAsync(cancellationToken).AsTask();
    }

    private async Task<FirmwareVersion> GetVersionAsync(CancellationToken cancellationToken)
    {
        var defaultVersion = await GetVersionFromManagementHeader(cancellationToken);
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

        return defaultVersion;
    }

    private async Task<FirmwareVersion> GetVersionFromManagementHeader(CancellationToken cancellationToken)
    {
        var versionBytes = await SelectAsync(cancellationToken).ConfigureAwait(false);

        var deviceText = Encoding.UTF8.GetString(versionBytes.Span);
        var versionString = deviceText.Split(' ').Last();
        var versionParts = versionString.Split('.').Select(int.Parse).ToArray();

        return versionParts.Length == 3
            ? new FirmwareVersion(versionParts[0], versionParts[1], versionParts[2])
            : new FirmwareVersion();
    }

    private Task<ReadOnlyMemory<byte>> SelectAsync(CancellationToken cancellationToken)
    {
        return _protocol switch
        {
            ISmartCardProtocol sc => sc.SelectAsync(ApplicationIds.Management, cancellationToken),
            IFidoHidProtocol fido => fido.SelectAsync(ApplicationIds.Management, cancellationToken),
            IOtpHidProtocol otp => GetOtpVersionAsync(otp, cancellationToken),
            _ => throw new NotSupportedException("No supported protocol available")
        };
    }

    private static async Task<ReadOnlyMemory<byte>> GetOtpVersionAsync(
        IOtpHidProtocol otpProtocol,
        CancellationToken cancellationToken)
    {
        // For OTP, read status bytes (first 3 bytes are version)
        var status = await otpProtocol.ReadStatusAsync(cancellationToken).ConfigureAwait(false);
        var version = otpProtocol.FirmwareVersion ?? new FirmwareVersion(status.Span[0], status.Span[1], status.Span[2]);
        var versionString = Encoding.UTF8.GetBytes($"YubiKey {version.Major}.{version.Minor}.{version.Patch}");
        return versionString;
    }

    private static (IProtocol protocol, IManagementBackend backend) CreateSmartCardBackend(
        ISmartCardConnection connection)
    {
        var protocol = PcscProtocolFactory<ISmartCardConnection>
            .Create()
            .Create(connection);
        
        var backend = new SmartCardBackend(protocol as ISmartCardProtocol ?? throw new InvalidOperationException());
        return (protocol, backend);
    }

    private static (IProtocol protocol, IManagementBackend backend) CreateFidoBackend(
        IFidoHidConnection connection)
    {
        var protocol = FidoProtocolFactory
            .Create()
            .Create(connection);

        var backend = new FidoHidBackend(protocol);
        return (protocol, backend);
    }

    private static (IProtocol protocol, IManagementBackend backend) CreateOtpBackend(
        IOtpHidConnection connection)
    {
        var protocol = OtpProtocolFactory
            .Create()
            .Create(connection);

        var backend = new OtpBackend(protocol);
        return (protocol, backend);
    }

}
