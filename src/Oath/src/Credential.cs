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

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Yubico.YubiKit.Oath;

/// <summary>
///     Represents an OATH credential stored on a YubiKey device.
/// </summary>
/// <remarks>
///     <para>
///         Credentials are uniquely identified by the combination of <see cref="DeviceId" />
///         and <see cref="Id" />. Two credentials are considered equal if and only if they
///         share the same device ID and raw credential ID bytes.
///     </para>
///     <para>
///         Credentials are naturally ordered by issuer/name for display purposes,
///         matching the canonical Python implementation's ordering.
///     </para>
/// </remarks>
public sealed partial class Credential : IEquatable<Credential>, IComparable<Credential>
{
    // Host-side TOTP credential ID convention shared by ykman, the Android and Swift SDKs:
    // "[period/][issuer:]name". YKOATH itself does not define the name format. ASCII digits only;
    // \d would also match other Unicode decimal digits, which int.Parse then rejects.
    [GeneratedRegex(@"^(([0-9]+)/)?(([^:]+):)?(.+)$")]
    private static partial Regex TotpIdPattern();

    /// <summary>
    ///     Gets the device identifier that this credential belongs to.
    /// </summary>
    public string DeviceId { get; }

    /// <summary>
    ///     Gets the raw credential ID bytes in wire format.
    /// </summary>
    public ReadOnlyMemory<byte> Id { get; }

    /// <summary>
    ///     Gets the credential issuer, if present.
    /// </summary>
    public string? Issuer { get; }

    /// <summary>
    ///     Gets the account name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the OATH credential type (TOTP or HOTP).
    /// </summary>
    public OathType OathType { get; }

    /// <summary>
    ///     Gets the time step period in seconds. Only meaningful for TOTP credentials.
    /// </summary>
    public int Period { get; }

    /// <summary>
    ///     Gets whether touch is required to calculate this credential.
    ///     <c>null</c> when unknown (e.g., from a LIST response).
    /// </summary>
    public bool? TouchRequired { get; }

    /// <summary>
    ///     Initializes a new <see cref="Credential" /> instance.
    /// </summary>
    public Credential(
        string deviceId,
        ReadOnlyMemory<byte> id,
        string? issuer,
        string name,
        OathType oathType,
        int period,
        bool? touchRequired)
    {
        DeviceId = deviceId;
        Id = id.ToArray(); // Defensive copy to prevent external mutation
        Issuer = issuer;
        Name = name;
        OathType = oathType;
        Period = period;
        TouchRequired = touchRequired;
    }

    /// <summary>
    ///     Formats a credential ID for the OATH applet wire protocol.
    /// </summary>
    /// <param name="issuer">The credential issuer, or <c>null</c>.</param>
    /// <param name="name">The account name.</param>
    /// <param name="oathType">The OATH type (TOTP or HOTP).</param>
    /// <param name="period">The TOTP period in seconds.</param>
    /// <returns>The credential ID as UTF-8 encoded bytes.</returns>
    internal static byte[] FormatCredentialId(
        string? issuer,
        string name,
        OathType oathType,
        int period = OathConstants.DefaultPeriod)
    {
        var credId = new StringBuilder();

        if (oathType == OathType.Totp && period != OathConstants.DefaultPeriod)
        {
            credId.Append(period);
            credId.Append('/');
        }

        if (issuer is not null)
        {
            credId.Append(issuer);
            credId.Append(':');
        }

        credId.Append(name);

        return Encoding.UTF8.GetBytes(credId.ToString());
    }

    /// <summary>
    ///     Parses a credential ID from the OATH applet wire protocol.
    /// </summary>
    /// <remarks>
    ///     For TOTP the optional <c>period/</c> prefix is the RFC 6238 §4.1 time step X in seconds, the
    ///     divisor in <c>T = (Current Unix time - T0) / X</c> (§4.2). A prefix that is not a positive
    ///     integer is malformed. HOTP has no period and reports 0.
    /// </remarks>
    /// <param name="credentialId">The raw credential ID bytes.</param>
    /// <param name="oathType">The OATH type (TOTP or HOTP).</param>
    /// <returns>A tuple of (issuer, name, period).</returns>
    /// <exception cref="ArgumentException">
    ///     <paramref name="oathType" /> is TOTP and the period prefix is zero, negative or out of range.
    /// </exception>
    internal static (string? Issuer, string Name, int Period) ParseCredentialId(
        ReadOnlySpan<byte> credentialId,
        OathType oathType)
    {
        string data = Encoding.UTF8.GetString(credentialId);

        if (oathType == OathType.Totp)
        {
            var match = TotpIdPattern().Match(data);
            if (match.Success)
            {
                string? periodStr = match.Groups[2].Success ? match.Groups[2].Value : null;
                string? issuer = match.Groups[4].Success ? match.Groups[4].Value : null;
                string name = match.Groups[5].Value;
                int period = OathConstants.DefaultPeriod;
                if (periodStr is not null
                    && (!int.TryParse(periodStr, NumberStyles.None, CultureInfo.InvariantCulture, out period)
                        || period <= 0))
                {
                    throw new ArgumentException(
                        "TOTP credential ID has an invalid period prefix; the period must be a positive number of seconds (RFC 6238 §4.1).",
                        nameof(credentialId));
                }

                return (issuer, name, period);
            }

            return (null, data, OathConstants.DefaultPeriod);
        }

        // HOTP: simple "issuer:name" or just "name"
        if (data.Contains(':', StringComparison.Ordinal) && data[0] != ':')
        {
            int colonIndex = data.IndexOf(':');
            return (data[..colonIndex], data[(colonIndex + 1)..], 0);
        }

        return (null, data, 0);
    }

    /// <inheritdoc />
    public bool Equals(Credential? other)
    {
        if (other is null)
        {
            return false;
        }

        return DeviceId == other.DeviceId && Id.Span.SequenceEqual(other.Id.Span);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Credential);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(DeviceId);
        foreach (byte b in Id.Span)
        {
            hash.Add(b);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public int CompareTo(Credential? other)
    {
        if (other is null)
        {
            return 1;
        }

        int cmp = string.Compare(Issuer ?? Name, other.Issuer ?? other.Name, StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
        {
            return cmp;
        }

        return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Returns equality based on <see cref="DeviceId" /> and <see cref="Id" />.
    /// </summary>
    public static bool operator ==(Credential? left, Credential? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>
    ///     Returns inequality based on <see cref="DeviceId" /> and <see cref="Id" />.
    /// </summary>
    public static bool operator !=(Credential? left, Credential? right) => !(left == right);
}