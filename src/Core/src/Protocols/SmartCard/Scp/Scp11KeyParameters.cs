// Copyright (C) 2025 Yubico.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

/// <summary>
///     Carries key and certificate inputs for SCP11a, SCP11b, or SCP11c setup.
/// </summary>
/// <remarks>
///     Supply inputs compatible with Core's fixed P-256/AES-128 setup path. Constructor shape checks do not establish
///     curve compatibility, certificate trust, validity, order, or key association. The caller selects a trusted
///     Security Domain public key and supplies the OCE chain in device-required order; Core sends that chain,
///     and the Security Domain checks it under its provisioning policy.
///     The parameters retain the exact key and certificate objects, copying only the certificate collection.
///     Dispose releases the private key and certificates, not the public key. Record with-clones share these
///     owners. Dispose sessions before parameters, including Security Domain sessions that reuse them on reset.
///     Setup receipt verification is distinct from applet authorization. In the inspected SCP11 1.3.0.13
///     Public Review (August 2023), §4.3, a/c host authentication also requires the first correct-MAC APDU
///     after establishment. This implementation is not a statement of verified final 1.4 profile conformance.
/// </remarks>
public sealed record Scp11KeyParameters : ScpKeyParameters
{
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Scp11KeyParameters" /> record for SCP11a or SCP11c.
    /// </summary>
    /// <param name="keyReference">The key reference (KID must be 0x11, 0x13, or 0x15).</param>
    /// <param name="pkSdEcka">The Security Domain public key.</param>
    /// <param name="skOceEcka">The Off-Card Entity private key (required for SCP11a/c).</param>
    /// <param name="oceKeyRef">The provisioned Off-Card Entity reference for SCP11a/c; null currently sends 00/00.</param>
    /// <param name="certificates">The certificate chain (required for SCP11a/c).</param>
    /// <exception cref="ArgumentException">Thrown if KID is invalid or required parameters are missing.</exception>
    public Scp11KeyParameters(
        KeyReference keyReference,
        ECPublicKey pkSdEcka,
        ECPrivateKey? skOceEcka,
        KeyReference? oceKeyRef,
        IEnumerable<X509Certificate2>? certificates)
    {
        ValidateKid(keyReference.Kid);

        KeyReference = keyReference;
        PkSdEcka = pkSdEcka ?? throw new ArgumentNullException(nameof(pkSdEcka));
        SkOceEcka = skOceEcka;
        OceKeyRef = oceKeyRef;
        Certificates = certificates?.ToList() ?? [];

        // Validate SCP11a/c requirements
        if (keyReference.Kid is ScpKid.SCP11a or ScpKid.SCP11c)
        {
            if (skOceEcka is null)
                throw new ArgumentNullException(nameof(skOceEcka), $"SCP11a and SCP11c require {nameof(skOceEcka)}");

            if (Certificates.Count == 0)
                throw new ArgumentException("SCP11a and SCP11c require a certificate chain", nameof(certificates));
        }
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Scp11KeyParameters" /> record for SCP11b.
    /// </summary>
    /// <param name="keyReference">The key reference (KID must be 0x13 for SCP11b).</param>
    /// <param name="pkSdEcka">The Security Domain public key.</param>
    public Scp11KeyParameters(KeyReference keyReference, ECPublicKey pkSdEcka)
        : this(keyReference, pkSdEcka, null, null, [])
    {

    }

    /// <summary>
    ///     Gets the public key of the Security Domain ECKA.
    ///     Required for all SCP11 variants.
    /// </summary>
    public ECPublicKey PkSdEcka { get; }

    /// <summary>
    ///     Gets the private key of the Off-Card Entity ECKA.
    ///     Required for SCP11a and SCP11c. Normal SCP11b setup uses the short constructor and no static OCE key;
    ///     the long constructor admits one without establishing that input's profile compatibility.
    /// </summary>
    public ECPrivateKey? SkOceEcka { get; }

    /// <summary>
    ///     Gets the Off-Card Entity key reference.
    ///     Supply the provisioned reference for SCP11a/c; null currently falls back to 00/00 on the wire.
    /// </summary>
    public KeyReference? OceKeyRef { get; }

    /// <summary>
    ///     Gets the certificate chain for the Off-Card Entity.
    ///     Required for SCP11a and SCP11c (non-empty). The short SCP11b constructor supplies an empty collection.
    /// </summary>
    public IReadOnlyList<X509Certificate2> Certificates { get; }

    public override void Dispose()
    {
        if (_disposed) return;

        try
        {
            SkOceEcka?.Dispose();
            foreach (var cert in Certificates)
            {
                cert.Dispose();
            }
        }
        finally
        {
            _disposed = true;
        }
    }

    private static void ValidateKid(byte kid)
    {
        if (kid is not (ScpKid.SCP11a or ScpKid.SCP11b or ScpKid.SCP11c))
            throw new ArgumentException(
                $"Invalid SCP11 KID: 0x{kid:X2}. Must be 0x11 (SCP11a), 0x13 (SCP11b), or 0x15 (SCP11c).");
    }
}