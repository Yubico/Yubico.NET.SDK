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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    /// The response class to retrieve session keys for establishing
    /// a secure connection with a YubiHSM 2 device.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If authentication with the credential's password failed, the
    /// <see cref="YubiKeyResponse.Status"/> will be set to
    /// <see cref="ResponseStatus.AuthenticationRequired"/> and
    /// <see cref="BaseYubiHsmAuthResponseWithRetries.RetriesRemaining"/>
    /// will contain the number of retries remaining for the credential's
    /// password.
    /// </para>
    /// <para>
    /// A credential may require the user to touch the YubiKey as part of the
    /// authentication procedure (see <see cref="Credential.TouchRequired"/>).
    /// If this proof of user presence fails, the
    /// <see cref="YubiKeyResponse.Status"/> will be set to
    /// <see cref="ResponseStatus.RetryWithTouch"/>, and the credential password
    /// retry count is not changed.
    /// </para>
    /// <para>
    /// The associated command class is <see cref="GetAes128SessionKeysCommand"/>.
    /// </para>
    /// </remarks>
    public class GetAes128SessionKeysResponse : BaseYubiHsmAuthResponseWithRetries,
                                                IYubiKeyResponseWithData<SessionKeys>
    {
        private const int encStart = 0;
        private const int macStart = 16;
        private const int rmacStart = 32;

        private const int keyLength = 16;
        private const int expectedDataLength = 48;

        /// <summary>
        /// Constructs a GetSessionKeysResponse based on a ResponseApdu
        /// received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public GetAes128SessionKeysResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Get the keys to create a secure session with a YubiHSM 2 device.
        /// </summary>
        /// <remarks>
        /// If the method cannot return the data, it will throw an exception.
        /// This happens when the <see cref="IYubiKeyResponse.Status"/>
        /// property indicates an error, or the data returned from the YubiKey
        /// was malformed or incomplete.
        /// </remarks>
        /// <returns>
        /// Session keys are used to establish an encrypted and authenticated
        /// session with a YubiHSM 2 device. The secure session is based on the
        /// Global Platform Secure Channel Protocol '03' (SCP03).
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="IYubiKeyResponse.Status"/> is not equal to
        /// <see cref="ResponseStatus.Success"/>.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// Invalid response data length.
        /// </exception>
        public SessionKeys GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            if (ResponseApdu.Data.Length != expectedDataLength)
            {
                throw new MalformedYubiKeyResponseException();
            }

            SessionKeys keys = new SessionKeys(
                ResponseApdu.Data.Slice(encStart, keyLength),
                ResponseApdu.Data.Slice(macStart, keyLength),
                ResponseApdu.Data.Slice(rmacStart, keyLength));

            return keys;
        }
    }
}
