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
    /// For SCP11b only keyReference and pkSdEcka are required.
    /// Note that this does not authenticate the off-card entity (OCE).
    /// For SCP11a and SCP11c the off-card entity (OCE) CA key reference must be provided,
    /// as well as the off-card entity (OCE) secret key and certificate chain.
    /// </summary>
    public sealed class Scp11KeyParameters : ScpKeyParameters, IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// The public key of the security domain which is used for key agreement between the off-card entity (OCE) and Yubikey (SCP11a/b/c).
        /// <remarks>'pkSdEcka' is short for Public Key Security Domain Elliptic Curve Key Agreement (Key)</remarks>
        /// </summary>
        public ECPublicKey PkSdEcka { get; private set; }

        /// <summary>
        /// The key reference of the off-card entity (OCE) (SCP11a/c).
        /// </summary>
        public KeyReference? OceKeyReference { get; private set; }
        
        /// <summary>
        /// The secret key of the off-card entity (OCE) which is used for key agreement between the off-card entity and the YubiKey (SCP11a/c).
        /// <remarks>'skOceEcka' is short for Secret Key Off-Card Entity Elliptic Curve Key Agreement (Key)</remarks>
        /// </summary>
        public ECPrivateKey? SkOceEcka { get; private set; }

        /// <summary>
        /// The certificate chain, containing the public key for the off-card entity (OCE) (SCP11a/c).
        /// </summary>
        public IReadOnlyList<X509Certificate2>? OceCertificates { get; private set; }

        /// <summary>
        /// Creates a new <see cref="Scp11KeyParameters"/> instance for SCP11b.
        /// </summary>
        /// <remarks>
        /// Note that this does not authenticate the off-card entity (OCE).
        /// </remarks>
        /// <param name="keyReference">The key reference associated with the key parameters.</param>
        /// <param name="pkSdEcka">The public key of the security domain which is used for key agreement between the off-card entity (OCE) and Yubikey.</param>
        public Scp11KeyParameters(KeyReference keyReference, ECPublicKey pkSdEcka) : base(keyReference)
        {
            if (keyReference.Id != ScpKeyIds.Scp11B)
            {
                throw new ArgumentException(
                    $"The KeyReference.Id (KID) must be 0x{ScpKeyIds.Scp11B:X2} for SCP11b", nameof(keyReference));
            }

            PkSdEcka = pkSdEcka;
        }
        
        /// <summary>
        /// Creates a new <see cref="Scp11KeyParameters"/> instance.
        /// This is used to initiate SCP11A and SCP11C connections.
        /// </summary>
        /// <param name="keyReference">The key reference associated with the key parameters.</param>
        /// <param name="pkSdEcka">The public key of the security domain (pkSdEcka) which is used for key agreement between the off-card entity (OCE) and Yubikey.</param>
        /// <param name="oceKeyReference">The off-card entity (OCE) key reference.</param>
        /// <param name="skOceEcka">The secret key (skOceEcka) of the off-card entity (OCE) used for key agreement between the off-card entity (OCE) and Yubikey.</param>
        /// <param name="oceCertificates">The certificate chain, containing the public key for the off-card entity (OCE).</param>
        public Scp11KeyParameters(
            KeyReference keyReference,
            ECPublicKey pkSdEcka,
            KeyReference oceKeyReference,
            ECPrivateKey skOceEcka,
            IReadOnlyCollection<X509Certificate2> oceCertificates) : base(keyReference)
        {
            const byte validKidsMask = ScpKeyIds.Scp11A | ScpKeyIds.Scp11C;
            if ((keyReference.Id & validKidsMask) != keyReference.Id)
            {
                throw new ArgumentException(
                    $"The KeyReference.Id (KID) must be 0x{ScpKeyIds.Scp11A:X2}, or 0x{ScpKeyIds.Scp11C:X2} for SCP11a/c",
                    nameof(keyReference));
            }

            if (oceCertificates.Count == 0)
            {
                throw new ArgumentException("Must provide a certificate chain for SCP11a/c", nameof(oceCertificates));
            }

            PkSdEcka = pkSdEcka;
            OceKeyReference = oceKeyReference;
            SkOceEcka = skOceEcka;
            OceCertificates = oceCertificates.ToList();
        }
        
        [Obsolete("Obsolete, use constructor with ECPrivateKey instead", false)]
        public Scp11KeyParameters(
            KeyReference keyReference,
            ECPublicKeyParameters pkSdEcka,
            KeyReference oceKeyReference,
            ECPrivateKeyParameters skOceEcka,
            IReadOnlyCollection<X509Certificate2> oceCertificates) 
            : this(keyReference, pkSdEcka as ECPublicKey, oceKeyReference, skOceEcka as ECPrivateKey, oceCertificates)
        {

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
            OceCertificates = null;
            _disposed = true;
        }
    }
}
