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
    public class KeyReference
    {
        public byte Id { get; }
        public byte VersionNumber { get; }

        public KeyReference(
            byte id,
            byte versionNumber
            )
        {
            Id = id;
            VersionNumber = versionNumber;
        }


        public ReadOnlySpan<byte> GetBytes => new[] { Id, VersionNumber }.AsSpan();

        public override string ToString() => $"KeyRef[Kid=0x{Id:X2}, Kvn=0x{VersionNumber:X2}";

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
