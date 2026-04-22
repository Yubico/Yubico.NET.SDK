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

using System.Diagnostics.CodeAnalysis;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// A validated WebAuthn origin.
/// </summary>
/// <remarks>
/// <para>
/// Represents the serialized origin (scheme://host[:port]) for WebAuthn operations.
/// Path, query, and fragment components are stripped. The origin must be a secure
/// context (https:// or http://localhost).
/// </para>
/// <para>
/// See: https://tools.ietf.org/html/rfc6454 (The Web Origin Concept)
/// See: https://w3c.github.io/webappsec-secure-contexts/ (Secure Contexts)
/// </para>
/// </remarks>
public sealed class WebAuthnOrigin : IEquatable<WebAuthnOrigin>
{
    /// <summary>
    /// Gets the URI scheme (e.g., "https").
    /// </summary>
    public string Scheme { get; }

    /// <summary>
    /// Gets the host component (e.g., "example.com").
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// Gets the port number, or -1 if using the default port for the scheme.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Gets the serialized origin string (scheme://host[:port]).
    /// </summary>
    public string StringValue { get; }

    private WebAuthnOrigin(string scheme, string host, int port, string stringValue)
    {
        Scheme = scheme;
        Host = host;
        Port = port;
        StringValue = stringValue;
    }

    /// <summary>
    /// Attempts to parse a URL string into a WebAuthn origin.
    /// </summary>
    /// <param name="url">The URL string to parse.</param>
    /// <param name="origin">The parsed origin, or null if parsing failed.</param>
    /// <returns>True if the URL is a valid secure origin; false otherwise.</returns>
    public static bool TryParse(string url, [NotNullWhen(true)] out WebAuthnOrigin? origin)
    {
        origin = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var scheme = uri.Scheme.ToLowerInvariant();
        var host = uri.Host;

        if (string.IsNullOrEmpty(host))
        {
            return false;
        }

        // Reject opaque URLs (data:, javascript:, file:, etc.)
        if (scheme is "data" or "javascript" or "file" or "about" or "blob")
        {
            return false;
        }

        // Only accept http and https schemes
        if (scheme is not "http" and not "https")
        {
            return false;
        }

        // Secure context validation: https, or http only for localhost
        if (scheme == "http" && !IsLoopback(host))
        {
            return false;
        }

        var port = uri.Port;
        string stringValue;

        if (IsDefaultPort(scheme, port))
        {
            stringValue = $"{scheme}://{host}";
            port = -1; // Indicate default port
        }
        else
        {
            stringValue = $"{scheme}://{host}:{port}";
        }

        origin = new WebAuthnOrigin(scheme, host, port, stringValue);
        return true;
    }

    /// <summary>
    /// Validates if the given RP ID is valid for this origin.
    /// </summary>
    /// <param name="rpId">The relying party identifier to validate.</param>
    /// <param name="isPublicSuffix">
    /// A predicate that returns true if a given domain is a public suffix
    /// (e.g., "com", "co.uk"). This is used for effective domain calculation.
    /// </param>
    /// <param name="enterpriseRpIds">
    /// Optional set of enterprise RP IDs that bypass the suffix check.
    /// </param>
    /// <returns>True if the RP ID is valid for this origin; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// Per WebAuthn §5.1.3, the RP ID must be a registrable suffix of the origin's
    /// effective domain. The effective domain is computed using the Public Suffix List
    /// to avoid allowing RPs to claim credentials across unrelated domains.
    /// </para>
    /// <para>
    /// Enterprise allow-list: If rpId appears in enterpriseRpIds, the suffix check
    /// is bypassed (useful for internal deployments).
    /// </para>
    /// </remarks>
    public bool IsRpIdValid(
        string rpId,
        Func<string, bool> isPublicSuffix,
        IReadOnlySet<string>? enterpriseRpIds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rpId);
        ArgumentNullException.ThrowIfNull(isPublicSuffix);

        // Enterprise allow-list bypass
        if (enterpriseRpIds?.Contains(rpId) == true)
        {
            return true;
        }

        // RP ID must be a suffix of the origin host (case-insensitive)
        var host = Host.ToLowerInvariant();
        var rpIdLower = rpId.ToLowerInvariant();

        if (!IsSuffixOf(rpIdLower, host))
        {
            return false;
        }

        // Exact match is always valid
        if (rpIdLower == host)
        {
            return true;
        }

        // For subdomain matches, ensure rpId is a registrable domain
        // (i.e., not a public suffix itself)
        if (isPublicSuffix(rpIdLower))
        {
            return false;
        }

        // Ensure the origin host's effective domain matches or is a subdomain of rpId
        var effectiveDomain = GetEffectiveDomain(host, isPublicSuffix);
        return rpIdLower == effectiveDomain || IsSuffixOf(rpIdLower, effectiveDomain);
    }

    /// <summary>
    /// Checks if the host is localhost per W3C Secure Contexts spec.
    /// </summary>
    private static bool IsLoopback(string host)
    {
        var lower = host.ToLowerInvariant();
        return lower == "localhost" || lower.EndsWith(".localhost");
    }

    /// <summary>
    /// Checks if the port is the default port for the given scheme.
    /// </summary>
    private static bool IsDefaultPort(string scheme, int port) =>
        scheme switch
        {
            "http" => port == 80,
            "https" => port == 443,
            _ => false
        };

    /// <summary>
    /// Checks if needle is a DNS suffix of haystack (including exact match).
    /// </summary>
    private static bool IsSuffixOf(string needle, string haystack)
    {
        if (needle == haystack)
        {
            return true;
        }

        // haystack must end with ".{needle}"
        if (haystack.Length <= needle.Length)
        {
            return false;
        }

        return haystack.EndsWith(needle) && haystack[haystack.Length - needle.Length - 1] == '.';
    }

    /// <summary>
    /// Computes the effective (registrable) domain using the Public Suffix List predicate.
    /// </summary>
    private static string GetEffectiveDomain(string host, Func<string, bool> isPublicSuffix)
    {
        var labels = host.Split('.');

        // Walk from right to left: find the longest public suffix, then take one more label
        for (var i = labels.Length - 1; i >= 0; i--)
        {
            var candidate = string.Join(".", labels[i..]);
            if (isPublicSuffix(candidate))
            {
                // Take one label before the public suffix
                if (i > 0)
                {
                    return string.Join(".", labels[(i - 1)..]);
                }

                // Host itself is a public suffix (rare, but possible)
                return host;
            }
        }

        // No public suffix matched - treat the whole host as effective domain
        return host;
    }

    public override bool Equals(object? obj) =>
        obj is WebAuthnOrigin other && Equals(other);

    public bool Equals(WebAuthnOrigin? other)
    {
        if (other is null)
        {
            return false;
        }

        return Scheme == other.Scheme &&
               Host == other.Host &&
               Port == other.Port;
    }

    public override int GetHashCode() =>
        HashCode.Combine(Scheme, Host, Port);

    public override string ToString() => StringValue;

    public static bool operator ==(WebAuthnOrigin? left, WebAuthnOrigin? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(WebAuthnOrigin? left, WebAuthnOrigin? right) =>
        !(left == right);
}
