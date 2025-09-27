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

using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Iso7816;

namespace Yubico.YubiKit.Core.Apdu;

internal class ApduFormatProcessor(ISmartCardConnection connection, IApduFormatter formatter) : IApduProcessor
{
    private bool _disposed;

    #region IApduProcessor Members

    public void Dispose()
    {
        if (_disposed) return;

        connection.Dispose();
        _disposed = true;
    }

    public async Task<ResponseApdu> TransmitAsync(CommandApdu command, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var payload = formatter.Format(
            command.Cla,
            command.Ins,
            command.P1,
            command.P2,
            command.Data,
            0,
            command.Data.Length,
            command.Le);

        var response = await connection.TransmitAndReceiveAsync(payload, cancellationToken);
        return new ResponseApdu(response);
    }

    #endregion
}