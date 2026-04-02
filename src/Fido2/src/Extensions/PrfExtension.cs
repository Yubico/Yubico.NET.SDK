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
/// Input for the PRF (Pseudo-Random Function) extension.
/// </summary>
/// <remarks>
/// <para>
/// The PRF extension is the WebAuthn-level interface to the CTAP hmac-secret extension.
/// It allows deriving secrets using arbitrary inputs (called "eval" and "evalByCredential").
/// </para>
/// <para>
/// At the CTAP level, this maps to hmac-secret with the salt values derived from
/// the PRF inputs via hashing.
/// </para>
/// <para>
/// See: https://w3c.github.io/webauthn/#prf-extension
/// </para>
/// </remarks>
public sealed class PrfInput
{
    /// <summary>
    /// Gets or sets the first PRF input for evaluation.
    /// </summary>
    /// <remarks>
    /// Used to derive the first salt for hmac-secret.
    /// Salt is computed as: SHA-256("WebAuthn PRF" || 0x00 || first).
    /// </remarks>
    public ReadOnlyMemory<byte>? First { get; init; }
    
    /// <summary>
    /// Gets or sets the second PRF input for evaluation (optional).
    /// </summary>
    /// <remarks>
    /// Used to derive the second salt for hmac-secret.
    /// Salt is computed as: SHA-256("WebAuthn PRF" || 0x00 || second).
    /// </remarks>
    public ReadOnlyMemory<byte>? Second { get; init; }
    
    /// <summary>
    /// Gets or sets per-credential PRF inputs.
    /// </summary>
    /// <remarks>
    /// Maps credential IDs (base64url) to PRF inputs.
    /// Used when different credentials should use different PRF inputs.
    /// </remarks>
    public IReadOnlyDictionary<string, PrfInputValues>? EvalByCredential { get; init; }
    
    /// <summary>
    /// Computes the salt for hmac-secret from a PRF input.
    /// </summary>
    /// <param name="input">The PRF input value.</param>
    /// <returns>The 32-byte salt for hmac-secret.</returns>
    public static byte[] ComputeSalt(ReadOnlySpan<byte> input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        
        // "WebAuthn PRF" || 0x00 || input
        var prefix = "WebAuthn PRF"u8;
        var data = new byte[prefix.Length + 1 + input.Length];
        prefix.CopyTo(data);
        data[prefix.Length] = 0x00;
        input.CopyTo(data.AsSpan(prefix.Length + 1));
        
        return sha256.ComputeHash(data);
    }
}

/// <summary>
/// Per-credential PRF input values.
/// </summary>
public sealed class PrfInputValues
{
    /// <summary>
    /// Gets or sets the first PRF input.
    /// </summary>
    public required ReadOnlyMemory<byte> First { get; init; }
    
    /// <summary>
    /// Gets or sets the second PRF input (optional).
    /// </summary>
    public ReadOnlyMemory<byte>? Second { get; init; }
}

/// <summary>
/// Output from the PRF extension.
/// </summary>
/// <remarks>
/// Contains the derived secrets from the PRF evaluation.
/// </remarks>
public sealed class PrfOutput
{
    /// <summary>
    /// Gets whether the authenticator supports PRF.
    /// </summary>
    /// <remarks>
    /// During makeCredential registration, this indicates PRF capability.
    /// </remarks>
    public bool Enabled { get; init; }
    
    /// <summary>
    /// Gets the first derived output.
    /// </summary>
    /// <remarks>
    /// 32-byte secret derived from the first PRF input.
    /// </remarks>
    public ReadOnlyMemory<byte>? First { get; init; }
    
    /// <summary>
    /// Gets the second derived output.
    /// </summary>
    /// <remarks>
    /// 32-byte secret derived from the second PRF input (if provided).
    /// </remarks>
    public ReadOnlyMemory<byte>? Second { get; init; }
    
    /// <summary>
    /// Decodes PRF output from decrypted hmac-secret outputs.
    /// </summary>
    /// <param name="decryptedOutput">The decrypted output from hmac-secret.</param>
    /// <param name="hasTwoOutputs">Whether two outputs were requested.</param>
    /// <returns>The decoded PRF output.</returns>
    public static PrfOutput FromHmacSecretOutput(
        ReadOnlySpan<byte> decryptedOutput, 
        bool hasTwoOutputs = false)
    {
        if (decryptedOutput.Length < 32)
        {
            throw new ArgumentException(
                "Decrypted output must be at least 32 bytes.", 
                nameof(decryptedOutput));
        }
        
        var first = decryptedOutput[..32].ToArray();
        byte[]? second = null;
        
        if (hasTwoOutputs && decryptedOutput.Length >= 64)
        {
            second = decryptedOutput[32..64].ToArray();
        }
        
        return new PrfOutput
        {
            Enabled = true,
            First = first,
            Second = second
        };
    }
}
