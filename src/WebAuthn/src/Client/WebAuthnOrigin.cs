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
/// A parsed origin within the SDK's supported scheme and host policy.
/// </summary>
/// <remarks>
/// <para>
/// Represents the serialized origin (scheme://host[:port]) for WebAuthn operations.
/// Path, query, and fragment components are stripped. HTTPS is supported; HTTP is supported only
/// for localhost and host names ending in .localhost. This scheme/host check is not a complete
/// browser Secure Contexts evaluation and does not check document ancestry or name resolution.
/// </para>
/// <para>
/// Origin parsing is independent of application RP validity and authorization. Arbitrary native-app,
/// opaque, and custom-scheme origins are not representable by this type; configuring
/// <see cref="WebAuthnClientOptions.RpIdValidator"/> does not extend its supported origins.
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
    /// <returns>True if the URL parses and meets the supported HTTPS or HTTP localhost/.localhost policy; false otherwise.</returns>
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

        // Restricted HTTP policy: localhost names only, not a complete Secure Contexts evaluation.
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
    /// Checks for localhost or a .localhost suffix; does not resolve names or accept loopback IP addresses.
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