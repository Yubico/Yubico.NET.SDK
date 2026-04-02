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

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
///     SCP key parameters for SCP11 authentication (supports SCP11a, SCP11b, SCP11c).
/// </summary>
public sealed record Scp11KeyParameters : ScpKeyParameters
{
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Scp11KeyParameters" /> record for SCP11a or SCP11c.
    /// </summary>
    /// <param name="keyReference">The key reference (KID must be 0x11, 0x13, or 0x15).</param>
    /// <param name="pkSdEcka">The Security Domain public key.</param>
    /// <param name="skOceEcka">The Off-Card Entity private key (required for SCP11a/c).</param>
    /// <param name="oceKeyRef">The Off-Card Entity key reference (required for SCP11a/c).</param>
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
            if (skOceEcka == null)
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
    ///     Required for SCP11a and SCP11c, null for SCP11b.
    /// </summary>
    public ECPrivateKey? SkOceEcka { get; }

    /// <summary>
    ///     Gets the Off-Card Entity key reference.
    ///     Required for SCP11a and SCP11c, null for SCP11b.
    /// </summary>
    public KeyReference? OceKeyRef { get; }

    /// <summary>
    ///     Gets the certificate chain for the Off-Card Entity.
    ///     Required for SCP11a and SCP11c (non-empty), empty for SCP11b.
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