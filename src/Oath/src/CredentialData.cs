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

using System.Security.Cryptography;
using System.Web;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Oath;

/// <summary>
///     Holds the data needed to create an OATH credential on a YubiKey.
/// </summary>
/// <remarks>
///     Properties can be assigned only during initialization. This instance owns a copy of the supplied
///     secret. Dispose it after all operations using it have completed; do not dispose concurrently with
///     an operation or a consumer of its secret view.
/// </remarks>
public sealed class CredentialData : IDisposable
{
    private readonly byte[] _secret = [];

    /// <summary>
    ///     Gets the account name, assigned during initialization. Outgoing identities must be nonempty,
    ///     fit 64 UTF-8 bytes including issuer/period metadata, and round-trip through the host ID convention.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets the OATH credential type, assigned during initialization.
    /// </summary>
    public required OathType OathType { get; init; }

    /// <summary>
    ///     Gets the hash algorithm for HMAC computation, assigned during initialization.
    /// </summary>
    public required OathHashAlgorithm HashAlgorithm { get; init; }

    /// <summary>
    ///     Gets a borrowed read-only view of the owned shared secret key, or initializes it by copying input.
    /// </summary>
    /// <remarks>
    ///     Initialization copies the supplied bytes; the caller still owns and must clear the input buffer.
    ///     PUT requires nonempty secret material; the SDK's storage padding does not supply missing material.
    ///     Reading this property does not allocate or transfer ownership. The view is valid only until this
    ///     instance is disposed, which clears its backing storage and all existing views. Do not modify the
    ///     backing storage through array-extraction APIs. If an independent copy is needed, the caller owns
    ///     that copy and must clear it separately.
    /// </remarks>
    public required ReadOnlyMemory<byte> Secret
    {
        get => _secret;
        init => _secret = value.ToArray();
    }

    /// <summary>
    ///     Gets the number of digits in the OTP code, from 6 through 8. Defaults to 6.
    /// </summary>
    public int Digits { get; init; } = OathConstants.DefaultDigits;

    /// <summary>
    ///     Gets the positive TOTP time step period in seconds. Defaults to 30. Ignored for HOTP.
    /// </summary>
    public int Period { get; init; } = OathConstants.DefaultPeriod;

    /// <summary>
    ///     Gets the HOTP initial moving factor (counter), from 0 through <see cref="int.MaxValue" />.
    ///     Defaults to 0. Ignored for TOTP. The four-byte wire field can represent larger values,
    ///     but this API supports only nonnegative <see cref="int" /> values.
    /// </summary>
    public int Counter { get; init; } = OathConstants.DefaultImf;

    /// <summary>
    ///     Gets the credential issuer, or <see langword="null" /> for no issuer. An empty issuer is not
    ///     representable in outgoing identities; issuer and name must round-trip without reinterpretation.
    /// </summary>
    public string? Issuer { get; init; }

    /// <summary>
    ///     Gets whether individual calculations require physical touch, assigned during initialization.
    ///     Enabling this policy requires YubiKey firmware 4.2.4 or later.
    /// </summary>
    public bool RequireTouch { get; init; }

    /// <summary>
    ///     Parses an <c>otpauth://</c> URI into a <see cref="CredentialData" /> instance.
    /// </summary>
    /// <param name="uri">
    ///     An otpauth URI as defined by the Google Authenticator Key URI Format,
    ///     e.g., <c>otpauth://totp/GitHub:user@example.com?secret=JBSWY3DPEHPK3PXP&amp;issuer=GitHub</c>.
    /// </param>
    /// <returns>A populated <see cref="CredentialData" /> instance whose owned secret must be disposed.</returns>
    /// <remarks>The URI string contains secret material and cannot be cleared by this method.</remarks>
    /// <exception cref="ArgumentException">
    ///     The URI scheme/type, algorithm, numeric fields, identity, or Base32 secret is invalid,
    ///     or the secret parameter is absent or decodes to no material. Digits must be 6 through 8,
    ///     TOTP period must be positive, and HOTP counter must be a nonnegative Int32 value.
    /// </exception>
    /// <exception cref="FormatException">A numeric query field is not an integer.</exception>
    /// <exception cref="OverflowException">A numeric query field does not fit an Int32.</exception>
    public static CredentialData ParseUri(string uri) => ParseUri(uri, requireTouch: false);

