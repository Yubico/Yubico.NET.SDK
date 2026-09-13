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
/// <param name="ValidFrom">Unix timestamp indicating when the code becomes valid.</param>
/// <param name="ValidTo">Unix timestamp indicating when the code expires.</param>
public sealed record Code(string Value, long ValidFrom, long ValidTo)
{
    internal readonly record struct ParsedCode(int Digits, int Value);

    /// <summary>
    ///     Formats a truncated OATH response into a <see cref="Code" />.
    /// </summary>
    /// <param name="credential">The credential that produced the response.</param>
    /// <param name="timestamp">The Unix timestamp used for calculation.</param>
    /// <param name="truncated">
    ///     The truncated response bytes. The first byte is the digit count,
    ///     followed by the truncated HMAC value.
    /// </param>
    /// <returns>A formatted <see cref="Code" /> with validity window.</returns>
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

        if (credential.OathType == OathType.Totp && credential.Period <= 0)
        {
            throw new BadResponseException("A TOTP credential must have a positive period.");
        }

        int rawCode = (truncated[1] << 24) | (truncated[2] << 16) | (truncated[3] << 8) | truncated[4];
        rawCode &= 0x7FFFFFFF;

        return new ParsedCode(digits, rawCode);
    }

    /// <summary>Formats a validated response after successful user-presence resolution.</summary>
    internal static Code FormatCode(Credential credential, long timestamp, ParsedCode parsedCode)
    {
        int modulus = parsedCode.Digits switch
        {
            6 => 1_000_000,
            7 => 10_000_000,
            8 => 100_000_000,
            _ => throw new ArgumentOutOfRangeException(nameof(parsedCode))
        };

        string value = (parsedCode.Value % modulus).ToString().PadLeft(parsedCode.Digits, '0');

        long validFrom;
        long validTo;
        if (credential.OathType == OathType.Totp)
        {
            long timeStep = timestamp / credential.Period;
            validFrom = timeStep * credential.Period;
            validTo = (timeStep + 1) * credential.Period;
        }
        else
        {
            validFrom = timestamp;
            validTo = long.MaxValue;
        }

        return new Code(value, validFrom, validTo);
    }
}