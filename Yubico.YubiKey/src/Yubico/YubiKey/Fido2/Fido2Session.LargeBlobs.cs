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
using System.Security;
using System.Threading.Tasks;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class deals with setting and getting
    // large blobs.
    public sealed partial class Fido2Session
    {
        /// <summary>
        /// Get the current <c>serializedLargeBlobArray</c> out of the YubiKey.
        /// </summary>
        public SerializedLargeBlobArray GetCurrentLargeBlobArray()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set the <c>serializedLargeBlobArray</c> in the YubiKey to contain the
        /// data in the input <c>largeBlobArray</c>.
        /// </summary>
        public void SetLargeBlobArray(SerializedLargeBlobArray largeBlobArray)
        {
            throw new NotImplementedException();
        }
    }
}
