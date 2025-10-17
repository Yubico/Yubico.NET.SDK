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
using System.Buffers;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.SmartCard;

public interface IProtocol : IDisposable
{
    void Configure(FirmwareVersion version, Configuration? configuration = null);
}

public interface ISmartCardProtocol : IProtocol
{
    Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        CommandApdu command,
        CancellationToken cancellationToken = default);

    Task<ReadOnlyMemory<byte>> SelectAsync(ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default);
}

public readonly record struct Configuration
{
    public bool? ForceShortApdus { get; init; }
}

internal class PcscProtocol : ISmartCardProtocol
{
    private bool _useExtendedApdus = true;
    private int MaxApduSize = SmartCardMaxApduSizes.Neo; // Lowest as default
    private IApduProcessor _processor;

    private const byte INS_SELECT = 0xA4;
    private const byte P1_SELECT = 0x04;
    private const byte P2_SELECT = 0x00;
    private const byte INS_SEND_REMAINING = 0xC0;
    private readonly byte _insSendRemaining;
    private readonly ILogger<PcscProtocol> _logger;
    private readonly ISmartCardConnection _connection;


    public PcscProtocol(
        ILogger<PcscProtocol> logger,
        ISmartCardConnection connection,
        ReadOnlyMemory<byte> insSendRemaining = default)
    {
        _logger = logger;
        _connection = connection;
        _insSendRemaining = insSendRemaining.Length > 0 ? insSendRemaining.Span[0] : INS_SEND_REMAINING;
        _processor = BuildBaseProcessor();
    }

    #region ISmartCardProtocol Members

    public void Dispose() => _connection.Dispose();

    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        CommandApdu command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Transmitting APDU: {CommandApdu}", command);

        var response = await _processor.TransmitAsync(command, cancellationToken).ConfigureAwait(false);
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
                new CommandApdu { Ins = INS_SELECT, P1 = P1_SELECT, P2 = P2_SELECT, Data = applicationId },
                cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
            throw new InvalidOperationException(
                $"Select command failed with status: {response.SW1:X2}{response.SW2:X2}");

        return response.Data;
    }

    #endregion

    private IApduProcessor BuildBaseProcessor()
    {
        var processor = _useExtendedApdus
            ? new ExtendedApduProcessor(_connection, new ExtendedApduFormatter(MaxApduSize))
            : new CommandChainingProcessor(_connection, new ShortApduFormatter());

        return new ChainedResponseProcessor(processor, _insSendRemaining);
    }

    public void Configure(FirmwareVersion firmwareVersion, Configuration? configuration)
    {
        if (firmwareVersion.IsAtLeast(4, 0, 0))
        {
            bool forceShortApdu = configuration.HasValue && configuration.Value.ForceShortApdus == true;
            _useExtendedApdus = _connection.SupportsExtendedApdu() && !forceShortApdu;
            MaxApduSize = firmwareVersion.IsAtLeast(4, 3, 0)
                ? SmartCardMaxApduSizes.Yk43
                : SmartCardMaxApduSizes.Yk4;
            ReconfigureProcessor();
        }
    }

    private void ReconfigureProcessor()
    {
        var newProcessor = BuildBaseProcessor();
        if (_processor is ScpProcessor scpp)
        {
            // Keep existing SCP state
            newProcessor = new ScpProcessor(newProcessor, scpp.Formatter, scpp.State);
        }

        _processor = newProcessor;
    }
}

internal class ScpProcessor(IApduProcessor @delegate, IApduFormatter formatter, ScpState state) : IApduProcessor
{
    public IApduFormatter Formatter { get; } = formatter;

    public Task<ResponseApdu> TransmitAsync(CommandApdu command, CancellationToken cancellationToken = default)
        => Transmit(command, encrypt: true, cancellationToken);

    internal ScpState State { get; } = state;
    private readonly ExtendedApduFormatter _extendedApduFormatter = new(SmartCardMaxApduSizes.Yk43);
    public async Task<ResponseApdu> Transmit(CommandApdu command, bool encrypt, CancellationToken cancellationToken)
    {
        Span<byte> dataPostEnc = ArrayPool<byte>.Shared.Rent(command.Data.Length);
        command.Data.Span.CopyTo(dataPostEnc);
        if (encrypt)
        {
            State.Encrypt(dataPostEnc);
        }

        Span<byte> dataPostMac = ArrayPool<byte>.Shared.Rent(dataPostEnc.Length);
        dataPostEnc.CopyTo(dataPostMac);

        var cla = command.Cla | 0x04; // isEncrypted

        ReadOnlySpan<byte> dataPostFormat = ArrayPool<byte>.Shared.Rent(dataPostEnc.Length); // TODO size?
        var newApdu = command with { Cla = (byte)cla, Data = dataPostMac.ToArray() };
        if (dataPostMac.Length > SmartCardMaxApduSizes.ShortApduMaxChunkSize)
        {
            dataPostFormat = _extendedApduFormatter.Format(newApdu).Span;
        }
        else
        {
            dataPostFormat = Formatter.Format(newApdu).Span;
        }

        ReadOnlySpan<byte> mac = State.MacEncode(dataPostFormat[..^8]);
        mac.CopyTo(dataPostMac);

        var response = await @delegate.TransmitAsync(newApdu with { Data = dataPostMac.ToArray() }, cancellationToken);
        var responseData = response.Data;

        if (responseData.Length > 0)
        {
            responseData = State.MacDecode(responseData, response.SW);
        }
    }
}


internal record ScpState
{

}