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
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Management;

public sealed class ManagementSession : ApplicationSession
{
    private const byte INS_GET_DEVICE_INFO = 0x1D;
    private const byte INS_DEVICE_RESET = 0x1F;
    private const byte INS_SET_DEVICE_CONFIG = 0x1C;

    private const int TagMoreDeviceInfo = 0x10;

    private static readonly Feature FeatureDeviceInfo =
        new("Device Info", 4, 1, 0);

    private static readonly Feature FeatureSetConfig =
        new("Set Config", 5, 0, 0);

    private static readonly Feature FeatureDeviceReset =
        new("Device Reset", 5, 6, 0);

    private readonly ILogger<ManagementSession> _logger;
    private readonly ScpKeyParameters? _scpKeyParams;

    private bool _isInitialized;

    private IProtocol _protocol;
    private ISmartCardProtocol? _smartCardProtocol;
    private IFidoProtocol? _fidoProtocol;

    private FirmwareVersion? _version;

    private ManagementSession(
        IConnection connection,
        ILoggerFactory loggerFactory,
        ScpKeyParameters? scpKeyParams = null)
    {
        _scpKeyParams = scpKeyParams;
        _logger = loggerFactory.CreateLogger<ManagementSession>();

        _protocol = connection switch
        {
            ISmartCardConnection sc => CreateSmartCardProtocol(sc, loggerFactory),
            IFidoConnection fido => CreateFidoProtocol(fido, loggerFactory),
            _ => throw new NotSupportedException(
                $"The connection type {connection.GetType().Name} is not supported by ManagementSession. " +
                $"Supported types: ISmartCardConnection, IFidoConnection.")
        };

        UpdateProtocolReferences();
    }

    public static async Task<ManagementSession> CreateAsync(
        IConnection connection,
        ProtocolConfiguration? configuration = null,
        ILoggerFactory? loggerFactory = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        loggerFactory ??= NullLoggerFactory.Instance;

        var session = new ManagementSession(connection, loggerFactory, scpKeyParams);
        await session.InitializeAsync(configuration, cancellationToken).ConfigureAwait(false);

        return session;
    }

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        _version = await GetVersionAsync(cancellationToken).ConfigureAwait(false);
        _protocol.Configure(_version, configuration);

        // Initialize SCP if key parameters were provided
        if (_scpKeyParams is not null && _smartCardProtocol is not null)
        {
            _protocol = await _smartCardProtocol
                .WithScpAsync(_scpKeyParams, cancellationToken)
                .ConfigureAwait(false);

            UpdateProtocolReferences();
        }

        _isInitialized = true;
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
            ReadOnlyMemory<byte> encodedResult;
            
            // For HID, use CTAP vendor command; for SmartCard, use APDU
            if (_fidoProtocol is not null)
            {
                // CTAP_READ_CONFIG (0xC2) with page number (single byte)
                var pagePayload = new byte[] { page };
                encodedResult = await _fidoProtocol
                    .SendVendorCommandAsync(0xC2, pagePayload, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var apdu = new ApduCommand { Cla = 0, Ins = INS_GET_DEVICE_INFO, P1 = page, P2 = 0 };
                encodedResult = await TransmitAsync(apdu, cancellationToken).ConfigureAwait(false);
            }
            
            if (encodedResult.Length - 1 != encodedResult.Span[0])
                throw new BadResponseException("Invalid length");

            var pageTlvs = TlvHelper.DecodeList(encodedResult.Span[1..]);
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
        var apdu = new ApduCommand
        {
            Cla = 0,
            Ins = INS_SET_DEVICE_CONFIG,
            P1 = 0,
            P2 = 0,
            Data = configBytes
        };

        return TransmitAsync(apdu, cancellationToken);
    }

    public async Task ResetDeviceAsync(CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureDeviceReset);

        await TransmitAsync(new ApduCommand { Cla = 0, Ins = INS_DEVICE_RESET, P1 = 0, P2 = 0 }, cancellationToken)
            .ConfigureAwait(false);
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

    private async Task<ReadOnlyMemory<byte>> SelectAsync(CancellationToken cancellationToken)
    {
        if (_smartCardProtocol is not null)
            return await _smartCardProtocol
                .SelectAsync(ApplicationIds.Management, cancellationToken)
                .ConfigureAwait(false);

        if (_fidoProtocol is not null)
            return await _fidoProtocol
                .SelectAsync(ApplicationIds.Management, cancellationToken)
                .ConfigureAwait(false);

        throw new NotSupportedException("No supported protocol available");
    }

    private Task<ReadOnlyMemory<byte>> TransmitAsync(ApduCommand command, CancellationToken cancellationToken)
    {
        if (_smartCardProtocol is not null)
        {
            return _smartCardProtocol.TransmitAndReceiveAsync(command, cancellationToken);
        }

        if (_fidoProtocol is not null)
        {
            return _fidoProtocol.TransmitAndReceiveAsync(command, cancellationToken);
        }

        throw new NotSupportedException("Protocol not supported");
    }

    private static IProtocol CreateSmartCardProtocol(
        ISmartCardConnection connection,
        ILoggerFactory loggerFactory)
    {
        return PcscProtocolFactory<ISmartCardConnection>
            .Create(loggerFactory)
            .Create(connection);
    }

    private static IProtocol CreateFidoProtocol(
        IFidoConnection connection,
        ILoggerFactory loggerFactory)
    {
        return FidoProtocolFactory<IFidoConnection>
            .Create(loggerFactory)
            .Create(connection);
    }

    private void UpdateProtocolReferences()
    {
        _smartCardProtocol = _protocol as ISmartCardProtocol;
        _fidoProtocol = _protocol as IFidoProtocol;
    }

    private void EnsureSupports(Feature feature)
    {
        if (!IsSupported(feature)) throw new NotSupportedException($"{feature.Name} is not supported on this YubiKey.");
    }

    private bool IsSupported(Feature feature)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Session not initialized. Call InitializeAsync first.");

        if (_version is null)
            return false;

        return _version >= feature.Version;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _protocol.Dispose();

        base.Dispose(disposing);
    }
}
