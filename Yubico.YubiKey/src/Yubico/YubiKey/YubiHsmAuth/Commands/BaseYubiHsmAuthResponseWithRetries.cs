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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    /// The base class of YubiHSM Auth response types that are paired with
    /// commands that require authentication.
    /// </summary>
    /// <remarks>
    /// Some commands require authentication with either the management key
    /// or a credential password. There is a limit to the number of retries
    /// that are allowed. In the event that authentication fails, this class
    /// will return the number of retries remaining.
    /// </remarks>
    public abstract class BaseYubiHsmAuthResponseWithRetries : BaseYubiHsmAuthResponse
    {
        /// <summary>
        /// When the retry count is present in the status word, the position
        /// of the retry count is in the last four bits of the status word.
        /// </summary>
        private const short _retriesMask = 0x000f;

        /// <summary>
        /// Checks whether the status word contains a retry count.
        /// </summary>
        /// <remarks>
        /// Use <see cref="RetriesRemaining"/> to get the value.
        /// </remarks>
        protected bool StatusWordContainsRetries => (StatusWord & ~_retriesMask) == SWConstants.VerifyFail;

        /// <summary>
        /// The number of retries remaining after failing to authenticate.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the command failed to authenticate, the
        /// <see cref="YubiKeyResponse.Status"/> is set to
        /// <see cref="ResponseStatus.AuthenticationRequired"/>, and this
        /// property returns the number of retries remaining. Otherwise this
        /// property returns null.
        /// </para>
        /// <para>
        /// This property represents the retries remaining on either the
        /// management key or a credential password. Refer to the implementing
        /// class's documentation for more information.
        /// </para>
        /// </remarks>
        public int? RetriesRemaining => StatusWordContainsRetries ? (int?)(StatusWord & _retriesMask) : null;

        /// <inheritdoc/>
        protected override ResponseStatusPair StatusCodeMap
        {
            get
            {
                // Add special handling for the situation where the status word also contains the retry count
                if (StatusWordContainsRetries)
                {
                    return new ResponseStatusPair(ResponseStatus.AuthenticationRequired,
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ResponseStatusMessages.YubiHsmAuthAuthenticationRequired,
                                RetriesRemaining));
                }
                else
                {
                    return base.StatusCodeMap;
                }
            }
        }

        protected BaseYubiHsmAuthResponseWithRetries(ResponseApdu responseApdu) : base(responseApdu)
        {
        }
    }
}
