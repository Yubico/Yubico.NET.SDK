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

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Experimental typed helper for building a <c>COSE_Sign_Args</c> map (CTAP v4 draft).
/// </summary>
/// <remarks>
/// <para>
/// <c>COSE_Sign_Args</c> is a CBOR map whose key 3 carries the request algorithm identifier; the
/// remaining keys are algorithm-specific. Today the only typed helper in this SDK is
/// <see cref="ArkgP256SignArgs"/> (alg = <c>-65539</c>).
/// </para>
/// <para>
/// This helper is not the generic previewSign contract. Generic previewSign signing accepts raw
/// <see cref="PreviewSignSigningParams.AdditionalArgs"/> bytes. Use
/// <see cref="PreviewSignCbor.EncodeAdditionalArgs"/> to convert this experimental typed helper
/// to those raw bytes when testing ARKG flows in v2.
/// </para>
/// <para>
/// This is a closed helper hierarchy: the constructor is <c>private protected</c>, so external
/// assemblies cannot extend it. <see cref="PreviewSignCbor.EncodeCoseSignArgs"/> exhaustively
/// pattern-matches the known subtypes and throws on unknown runtime types.
/// </para>
/// </remarks>
public abstract record class CoseSignArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="CoseSignArgs"/>. <c>private protected</c>:
    /// only subtypes declared in this assembly may extend the hierarchy.
    /// </summary>
    private protected CoseSignArgs()
    {
    }

    /// <summary>The COSE algorithm identifier written under key 3 of the <c>COSE_Sign_Args</c> map.</summary>
    public abstract int Algorithm { get; }

    /// <summary>
    /// Experimental convenience factory: constructs an <see cref="ArkgP256SignArgs"/> without naming the leaf type.
    /// </summary>
    /// <param name="keyHandle">The 81-byte ARKG key handle (16-byte HMAC tag concatenated with
    /// 65-byte SEC1 uncompressed P-256 ephemeral public key).</param>
    /// <param name="context">The ARKG context (≤64 bytes) bound to the derivation.</param>
    /// <returns>A typed <see cref="ArkgP256SignArgs"/>.</returns>
    public static CoseSignArgs ArkgP256(ReadOnlyMemory<byte> keyHandle, ReadOnlyMemory<byte> context)
        => new ArkgP256SignArgs(keyHandle, context);
}
