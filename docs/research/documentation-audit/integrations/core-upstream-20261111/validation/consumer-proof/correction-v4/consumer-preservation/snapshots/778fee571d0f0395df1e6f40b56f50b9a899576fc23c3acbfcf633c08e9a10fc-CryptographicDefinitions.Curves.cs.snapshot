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

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Yubico.YubiKit.Core.Cryptography;

public static partial class CryptographicDefinitions
{
    /// <summary>Mathematical curves. String identities are case-sensitive and distinct from OIDs.</summary>
    public static class Curves
    {
        /// <summary>NIST P-256 (secp256r1), a 256-bit short-Weierstrass curve.</summary>
        public static CurveInfo P256 { get; } = new("p-256", "P-256", CurveForm.ShortWeierstrass, 256, 256, 32, Oids.P256);
        /// <summary>NIST P-384 (secp384r1), a 384-bit short-Weierstrass curve.</summary>
        public static CurveInfo P384 { get; } = new("p-384", "P-384", CurveForm.ShortWeierstrass, 384, 384, 48, Oids.P384);
        /// <summary>NIST P-521 (secp521r1), a 521-bit short-Weierstrass curve with 66-byte coordinates.</summary>
        public static CurveInfo P521 { get; } = new("p-521", "P-521", CurveForm.ShortWeierstrass, 521, 521, 66, Oids.P521);
        /// <summary>The Montgomery Curve25519 curve, not the concrete X25519 construction.</summary>
        public static CurveInfo Curve25519 { get; } = new("curve25519", "Curve25519", CurveForm.Montgomery, 255, 253, 32);
        /// <summary>The Edwards25519 curve, not a selection of Ed25519 or an EdDSA variant.</summary>
        public static CurveInfo Edwards25519 { get; } = new("edwards25519", "Edwards25519", CurveForm.Edwards, 255, 253, 32);
        /// <summary>SECG secp256k1, a 256-bit short-Weierstrass curve distinct from P-256.</summary>
        public static CurveInfo Secp256k1 { get; } = new("secp256k1", "secp256k1", CurveForm.ShortWeierstrass, 256, 256, 32, Oids.Secp256k1);
        /// <summary>Brainpool brainpoolP256r1, a 256-bit short-Weierstrass curve.</summary>
        public static CurveInfo BrainpoolP256r1 { get; } = new("brainpoolp256r1", "brainpoolP256r1", CurveForm.ShortWeierstrass, 256, 256, 32, Oids.BrainpoolP256r1);
        /// <summary>Brainpool brainpoolP384r1, a 384-bit short-Weierstrass curve.</summary>
        public static CurveInfo BrainpoolP384r1 { get; } = new("brainpoolp384r1", "brainpoolP384r1", CurveForm.ShortWeierstrass, 384, 384, 48, Oids.BrainpoolP384r1);
        /// <summary>Brainpool brainpoolP512r1, a 512-bit short-Weierstrass curve.</summary>
        public static CurveInfo BrainpoolP512r1 { get; } = new("brainpoolp512r1", "brainpoolP512r1", CurveForm.ShortWeierstrass, 512, 512, 64, Oids.BrainpoolP512r1);

        /// <summary>All modeled curves in declaration order.</summary>
        public static ImmutableList<CurveInfo> All { get; } =
            [P256, P384, P521, Curve25519, Edwards25519, Secp256k1, BrainpoolP256r1, BrainpoolP384r1, BrainpoolP512r1];

        private static readonly FrozenDictionary<string, CurveInfo> ById = CreateIndex(All, curveInfo => curveInfo.Id);
        private static readonly FrozenDictionary<string, CurveInfo> ByOid = CreateIndex(
            All.Where(curveInfo => curveInfo.NamedCurveOid is not null), curveInfo => curveInfo.NamedCurveOid ?? throw new InvalidOperationException());

        /// <summary>Gets the mathematical curve with a case-sensitive catalog identity.</summary>
        /// <param name="curveId">A <see cref="CurveInfo.Id"/>, not a display label or OID.</param>
        /// <returns>The catalog's immutable curve instance.</returns>
        /// <exception cref="NotSupportedException">The identity is not in the catalog.</exception>
        public static CurveInfo Get(string curveId) => TryGet(curveId, out var curveInfo) ? curveInfo
            : throw new NotSupportedException($"Unknown curve identity '{curveId}'.");
        /// <summary>Tries to find a mathematical curve by its case-sensitive catalog identity.</summary>
        /// <param name="curveId">A <see cref="CurveInfo.Id"/>, not a display label or OID.</param>
        /// <param name="curveInfo">The immutable curve instance on success; null on a miss.</param>
        /// <returns>True if the identity is modeled; false without throwing on an unknown identity.</returns>
        public static bool TryGet(string curveId, [NotNullWhen(true)] out CurveInfo? curveInfo) => ById.TryGetValue(curveId, out curveInfo);

        /// <summary>Looks up only a standard named-curve OID, never a key algorithm or operation OID.</summary>
        /// <param name="curveOid">The dotted-decimal named-curve OID.</param>
        /// <returns>The catalog's immutable curve instance.</returns>
        /// <exception cref="NotSupportedException">No modeled curve has that named-curve OID.</exception>
        public static CurveInfo GetByOid(string curveOid) => TryGetByOid(curveOid, out var curveInfo) ? curveInfo
            : throw new NotSupportedException($"Unsupported named-curve OID '{curveOid}'.");
        /// <summary>Tries to resolve a named-curve OID, never a key algorithm or operation OID.</summary>
        /// <param name="curveOid">A dotted-decimal curve OID, or null when no OID is available.</param>
        /// <param name="curveInfo">The immutable curve instance on success; null otherwise.</param>
        /// <returns>False for null or unmodeled OIDs.</returns>
        public static bool TryGetByOid(string? curveOid, [NotNullWhen(true)] out CurveInfo? curveInfo)
        {
            curveInfo = null;
            return curveOid is not null && ByOid.TryGetValue(curveOid, out curveInfo);
        }
    }
}