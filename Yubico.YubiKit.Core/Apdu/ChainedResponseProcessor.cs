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

using Yubico.YubiKit.Core.Iso7816;

namespace Yubico.YubiKit.Core.Apdu;

internal class ChainedResponseProcessor : IApduProcessor
{
    private static readonly byte SW1_HAS_MORE_DATA = 0x61;
    private readonly IApduProcessor _apduTransmitter;
    private readonly CommandApdu GetMoreDataApdu;

    public ChainedResponseProcessor(
        IApduProcessor apduTransmitter,
        byte insSendRemaining)
    {
        _apduTransmitter = apduTransmitter;
        GetMoreDataApdu = new CommandApdu((byte)0, insSendRemaining, (byte)0, (byte)0);
    }

    #region IApduProcessor Members

    public IApduFormatter Formatter => _apduTransmitter.Formatter;

    public async Task<ResponseApdu> TransmitAsync(CommandApdu command, CancellationToken cancellationToken = default)
    {
        var response = await _apduTransmitter.TransmitAsync(command, cancellationToken);

        using var ms = new MemoryStream();
        while (response.SW1 == SW1_HAS_MORE_DATA)
        {
            ms.Write(response.Data.Span);
            response = await _apduTransmitter.TransmitAsync(GetMoreDataApdu, cancellationToken);
        }

        ms.Write(response.Data.Span);
        ms.WriteByte(response.SW1);
        ms.WriteByte(response.SW2);
        return new ResponseApdu(ms.ToArray());
    }

    #endregion
}