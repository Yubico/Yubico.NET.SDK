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

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>The mathematical form of an elliptic curve, not a signing algorithm.</summary>
public enum CurveForm
{
    /// <summary>Short-Weierstrass form, used by ordinary EC key representations.</summary>
    ShortWeierstrass,
    /// <summary>Montgomery form, such as Curve25519; not an operation identifier.</summary>
    Montgomery,
    /// <summary>Edwards form, such as Edwards25519; not an EdDSA variant selection.</summary>
    Edwards
}

/// <summary>Immutable mathematical curve metadata. Representation widths belong to <see cref="KeyInfo"/>.</summary>
public sealed record CurveInfo
{
    internal CurveInfo(string curveId, string displayName, CurveForm form, int fieldSizeBits,
        int orderSizeBits, int coordinateLengthBytes, string? namedCurveOid = null)
    {
        Id = curveId;
        DisplayName = displayName;
        Form = form;
        FieldSizeBits = fieldSizeBits;
        OrderSizeBits = orderSizeBits;
        CoordinateLengthBytes = coordinateLengthBytes;
        NamedCurveOid = namedCurveOid;
    }

    /// <summary>Stable, case-sensitive catalog identity, independent of display names and wire identifiers.</summary>
    public string Id { get; }
    /// <summary>A human-readable curve label; use <see cref="Id"/> for catalog lookups.</summary>
    public string DisplayName { get; }
    /// <summary>The mathematical curve form, independent of key construction and operation.</summary>
    public CurveForm Form { get; }
    /// <summary>Bit length of the field modulus, not a key encoding width or security-strength estimate.</summary>
    public int FieldSizeBits { get; }
    /// <summary>Bit length of the prime subgroup order, not the full curve group order.</summary>
    public int OrderSizeBits { get; }
    /// <summary>Width in bytes needed to store one field coordinate; does not specify a point or scalar encoding.</summary>
    public int CoordinateLengthBytes { get; }
    /// <summary>Standard ASN.1 named-curve OID, or null when none is modeled; never an RFC 8410 key algorithm OID.</summary>
    public string? NamedCurveOid { get; }
}