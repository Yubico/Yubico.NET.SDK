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
using Yubico.YubiKey.Otp.Commands;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Pipelines
{
    // Note: This transform doesn't ever need to be applied to the FidoConnection type. This is because OTP will never
    // be available over FIDO.
    internal class OtpErrorTransform : IApduTransform
    {
        private readonly IApduTransform _nextTransform;

        public OtpErrorTransform(IApduTransform nextTransform)
        {
            _nextTransform = nextTransform;
        }

        public ResponseApdu Invoke(CommandApdu command, Type commandType, Type responseType)
        {
            // If this is just a regular ReadStatusCommand, or it's a command that doesn't ask for a status response
            // in return, invoke the pipeline as usual.
            if (commandType == typeof(ReadStatusCommand)
                || responseType != typeof(ReadStatusResponse))
            {
                return _nextTransform.Invoke(command, commandType, responseType);
            }

            // Otherwise we assume this to be a command that applies a config (and therefore looks for a status response).
            // In order to detect failures, we grab the status structure before applying said command so that we have a
            // sequence number to compare to.
            int beforeSequence = new ReadStatusResponse(
                _nextTransform.Invoke(
                    new ReadStatusCommand().CreateCommandApdu(),
                    typeof(ReadStatusCommand),
                    typeof(ReadStatusResponse)))
                .GetData()
                .SequenceNumber;

            ResponseApdu response = _nextTransform.Invoke(command, commandType, responseType);
            int afterSequence = new ReadStatusResponse(response).GetData().SequenceNumber;
            int expectedSequence = (beforeSequence + 1) % 0x100;

            // If we see the sequence number change, we can assume that the configuration was applied successfully. Otherwise
            // we just invent an error in the response.
            return afterSequence != expectedSequence
                ? new ResponseApdu(response.Data.ToArray(), SWConstants.WarningNvmUnchanged)
                : response;
        }

        public void Setup() => _nextTransform.Setup();

        public void Cleanup() => _nextTransform.Cleanup();
    }
}
