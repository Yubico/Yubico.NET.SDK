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

namespace Yubico.YubiKey
{
    /// <summary>
    /// Represents a connection to a YubiKey device, enabling the sending of commands and retrieval of responses.
    /// </summary>
    public interface IYubiKeyConnection : IDisposable
    {
        /// <summary>
        /// Sends a command to the YubiKey device and returns the response.
        /// </summary>
        /// <typeparam name="TResponse">The type of response expected from the YubiKey device.</typeparam>
        /// <param name="yubiKeyCommand">The command to be sent to the YubiKey device.</param>
        /// <returns>The response received from the YubiKey device.</returns>
        TResponse SendCommand<TResponse>(IYubiKeyCommand<TResponse> yubiKeyCommand)
            where TResponse : IYubiKeyResponse;

        /// <summary>
        /// An object representing the response received from the YubiKey after selecting the application.  
        /// </summary>
        InterIndustry.Commands.ISelectApplicationData? SelectApplicationData { get; set; }
    }
}
