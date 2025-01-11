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

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    /// This class represents a <see cref="YubiHsmAuth.Credential"/> stored in the YubiKey's
    /// YubiHSM Auth application, and the number of retries remaining.
    /// </summary>
    /// <remarks>
    /// This class is used in <see cref="Commands.ListCredentialsResponse"/>.
    /// </remarks>
    public class CredentialRetryPair
    {
        /// <summary>
        /// The Credential stored in the YubiHSM Auth application.
        /// </summary>
        public Credential Credential { get; }

        /// <summary>
        /// The number of retries remaining to access the Credential.
        /// </summary>
        public int Retries { get; }

        /// <summary>
        /// Constructs an instance of the <see cref="CredentialRetryPair"/> class.
        /// </summary>
        /// <param name="credential">
        /// <inheritdoc cref="Credential" path="/summary"/>
        /// </param>
        /// <param name="retries">
        /// <inheritdoc cref="Retries" path="/summary"/> Must be a non-negative value.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="retries"/> is negative.
        /// </exception>
        public CredentialRetryPair(Credential credential, int retries)
        {
            Credential = credential;

            if (retries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retries), ExceptionMessages.RetryCountNegative);
            }
            else
            {
                Retries = retries;
            }
        }
    }
}
