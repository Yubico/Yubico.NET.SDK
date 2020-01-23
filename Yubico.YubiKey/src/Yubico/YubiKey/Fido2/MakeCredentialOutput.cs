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
    /// Data returned by a FIDO2 MakeCredential operation.
    /// </summary>
    /// <typeparam name="TAttestation">The type of attestation format returned for this MakeCredential operation.</typeparam>
    [CborSerializable]
    internal sealed class MakeCredentialOutput<TAttestation> : IMakeCredentialOutput, IDisposable where TAttestation : class, IDisposable, new()
    {
        /// <inheritdoc/>
        [CborLabelId(0x01)]
        public string AttestationFormatIdentifier { get; set; } = string.Empty;

        /// <inheritdoc/>
        [CborLabelId(0x02)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public byte[] AuthenticatorData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The attesation data.
        /// </summary>
        [CborLabelId(0x03)]
        public TAttestation AttestationStatement { get; set; } = new TAttestation();

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AttestationStatement.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() =>
            Dispose(true);
        #endregion
    }
}
