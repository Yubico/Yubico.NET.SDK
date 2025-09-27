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
    private static byte SW1_HAS_MORE_DATA = 0x61;
    private readonly byte _insSendRemaining;
    private readonly IApduProcessor _processor;

    public ChainedResponseProcessor(
        IApduProcessor processor,
        byte insSendRemaining)
    {
        _processor = processor;
        _insSendRemaining = insSendRemaining;
    }

    #region IApduProcessor Members

    public void Dispose()
    {
        // TODO release managed resources here
    }

    public Task<ResponseApdu> TransmitAsync(CommandApdu command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    #endregion
}