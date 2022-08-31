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
    /// The response to the <see cref="GetApplicationVersionCommand"/> command,
    /// containing the version of the YubiHSM Auth application as a major,
    /// minor, and patch value.
    /// </summary>
    public class GetApplicationVersionResponse : BaseYubiHsmAuthResponse, IYubiKeyResponseWithData<ApplicationVersion>
    {
        public GetApplicationVersionResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
        }

        public ApplicationVersion GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            ReadOnlySpan<byte> versionData = ResponseApdu.Data.Span;

            ApplicationVersion version = new ApplicationVersion()
            {
                Major = versionData[0],
                Minor = versionData[1],
                Patch = versionData[2],
            };

            return version;
        }
    }
}
