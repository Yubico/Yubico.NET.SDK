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

using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>The purpose of an operation definition, not an algorithm or protocol identifier.</summary>
public enum CryptographicOperation
{
    /// <summary>Digital signature generation or verification.</summary>
    Signature,
    /// <summary>Raw shared-secret agreement without an implicit KDF.</summary>
    KeyAgreement,
    /// <summary>Key derivation, possibly including a key-agreement step.</summary>
    KeyDerivation,
    /// <summary>Encryption or decryption; integrity protection is not implied.</summary>
    Encryption
}

/// <summary>Small declarative structural constraint; not a provider, device or protocol policy.</summary>
/// <remarks>All specified fields must match. An algorithm's collection of constraints is a set of OR alternatives.</remarks>
public sealed record KeyApplicability
{
    internal KeyApplicability(KeyFamily family, CurveForm? curveForm = null, string? curveId = null,
        int minimumKeySizeBits = 0, int? maximumKeySizeBits = null, KeyType? keyType = null)
    {
        Family = family;
        CurveForm = curveForm;
        CurveId = curveId;
        MinimumKeySizeBits = minimumKeySizeBits;
        MaximumKeySizeBits = maximumKeySizeBits;
        KeyType = keyType;
    }

    /// <summary>The required structural key family.</summary>
    public KeyFamily Family { get; }
    /// <summary>The required mathematical curve form, or null for no form constraint.</summary>
    public CurveForm? CurveForm { get; }
    /// <summary>The required case-sensitive curve identity, or null for no curve constraint.</summary>
    public string? CurveId { get; }
    /// <summary>Inclusive minimum nominal key size in bits; zero means no lower bound.</summary>
    public int MinimumKeySizeBits { get; }
    /// <summary>Inclusive maximum nominal key size in bits, or null for no upper bound.</summary>
    public int? MaximumKeySizeBits { get; }
    /// <summary>The required concrete construction, or null when family/form/size constraints suffice.</summary>
    public KeyType? KeyType { get; }

    /// <summary>Determines whether every specified structural constraint matches a concrete key.</summary>
    /// <param name="keyInfo">The concrete representation to inspect.</param>
    /// <returns>True for a structural match; does not establish provider, device or profile support.</returns>
    public bool Matches(KeyInfo keyInfo) => keyInfo.Family == Family
        && (CurveForm is null || keyInfo.Curve?.Form == CurveForm)
        && (CurveId is null || keyInfo.Curve?.Id == CurveId)
        && keyInfo.KeySizeBits >= MinimumKeySizeBits
        && keyInfo.KeySizeBits <= MaximumKeySizeBits.GetValueOrDefault(int.MaxValue)
        && keyInfo.KeyType == KeyType.GetValueOrDefault(keyInfo.KeyType);
}

/// <summary>Immutable operation metadata. Applicability does not mean support or recommendation.</summary>
public sealed record AlgorithmInfo
{
    internal AlgorithmInfo(string algorithmId, string displayName, CryptographicOperation operation,
        IEnumerable<KeyApplicability> applicability, string? algorithmOid = null, int? coseAlgorithmId = null,
        HashAlgorithmName? hashAlgorithm = null, RSASignaturePaddingMode? signaturePadding = null,
        HashAlgorithmName? maskGenerationHashAlgorithm = null, int? saltLengthBytes = null,
        CipherMode? cipherMode = null, string? derivation = null)
    {
        Id = algorithmId;
        DisplayName = displayName;
        Operation = operation;
        Applicability = [.. applicability];
        AlgorithmOid = algorithmOid;
        CoseAlgorithmId = coseAlgorithmId;
        HashAlgorithm = hashAlgorithm;
        SignaturePadding = signaturePadding;
        MaskGenerationHashAlgorithm = maskGenerationHashAlgorithm;
        SaltLengthBytes = saltLengthBytes;
        CipherMode = cipherMode;
        Derivation = derivation;
    }

