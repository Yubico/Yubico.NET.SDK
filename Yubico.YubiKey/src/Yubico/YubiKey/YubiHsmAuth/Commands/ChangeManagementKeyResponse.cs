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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    /// The response class for changing the management key.
    /// </summary>
    /// <remarks>
    /// The partner class is <see cref="ChangeManagementKeyCommand"/>.
    /// </remarks>
    public class ChangeManagementKeyResponse : BaseYubiHsmAuthResponse
    {
        /// <summary>
        /// Constructs a ChangeManagementKeyResponse based on a ResponseApdu
        /// received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public ChangeManagementKeyResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }
    }
}
