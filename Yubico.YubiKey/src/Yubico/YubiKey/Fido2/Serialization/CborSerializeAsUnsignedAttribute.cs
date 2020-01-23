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

namespace Yubico.YubiKey.Fido2.Serialization
{
    /// <summary>
    /// Marks the property as encoded in CBOR using an unsigned major type.
    /// </summary>
    /// <remarks>
    /// For CLR compatibility reasons, all integers in public interfaces are signed.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class CborSerializeAsUnsignedAttribute : Attribute
    {
        public CborSerializeAsUnsignedAttribute()
        {

        }
    }
}
