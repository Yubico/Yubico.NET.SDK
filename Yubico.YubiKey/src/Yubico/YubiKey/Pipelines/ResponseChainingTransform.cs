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
using System.Collections.Generic;
using Yubico.YubiKey.InterIndustry.Commands;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Pipelines
{
    /// <summary>
    /// A transform that automatically detects large responses and issues GET_RESPONSE APDUs until
    /// all data has been returned.
    /// </summary>
    internal class ResponseChainingTransform : IApduTransform
    {
        private readonly IApduTransform _pipeline;

        public ResponseChainingTransform(IApduTransform pipeline)
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

            ResponseApdu response = _pipeline.Invoke(command, commandType, responseType);

            // Unless we see that bytes are available, there's nothing for this transform to do.
            if (response.SW1 != SW1Constants.BytesAvailable)
            {
                return response;
            }

            var tempBuffer = new List<byte>();

            do
            {
                tempBuffer.AddRange(response.Data.ToArray());

                var getResponseCommand = new GetResponseCommand(command, response.SW2);
                response = _pipeline.Invoke(getResponseCommand.CreateCommandApdu(), commandType, responseType);
            }
            while (response.SW1 == SW1Constants.BytesAvailable);

            if (response.SW == SWConstants.Success)
            {
                tempBuffer.AddRange(response.Data.ToArray());
            }

            return new ResponseApdu(tempBuffer.ToArray(), response.SW);
        }

        public void Setup() => _pipeline.Setup();
    }
}
