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

using System;
using Microsoft.Extensions.Logging;
using Yubico.Core.Iso7816;
using Yubico.Core.Logging;
using Yubico.YubiKey.Otp;
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey.Pipelines
{
    // Note: This transform doesn't ever need to be applied to the FidoConnection type. This is because OTP will never
    // be available over FIDO.
    internal class OtpErrorTransform : IApduTransform
    {
        private readonly ILogger _logger = Log.GetLogger<OtpErrorTransform>();

        private readonly IApduTransform _nextTransform;

        public OtpErrorTransform(IApduTransform nextTransform)
        {
            _nextTransform = nextTransform;
        }

        public ResponseApdu Invoke(CommandApdu command, Type commandType, Type responseType)
        {
            // If this is just a regular ReadStatusCommand, or it's a command that doesn't ask for a status response
            // in return, invoke the pipeline as usual.
            if (commandType == typeof(ReadStatusCommand) ||
                responseType != typeof(ReadStatusResponse))
            {
                return InvokeCommand(command, commandType, responseType);
            }

            // Otherwise we assume this to be a command that applies a config (and therefore looks for a status response).
            // In order to detect failures, we grab the status structure before applying said command so that we have a
            // sequence number to compare to.
            var beforeStatus = GetCurrentStatus();
            var responseApdu = InvokeCommand(command, commandType, responseType, out var afterStatus);

            try
            {
                // If we see the sequence number change, we can assume that the configuration was applied successfully.
                // Otherwise we just invent an error in the response.
                return IsValidSequenceProgression(beforeStatus, afterStatus)
                    ? responseApdu
                    : CreateFailedApdu(responseApdu.Data.ToArray());
            }
            catch (KeyboardConnectionException e)
            {
                _logger.LogWarning(e, "Handling keyboard connection exception. Translating to APDU response.");

                return CreateFailedApdu([]);
            }
        }

        public void Setup() => _nextTransform.Setup();

        public void Cleanup() => _nextTransform.Cleanup();

        // Internal for testing
        internal static bool IsValidSequenceProgression(OtpStatus beforeStatus, OtpStatus afterStatus)
        {
            byte before = beforeStatus.SequenceNumber;
            byte after = afterStatus.SequenceNumber;

            bool normalIncrement = after == before + 1;
            bool validReset = before > 0 && after == 0 &&
                afterStatus is { LongPressConfigured: false, ShortPressConfigured: false };

            return normalIncrement || validReset;
        }

        private ResponseApdu InvokeCommand(
            CommandApdu commandApdu,
            Type commandType,
            Type responseType,
            out OtpStatus afterStatus)
        {
            var responseApdu = _nextTransform.Invoke(commandApdu, commandType, responseType);
            afterStatus = ReadStatus(responseApdu);
            return responseApdu;
        }

        private ResponseApdu InvokeCommand(CommandApdu commandApdu, Type commandType, Type responseType) =>
            _nextTransform.Invoke(commandApdu, commandType, responseType);

        private OtpStatus GetCurrentStatus()
        {
            var command = new ReadStatusCommand();
            var responseApdu = _nextTransform.Invoke(
                command.CreateCommandApdu(),
                typeof(ReadStatusCommand),
                typeof(ReadStatusResponse));

            return command
                .CreateResponseForApdu(responseApdu)
                .GetData();
        }

        private static OtpStatus ReadStatus(ResponseApdu responseApdu)
        {
            var readStatusResponse = new ReadStatusResponse(responseApdu);
            var afterStatus = readStatusResponse.GetData();
            return afterStatus;
        }

        private static ResponseApdu CreateFailedApdu(byte[] data) => new(data, SWConstants.WarningNvmUnchanged);
    }
}
