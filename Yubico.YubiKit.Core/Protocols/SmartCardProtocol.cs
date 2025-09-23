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
using Yubico.YubiKit.Core.Processors;

namespace Yubico.YubiKit.Core.Protocols;

internal class SmartCardProtocol : ISmartCardProtocol
{
    private const byte INS_SELECT = 0xa4;
    private const byte P1_SELECT = 0x04;
    private const byte P2_SELECT = 0x00;
    private const byte INS_SEND_REMAINING = 0xc0;
    private readonly ISmartCardConnection _connection;
    private readonly bool _extendedApdus = true;
    private readonly ILogger<SmartCardProtocol> _logger;
    private readonly int maxApduSize = SmartCardMaxApduSizes.Yk43;
    private byte insSendRemaining;

    private ApduProcessor processor;

    public SmartCardProtocol(ILogger<SmartCardProtocol> logger, ISmartCardConnection connection)
    {
        _logger = logger;
        _connection = connection;
        (processor, _) = BuildBaseProcessor();
    }

    private (ApduProcessor Processor, ApduFormatter Formatter) BuildBaseProcessor()
    {
        ApduProcessor result;
        ApduFormatter formatter;
        if (_extendedApdus)
        {
            formatter = new ExtendedApduFormatter(maxApduSize);
            result = new ApduFormatProcessor(_connection, formatter);
        }
        else
        {
            formatter = new ShortApduFormatter();
            // Short APDUs need command chaining
            result = new CommandChainingProcessor(_connection, formatter);
        }

        // Always wrap with response chaining
        result = new ChainedResponseProcessor(result, insSendRemaining);

        return (result, formatter);
    }
}