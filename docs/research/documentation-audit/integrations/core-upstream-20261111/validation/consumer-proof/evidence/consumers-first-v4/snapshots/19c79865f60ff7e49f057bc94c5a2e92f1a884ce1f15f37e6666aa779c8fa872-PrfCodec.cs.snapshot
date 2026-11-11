// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;

namespace Yubico.YubiKit.WebAuthn.Extensions.Codec;

/// <summary>
/// Pure translation between the WebAuthn prf extension (L3 §10.1.4) and the CTAP hmac-secret
/// extension (CTAP 2.1 §12.5; CTAP 2.2–2.3 §12.7) it is built on. Salt derivation lives on
/// <see cref="Inputs.PrfInput.ComputeSalt"/>; the encrypted request map is produced by the CTAP2
/// <see cref="ExtensionBuilder.WithHmacSecret(IPinUvAuthProtocol, ReadOnlySpan{byte}, IReadOnlyDictionary{int, object?}, ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>.
/// </summary>
internal static class PrfCodec
{
    /// <summary>CTAP 2.1 §12.5 / CTAP 2.2–2.3 §12.7: each PRF output is a 32-byte HMAC-SHA-256 value.</summary>
    public const int OutputLength = 32;

    /// <summary>
    /// L3 §10.1.4 registration: "Set enabled to the value of hmac-secret in the authenticator extensions
    /// output. If not present, set enabled to false."
    /// </summary>
    public static PrfRegistrationOutput ParseRegistrationOutput(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> extensions)
    {
        if (!extensions.TryGetValue(ExtensionIdentifiers.HmacSecret, out var rawValue))
        {
            return new PrfRegistrationOutput(Enabled: false);
        }

        var reader = new CborReader(rawValue, CborConformanceMode.Lax);
        // Only a CBOR true enables; anything else (false, or a value of another type) reports disabled.
        bool enabled = reader.PeekState() == CborReaderState.Boolean && reader.ReadBoolean();
        return new PrfRegistrationOutput(enabled);
    }

    /// <summary>
    /// Decrypts a CTAP hmac-secret getAssertion output (AES-256-CBC under the shared secret, no
    /// padding) and splits it into one or two 32-byte PRF results. Returns null when the plaintext is
    /// not exactly one or two outputs long. Omitting that result is this SDK's processing policy,
    /// not a malformed-output rule prescribed by WebAuthn §9.4.
    /// </summary>
    /// <remarks>
    /// The aggregate plaintext is cleared, but the array-range temporaries and returned result arrays
    /// are not. Returned read-only views have no disposal or zeroing guarantee.
    /// </remarks>
    public static PrfEvaluationResults? DecryptResults(
        IPinUvAuthProtocol protocol,
        ReadOnlySpan<byte> sharedSecret,
        ReadOnlyMemory<byte> hmacSecretOutput)
    {
        byte[] plaintext;
        try
        {
            plaintext = protocol.Decrypt(sharedSecret, hmacSecretOutput.Span);
        }
        catch (Exception ex) when (ex is ArgumentException or System.Security.Cryptography.CryptographicException)
        {
            return null;
        }

        try
        {
            return plaintext.Length switch
            {
                OutputLength => new PrfEvaluationResults(plaintext[..OutputLength].ToArray()),
                2 * OutputLength => new PrfEvaluationResults(
                    plaintext[..OutputLength].ToArray(),
                    plaintext[OutputLength..].ToArray()),
                _ => null
            };
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(plaintext);
        }
    }
}