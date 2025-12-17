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

    public async Task<ApduResponse> TransmitAsync(ApduCommand command, bool useScp = true,
        CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();

        var response = await apduTransmitter.TransmitAsync(command, useScp, cancellationToken).ConfigureAwait(false);
        while (response.SW1 == Sw1HasMoreData)
        {
            ms.Write(response.Data.Span);
            response = await apduTransmitter.TransmitAsync(_getMoreDataApdu, useScp, cancellationToken)
                .ConfigureAwait(false);
        }

        ms.Write(response.Data.Span);
        ms.WriteByte(response.SW1);
        ms.WriteByte(response.SW2);

        return new ApduResponse(ms.ToArray());
    }

    #endregion
}