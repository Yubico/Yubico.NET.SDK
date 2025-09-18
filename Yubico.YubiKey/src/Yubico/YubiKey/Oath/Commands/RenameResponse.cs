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

namespace Yubico.YubiKey.Oath.Commands;

/// <summary>
///     The response to the <see cref="RenameCommand" /> command.
/// </summary>
public class RenameResponse : OathResponse
{
    /// <summary>
    ///     Constructs a RenameResponse instance based on a
    ///     ResponseApdu received from the YubiKey.
    /// </summary>
    /// <param name="responseApdu">
    ///     The ResponseApdu returned by the YubiKey.
    /// </param>
    public RenameResponse(ResponseApdu responseApdu) :
        base(responseApdu)
    {
    }

    /// <inheritdoc />
    protected override ResponseStatusPair StatusCodeMap =>
        StatusWord switch
        {
            OathSWConstants.NoSuchObject => new ResponseStatusPair(
                ResponseStatus.NoData, ResponseStatusMessages.OathNoSuchObject),
            _ => base.StatusCodeMap
        };
}
