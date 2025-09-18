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
using Yubico.YubiKey.InterIndustry.Commands;

namespace Yubico.YubiKey.Oath.Commands;

/// <summary>
///     Selects an OATH application.
/// </summary>
public class SelectOathCommand : BaseSelectApplicationCommand<SelectOathResponse>
{
    /// <summary>
    ///     Constructs an instance of the <see cref="SelectOathCommand" /> class.
    /// </summary>
    public SelectOathCommand() : base(YubiKeyApplication.Oath)
    {
    }

    /// <inheritdoc />
    public override SelectOathResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);
}
