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

using Yubico.YubiKit.Core.Core.Connections;
using Yubico.YubiKit.Core.Core.Iso7816;

namespace Yubico.YubiKit.Core.Core.Apdu;

internal class CommandChainingProcessor(ISmartCardConnection connection, IApduFormatter formatter)
    : ApduFormatProcessor(connection, formatter)
{
    private const int ChunkSize = 255;

    public override async Task<ResponseApdu> TransmitAsync(CommandApdu command,
        CancellationToken cancellationToken = default)
    {
        var data = command.Data;
        if (data.Length <= ChunkSize)
            return await base.TransmitAsync(command, cancellationToken).ConfigureAwait(false);

        var offset = 0;
        while (offset + ChunkSize < data.Length)
        {
            var chunk = data.Slice(offset, ChunkSize);
            var chainedCommand = new CommandApdu(
                (byte)(command.Cla | 0x10),
                command.Ins,
                command.P1,
                command.P2,
                chunk);

            var result = await base.TransmitAsync(chainedCommand, cancellationToken).ConfigureAwait(false);
            if (result.SW != SWConstants.Success)
                return result;

            offset += ChunkSize;
        }

        var finalChunk = data[offset..];
        var finalCommand = new CommandApdu(command.Cla, command.Ins, command.P1, command.P2, finalChunk, command.Le);
        return await base.TransmitAsync(finalCommand, cancellationToken).ConfigureAwait(false);
    }
}