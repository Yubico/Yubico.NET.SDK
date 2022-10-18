// Copyright 2022 Yubico AB
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
using System.Security;
using System.Threading.Tasks;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class deals with asserting (authenticating)
    // a credential stored on the YubiKey.
    public sealed partial class Fido2Session
    {
        /// <summary>
        /// Gets one or more assertions for a particular relying party.
        /// </summary>
        /// <remarks>
        /// Detailed information about the parameters structure and its expected values can be found on the
        /// <see cref="GetAssertionParameters"/> page.
        /// </remarks>
        /// <param name="parameters">
        /// A fully populated <see cref="GetAssertionParameters"/> structure that
        /// follows all of the rules set forth by that object.
        /// </param>
        /// <returns>
        /// A collection of objects that contain the credential assertion and supporting data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="parameters"/> argument was null.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The YubiKey has not been authenticated. Call <see cref="VerifyPin"/> or <see cref="VerifyUv"/> before
        /// calling this method.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The YubiKey either required touch for a user presence check or a biometric touch for user authentication.
        /// The YubiKey timed out waiting for this action to be performed.
        /// </exception>
        public ICollection<GetAssertionData> GetAssertion(GetAssertionParameters parameters)
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (AuthToken.HasValue == false)
            {
                throw new SecurityException(ExceptionMessages.Fido2NotAuthed);
            }

            parameters.Protocol = AuthProtocol.Protocol;

            parameters.PinUvAuthParam = AuthProtocol.AuthenticateUsingPinToken(
                AuthToken.Value.ToArray(),
                parameters.ClientDataHash.ToArray());

            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.TouchRequest
            };

            try
            {
                var touchNotifyTask = Task.Run(() => keyCollector(keyEntryData));
                var getAssertionTask = Task.Run(() => Connection.SendCommand(new GetAssertionCommand(parameters)));

                // Ideally, we'd like the UI to be able to cancel this operation, but there's no good way
                // of doing this. The YubiKey blocks until UP has been satisfied or it times out. As far
                // as I can see, there is no way to tell the YubiKey "stop". While we could let the thread
                // run out in the background, the YubiKey would also not be able to process other operations.
                // So doing so might cause weird failures and timeouts in other areas of the code. Better
                // to just wait until timeout no matter what.
                int completedTask = Task.WaitAny(touchNotifyTask, getAssertionTask);

                if (completedTask == 0)
                {
                    if (touchNotifyTask.Result == false)
                    {
                        return new List<GetAssertionData>();
                    }

                    getAssertionTask.Wait();
                }

                GetAssertionResponse response = getAssertionTask.Result;

                if (GetCtapError(response) == CtapStatus.OperationDenied
                    || GetCtapError(response) == CtapStatus.ActionTimeout)
                {
                    throw new TimeoutException(ExceptionMessages.Fido2NotAuthed);
                }

                GetAssertionData getAssertionData = response.GetData();
                int numberOfCredentials = getAssertionData.NumberOfCredentials ?? 1;

                var assertions = new List<GetAssertionData>(numberOfCredentials) { getAssertionData };

                for (int i = 1; i < getAssertionData.NumberOfCredentials; i++)
                {
                    response = Connection.SendCommand(new GetNextAssertionCommand());
                    assertions.Add(response.GetData());
                }

                return assertions;
            }
            finally
            {
                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }
        }
    }
}
