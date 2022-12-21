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
using System.Globalization;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The response to the <see cref="GetLargeBlobCommand"/> command, returning
    /// the large blob.
    /// </summary>
    public sealed class SetLargeBlobResponse : Fido2Response, IYubiKeyResponse
    {
        /// <summary>
        /// Constructs a <c>SetLargeBlobResponse</c> instance based on a ResponseApdu
        /// received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public SetLargeBlobResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }
    }
}

