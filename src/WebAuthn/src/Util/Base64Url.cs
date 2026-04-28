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

namespace Yubico.YubiKit.WebAuthn.Util;

/// <summary>
/// Base64URL encoding/decoding per RFC 4648 §5 (URL-safe, no padding).
/// </summary>
internal static class Base64Url
{
    /// <summary>
    /// Encodes bytes to a base64url string (no padding).
    /// </summary>
    public static string Encode(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

        // Standard Base64 encode
        var base64 = Convert.ToBase64String(data);

        // Replace characters and remove padding
        return base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Decodes a base64url string to bytes.
    /// </summary>
    public static byte[] Decode(string base64Url)
    {
        if (string.IsNullOrEmpty(base64Url))
        {
            return [];
        }

        // Restore standard Base64 characters
        var base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        // Add padding if needed
        var padding = (4 - (base64.Length % 4)) % 4;
        if (padding > 0)
        {
            base64 += new string('=', padding);
        }

        return Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Attempts to decode a base64url string to a span.
    /// </summary>
    public static bool TryDecode(string base64Url, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;

        if (string.IsNullOrEmpty(base64Url))
        {
            return true; // Empty string is valid (0 bytes)
        }

        try
        {
            var decoded = Decode(base64Url);
            if (decoded.Length > destination.Length)
            {
                return false;
            }

            decoded.CopyTo(destination);
            bytesWritten = decoded.Length;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
