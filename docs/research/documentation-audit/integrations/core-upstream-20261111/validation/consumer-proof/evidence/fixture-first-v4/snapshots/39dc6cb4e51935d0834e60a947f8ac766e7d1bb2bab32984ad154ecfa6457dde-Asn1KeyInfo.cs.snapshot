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

/// <summary>The parameters field of an ASN.1 key AlgorithmIdentifier.</summary>
public enum Asn1ParameterKind
{
    /// <summary>No parameters field is encoded. RFC 8410 Ed/X identifiers forbid even NULL.</summary>
    Absent,
    /// <summary>A single ASN.1 NULL is encoded. RSA decoders also tolerate an absent field on input.</summary>
    Null,
    /// <summary>A single named-curve OBJECT IDENTIFIER is encoded after id-ecPublicKey.</summary>
    NamedCurve
}

/// <summary>ASN.1 key AlgorithmIdentifier metadata, not operation algorithm metadata.</summary>
public sealed record Asn1KeyInfo
{
    internal Asn1KeyInfo(string keyAlgorithmOid, Asn1ParameterKind parameterKind, string? namedCurveOid = null)
    {
        AlgorithmOid = keyAlgorithmOid;
        ParameterKind = parameterKind;
        NamedCurveOid = namedCurveOid;
    }

    /// <summary>The key AlgorithmIdentifier OID; shared identifiers do not determine a curve or RSA modulus size.</summary>
    public string AlgorithmOid { get; }
    /// <summary>The parameters encoding emitted for this key representation.</summary>
    public Asn1ParameterKind ParameterKind { get; }
    /// <summary>The curve OID used as parameters, or null unless <see cref="ParameterKind"/> is NamedCurve.</summary>
    public string? NamedCurveOid { get; }
}