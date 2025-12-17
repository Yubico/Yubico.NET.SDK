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

using System.Buffers;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.SmartCard;

internal class ChainedResponseReceiver(
    FirmwareVersion? firmwareVersion,
    IApduProcessor apduTransmitter,
    byte insSendRemaining) : IApduProcessor
{
    private const byte Sw1HasMoreData = 0x61;
    private readonly ApduCommand _getMoreDataApdu = new(0, insSendRemaining, 0, 0);

    public FirmwareVersion? FirmwareVersion { get; } = firmwareVersion;

    #region IApduProcessor Members

    public IApduFormatter Formatter => apduTransmitter.Formatter;

    public async Task<ApduResponse> TransmitAsync(
        ApduCommand command,
        bool useScp = true,
        CancellationToken cancellationToken = default)
    {
        var response = await apduTransmitter.TransmitAsync(command, useScp, cancellationToken).ConfigureAwait(false);
        if (response.SW1 != Sw1HasMoreData)
            return response;

        var buffer = new ArrayBufferWriter<byte>();
        while (response.SW1 == Sw1HasMoreData) // Partial response, more data available
        {
            buffer.Write(response.Data.Span);
            response = await apduTransmitter.TransmitAsync(_getMoreDataApdu, useScp, cancellationToken)
                .ConfigureAwait(false);
        }

        buffer.Write(response.Data.Span);
        buffer.Write([response.SW1]);
        buffer.Write([response.SW2]);

        var completeResponse = new ApduResponse(buffer.WrittenMemory);
        buffer.Clear();

        return completeResponse;
    }

    #endregion
}