    /// <summary>
    ///     Parses an <c>otpauth://</c> URI and applies the credential's physical-touch policy.
    /// </summary>
    /// <param name="uri">The otpauth URI to parse.</param>
    /// <param name="requireTouch">
    ///     Whether individual calculations require physical touch. Setting this to <see langword="true" />
    ///     requires YubiKey firmware 4.2.4 or later.
    /// </param>
    /// <returns>A populated <see cref="CredentialData" /> instance that owns its decoded secret.</returns>
    /// <remarks>
    ///     Dispose the returned instance after use. Decoded temporary bytes are cleared on success and failure;
    ///     the URI string and encoded secret strings cannot be cleared by this method.
    ///     Non-secret request fields are validated before decoding. Missing HOTP counter defaults to zero;
    ///     query issuer continues to take precedence over the label issuer.
    /// </remarks>
    /// <exception cref="ArgumentException">
    ///     The URI scheme/type, algorithm, numeric fields, identity, or Base32 secret is invalid,
    ///     or the secret parameter is absent or decodes to no material. Digits must be 6 through 8,
    ///     TOTP period must be positive, and HOTP counter must be a nonnegative Int32 value.
    /// </exception>
    /// <exception cref="FormatException">A numeric query field is not an integer.</exception>
    /// <exception cref="OverflowException">A numeric query field does not fit an Int32.</exception>
    public static CredentialData ParseUri(string uri, bool requireTouch)
    {
        var parsed = new Uri(uri.Trim());

        if (!string.Equals(parsed.Scheme, "otpauth", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid URI scheme, expected 'otpauth'.", nameof(uri));
        }

        string? host = parsed.Host;
        if (string.IsNullOrEmpty(host))
        {
            throw new ArgumentException("Missing OATH type in URI.", nameof(uri));
        }

        OathType oathType = host.ToUpperInvariant() switch
        {
            "TOTP" => OathType.Totp,
            "HOTP" => OathType.Hotp,
            _ => throw new ArgumentException($"Invalid OATH type: '{host}'.", nameof(uri))
        };

        var queryParams = HttpUtility.ParseQueryString(parsed.Query);

        string? issuer = null;
        // Decode the path, strip leading '/'
        string path = Uri.UnescapeDataString(parsed.AbsolutePath);
        if (path.StartsWith('/'))
        {
            path = path[1..];
        }

        string name;
        if (path.Contains(':', StringComparison.Ordinal))
        {
            int colonIndex = path.IndexOf(':');
            issuer = path[..colonIndex];
            name = path[(colonIndex + 1)..];
        }
        else
        {
            name = path;
        }

        // Query parameter issuer takes precedence
        string? queryIssuer = queryParams["issuer"];
        if (queryIssuer is not null)
        {
            issuer = queryIssuer;
        }

        string? secretParam = queryParams["secret"];
        if (secretParam is null)
        {
            throw new ArgumentException("Missing 'secret' parameter in URI.", nameof(uri));
        }

        string? algorithmParam = queryParams["algorithm"];
        OathHashAlgorithm hashAlgorithm = (algorithmParam?.ToUpperInvariant()) switch
        {
            "SHA256" => OathHashAlgorithm.Sha256,
            "SHA512" => OathHashAlgorithm.Sha512,
            "SHA1" or null => OathHashAlgorithm.Sha1,
            _ => throw new ArgumentException($"Invalid algorithm: '{algorithmParam}'.", nameof(uri))
        };

        string? digitsParam = queryParams["digits"];
        int digits = digitsParam is not null
            ? int.Parse(digitsParam, System.Globalization.CultureInfo.InvariantCulture)
            : OathConstants.DefaultDigits;

        // Key URI "period": the RFC 6238 §4.1 time step in seconds; it is the divisor in §4.2, so it
        // must be positive. Only meaningful for TOTP; HOTP ignores it.
        string? periodParam = queryParams["period"];
        int period = periodParam is not null
            ? int.Parse(periodParam, System.Globalization.CultureInfo.InvariantCulture)
            : OathConstants.DefaultPeriod;
        if (oathType == OathType.Totp && period <= 0)
        {
            throw new ArgumentException("TOTP 'period' must be a positive number of seconds.", nameof(uri));
        }

        string? counterParam = queryParams["counter"];
        int counter = counterParam is not null
            ? int.Parse(counterParam, System.Globalization.CultureInfo.InvariantCulture)
            : OathConstants.DefaultImf;

        ValidateRequestNumbers(oathType, digits, period, counter);
        _ = Credential.FormatCredentialId(issuer, name, oathType, period);

        byte[] secret = ParseBase32Key(secretParam);
        try
        {
            ValidateSecretMaterial(secret);
            return new CredentialData
            {
                Name = name,
                OathType = oathType,
                HashAlgorithm = hashAlgorithm,
                Secret = secret,
                Digits = digits,
                Period = period,
                Counter = counter,
                Issuer = issuer,
                RequireTouch = requireTouch
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    /// <summary>
    ///     Gets the credential ID in wire format for the OATH applet.
    /// </summary>
    /// <returns>The credential ID as UTF-8 encoded bytes.</returns>
    /// <exception cref="ArgumentException">
    ///     The name is empty, the issuer is empty rather than null, the complete ID exceeds 64 UTF-8 bytes,
    ///     or the identity cannot round-trip through the existing host convention. A TOTP period must be positive.
    /// </exception>
    public byte[] GetId() => Credential.FormatCredentialId(Issuer, Name, OathType, Period);

    internal void ValidateRequest()
    {
        ValidateRequestNumbers(OathType, Digits, Period, Counter);
        ValidateSecretMaterial(_secret);
    }

    private static void ValidateSecretMaterial(ReadOnlySpan<byte> secret)
    {
        if (secret.IsEmpty)
        {
            throw new ArgumentException("OATH secret material must not be empty.", nameof(Secret));
        }
    }

    private static void ValidateRequestNumbers(OathType oathType, int digits, int period, int counter)
    {
        if (digits is < 6 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(digits), "OATH codes must have 6 through 8 digits.");
        }

        if (oathType == OathType.Totp && period <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "TOTP period must be positive.");
        }

        if (oathType == OathType.Hotp && counter < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(counter), "HOTP counter must be a nonnegative Int32 value.");
        }
    }

    /// <summary>
    ///     Clears the owned shared secret, including any previously borrowed views. Safe to call repeatedly.
    /// </summary>
    /// <remarks>Does not clear caller-owned input buffers or independently allocated copies.</remarks>
    public void Dispose() => CryptographicOperations.ZeroMemory(_secret);

    /// <summary>
    ///     Shortens an HMAC key per RFC 2104: if the key is longer than the hash
    ///     algorithm's block size, it is hashed to the digest size.
    /// </summary>
    /// <param name="key">The borrowed original key bytes.</param>
    /// <param name="algorithm">The hash algorithm to use.</param>
    /// <returns>An independently owned copy or hash, which the caller must clear.</returns>
    internal static byte[] HmacShortenKey(ReadOnlySpan<byte> key, OathHashAlgorithm algorithm)
    {
        int blockSize = algorithm switch
        {
            OathHashAlgorithm.Sha1 => 64,
            OathHashAlgorithm.Sha256 => 64,
            OathHashAlgorithm.Sha512 => 128,
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };

        if (key.Length <= blockSize)
        {
            return key.ToArray();
        }

        return algorithm switch
        {
            OathHashAlgorithm.Sha1 => SHA1.HashData(key),
            OathHashAlgorithm.Sha256 => SHA256.HashData(key),
            OathHashAlgorithm.Sha512 => SHA512.HashData(key),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
    }

    /// <summary>
    ///     Appends zero bytes to secrets shorter than the SDK's 14-byte storage-padding threshold.
    /// </summary>
    /// <param name="secret">The original secret bytes.</param>
    /// <returns>The padded secret, or the original if already at or above the storage-padding threshold.</returns>
    internal static byte[] PadSecret(byte[] secret)
    {
        if (secret.Length >= OathConstants.HmacMinimumKeySize)
        {
            return secret;
        }

        byte[] padded = new byte[OathConstants.HmacMinimumKeySize];
        secret.CopyTo(padded.AsSpan());
        return padded;
    }

    /// <summary>
    ///     Processes the secret for storage: shortens keys exceeding the hash block size per RFC 2104,
    ///     then applies the SDK's 14-byte storage padding.
    /// </summary>
    /// <returns>An independently owned processed secret ready for PUT, which the caller must clear.</returns>
    internal byte[] GetProcessedSecret()
    {
        byte[] shortened = HmacShortenKey(_secret, HashAlgorithm);
        if (shortened.Length >= OathConstants.HmacMinimumKeySize)
        {
            return shortened;
        }

        try
        {
            return PadSecret(shortened);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(shortened);
        }
    }

    /// <summary>
    ///     Parses a Base32-encoded key string, supporting ASCII case, literal spaces, and complete or absent padding.
    /// </summary>
    /// <param name="key">The Base32-encoded string.</param>
    /// <returns>Independently owned decoded bytes, which the caller must clear.</returns>
    internal static byte[] ParseBase32Key(string key) => Base32.Decode(key.AsSpan());
}