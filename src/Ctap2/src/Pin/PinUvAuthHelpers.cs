// Copyright 2025 Yubico AB
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

using System.Security.Cryptography;

namespace Yubico.YubiKit.Ctap2.Pin;

/// <summary>
/// Shared helpers for PIN/UV authentication protocol implementations.
/// </summary>
/// <remarks>
/// Encapsulates the common ECDH P-256 key agreement logic used by both V1 and V2 protocols.
/// The raw shared secret (Z) is returned to the caller, who applies protocol-specific KDF.
/// </remarks>
internal static class PinUvAuthHelpers
{
    // COSE key parameter labels
    private const int CoseKeyType = 1;
    private const int CoseAlgorithm = 3;
    private const int CoseEC2Curve = -1;
    private const int CoseEC2X = -2;
    private const int CoseEC2Y = -3;

    // COSE values
    private const int CoseKeyTypeEC2 = 2;
    private const int CoseAlgEcdhEsHkdf256 = -25;
    private const int CoseEC2CurveP256 = 1;

    /// <summary>
    /// Performs ECDH P-256 key agreement with the peer's COSE key.
    /// </summary>
    /// <param name="peerCoseKey">The borrowed peer key, required by CTAP to contain kty=2, alg=-25, crv=1 and 32-byte big-endian X/Y coordinates, without other optional parameters.</param>
    /// <returns>
    /// A new platform public-key dictionary and a caller-owned raw ECDH secret (Z), the 32-byte
    /// big-endian X coordinate of the shared point. Clear Z after deriving protocol-specific keys,
    /// including when that derivation fails. Public coordinates are not secret material.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerCoseKey"/> is null.</exception>
    /// <remarks>
    /// Explicit checks currently cover only coordinate types and lengths, not required COSE labels
    /// or optional-parameter restrictions. Import forces P-256 and delegates point validation to the
    /// provider. The ephemeral crypto objects are disposed, but allocation of the public dictionary
    /// after derivation is not protected by a Z-clearing guard.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown for missing/invalid coordinate types or lengths, or a wrapped cryptographic error from raw-secret derivation.</exception>
    /// <exception cref="CryptographicException">Peer-key import can raise an unwrapped provider error before the derivation catch.</exception>
    internal static (Dictionary<int, object?> KeyAgreement, byte[] RawSharedSecret)
        PerformEcdhKeyAgreement(IReadOnlyDictionary<int, object?> peerCoseKey)
    {
        ArgumentNullException.ThrowIfNull(peerCoseKey);

        // Check coordinate storage here; required kty/alg/crv labels are not yet validated.
        if (!peerCoseKey.TryGetValue(CoseEC2X, out var xObj) || xObj is not byte[] peerX)
        {
            throw new ArgumentException("Peer COSE key missing or invalid X coordinate (-2).", nameof(peerCoseKey));
        }

        if (!peerCoseKey.TryGetValue(CoseEC2Y, out var yObj) || yObj is not byte[] peerY)
        {
            throw new ArgumentException("Peer COSE key missing or invalid Y coordinate (-3).", nameof(peerCoseKey));
        }

        // Validate coordinate lengths (P-256 uses 32-byte coordinates)
        if (peerX.Length != 32)
        {
            throw new ArgumentException($"Invalid X coordinate length: expected 32, got {peerX.Length}.", nameof(peerCoseKey));
        }

        if (peerY.Length != 32)
        {
            throw new ArgumentException($"Invalid Y coordinate length: expected 32, got {peerY.Length}.", nameof(peerCoseKey));
        }

        // Generate ephemeral ECDH key pair
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var ourParams = ecdh.ExportParameters(includePrivateParameters: false);

        // Import as P-256; provider point-validation errors here are outside the derivation catch.
        var peerParams = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = peerX, Y = peerY }
        };

        using var peerEcdh = ECDiffieHellman.Create(peerParams);

        // Derive raw shared secret (X coordinate of ECDH shared point)
        byte[] z;
        try
        {
            z = ecdh.DeriveRawSecretAgreement(peerEcdh.PublicKey);
        }
        catch (CryptographicException ex)
        {
            throw new ArgumentException("Failed to derive shared secret. Peer key may be invalid.", nameof(peerCoseKey), ex);
        }

        // Build our COSE key for key agreement
        var keyAgreement = new Dictionary<int, object?>
        {
            { CoseKeyType, CoseKeyTypeEC2 },
            { CoseAlgorithm, CoseAlgEcdhEsHkdf256 },
            { CoseEC2Curve, CoseEC2CurveP256 },
            { CoseEC2X, ourParams.Q.X },
            { CoseEC2Y, ourParams.Q.Y }
        };

        return (keyAgreement, z);
    }
}