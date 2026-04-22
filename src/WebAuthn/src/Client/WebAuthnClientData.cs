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

using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.WebAuthn.Util;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// Client data for WebAuthn operations.
/// </summary>
/// <remarks>
/// <para>
/// Encapsulates the clientDataJSON and its SHA-256 hash for transmission
/// to the authenticator.
/// </para>
/// <para>
/// Per WebAuthn spec, the JSON key order MUST be: type, challenge, origin, crossOrigin
/// (when present), topOrigin (when present).
/// </para>
/// </remarks>
public sealed class WebAuthnClientData
{
    /// <summary>
    /// Gets the raw client data JSON bytes.
    /// </summary>
    public ReadOnlyMemory<byte> JsonBytes { get; }

    /// <summary>
    /// Gets the SHA-256 hash of the client data JSON (exactly 32 bytes).
    /// </summary>
    public ReadOnlyMemory<byte> Hash { get; }

    /// <summary>
    /// Gets the operation type ("webauthn.create" or "webauthn.get").
    /// </summary>
    public string Type { get; }

    private WebAuthnClientData(ReadOnlyMemory<byte> jsonBytes, ReadOnlyMemory<byte> hash, string type)
    {
        JsonBytes = jsonBytes;
        Hash = hash;
        Type = type;
    }

    /// <summary>
    /// Creates client data for a WebAuthn operation.
    /// </summary>
    /// <param name="type">The operation type ("webauthn.create" or "webauthn.get").</param>
    /// <param name="challenge">The challenge from the relying party.</param>
    /// <param name="origin">The origin URL.</param>
    /// <param name="crossOrigin">Whether this is a cross-origin request. If null, the field is omitted.</param>
    /// <param name="topOrigin">The top-level origin for cross-origin requests. If null, the field is omitted.</param>
    /// <returns>A WebAuthnClientData instance with populated JSON and hash.</returns>
    public static WebAuthnClientData Create(
        string type,
        ReadOnlyMemory<byte> challenge,
        WebAuthnOrigin origin,
        bool? crossOrigin = null,
        string? topOrigin = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(origin);

        var json = BuildJson(type, challenge.Span, origin.StringValue, crossOrigin, topOrigin);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(jsonBytes, hash);

        return new WebAuthnClientData(jsonBytes, hash.ToArray(), type);
    }

    /// <summary>
    /// Hand-builds JSON with exact key ordering per WebAuthn spec.
    /// </summary>
    /// <remarks>
    /// Key order MUST be: type, challenge, origin, crossOrigin (if not null), topOrigin (if not null).
    /// String escaping uses System.Text.Json.JsonEncodedText for spec-compliant output.
    /// </remarks>
    private static string BuildJson(
        string type,
        ReadOnlySpan<byte> challenge,
        string originString,
        bool? crossOrigin,
        string? topOrigin)
    {
        var sb = new StringBuilder();
        sb.Append('{');

        // "type": "<value>"
        sb.Append("\"type\":");
        AppendJsonString(sb, type);

        // "challenge": "<base64url>"
        sb.Append(",\"challenge\":");
        var challengeBase64Url = Base64Url.Encode(challenge);
        AppendJsonString(sb, challengeBase64Url);

        // "origin": "<value>"
        sb.Append(",\"origin\":");
        AppendJsonString(sb, originString);

        // "crossOrigin": true/false (included even if false per Swift reference)
        sb.Append(",\"crossOrigin\":");
        sb.Append(crossOrigin == true ? "true" : "false");

        // "topOrigin": "<value>" (only if not null)
        if (topOrigin is not null)
        {
            sb.Append(",\"topOrigin\":");
            AppendJsonString(sb, topOrigin);
        }

        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// Appends a properly JSON-escaped string value (with surrounding quotes).
    /// </summary>
    private static void AppendJsonString(StringBuilder sb, string value)
    {
        sb.Append('"');

        // Escape special characters per JSON spec
        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        // Unicode escape for control characters
                        sb.Append($"\\u{(int)c:x4}");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }

        sb.Append('"');
    }
}
