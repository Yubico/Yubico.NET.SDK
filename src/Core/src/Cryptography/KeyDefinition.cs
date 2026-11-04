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

using Yubico.YubiKit.Core.Cryptography.Cose;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Represents the definition of a cryptographic key, including its type, length, and other properties.
/// </summary>
public class KeyDefinition
{
    /// <summary>
    /// Gets or sets the type of the key.
    /// </summary>
    public KeyType KeyType { get; init; }

    /// <summary>
    /// Gets or sets the length of the key in bytes.
    /// </summary>
    public int LengthInBytes { get; init; }

    /// <summary>
    /// Gets or sets the length of the key in bits.
    /// </summary>
    public int LengthInBits { get; init; }

    /// <summary>
    /// Gets the algorithm OID. For symmetric definitions this is the existing CBC-mode OID,
    /// not a mode-independent key identifier or an operation selection.
    /// </summary>
    public string AlgorithmOid { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the curve OID for elliptic curve keys.
    /// </summary>
    public string? CurveOid { get; init; }

    /// <summary>
    /// Indicates whether the key is an elliptic curve (EC) key.
    /// </summary>
    public bool IsEllipticCurve => KeyType.IsEllipticCurve();

    /// <summary>
    /// Indicates whether the key is an RSA key.
    /// </summary>
    public bool IsRsa => KeyType.IsRsa();

    /// <summary>
    /// Indicates whether the key is a symmetric key.
    /// </summary>
    public bool IsSymmetric => KeyType.IsSymmetric();

    /// <summary>
    /// Indicates whether the key is a asymmetric key.
    /// </summary>
    public bool IsAsymmetric => KeyType.IsAsymmetric();

    /// <summary>
    /// Gets the human-readable key name. Use KeyType for symbolic identity; enum ToString is not stable storage.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the COSE key definition associated with this key.
    /// </summary>
    public CoseKeyDefinition? CoseKeyDefinition { get; init; }

    public override string ToString() => $"{DisplayName} ({LengthInBits} bits)";

    public override bool Equals(object? obj) =>
        obj is KeyDefinition other &&
        KeyType == other.KeyType &&
        LengthInBytes == other.LengthInBytes &&
        LengthInBits == other.LengthInBits &&
        AlgorithmOid == other.AlgorithmOid &&
        CurveOid == other.CurveOid &&
        DisplayName == other.DisplayName &&
        CoseKeyDefinition == other.CoseKeyDefinition;

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(KeyType);
        hashCode.Add(LengthInBytes);
        hashCode.Add(LengthInBits);
        hashCode.Add(AlgorithmOid);
        hashCode.Add(CurveOid);
        hashCode.Add(DisplayName);
        hashCode.Add(CoseKeyDefinition);
        return hashCode.ToHashCode();
    }
}
