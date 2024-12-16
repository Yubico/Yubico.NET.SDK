// Copyright 2024 Yubico AB
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
using System.Globalization;

namespace Yubico.YubiKey.Scp
{
    /// <summary>
    /// Represents a reference to a cryptographic key stored on the YubiKey.
    /// </summary>
    public class KeyReference
    {
        /// <summary>
        /// The Key Id (KID) of the key.
        /// </summary>
        public byte Id { get; }

        /// <summary>
        /// The Key Version Number (KVN) of the key.
        /// </summary>
        public byte VersionNumber { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyReference"/> class.
        /// </summary>
        /// <param name="id">The ID of the key (KID). Accepted values depend on the usage. See <see cref="ScpKeyIds"/>
        /// for a list of possible Key Id's.</param>
        /// <param name="versionNumber">The version number of the key (KVN). Must be between 1 and 127.</param>
        /// <remarks>See the GlobalPlatform Technology Card Specification v2.3 Amendment F §5.1 Cryptographic Keys for more information on the available KIDs.</remarks>
        public KeyReference(
            byte id,
            byte versionNumber)
        {
            if (versionNumber > 127)
            {
                throw new ArgumentException(nameof(versionNumber), "Key version number (KVN) must be between 1 and 127");
            }

            if (id > 127)
            {
                throw new ArgumentException(nameof(id), "Key ID (KID) must be between 0 and 127");
            }

            Id = id;
            VersionNumber = versionNumber;
        }

        /// <summary>
        /// Returns a span of bytes that represent the key reference.
        /// </summary>
        public ReadOnlyMemory<byte> GetBytes => new[] { Id, VersionNumber };

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object in the format
        /// <para>"KeyRef[Kid=0x{Id:X2}, Kvn=0x{VersionNumber:X2}]"
        /// </para>
        /// </returns>
        /// <example>"KeyRef[Kid=0x01, Kvn=0x02]"</example>
        public override string ToString() => $"KeyRef[Kid=0x{Id:X2}, Kvn=0x{VersionNumber:X2}]";

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (KeyReference)obj;
            return Id == other.Id && VersionNumber == other.VersionNumber;
        }

        public override int GetHashCode() => HashCode.Combine(Id, VersionNumber);
    }
}
