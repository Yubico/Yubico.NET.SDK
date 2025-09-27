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

internal class CommandChainingProcessor : IApduProcessor
{
    public CommandChainingProcessor(ISmartCardConnection connection, IApduFormatter formatter)
    {
        throw new NotImplementedException();
    }

    #region IApduProcessor Members

    public void Dispose() => throw new NotImplementedException();

    public Task<ResponseApdu> TransmitAsync(CommandApdu command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    #endregion
}