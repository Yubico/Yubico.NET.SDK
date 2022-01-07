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
using Yubico.YubiKey.Piv.Objects;

namespace Yubico.YubiKey.Piv
{
    // This portion of the PivSession class contains code for dealing with
    // PIV Objects.
    public sealed partial class PivSession : IDisposable
    {
        /// <summary>
        /// Placeholder so links work.
        /// </summary>
        public T ReadObject<T>() where T : PivDataObject, new()
        {
            var returnValue = new T();
            returnValue.SetDataTag(returnValue.GetDefinedDataTag());

            return returnValue;
        }

        /// <summary>
        /// Placeholder so links work.
        /// </summary>
        public void WriteObject(PivDataObject pivDataObject)
        {
            if (pivDataObject is null)
            {
                throw new ArgumentNullException(nameof(pivDataObject));
            }

            throw new NotImplementedException();
        }
    }
}
