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
using Yubico.YubiKit.Core.Protocols;

namespace Yubico.YubiKit.Core.Apdu;

internal class ApduFormatProcessor : IApduProcessor
{
    private readonly ISmartCardConnection _connection;
    private readonly IApduFormatter _formatter;
    private bool _disposed;

    public ApduFormatProcessor(ISmartCardConnection connection, IApduFormatter formatter)
    {
        _connection = connection;
        _formatter = formatter;
    }

    #region IApduProcessor Members

    public void Dispose() => throw new NotImplementedException();

    public async Task<ResponseApdu> TransmitAsync(CommandApdu command, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var payload = _formatter.Format(
            command.Cla,
            command.Ins,
            command.P1,
            command.P2,
            command.Data,
            0,
            command.Data.Length,
            command.Le);

        var response = await _connection.TransmitAndReceiveAsync(payload);
        return response;
    }

    #endregion

    public virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) _connection.Dispose();
        _disposed = true;
    }
}