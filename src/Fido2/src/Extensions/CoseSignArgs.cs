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

using Yubico.YubiKit.Fido2.Cose;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Typed <c>COSE_Sign_Args</c> map (CTAP v4 draft) — the value carried under
/// <c>previewSign</c> authentication input key 7.
/// </summary>
/// <remarks>
/// <para>
/// <c>COSE_Sign_Args</c> is a CBOR map whose key 3 carries the request algorithm identifier; the
/// remaining keys are algorithm-specific. Today the only inhabitant supported by the YubiKey is
/// <see cref="ArkgP256SignArgs"/> (alg = <c>-65539</c>). New algorithms slot in by adding a
/// new sealed subtype.
/// </para>
/// <para>
/// This is a closed union: the constructor is <c>private protected</c>, so external assemblies
/// cannot extend the hierarchy. <see cref="PreviewSignCbor.EncodeCoseSignArgs"/> exhaustively
/// pattern-matches the known subtypes and throws on any unknown runtime type.
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
    /// Convenience factory: constructs an <see cref="ArkgP256SignArgs"/> without naming the leaf type.
    /// </summary>
    /// <param name="keyHandle">The 81-byte ARKG key handle (16-byte HMAC tag concatenated with
    /// 65-byte SEC1 uncompressed P-256 ephemeral public key).</param>
    /// <param name="context">The ARKG context (≤64 bytes) bound to the derivation.</param>
    /// <returns>A typed <see cref="ArkgP256SignArgs"/>.</returns>
    public static CoseSignArgs ArkgP256(ReadOnlyMemory<byte> keyHandle, ReadOnlyMemory<byte> context)
        => new ArkgP256SignArgs(keyHandle, context);
}

/// <summary>
/// <c>COSE_Sign_Args</c> for ARKG-P256-ESP256 (alg = <c>-65539</c>). Wire shape:
/// <c>{3: -65539, -1: kh, -2: ctx}</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>KeyHandle</c> is the 81-byte ARKG ciphertext (16-byte HMAC tag concatenated with a 65-byte
/// SEC1 uncompressed-form ephemeral public key, leading <c>0x04</c>) returned by ARKG public-key
/// derivation. <c>Context</c> is the HKDF context (≤64 bytes) bound to the derivation.
/// </para>
/// <para>
/// <b>Memory ownership:</b> Both <see cref="KeyHandle"/> and <see cref="Context"/> are
/// <see cref="ReadOnlyMemory{T}"/> passthroughs — the encoder reads them at CBOR-write time
/// and never copies. The caller owns the underlying buffers and is responsible for zeroing
/// any sensitive material after the request is on the wire (see repo CLAUDE.md "Security"
/// section: ROM passthrough is safe in record types because all copies reference the same
/// caller-owned memory).
/// </para>
/// <para>
/// <b>Algorithm identifier:</b> <see cref="CoseAlgorithm.ArkgP256"/> (<c>-65539</c>) — this is
/// the wire signing-op alg, not the seed-key COSE-key alg (<c>-65700</c>). See
/// <see cref="CoseAlgorithm.ArkgP256"/> XML doc for the full disambiguation.
/// </para>
/// </remarks>
public sealed record class ArkgP256SignArgs : CoseSignArgs
{
    /// <summary>
    /// The 16-byte HMAC tag length within an ARKG-P256 key handle.
    /// </summary>
    private const int ArkgTagLength = 16;

    /// <summary>
    /// The 65-byte SEC1 uncompressed P-256 point length (1-byte 0x04 prefix + 32-byte X + 32-byte Y).
    /// </summary>
    private const int Sec1UncompressedP256Length = 65;

    /// <summary>
    /// Total ARKG-P256 key handle length: <see cref="ArkgTagLength"/> + <see cref="Sec1UncompressedP256Length"/> = 81.
    /// </summary>
    private const int ArkgP256KeyHandleLength = ArkgTagLength + Sec1UncompressedP256Length;

    /// <summary>
    /// Maximum ARKG context length, bounded by the HKDF single-byte length-prefix encoding.
    /// </summary>
    private const int MaxContextLength = 64;

    /// <summary>
    /// Algorithm identifier on the wire — fixed at <c>-65539</c>
    /// (<see cref="CoseAlgorithm.ArkgP256"/>).
    /// </summary>
    public override int Algorithm => CoseAlgorithm.ArkgP256.Value;

    /// <summary>
    /// The 81-byte ARKG-P256 key handle: 16-byte HMAC tag concatenated with 65-byte SEC1
    /// uncompressed ephemeral public key.
    /// </summary>
    public ReadOnlyMemory<byte> KeyHandle { get; }

    /// <summary>The ARKG context (≤64 bytes) bound to the derivation.</summary>
    public ReadOnlyMemory<byte> Context { get; }

    /// <summary>
    /// Initializes a new <see cref="ArkgP256SignArgs"/> with the supplied key handle and context.
    /// </summary>
    /// <param name="keyHandle">The 81-byte ARKG-P256 key handle.</param>
    /// <param name="context">The ARKG HKDF context (0–64 bytes).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="keyHandle"/> is not exactly 81 bytes, or
    /// <paramref name="context"/> exceeds 64 bytes.
    /// </exception>
    public ArkgP256SignArgs(ReadOnlyMemory<byte> keyHandle, ReadOnlyMemory<byte> context)
    {
        // 81-byte fixed shape: 16-byte HMAC tag || 65-byte SEC1 uncompressed P-256 point.
        // Hard-validate at construct time so accidental concatenations / bad hex decodes fail
        // before they reach firmware (which would just return CTAP2_ERR_INVALID_OPTION).
        if (keyHandle.Length != ArkgP256KeyHandleLength)
        {
            throw new ArgumentException(
                $"ARKG-P256 key handle must be exactly {ArkgP256KeyHandleLength} bytes "
                + $"({ArkgTagLength}-byte HMAC tag || {Sec1UncompressedP256Length}-byte SEC1 pubkey); "
                + $"got {keyHandle.Length}.",
                nameof(keyHandle));
        }

        if (context.Length > MaxContextLength)
        {
            throw new ArgumentException(
                $"ARKG context must be ≤{MaxContextLength} bytes per HKDF length-byte prefix encoding; "
                + $"got {context.Length}.",
                nameof(context));
        }

        KeyHandle = keyHandle;
        Context = context;
    }
}