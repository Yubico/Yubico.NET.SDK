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
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Scp
{
    /// <summary>
    /// SCP key parameters for performing SCP11 authentication.
    /// For SCP11b only keyRef and pkSdEcka are required. Note that this does not authenticate the off-card entity.
    /// For SCP11a and SCP11c the off-card entity CA key reference must be provided, as well as the off-card entity secret key and certificate chain.
    /// </summary>
    public sealed class Scp11KeyParameters : ScpKeyParameters, IDisposable
    {
        private X509Certificate2[]? _oceCertificates;
        private bool _disposed;

        /// <summary>
        /// The public key of the Security Domain Elliptic Curve Key Agreement (ECKA) key (SCP11a/b/c). Required.
        /// <remarks>'pkSdEcka' is short for PublicKey SecurityDomain Elliptic Curve KeyAgreement (Key)</remarks>
        /// </summary>
        public ECPublicKeyParameters PkSdEcka { get; private set; }

        /// <summary>
        /// The key reference of the off-card entity (SCP11a/c). Optional.
        /// <remarks>'oceKeyReference' is short for Off-Card Entity Key Reference</remarks>
        /// </summary>
        public KeyReference? OceKeyReference { get; private set; }

        /// <summary>
        /// The private key of the off-card entity Elliptic Curve Key Agreement (ECKA) key (SCP11a/c). Optional.
        /// <remarks>'skOceEcka' is short for Secret Key Off-Card Entity Elliptic Curve KeyAgreement (Key)</remarks>
        /// </summary>
        public ECPrivateKeyParameters? SkOceEcka { get; private set; }

        /// <summary>
        /// The certificate chain, containing the public key for the off-card entity (SCP11a/c).
        /// </summary>
        public IReadOnlyList<X509Certificate2>? OceCertificates => _oceCertificates;

        /// <summary>
        /// Creates a new <see cref="Scp11KeyParameters"/> instance.
        /// This is used to initiate SCP11A and SCP11C connections.
        /// </summary>
        /// <param name="keyReference">The key reference.</param>
        /// <param name="pkSdEcka">The security domain elliptic curve key agreement key public key.</param>
        /// <param name="oceKeyReference">The off-card entity key reference. Optional.</param>
        /// <param name="skOceEcka">The off-card entity elliptic curve key agreement key private key. Optional.</param>
        /// <param name="certificates">The off-card entity certificate chain. Optional.</param>
        public Scp11KeyParameters(
            KeyReference keyReference,
            ECPublicKeyParameters pkSdEcka,
            KeyReference? oceKeyReference = null,
            ECPrivateKeyParameters? skOceEcka = null,
            IEnumerable<X509Certificate2>? certificates = null)
            : base(keyReference)
        {
            PkSdEcka = pkSdEcka;
            OceKeyReference = oceKeyReference;
            SkOceEcka = skOceEcka;
            _oceCertificates = certificates?.ToArray();

            ValidateParameters();
        }

        /// <summary>
        /// Creates a new <see cref="Scp11KeyParameters"/> instance for SCP11b.
        /// </summary>
        /// <param name="keyReference">The key reference.</param>
        /// <param name="pkSdEcka">The security domain elliptic curve key agreement key public key.</param>
        public Scp11KeyParameters(
            KeyReference keyReference,
            ECPublicKeyParameters pkSdEcka)
            : base(keyReference)
        {
            PkSdEcka = pkSdEcka;

            ValidateParameters();
        }


        private void ValidateParameters()
        {
            switch (KeyReference.Id)
            {
                case ScpKeyIds.Scp11B:
                    if (
                        OceKeyReference != null ||
                        SkOceEcka != null ||
                        OceCertificates?.Count > 0
                        )
                    {
                        throw new ArgumentException($"Cannot provide {nameof(OceKeyReference)}, {nameof(SkOceEcka)} or {nameof(OceCertificates)} for SCP11b");
                    }

                    break;
                case ScpKeyIds.Scp11A:
                case ScpKeyIds.Scp11C:
                    if (
                        OceKeyReference == null ||
                        SkOceEcka == null ||
                        OceCertificates?.Count == 0
                        )
                    {
                        throw new ArgumentException($"Must provide {nameof(OceKeyReference)}, {nameof(SkOceEcka)} or {nameof(OceCertificates)} for SCP11a/c");
                    }

                    break;
                default:
                    throw new ArgumentException("KID must be 0x11, 0x13, or 0x15 for SCP11");
            }
        }

        /// <summary>
        /// This will clear all references and sensitive buffers  
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CryptographicOperations.ZeroMemory(SkOceEcka?.Parameters.D);
            PkSdEcka = null!;
            OceKeyReference = null;
            SkOceEcka = null;
            _oceCertificates = null;
            _disposed = true;
        }
    }
}
