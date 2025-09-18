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
using Yubico.YubiKey.Oath.Commands;

namespace Yubico.YubiKey.Pipelines;

/// <summary>
///     An OATH-specific transform that automatically detects large responses
///     and issues GET_RESPONSE APDUs until all data has been returned.
/// </summary>
internal class OathResponseChainingTransform : ResponseChainingTransform
{
    public OathResponseChainingTransform(IApduTransform pipeline) : base(pipeline)
    {
    }

    protected override IYubiKeyCommand<YubiKeyResponse> CreateGetResponseCommand(
        CommandApdu originatingCommand,
        short SW2) =>
        new GetResponseCommand(originatingCommand, SW2);
}
