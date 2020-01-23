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
using System.Collections.Generic;
using System.Text;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.InterIndustry.Commands
{
    /// <summary>
    /// Response from SelectApplication command.
    /// </summary>
    public class GenericSelectResponse : YubiKeyResponse, ISelectApplicationResponse<GenericSelectApplicationData>
    {
        private readonly ReadOnlyMemory<byte> _rawData;

        /// <summary>
        /// Constructs an instance of the <see cref="GenericSelectResponse" /> class.
        /// </summary>
        public GenericSelectResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
            _rawData = responseApdu.Data;
        }

        /// <inheritdoc/>
        public GenericSelectApplicationData GetData() => new GenericSelectApplicationData(_rawData);
    }
}
