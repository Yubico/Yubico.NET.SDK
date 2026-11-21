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

using System.Formats.Cbor;
using static Yubico.YubiKit.Ctap2.Cbor.CoseKeyWriter;

namespace Yubico.YubiKit.Ctap2.Extensions;

/// <summary>
/// Input parameters for the hmac-secret extension during getAssertion.
/// </summary>
/// <remarks>
/// <para>
/// The hmac-secret extension allows relying parties to derive a secret value
/// from a credential. This is useful for:
/// <list type="bullet">
///   <item><description>Disk encryption keys</description></item>
///   <item><description>Password manager vault keys</description></item>
///   <item><description>Credential-scoped symmetric key derivation</description></item>
/// </list>
/// </para>
/// <para>
/// The authenticator creates random credential secrets, with separate verified and unverified
/// variants. PINs and biometrics authorize use; they are not the source of those secrets. Repeatable
/// output requires the same credential, salt and actual UV mode. hmac-secret does not support up:false.
/// Salt authentication is separate from the ceremony's pinUvAuthParam over clientDataHash.
/// </para>
/// <para>
/// This DTO borrows its dictionary and byte buffers until encoding/transmission completes. It does
/// not validate capability advertisement, key-agreement coherence or protocol-specific lengths.
/// Clear caller-owned sensitive inputs and encoded copies after use; the DTO has no cleanup owner.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-hmac-secret-extension
/// </para>
/// </remarks>
public sealed class HmacSecretInput
{
    /// <summary>
    /// Gets or sets the platform's key agreement public key (COSE format).
    /// </summary>
    /// <remarks>
    /// This is an ephemeral EC P-256 public key used for ECDH with the authenticator.
    /// Must be encoded as a COSE_Key.
    /// </remarks>
    public required IReadOnlyDictionary<int, object?> KeyAgreement { get; init; }

    /// <summary>
    /// Gets or sets the encrypted salt values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contains one or two 32-byte salts encrypted with the shared secret:
    /// <list type="bullet">
    ///   <item><description>Protocol 1: 32 bytes for one salt, 64 bytes for two; no IV prefix.</description></item>
    ///   <item><description>Protocol 2: 48 bytes for one salt, 80 bytes for two; includes a 16-byte IV.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For PIN protocol 2, the ciphertext includes the IV prefix.
    /// </para>
    /// </remarks>
    public required ReadOnlyMemory<byte> SaltEnc { get; init; }

    /// <summary>
    /// Gets or sets the HMAC over the encrypted salts.
    /// </summary>
    /// <remarks>
    /// Computed as: authenticate(sharedSecret, saltEnc).
    /// For PIN protocol 1: first 16 bytes of HMAC-SHA-256.
    /// For PIN protocol 2: full 32 bytes of HMAC-SHA-256.
    /// </remarks>
    public required ReadOnlyMemory<byte> SaltAuth { get; init; }

    /// <summary>
    /// Gets or sets the PIN/UV auth protocol version.
    /// </summary>
    /// <remarks>
    /// Must match the protocol used to compute keyAgreement, saltEnc, and saltAuth.
    /// Typical values: 1 or 2.
    /// </remarks>
    public int PinUvAuthProtocol { get; init; } = 2;

    /// <summary>
    /// Encodes this hmac-secret input as CBOR.
    /// </summary>
    /// <param name="writer">The CBOR writer.</param>
    public void Encode(CborWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        // hmac-secret input: { 1: keyAgreement, 2: saltEnc, 3: saltAuth, 4: pinUvAuthProtocol }
        writer.WriteStartMap(4);

        // 0x01: keyAgreement (COSE_Key)
        writer.WriteInt32(1);
        WriteCoseKey(writer, KeyAgreement);

        // 0x02: saltEnc
        writer.WriteInt32(2);
        writer.WriteByteString(SaltEnc.Span);

        // 0x03: saltAuth
        writer.WriteInt32(3);
        writer.WriteByteString(SaltAuth.Span);

        // 0x04: pinUvAuthProtocol
        writer.WriteInt32(4);
        writer.WriteInt32(PinUvAuthProtocol);

        writer.WriteEndMap();
    }

    /// <summary>
    /// Encodes this hmac-secret input as a CBOR byte array.
    /// </summary>
    /// <returns>A new caller-owned CBOR copy of the encrypted salts and authentication material.</returns>
    public byte[] Encode()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        Encode(writer);
        return writer.Encode();
    }
}