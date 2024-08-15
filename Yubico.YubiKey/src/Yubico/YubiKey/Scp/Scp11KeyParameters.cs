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

namespace Yubico.YubiKey.Scp
{
    /// <summary>
    /// SCP key parameters for performing SCP11 authentication.
    /// For SCP11b only keyRef and pkSdEcka are required. Note that this does not authenticate the off-card entity.
    /// For SCP11a and SCP11c the off-card entity CA key reference must be provided, as well as the off-card entity secret key and certificate chain.
    /// </summary>
    public class Scp11KeyParameters : ScpKeyParameters
    {
        public ECParameters SecurityDomainEllipticCurveKeyAgreementKeyPublicKey { get; } // TODO Add docs
        public KeyReference? OffCardEntityKeyReference { get; }
        public ECParameters? OffCardEntityEllipticCurveAgreementPrivateKey { get; }
        public IReadOnlyList<X509Certificate2> Certificates { get; }

        public Scp11KeyParameters(
            KeyReference keyReference,
            ECParameters pkSdEcka,
            KeyReference? oceKeyReference = null,
            ECParameters? skOceEcka = null,
            IEnumerable<X509Certificate2>? certificates = null)
            : base(keyReference)
        {
            
            SecurityDomainEllipticCurveKeyAgreementKeyPublicKey = pkSdEcka;
            OffCardEntityKeyReference = oceKeyReference;
            OffCardEntityEllipticCurveAgreementPrivateKey = skOceEcka;
            Certificates = certificates?.ToList() ?? new List<X509Certificate2>();

            ValidateParameters();
        }

        public Scp11KeyParameters(KeyReference keyReference, ECParameters pkSdEcka)
            : this(keyReference, pkSdEcka, null, null, null)
        {
            
        }

        private void ValidateParameters()
        {
            switch (KeyReference.Id)
            {
                case ScpKid.Scp11b:
                    if (
                        OffCardEntityKeyReference != null ||
                        OffCardEntityEllipticCurveAgreementPrivateKey != null ||
                        Certificates.Count > 0
                        )
                    {
                        throw new ArgumentException("Cannot provide oceKeyRef, skOceEcka or certificates for SCP11b");
                    }

                    break;
                case ScpKid.Scp11a:
                case ScpKid.Scp11c:
                    if (
                        OffCardEntityKeyReference == null ||
                        OffCardEntityEllipticCurveAgreementPrivateKey == null ||
                        Certificates.Count == 0
                        )
                    {
                        throw new ArgumentException("Must provide oceKeyRef, skOceEcka or certificates for SCP11a/c");
                    }

                    break;
                default:
                    throw new ArgumentException("KID must be 0x11, 0x13, or 0x15 for SCP11");
            }
        }
    }
}
