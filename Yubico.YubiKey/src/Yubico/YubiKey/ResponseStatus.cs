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

namespace Yubico.YubiKey
{
    /// <summary>
    /// An application independent way of reporting the status in a command's response.
    /// </summary>
    public enum ResponseStatus
    {
        /// <summary>
        /// The command succeeded.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The command failed to complete.
        /// </summary>
        Failed = 1,

        /// <summary>
        /// The command needs to be retried after the user touches the YubiKey.
        /// </summary>
        RetryWithTouch = 2,

        /// <summary>
        /// The command needed authentication, which had not been properly
        /// provided.
        /// </summary>
        AuthenticationRequired = 3,

        /// <summary>
        /// The command could not complete because some conditions were not
        /// satisfied.
        /// </summary>
        ConditionsNotSatisfied = 4,

        /// <summary>
        /// The command requested information of a YubiKey, but the data did not
        /// exist.
        /// </summary>
        /// <remarks>
        /// This response simply means that the requested data is not on the
        /// YubiKey. It does not even necessarily mean that the YubiKey will
        /// never have such data or does not support that data element.
        /// <p>
        /// For an example of how this value is used, see
        /// <see cref="Piv.Commands.GetDataResponse"/>.
        /// </p>
        /// </remarks>
        NoData = 5,
    }
}
