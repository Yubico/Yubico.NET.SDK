// Copyright 2021 Yubico AB
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

using System;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Yubico.Core.Iso7816;
using Yubico.Core.Logging;

namespace Yubico.YubiKey.Pipelines
{
    /// <summary>
    /// A pipeline that performs command chaining to allow sending larger
    /// command APDUs.
    /// </summary>
    internal class CommandChainingTransform : IApduTransform
    {
        private readonly ILogger _log = Log.GetLogger<CommandChainingTransform>();

        public int MaxChunkSize { get; internal set; } = 255;

        readonly IApduTransform _pipeline;

        public CommandChainingTransform(IApduTransform pipeline)
        {
            _pipeline = pipeline;
        }

        public void Cleanup() => _pipeline.Cleanup();

        public ResponseApdu Invoke(CommandApdu command, Type commandType, Type responseType)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            // Send regular short APDU
            int commandDataSize = command.Data.Length;
            if (commandDataSize <= MaxChunkSize)
            {
                _log.LogDebug("Sending short APDU");
                return _pipeline.Invoke(command, commandType, responseType);
            }

            // Send chained short APDU
            var sourceData = command.Data;
            ResponseApdu? responseApdu = null;
            _log.LogDebug("APDU size exceeds size of short APDU, proceeding to send data in chunks instead");
            while (!sourceData.IsEmpty)
            {
                int chunkLength = Math.Min(MaxChunkSize, sourceData.Length);
                var dataChunk = sourceData[..chunkLength];
                sourceData = sourceData[chunkLength..];

                var partialApdu = new CommandApdu
                {
                    Cla = (byte)(command.Cla | (sourceData.IsEmpty
                        ? 0
                        : 0x10)),
                    Ins = command.Ins,
                    P1 = command.P1,
                    P2 = command.P2,
                    Data = dataChunk
                };

                responseApdu = _pipeline.Invoke(partialApdu, commandType, responseType);

                // Stop sending data when the YubiKey response is 0x67 (when the chained data
                // sent exceeds the max allowed length) and let caller handle the response
                if (responseApdu.SW != SWConstants.WrongLength)
                {
                    continue;
                }

                _log.LogWarning("Sent data exceeds max allowed length by YubiKey. (SW: 0x{StatusWord})",
                    responseApdu.SW.ToString("X4", CultureInfo.InvariantCulture));
                
                return responseApdu;
            }

            return responseApdu!;
        }

        public void Setup() => _pipeline.Setup();
    }
}
