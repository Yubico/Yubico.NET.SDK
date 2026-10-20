// Copyright 2026 Yubico AB
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

using System.Numerics;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
///     Checks that the public and private components of a key belong together before the SDK wraps
///     them. The YubiKey derives its own public key from the private material it is sent, so a wrapper
///     that reported an unrelated public component would describe a key the device never had.
/// </summary>
/// <remarks>
///     <para>
///         References: RFC 8017 §3.1 (λ(n)), §3.2 (private key representation and e·d ≡ 1 mod λ(n)),
///         Appendix A.1.2 (CRT components); SEC 1 v2 §3.2.2 / FIPS 186-5 §6.2.1 (Q = d·G).
///     </para>
///     <para>
///         <see cref="Curve25519PrivateKey" /> needs no counterpart here: it holds no public value and the
///         decoder rejects a PKCS#8 v2 <c>publicKey</c> field, so it can never carry an unchecked one.
///     </para>
/// </remarks>
internal static class PrivateKeyConsistency
{
    /// <summary>
    ///     Verifies that <c>Q = d·G</c> by signing a fixed digest with the key as supplied and verifying
    ///     with a public-only key built from the same <c>Q</c>. A signature made with <c>d</c> verifies
    ///     under <c>Q</c> only if they match; an off-curve <c>Q</c> fails the same way. No D-only key is
    ///     ever imported, so no public point is derived by the runtime.
    /// </summary>
    /// <exception cref="CryptographicException">The point does not match the scalar, or is invalid.</exception>
    public static void ThrowIfPublicPointDoesNotMatch(in ECParameters parameters)
    {
        if (parameters.Q.X is null || parameters.Q.Y is null)
            return; // RFC 5915 makes publicKey OPTIONAL; nothing to check against.

        bool matches;
        try
        {
            // Some platforms (OpenSSL) already refuse a mismatched import here; others (CNG, SecKey)
            // accept it and re-derive Q, which the verify step below catches. Either way the caller sees
            // one exception type.
            using var signer = ECDsa.Create(parameters);
            using var verifier = ECDsa.Create(new ECParameters { Curve = parameters.Curve, Q = parameters.Q });

            Span<byte> digest = stackalloc byte[32]; // fixed, non-secret; only the key relation matters
            Span<byte> signature = stackalloc byte[MaxEcdsaSignatureLength];
            matches = signer.TrySignHash(digest, signature, out var written)
                      && verifier.VerifyHash(digest, signature[..written]);
        }
        catch (Exception e)
        {
            throw new CryptographicException("EC public point does not correspond to the private scalar.", e);
        }

        if (!matches)
            throw new CryptographicException("EC public point does not correspond to the private scalar.");
    }

    /// <summary>
    ///     Verifies the RSA relations of RFC 8017 §3.1, §3.2 and Appendix A.1.2:
    ///     <c>n = p·q</c>, <c>dP = d mod (p−1)</c>, <c>dQ = d mod (q−1)</c>, <c>qInv·q ≡ 1 (mod p)</c> and
    ///     <c>e·d ≡ 1 (mod lcm(p−1, q−1))</c>.
    /// </summary>
    /// <remarks>
    ///     Uses <see cref="BigInteger" />, which is neither constant-time nor zeroable. The private values
    ///     already exist in the caller-supplied managed arrays, so this adds no exposure the import does not
    ///     already have.
    /// </remarks>
    /// <exception cref="CryptographicException">A component is missing or a relation does not hold.</exception>
    public static void ThrowIfInconsistent(in RSAParameters parameters)
    {
        if (parameters.Modulus is null || parameters.Exponent is null || parameters.D is null ||
            parameters.P is null || parameters.Q is null || parameters.DP is null ||
            parameters.DQ is null || parameters.InverseQ is null)
        {
            throw new CryptographicException("RSA private key is missing a required component.");
        }

        var n = ToBigInteger(parameters.Modulus);
        var e = ToBigInteger(parameters.Exponent);
        var d = ToBigInteger(parameters.D);
        var p = ToBigInteger(parameters.P);
        var q = ToBigInteger(parameters.Q);
        var dP = ToBigInteger(parameters.DP);
        var dQ = ToBigInteger(parameters.DQ);
        var qInv = ToBigInteger(parameters.InverseQ);

        if (p <= 1 || q <= 1 || n != p * q)
            throw new CryptographicException("RSA modulus is not the product of the private primes.");

        var pMinus1 = p - 1;
        var qMinus1 = q - 1;
        if (dP != d % pMinus1 || dQ != d % qMinus1)
            throw new CryptographicException("RSA CRT exponents do not match the private exponent.");

        if (qInv * q % p != 1)
            throw new CryptographicException("RSA CRT coefficient is not the inverse of q modulo p.");

        var lambda = pMinus1 / BigInteger.GreatestCommonDivisor(pMinus1, qMinus1) * qMinus1;
        if (e * d % lambda != 1)
            throw new CryptographicException("RSA public and private exponents are not inverses.");
    }

    // DER ECDSA signature for P-384 (the largest curve the SDK wraps): 2 × (1 + 1 + 49) + 2 ≈ 104; room to spare.
    private const int MaxEcdsaSignatureLength = 144;

    private static BigInteger ToBigInteger(byte[] value) => new(value, isUnsigned: true, isBigEndian: true);
}