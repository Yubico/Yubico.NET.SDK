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

namespace Yubico.YubiKit.Oath;

/// <summary>
///     Represents a calculated OATH one-time password code.
/// </summary>
/// <param name="Value">The formatted OTP code string, zero-padded to the required digit count.</param>
/// <param name="ValidFrom">Unix timestamp indicating when the code becomes valid.</param>
/// <param name="ValidTo">Unix timestamp indicating when the code expires.</param>
public sealed record Code(string Value, long ValidFrom, long ValidTo)
{
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
        int digits = truncated[0];

        int rawCode = (truncated[1] << 24) | (truncated[2] << 16) | (truncated[3] << 8) | truncated[4];
        rawCode &= 0x7FFFFFFF;

        int modulus = 1;
        for (int i = 0; i < digits; i++)
        {
            modulus *= 10;
        }

        string value = (rawCode % modulus).ToString().PadLeft(digits, '0');

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