    /// <summary>Stable, case-sensitive catalog identity, distinct from display labels, OIDs and COSE numbers.</summary>
    public string Id { get; }
    /// <summary>A human-readable label including selected parameters; use <see cref="Id"/> for identity.</summary>
    public string DisplayName { get; }
    /// <summary>The operation's purpose, independent of the key representation.</summary>
    public CryptographicOperation Operation { get; }
    /// <summary>OR alternatives: a key matches if any constraint matches all of its specified fields.</summary>
    public ImmutableList<KeyApplicability> Applicability { get; }
    /// <summary>Operation OID, or null when none is modeled; may identify multiple parameterized definitions.</summary>
    public string? AlgorithmOid { get; }
    /// <summary>Registered COSE alg number, independent of kty/crv; null if no mapping is modeled.</summary>
    /// <remarks>
    /// Protocol parameter/security requirements still apply. In particular COSE AES-CBC requires
    /// RFC 9459's PKCS#7 padding and companion integrity protection; a CBC mapping is not a codec.
    /// Structural applicability on a curve does not establish that COSE permits that curve with this
    /// identifier. Validate the complete key/algorithm pairing under the consuming protocol profile.
    /// </remarks>
    public int? CoseAlgorithmId { get; }
    /// <summary>A separately selected hash, or null when not separately parameterized.</summary>
    /// <remarks>
    /// Null can mean an unspecified hash (generic ECDSA), an intrinsic construction (Ed25519),
    /// or no hash parameter (CBC); it does not by itself mean the operation is generic or hash-free.
    /// </remarks>
    public HashAlgorithmName? HashAlgorithm { get; }
    /// <summary>The RSA signature padding, or null for other families or unspecified RSA encoding.</summary>
    public RSASignaturePaddingMode? SignaturePadding { get; }
    /// <summary>The MGF1 hash for RSA-PSS, or null when not applicable or not selected.</summary>
    public HashAlgorithmName? MaskGenerationHashAlgorithm { get; }
    /// <summary>The RSA-PSS salt length in bytes, or null when not applicable or not selected.</summary>
    public int? SaltLengthBytes { get; }
    /// <summary>RSA-PSS trailerField; 1 for the modeled EMSA-PSS encodings, otherwise null.</summary>
    public int? TrailerField => SignaturePadding == RSASignaturePaddingMode.Pss ? 1 : null;
    /// <summary>The block cipher mode, or null when not applicable or not selected.</summary>
    public CipherMode? CipherMode { get; }
    /// <summary>The named KDF construction, or null when none is modeled for the operation.</summary>
    public string? Derivation { get; }

    /// <summary>Compares metadata by value, including the ordered applicability alternatives.</summary>
    public bool Equals(AlgorithmInfo? other) => other is not null
        && (Id, DisplayName, Operation, AlgorithmOid, CoseAlgorithmId, HashAlgorithm, SignaturePadding,
            MaskGenerationHashAlgorithm, SaltLengthBytes, CipherMode, Derivation).Equals(
            (other.Id, other.DisplayName, other.Operation, other.AlgorithmOid, other.CoseAlgorithmId,
                other.HashAlgorithm, other.SignaturePadding, other.MaskGenerationHashAlgorithm,
                other.SaltLengthBytes, other.CipherMode, other.Derivation))
        && Applicability.SequenceEqual(other.Applicability);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(DisplayName);
        hash.Add(Operation);
        foreach (var constraint in Applicability) hash.Add(constraint);
        hash.Add(AlgorithmOid);
        hash.Add(CoseAlgorithmId);
        hash.Add(HashAlgorithm);
        hash.Add(SignaturePadding);
        hash.Add(MaskGenerationHashAlgorithm);
        hash.Add(SaltLengthBytes);
        hash.Add(CipherMode);
        hash.Add(Derivation);
        return hash.ToHashCode();
    }
}