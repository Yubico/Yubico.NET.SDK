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

namespace Yubico.YubiKit.Core.SmartCard;

internal class CommandChainingProcessor(ISmartCardConnection connection, IApduFormatter formatter)
    : ExtendedApduProcessor(connection, formatter)
{
    private const int HasMoreData = 0x10;
    private const int ShortApduMaxChunk = SmartCardMaxApduSizes.ShortApduMaxChunkSize;

    public override async Task<ApduResponse> TransmitAsync(ApduCommand command, bool useScp = true,
        CancellationToken cancellationToken = default)
    {
        var data = command.Data;
        if (data.Length <= ShortApduMaxChunk)
            return await base.TransmitAsync(command, useScp, cancellationToken).ConfigureAwait(false);

        var offset = 0;
        while (offset + ShortApduMaxChunk < data.Length)
        {
            var chunk = data[offset..ShortApduMaxChunk];
            var chainedCommand = command with { Cla = (byte)(command.Cla | HasMoreData), Data = chunk };

            var result = await base.TransmitAsync(chainedCommand, useScp, cancellationToken).ConfigureAwait(false);
            if (result.SW != SWConstants.Success)
                return result;

            offset += ShortApduMaxChunk;
        }

        var finalChunk = data[offset..];
        var finalCommand = new ApduCommand(command.Cla, command.Ins, command.P1, command.P2, finalChunk, command.Le);
        return await base.TransmitAsync(finalCommand, useScp, cancellationToken).ConfigureAwait(false);
    }
}