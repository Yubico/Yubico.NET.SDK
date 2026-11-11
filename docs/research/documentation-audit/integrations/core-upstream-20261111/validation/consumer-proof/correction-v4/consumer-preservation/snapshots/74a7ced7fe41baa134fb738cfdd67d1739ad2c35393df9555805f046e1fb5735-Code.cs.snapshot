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

using Yubico.YubiKit.Core;

namespace Yubico.YubiKit.Oath;

/// <summary>
///     Represents a calculated OATH one-time password code.
/// </summary>
/// <param name="Value">The formatted OTP code string, zero-padded to the required digit count.</param>
/// <param name="ValidFrom">
///     Unix time in seconds: the inclusive start of the nominal TOTP generation window, or the
///     requested/current calculation timestamp for HOTP.
/// </param>
/// <param name="ValidTo">
///     Unix time in seconds: the exclusive end of the nominal TOTP generation window, or
///     <see cref="long.MaxValue" /> for HOTP as a no-time-expiry sentinel, not a date to format.
/// </param>
/// <remarks>
///     These bounds describe generation metadata, not a verifier's acceptance policy. TOTP acceptance
///     depends on the verifier's time window and replay rules; HOTP acceptance depends on its counter state.
/// </remarks>
public sealed record Code(string Value, long ValidFrom, long ValidTo)
{
    internal readonly record struct ParsedCode(int Digits, int Value);

    /// <summary>
    ///     Formats a truncated OATH response into a <see cref="Code" />.
    /// </summary>
    /// <param name="credential">The credential that produced the response.</param>
    /// <param name="timestamp">The Unix time in seconds used for calculation.</param>
    /// <param name="truncated">
    ///     The truncated response bytes. The first byte is the digit count,
    ///     followed by the truncated HMAC value.
    /// </param>
    /// <returns>A formatted <see cref="Code" /> with generation-time metadata.</returns>
    internal static Code FormatCode(Credential credential, long timestamp, ReadOnlySpan<byte> truncated)
    {
        ParsedCode parsedCode = ParseCode(credential, truncated);
        return FormatCode(credential, timestamp, parsedCode);
    }

    /// <summary>
    ///     Validates and parses a truncated OATH response without creating the immutable code string.
    /// </summary>
    internal static ParsedCode ParseCode(Credential credential, ReadOnlySpan<byte> truncated)
    {
        ParsedCode parsedCode = ParseTruncatedResponse(truncated);
        if (credential.OathType == OathType.Totp && credential.Period <= 0)
        {
            throw new BadResponseException("A TOTP credential must have a positive period.");
        }

        return parsedCode;
    }

    /// <summary>Parses the wire value independently of host-side credential naming.</summary>
    internal static ParsedCode ParseTruncatedResponse(ReadOnlySpan<byte> truncated)
    {
        if (truncated.Length != 5)
        {
            throw new BadResponseException(
                $"Invalid truncated OATH response length. Expected 5 bytes, got {truncated.Length}.");
        }

        int digits = truncated[0];
        if (digits is < 6 or > 8)
        {
            throw new BadResponseException(
                $"Invalid OATH code digit count {digits}. Expected a value from 6 through 8.");
        }

        int rawCode = (truncated[1] << 24) | (truncated[2] << 16) | (truncated[3] << 8) | truncated[4];
        rawCode &= 0x7FFFFFFF;

        return new ParsedCode(digits, rawCode);
    }

    /// <summary>Formats a validated response after successful user-presence resolution.</summary>
    internal static Code FormatCode(Credential credential, long timestamp, ParsedCode parsedCode)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(timestamp);
        (long validFrom, long validTo) = credential.OathType == OathType.Totp
            ? GetTotpWindow(timestamp, credential.Period)
            : (timestamp, long.MaxValue);

        int modulus = parsedCode.Digits switch
        {
            6 => 1_000_000,
            7 => 10_000_000,
            8 => 100_000_000,
            _ => throw new ArgumentOutOfRangeException(nameof(parsedCode))
        };

        string value = (parsedCode.Value % modulus).ToString().PadLeft(parsedCode.Digits, '0');

        return new Code(value, validFrom, validTo);
    }

    /// <summary>Validates the supported time domain without overflowing the exclusive window end.</summary>
    internal static (long From, long To) GetTotpWindow(long timestamp, int period)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(timestamp);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(period);
        long from = timestamp - timestamp % period;
        if (from > long.MaxValue - period)
            throw new ArgumentOutOfRangeException(nameof(timestamp), timestamp,
                "The TOTP generation window must fit in Int64 Unix seconds.");

        return (from, from + period);
    }
}