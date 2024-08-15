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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Pipelines
{
    /// <summary>
    /// A pipeline that performs command chaining to allow sending larger
    /// command APDUs.
    /// </summary>
    internal class CommandChainingTransform : IApduTransform
    {
        public int MaxSize { get; internal set; } = 255;

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

            if (command.Data.IsEmpty || command.Data.Length <= MaxSize)
            {
                return _pipeline.Invoke(command, commandType, responseType);
            }

            var sourceData = command.Data;
            ResponseApdu? responseApdu = null;

            while (!sourceData.IsEmpty)
            {
                int length = Math.Min(MaxSize, sourceData.Length);
                var data = sourceData.Slice(0, length);
                sourceData = sourceData.Slice(length);

                var partialApdu = new CommandApdu
                {
                    Cla = (byte)(command.Cla | (sourceData.IsEmpty ? 0 : 0x10)),
                    Ins = command.Ins,
                    P1 = command.P1,
                    P2 = command.P2,
                    Data = data
                };

                responseApdu = _pipeline.Invoke(partialApdu, commandType, responseType);
            }

            return responseApdu!; // Covered by Debug.Assert above. TODO err??
        }

        public void Setup() => _pipeline.Setup();
    }
}
