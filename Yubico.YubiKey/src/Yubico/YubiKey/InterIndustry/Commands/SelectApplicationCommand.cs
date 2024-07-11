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

using System;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.InterIndustry.Commands
{
    /// <summary>
    /// Selects a smart card application.
    /// </summary>
    public class SelectApplicationCommand : BaseSelectApplicationCommand<GenericSelectResponse>
    {
        /// <summary>
        /// Select Application using its raw application Id.  This is for advanced scenarios only.
        /// </summary>
        /// <param name="applicationId">ID of the Application</param>
        public SelectApplicationCommand(byte[] applicationId) : base(applicationId)
        {
        }

        /// <summary>
        /// Constructs an instance of the <see cref="SelectApplicationCommand" /> class.
        /// </summary>
        /// <param name="yubiKeyApplication">Application</param>
        public SelectApplicationCommand(YubiKeyApplication yubiKeyApplication) : base(yubiKeyApplication)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="responseApdu"></param>
        /// <returns></returns>
        public override GenericSelectResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GenericSelectResponse(responseApdu);
    }
}
