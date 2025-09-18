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
///     The response class for resetting the YubiHSM Auth application.
/// </summary>
/// <remarks>
///     The partner class is <see cref="ResetApplicationCommand" />.
/// </remarks>
public class ResetApplicationResponse : BaseYubiHsmAuthResponse
{
    /// <summary>
    ///     Constructs an ResetApplicationResponse based on a ResponseApdu
    ///     received from the YubiKey.
    /// </summary>
    /// <param name="responseApdu">
    ///     The ResponseApdu returned by the YubiKey.
    /// </param>
    public ResetApplicationResponse(ResponseApdu responseApdu) :
        base(responseApdu)
    {
    }
}
