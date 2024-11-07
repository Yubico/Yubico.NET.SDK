﻿// Copyright 2024 Yubico AB
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
using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage("Style", "IDE0032:Use auto property")]
    public sealed class Scp11KeyParameters : ScpKeyParameters, IDisposable
    {
        private KeyReference? _oceKeyReference;
        private ECPublicKeyParameters _pkSdEcka;
        private ECPrivateKeyParameters? _skOceEcka;
        private X509Certificate2[]? _certificates;
        private bool _disposed;

        /// <summary>
        /// The public key of the Security Domain Elliptic Curve Key Agreement (ECKA) key.
        /// <remarks>pkSdEcka is short for PublicKey SecurityDomain Elliptic Curve KeyAgreement Key</remarks>
        /// </summary>
        public ECPublicKeyParameters PkSdEcka => _pkSdEcka; 

        /// <summary>
        /// The key reference of the off-card entity. Optional.
        /// <remarks>oceKeyReference is short for Off-Card Entity Key Reference</remarks>
        /// </summary>
        public KeyReference? OceKeyReference => _oceKeyReference;

        /// <summary>
        /// The private key of the off-card entity Elliptic Curve Key Agreement (ECKA) key. Optional.
        /// <remarks>skOceEcka is short for Secret Key Off-Card Entity Elliptic Curve KeyAgreement Key</remarks>
        /// </summary>
        public ECPrivateKeyParameters? SkOceEcka => _skOceEcka;

        /// <summary>
        /// The certificate chain for the off-card entity. This is used for SCP11a and SCP11c. Optional. //TODO Clarify which ones are for SCP11a and which ones are for SCP11c and which ones are for SCP11b
        /// </summary>
        public IReadOnlyList<X509Certificate2>? Certificates => _certificates;

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
            _pkSdEcka = pkSdEcka;
            _oceKeyReference = oceKeyReference;
            _skOceEcka = skOceEcka;
            _certificates = certificates?.ToArray();

            ValidateParameters();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scp11KeyParameters"/> class
        /// with the specified key reference and security domain elliptic curve key agreement key public key.
        /// This is used to create SCP11B connections which are authenticated connections.
        /// </summary>
        /// <param name="keyReference">The key reference.</param>
        /// <param name="pkSdEcka">The security domain elliptic curve key agreement key public key.</param>
        public Scp11KeyParameters(KeyReference keyReference, ECPublicKeyParameters pkSdEcka)
            : this(keyReference, pkSdEcka, null, null, null)
        {
            
        }

        private void ValidateParameters()
        {
            switch (KeyReference.Id)
            {
                case ScpKid.Scp11b:
                    if (
                        OceKeyReference != null ||
                        SkOceEcka != null ||
                        Certificates?.Count > 0
                        )
                    {
                        throw new ArgumentException("Cannot provide oceKeyRef, skOceEcka or certificates for SCP11b");
                    }

                    break;
                case ScpKid.Scp11a:
                case ScpKid.Scp11c:
                    if (
                        OceKeyReference == null ||
                        SkOceEcka == null ||
                        Certificates?.Count == 0
                        )
                    {
                        throw new ArgumentException("Must provide oceKeyRef, skOceEcka or certificates for SCP11a/c");
                    }

                    break;
                default:
                    throw new ArgumentException("KID must be 0x11, 0x13, or 0x15 for SCP11");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                CryptographicOperations.ZeroMemory(_skOceEcka?.Parameters.D);
                _pkSdEcka = null!;
                _oceKeyReference = null;
                _skOceEcka = null;
                _certificates = Array.Empty<X509Certificate2>();

                _disposed = true;
            }
        }
    }
}
