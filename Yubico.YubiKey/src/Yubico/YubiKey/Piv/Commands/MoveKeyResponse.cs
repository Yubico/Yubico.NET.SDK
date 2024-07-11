// Copyright 2024 Yubico AB
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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// The <see cref="MoveKeyResponse"/> for the corresponding <see cref="MoveKeyCommand"/>
    /// <seealso cref="PivResponse"/>
    /// <seealso cref="YubiKeyResponse"/>
    /// </summary>
    public class MoveKeyResponse : PivResponse
    {
        /// <summary>
        /// The constructor for the <see cref="MoveKeyResponse"/>
        /// </summary>
        /// <param name="responseApdu">The return data with which the Yubikey responded
        /// to the <see cref="MoveKeyCommand"/></param>
        /// <seealso cref="MoveKeyCommand"/>
        public MoveKeyResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
        }
    }
}
