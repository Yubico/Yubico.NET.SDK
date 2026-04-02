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

namespace Yubico.YubiKit.Fido2.Extensions;

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
///   <item><description>Any application requiring a stable secret derived from biometrics/PIN</description></item>
/// </list>
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
    ///   <item><description>For one salt: encrypt(sharedSecret, salt1) = 32 bytes</description></item>
    ///   <item><description>For two salts: encrypt(sharedSecret, salt1 || salt2) = 64 bytes</description></item>
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
    /// <returns>The CBOR-encoded input.</returns>
    public byte[] Encode()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        Encode(writer);
        return writer.Encode();
    }
    
    private static void WriteCoseKey(CborWriter writer, IReadOnlyDictionary<int, object?> key)
    {
        writer.WriteStartMap(key.Count);
        foreach (var kvp in key.OrderBy(k => k.Key))
        {
            writer.WriteInt32(kvp.Key);
            switch (kvp.Value)
            {
                case int i:
                    writer.WriteInt32(i);
                    break;
                case byte[] bytes:
                    writer.WriteByteString(bytes);
                    break;
                case null:
                    writer.WriteNull();
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported COSE key value type: {kvp.Value?.GetType().Name}");
            }
        }
        writer.WriteEndMap();
    }
}

/// <summary>
/// Output from the hmac-secret extension during getAssertion.
/// </summary>
/// <remarks>
/// <para>
/// Contains the encrypted derived secret(s). The client must decrypt using
/// the shared secret established during key agreement.
/// </para>
/// </remarks>
public sealed class HmacSecretOutput
{
    /// <summary>
    /// Gets the encrypted output value(s).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contains one or two 32-byte derived secrets, encrypted:
    /// <list type="bullet">
    ///   <item><description>For one salt: encrypt(sharedSecret, output1) = 32+ bytes</description></item>
    ///   <item><description>For two salts: encrypt(sharedSecret, output1 || output2) = 64+ bytes</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For PIN protocol 2, includes the IV prefix (16 bytes).
    /// </para>
    /// </remarks>
    public required ReadOnlyMemory<byte> Output { get; init; }
    
    /// <summary>
    /// Decodes hmac-secret output from CBOR bytes.
    /// </summary>
    /// <param name="data">The CBOR-encoded output.</param>
    /// <returns>The decoded output.</returns>
    public static HmacSecretOutput Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Lax);
        return Decode(reader);
    }
    
    /// <summary>
    /// Decodes hmac-secret output from a CBOR reader.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The decoded output.</returns>
    public static HmacSecretOutput Decode(CborReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        
        // The output is just the encrypted byte string
        var output = reader.ReadByteString();
        
        return new HmacSecretOutput { Output = output };
    }
}
