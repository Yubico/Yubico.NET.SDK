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
using Yubico.YubiKit.Core.Apdu;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Iso7816;

namespace Yubico.YubiKit.Core.Protocols;

public interface IProtocol : IDisposable
{
}

public interface ISmartCardProtocol : IProtocol
{
    Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        CommandApdu command,
        CancellationToken cancellationToken = default);

    Task<ReadOnlyMemory<byte>> SelectAsync(ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default);
}

internal class SmartCardProtocol : ISmartCardProtocol
{
    private const byte INS_SELECT = 0xa4;
    private const byte P1_SELECT = 0x04;
    private const byte P2_SELECT = 0x00;
    private const byte INS_SEND_REMAINING = 0xc0;
    private const int MaxApduSize = SmartCardMaxApduSizes.Neo;
    private readonly ISmartCardConnection _connection;
    private readonly bool _extendedApdus = false;
    private readonly byte _insSendRemaining;
    private readonly ILogger<SmartCardProtocol> _logger;
    private readonly ChainedResponseProcessor _processor;

    public SmartCardProtocol(ILogger<SmartCardProtocol> logger, ISmartCardConnection connection,
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
        var response = await _processor.TransmitAsync(command, cancellationToken);
        if (response is not { SW1: 0x90, SW2: 0x00 })
            throw new InvalidOperationException(
                $"Command failed with status: {response.SW1:X2}{response.SW2:X2}");

        return response.Data;
    }

    public async Task<ReadOnlyMemory<byte>> SelectAsync(ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default)
    {
        var response =
            await _processor.TransmitAsync(
                new CommandApdu { Ins = INS_SELECT, P1 = P1_SELECT, P2 = P2_SELECT, Data = applicationId },
                cancellationToken);

        if (response is not { SW1: 0x90, SW2: 0x00 })
            throw new InvalidOperationException(
                $"Select command failed with status: {response.SW1:X2}{response.SW2:X2}");

        return response.Data;
    }

    #endregion

    private ChainedResponseProcessor BuildBaseProcessor()
    {
        IApduProcessor processor = _extendedApdus
            ? new ApduFormatProcessor(_connection, new ExtendedApduFormatter(MaxApduSize))
            : new CommandChainingProcessor(_connection, new ShortApduFormatter());

        return new ChainedResponseProcessor(processor, _insSendRemaining);
    }
}