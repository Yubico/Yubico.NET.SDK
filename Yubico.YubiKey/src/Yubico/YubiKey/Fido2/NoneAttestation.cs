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
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Represents a 'none' format CTAP2 attestation, containing no attestation information.
    /// </summary>
    /// <remarks>
    /// This is serialized as an empty CBOR map.
    /// </remarks>
    [CborSerializable]
    internal sealed class NoneAttesation : IDisposable
    {
        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        void Dispose(bool _)
        {
            if (!disposedValue)
            {
                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);
        #endregion
    }
}
