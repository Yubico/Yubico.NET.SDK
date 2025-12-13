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
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.SmartCard;

public interface IProtocol : IDisposable
{
    void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null);
}

/// <summary>
///     Protocol interface for SmartCard communication.
/// </summary>
public interface ISmartCardProtocol : IProtocol
{
    Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ApduCommand command,
        CancellationToken cancellationToken = default);

    Task<ReadOnlyMemory<byte>> SelectAsync(ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default);
}

public readonly record struct ProtocolConfiguration
{
    public bool? ForceShortApdus { get; init; }
}

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
        ILogger<PcscProtocol> logger,
        ISmartCardConnection connection,
        ReadOnlyMemory<byte> insSendRemaining = default)
    {
        _logger = logger;
        _connection = connection;
        InsSendRemaining = insSendRemaining.Length > 0 ? insSendRemaining.Span[0] : INS_SEND_REMAINING;
        _processor = BuildBaseProcessor();
    }

    public bool UseExtendedApdus { get; private set; } = true;

    public int MaxApduSize { get; private set; } = SmartCardMaxApduSizes.Neo; // Lowest as default

    public FirmwareVersion? FirmwareVersion { get; private set; }

    #region ISmartCardProtocol Members

    public void Dispose() => _connection.Dispose();

    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ApduCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Transmitting APDU: {CommandApdu}", command);

        var response = await _processor.TransmitAsync(command, true, cancellationToken).ConfigureAwait(false);
        if (!response.IsOK())
            throw new InvalidOperationException(
                $"Command failed with status: {response.SW1:X2}{response.SW2:X2}");

        return response.Data;
    }

    public async Task<ReadOnlyMemory<byte>> SelectAsync(
        ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Selecting application ID: {ApplicationId}", Convert.ToHexString(applicationId.ToArray()));

        var response =
            await _processor.TransmitAsync(
                new ApduCommand { Ins = INS_SELECT, P1 = P1_SELECT, P2 = P2_SELECT, Data = applicationId },
                false,
                cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
            throw new InvalidOperationException(
                $"Select command failed with status: {response.SW1:X2}{response.SW2:X2}");

        return response.Data;
    }

    public void Configure(FirmwareVersion firmwareVersion, ProtocolConfiguration? configuration)
    {
        FirmwareVersion = firmwareVersion;
        if (FirmwareVersion.IsAtLeast(4, 0, 0))
        {
            var forceShortApdu = configuration is { ForceShortApdus: true };
            UseExtendedApdus = _connection.SupportsExtendedApdu() && !forceShortApdu;
            MaxApduSize = firmwareVersion.IsAtLeast(4, 3, 0)
                ? SmartCardMaxApduSizes.Yk43
                : SmartCardMaxApduSizes.Yk4;

            ReconfigureProcessor();
        }
    }

    #endregion

    private IApduProcessor BuildBaseProcessor()
    {
        var processor = UseExtendedApdus
            ? new ExtendedApduProcessor(_connection, new ExtendedApduFormatter(MaxApduSize))
            : new CommandChainingProcessor(_connection, new ShortApduFormatter());

        return new ChainedResponseProcessor(FirmwareVersion, processor, InsSendRemaining);
    }

    private void ReconfigureProcessor() =>
        _processor = BuildBaseProcessor();

    internal IApduProcessor GetBaseProcessor() => BuildBaseProcessor();
}