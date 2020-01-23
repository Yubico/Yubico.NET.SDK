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
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Represents a 'packed' format CTAP2 attestation, containing an algorithm, signature, and certificate(s).
    /// </summary>
    [CborSerializable]
    internal sealed class PackedAttestation : IDisposable
    {
        /// <summary>
        /// A COSEAlgorithmIdentifier containing the identifier of the algorithm used to generate the attestation signature.
        /// </summary>
        [CborPropertyName("alg")]
        public CoseAlgorithmIdentifier Algorithm { get; set; }

        /// <summary>
        /// A byte string containing the attestation signature.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        [CborPropertyName("sig")]
        public byte[] Signature { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The elements of this array contain the attestation certificate and optionally its certificate chain, each encoded in X.509 format. The attestation certificate is always the first element in the array.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        [CborPropertyName("x5c")]
        public X509Certificate2[] X509Certificates { get; set; } = Array.Empty<X509Certificate2>();

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (X509Certificate2 certificate in X509Certificates)
                    {
                        certificate.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose() =>
            Dispose(true);
        #endregion
    }
}
