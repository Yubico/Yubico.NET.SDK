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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// The response to the U2F Set Legacy Device Config command.
    /// </summary>
    /// <remarks>
    /// This is the partner response class to <see cref="SetLegacyDeviceConfigCommand"/>.
    /// <para>
    /// After executing the <c>SetLegacyDeviceConfigCommand</c>, the result is an
    /// instance of this class. There is no data to return. Simply check the
    /// <c>Status</c> property. If it is <c>ResponseStatus.Success</c> the
    /// command succeeded.
    /// </para>
    /// </remarks>
    public sealed class SetLegacyDeviceConfigResponse : YubiKeyResponse, IYubiKeyResponse
    {
        /// <summary>
        /// Constructs a SetLegacyDeviceConfigResponse from the given ResponseApdu.
        /// </summary>
        /// <param name="responseApdu">The response to a
        /// <see cref="SetLegacyDeviceConfigCommand"/>.
        /// </param>
        public SetLegacyDeviceConfigResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }
    }
}
