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

namespace Yubico.YubiKit.Oath;

/// <summary>
///     Holds the data needed to create an OATH credential on a YubiKey.
/// </summary>
/// <remarks>
///     This instance owns a copy of the supplied secret. Dispose it after all operations using it
///     have completed; do not dispose concurrently with an operation or a consumer of its secret view.
/// </remarks>
public sealed class CredentialData : IDisposable
{
    private readonly byte[] _secret = [];

    /// <summary>
    ///     Gets or sets the account name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets or sets the OATH credential type.
    /// </summary>
    public required OathType OathType { get; init; }

    /// <summary>
    ///     Gets or sets the hash algorithm for HMAC computation.
    /// </summary>
    public required OathHashAlgorithm HashAlgorithm { get; init; }

    /// <summary>
    ///     Gets a borrowed read-only view of the owned shared secret key, or initializes it by copying input.
    /// </summary>
    /// <remarks>
    ///     Initialization copies the supplied bytes; the caller still owns and must clear the input buffer.
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
    ///     Gets or sets the number of digits in the OTP code. Defaults to 6.
    /// </summary>
    public int Digits { get; init; } = OathConstants.DefaultDigits;

    /// <summary>
    ///     Gets or sets the TOTP time step period in seconds. Defaults to 30.
    /// </summary>
    public int Period { get; init; } = OathConstants.DefaultPeriod;

    /// <summary>
    ///     Gets or sets the HOTP initial moving factor (counter). Defaults to 0.
    /// </summary>
    public int Counter { get; init; } = OathConstants.DefaultImf;

    /// <summary>
    ///     Gets or sets the credential issuer.
    /// </summary>
    public string? Issuer { get; init; }

    /// <summary>Gets whether using this credential requires physical touch.</summary>
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
    ///     Thrown when the URI scheme is not <c>otpauth</c>, the OATH type is missing or invalid,
    ///     or the <c>secret</c> query parameter is missing.
    /// </exception>
    public static CredentialData ParseUri(string uri) => ParseUri(uri, requireTouch: false);

    /// <summary>
    ///     Parses an <c>otpauth://</c> URI and applies the credential's physical-touch policy.
    /// </summary>
    /// <param name="uri">The otpauth URI to parse.</param>
    /// <param name="requireTouch">Whether using the credential requires physical touch.</param>
    /// <returns>A populated <see cref="CredentialData" /> instance that exclusively owns its secret.</returns>
    /// <remarks>
    ///     Dispose the returned instance after use. Decoded temporary bytes are cleared on success and failure;
    ///     the URI string and encoded secret strings cannot be cleared by this method.
    /// </remarks>
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

        byte[] secret = ParseBase32Key(secretParam);
        try
        {
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
    public byte[] GetId() => Credential.FormatCredentialId(Issuer, Name, OathType, Period);

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
    ///     Pads a secret to the minimum HMAC key size (14 bytes) by appending zero bytes.
    /// </summary>
    /// <param name="secret">The original secret bytes.</param>
    /// <returns>The padded secret, or the original if already at or above minimum size.</returns>
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
    ///     Processes the secret for storage: shortens per RFC 2104, then pads to minimum size.
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
    ///     Parses a Base32-encoded key string, supporting unpadded input and whitespace.
    /// </summary>
    /// <param name="key">The Base32-encoded string.</param>
    /// <returns>The decoded bytes.</returns>
    internal static byte[] ParseBase32Key(string key)
    {
        key = key.ToUpperInvariant().Replace(" ", "");

        // Add padding if necessary
        int paddingNeeded = (8 - (key.Length % 8)) % 8;
        if (paddingNeeded > 0)
        {
            key += new string('=', paddingNeeded);
        }

        return FromBase32(key);
    }

    private static byte[] FromBase32(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        // Remove padding
        base32 = base32.TrimEnd('=');

        if (base32.Length == 0)
        {
            return [];
        }

        int byteCount = base32.Length * 5 / 8;
        byte[] result = new byte[byteCount];

        int buffer = 0;
        int bitsInBuffer = 0;
        int index = 0;

        try
        {
            foreach (char c in base32)
            {
                int val = alphabet.IndexOf(c);
                if (val < 0)
                {
                    throw new ArgumentException($"Invalid Base32 character: '{c}'.");
                }

                buffer = (buffer << 5) | val;
                bitsInBuffer += 5;

                if (bitsInBuffer >= 8)
                {
                    bitsInBuffer -= 8;
                    result[index++] = (byte)(buffer >> bitsInBuffer);
                    buffer &= (1 << bitsInBuffer) - 1;
                }
            }

            return result;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(result);
            throw;
        }
    }
}