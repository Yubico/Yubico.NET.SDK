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

namespace Yubico.YubiKit.WebAuthn.Client.Validation;

/// <summary>
/// Validates RP ID against origin per WebAuthn specification.
/// </summary>
internal static class RpIdValidator
{
    /// <summary>
    /// Ensures the RP ID is valid for the given origin.
    /// </summary>
    /// <param name="rpId">The relying party identifier.</param>
    /// <param name="origin">The WebAuthn origin.</param>
    /// <param name="enterpriseRpIds">Set of enterprise-allowed RP IDs that bypass suffix checks.</param>
    /// <param name="isPublicSuffix">Predicate to determine if a domain is a public suffix (e.g., "com", "co.uk").</param>
    /// <exception cref="WebAuthnClientError">Thrown when RP ID is invalid for the origin.</exception>
    /// <remarks>
    /// <para>
    /// Per WebAuthn spec, the RP ID must be either:
    /// 1. Exactly equal to the origin's host, OR
    /// 2. A registrable domain suffix of the origin's host (and not a public suffix), OR
    /// 3. Present in the enterprise allow-list.
    /// </para>
    /// <para>
    /// Example valid combinations:
    /// - origin: https://example.com, rpId: example.com (exact match)
    /// - origin: https://login.example.com, rpId: example.com (suffix match, not public suffix)
    /// - origin: https://example.com, rpId: partner.test (enterprise allow-list entry)
    /// </para>
    /// <para>
    /// Example invalid combinations:
    /// - origin: https://example.com, rpId: evil.com (no match)
    /// - origin: https://example.com, rpId: com (public suffix)
    /// </para>
    /// </remarks>
    public static void EnsureValid(
        string rpId,
        WebAuthnOrigin origin,
        IReadOnlySet<string> enterpriseRpIds,
        Func<string, bool> isPublicSuffix)
    {
        ArgumentNullException.ThrowIfNull(rpId);
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(enterpriseRpIds);
        ArgumentNullException.ThrowIfNull(isPublicSuffix);

        var originHost = origin.Host;

        // Case 1: Exact match
        if (string.Equals(rpId, originHost, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Case 2: Suffix match (registrable domain, not a public suffix)
        if (originHost.EndsWith("." + rpId, StringComparison.OrdinalIgnoreCase))
        {
            if (isPublicSuffix(rpId))
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidRequest,
                    $"RP ID '{rpId}' is a public suffix and cannot be used as a domain suffix for origin '{origin}'");
            }

            // Valid suffix match
            return;
        }

        // Case 3: Enterprise allow-list
        if (enterpriseRpIds.Contains(rpId))
        {
            return;
        }

        // No valid match
        throw new WebAuthnClientError(
            WebAuthnClientErrorCode.InvalidRequest,
            $"RP ID '{rpId}' is not valid for origin '{origin}'. " +
            $"The RP ID must be the origin's host, a registrable domain suffix, or an enterprise-allowed RP ID.");
    }
}
