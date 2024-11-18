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
using System.Diagnostics;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Scp.Commands
{
    internal class ScpResponse : YubiKeyResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScpResponse"/> class.
        /// </summary>
        /// <param name="responseApdu">The ResponseApdu from the YubiKey.</param>
        /// <exception cref="ArgumentNullException">responseApdu</exception>  
        public ScpResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        public void ThrowIfFailed(string? message = null, bool includeStatusWord = true)
        {
            switch (Status)
            {
                case ResponseStatus.Success:
                    Debug.Assert(Status == ResponseStatus.Success);
                    return;
                case ResponseStatus.Failed:
                case ResponseStatus.RetryWithTouch:
                case ResponseStatus.AuthenticationRequired:
                case ResponseStatus.ConditionsNotSatisfied:
                case ResponseStatus.NoData:
                default:
                    throw new SecureChannelException(
                        includeStatusWord
                            ? AddStatusWord(message ?? StatusMessage)
                            : message ?? StatusMessage);
            }

            string AddStatusWord(string originalMessage) => $"{originalMessage} (StatusWord: {StatusWord})";
        }
    }
}
