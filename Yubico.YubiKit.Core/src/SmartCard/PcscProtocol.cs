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
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.SmartCard;

internal class PcscProtocol : ISmartCardProtocol
{
    private const byte INS_SELECT = 0xA4;
    private const byte P1_SELECT = 0x04;
    private const byte P2_SELECT = 0x00;
    private const byte INS_SEND_REMAINING = 0xC0;

    private readonly ISmartCardConnection _connection;
    private readonly ILogger<PcscProtocol> _logger;
    internal readonly byte InsSendRemaining;
    private IApduProcessor _processor;

    public PcscProtocol(
        ISmartCardConnection connection,
        ReadOnlyMemory<byte> insSendRemaining = default,
        ILogger<PcscProtocol>? logger = null)
    {
        _logger = logger ?? NullLogger<PcscProtocol>.Instance;
        _connection = connection;
        InsSendRemaining = insSendRemaining.Length > 0 ? insSendRemaining.Span[0] : INS_SEND_REMAINING;
        UseExtendedApdus = _connection.SupportsExtendedApdu();
        _processor = BuildBaseProcessor();
    }

    internal bool UseExtendedApdus { get; private set; }

    public int MaxApduSize { get; private set; } = SmartCardMaxApduSizes.Neo; // Lowest as default

    public FirmwareVersion? FirmwareVersion { get; private set; }

    private IApduProcessor BuildBaseProcessor()
    {
        var processor = UseExtendedApdus
            ? new ApduTransmitter(_connection, new ApduFormatterExtended(MaxApduSize))
            : new ChainedApduTransmitter(_connection, new ApduFormatterShort());

        return new ChainedResponseReceiver(FirmwareVersion, processor, InsSendRemaining);
    }

    private void ReconfigureProcessor() =>
        _processor = BuildBaseProcessor();

    internal IApduProcessor GetBaseProcessor() => BuildBaseProcessor();

    #region ISmartCardProtocol Members

    public void Dispose() => _connection.Dispose();

    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ApduCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Transmitting APDU: {CommandApdu}", command);

        var response = await _processor.TransmitAsync(command, false, cancellationToken).ConfigureAwait(false);
        return !response.IsOK()
            ? throw ApduException.FromResponse(response, command, "APDU command failed")
            : response.Data;
    }

    public async Task<ReadOnlyMemory<byte>> SelectAsync(
        ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Selecting application ID: {ApplicationId}", Convert.ToHexString(applicationId.ToArray()));

        var selectCommand = new ApduCommand { Ins = INS_SELECT, P1 = P1_SELECT, P2 = P2_SELECT, Data = applicationId };
        var response = await _processor.TransmitAsync(selectCommand, false, cancellationToken).ConfigureAwait(false);
        return !response.IsOK()
            ? throw ApduException.FromResponse(response, selectCommand, "SELECT command failed")
            : response.Data;
    }

    public void Configure(FirmwareVersion firmwareVersion, ProtocolConfiguration? configuration = null)
    {
        FirmwareVersion = firmwareVersion;
        if (!FirmwareVersion.IsAtLeast(FirmwareVersion.V4_0_0))
            return;

        var forceShortApdu = configuration is { ForceShortApdus: true };
        UseExtendedApdus = _connection.SupportsExtendedApdu() && !forceShortApdu; // TODO always true for UsbPcsc... 
        MaxApduSize = firmwareVersion.IsAtLeast(FirmwareVersion.V4_3_0)
            ? SmartCardMaxApduSizes.Yk43
            : SmartCardMaxApduSizes.Yk4;

        ReconfigureProcessor();
    }

    #endregion
}