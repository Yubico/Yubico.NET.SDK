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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands;

/// <summary>
///     The response class for changing the management key.
/// </summary>
/// <remarks>
///     <para>
///         If authentication failed, the <see cref="YubiKeyResponse.Status" />
///         will be set to
///         <see cref="ResponseStatus.AuthenticationRequired" /> and
///         <see cref="BaseYubiHsmAuthResponseWithRetries.RetriesRemaining" />
///         will contain the number of retries remaining for the management key.
///     </para>
///     <para>
///         The associated command class is <see cref="ChangeManagementKeyCommand" />.
///     </para>
/// </remarks>
public class ChangeManagementKeyResponse : BaseYubiHsmAuthResponseWithRetries
{
    /// <summary>
    ///     Constructs a ChangeManagementKeyResponse based on a ResponseApdu
    ///     received from the YubiKey.
    /// </summary>
    /// <param name="responseApdu">
    ///     The ResponseApdu returned by the YubiKey.
    /// </param>
    public ChangeManagementKeyResponse(ResponseApdu responseApdu) :
        base(responseApdu)
    {
    }
}
