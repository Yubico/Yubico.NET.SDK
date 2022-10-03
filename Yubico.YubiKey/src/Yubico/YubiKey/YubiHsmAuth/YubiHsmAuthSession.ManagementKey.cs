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
using Yubico.YubiKey.YubiHsmAuth.Commands;

namespace Yubico.YubiKey.YubiHsmAuth
{
    // This portion of the YubiHSM Auth Session class contains operations
    // related to the management key
    public partial class YubiHsmAuthSession
    {
        /// <summary>
        /// Get the number of retries remaining for the management key.
        /// </summary>
        /// <remarks>
        /// When supplying the management key for an operation, there is a
        /// limit of 8 retries before the application is locked and must be
        /// completely reset. Supplying the correct management key before the
        /// application is locked will reset the retry counter to 8.
        /// </remarks>
        /// <returns>
        /// The number of retries, as an integer.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The command to retrieve the number of retries failed.
        /// </exception>
        public int GetManagementKeyRetries()
        {
            GetManagementKeyRetriesResponse retryCountResponse =
                Connection.SendCommand(new GetManagementKeyRetriesCommand());

            if (retryCountResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(retryCountResponse.StatusMessage);
            }

            return retryCountResponse.GetData();
        }
    }
}
