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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// The response to the Echo Command.
    /// </summary>
    /// <remarks>
    /// This is the partner response class to <see cref="EchoCommand"/>.
    /// </remarks>
    public sealed class EchoResponse : U2fResponse, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        /// <summary>
        /// Constructs an EchoResponse from the given <see cref="ResponseApdu"/>.
        /// </summary>
        /// <param name="responseApdu">
        /// The response to a <see cref="EchoCommand"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="responseApdu"/> is `null`.
        /// </exception>
        public EchoResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the echoed data from the response.
        /// </summary>
        /// <remarks>
        /// If the status of the response is not 'Success', this method will throw
        /// an exception.
        /// </remarks>
        /// <returns>
        /// The data in the response APDU, as a byte array.
        /// </returns>
        public ReadOnlyMemory<byte> GetData() => Status switch
        {
            ResponseStatus.Success => ResponseApdu.Data,
            _ => throw new InvalidOperationException(StatusMessage),
        };
    }
}
