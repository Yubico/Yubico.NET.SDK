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
using Yubico.YubiKit.Core.Processors;

namespace Yubico.YubiKit.Core.Protocols;

public interface IProtocol : IDisposable
{
}

public interface ISmartCardProtocol : IProtocol
{
    Task<ResponseApdu> TransmitAndReceiveAsync(
        CommandApdu command,
        CancellationToken cancellationToken = default);
}

internal class SmartCardProtocol : ISmartCardProtocol
{
    private const byte INS_SELECT = 0xa4;
    private const byte P1_SELECT = 0x04;
    private const byte P2_SELECT = 0x00;
    private const byte INS_SEND_REMAINING = 0xc0;
    private readonly ISmartCardConnection _connection;
    private readonly bool _extendedApdus = false;
    private readonly ILogger<SmartCardProtocol> _logger;
    private readonly int _maxApduSize = SmartCardMaxApduSizes.Neo;

    private readonly ApduProcessor _processor;
    private byte _insSendRemaining;

    public SmartCardProtocol(ILogger<SmartCardProtocol> logger, ISmartCardConnection connection)
    {
        _logger = logger;
        _connection = connection;
        (_processor, _) = BuildBaseProcessor();
    }

    #region ISmartCardProtocol Members

    public void Dispose()
    {
        _connection.Dispose();
        _processor.Dispose();
    }

    public async Task<ResponseApdu> TransmitAndReceiveAsync(
        CommandApdu command,
        CancellationToken cancellationToken = default) =>
        await _connection.TransmitAndReceiveAsync(command, cancellationToken);

    #endregion

    private (ApduProcessor Processor, ApduFormatter Formatter) BuildBaseProcessor()
    {
        ApduProcessor result;
        ApduFormatter formatter;
        if (_extendedApdus)
        {
            formatter = new ExtendedApduFormatter(_maxApduSize);
            result = new ApduFormatProcessor(_connection, formatter);
        }
        else
        {
            formatter = new ShortApduFormatter();
            result = new CommandChainingProcessor(_connection, formatter);
        }

        result = new ChainedResponseProcessor(result, _insSendRemaining);

        return (result, formatter);
    }
}