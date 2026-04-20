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

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Represents the PW Status Bytes (DO C4) containing PIN policy, maximum lengths,
///     and remaining retry attempts.
/// </summary>
/// <remarks>
///     Wire format (7 bytes):
///     <code>
///     Byte 0:   Signature PIN policy (0x00=Always, 0x01=Once)
///     Byte 1:   Max length User PIN
///     Byte 2:   Max length Reset Code
///     Byte 3:   Max length Admin PIN
///     Byte 4:   Remaining attempts User PIN
///     Byte 5:   Remaining attempts Reset Code
///     Byte 6:   Remaining attempts Admin PIN
///     </code>
/// </remarks>
public sealed class PwStatus
{
    /// <summary>
    ///     The signature PIN policy (whether PIN must be verified before each signature).
    /// </summary>
    public PinPolicy SignaturePinPolicy { get; init; }

    /// <summary>
    ///     Maximum length of the User PIN.
    /// </summary>
    public int MaxLenUser { get; init; }

    /// <summary>
    ///     Maximum length of the Reset Code.
    /// </summary>
    public int MaxLenReset { get; init; }

    /// <summary>
    ///     Maximum length of the Admin PIN.
    /// </summary>
    public int MaxLenAdmin { get; init; }

    /// <summary>
    ///     Remaining retry attempts for the User PIN.
    /// </summary>
    public int AttemptsUser { get; init; }

    /// <summary>
    ///     Remaining retry attempts for the Reset Code.
    /// </summary>
    public int AttemptsReset { get; init; }

    /// <summary>
    ///     Remaining retry attempts for the Admin PIN.
    /// </summary>
    public int AttemptsAdmin { get; init; }

    /// <summary>
    ///     Gets the maximum PIN length for the specified PIN type.
    /// </summary>
    public int GetMaxLen(Pw pw) =>
        pw switch
        {
            Pw.User => MaxLenUser,
            Pw.Reset => MaxLenReset,
            Pw.Admin => MaxLenAdmin,
            _ => throw new ArgumentOutOfRangeException(nameof(pw)),
        };

    /// <summary>
    ///     Gets the remaining retry attempts for the specified PIN type.
    /// </summary>
    public int GetAttempts(Pw pw) =>
        pw switch
        {
            Pw.User => AttemptsUser,
            Pw.Reset => AttemptsReset,
            Pw.Admin => AttemptsAdmin,
            _ => throw new ArgumentOutOfRangeException(nameof(pw)),
        };

    /// <summary>
    ///     Parses PW Status Bytes from the 7-byte encoded form.
    /// </summary>
    /// <param name="encoded">The 7-byte PW status data.</param>
    /// <exception cref="ArgumentException">Thrown when data is less than 7 bytes.</exception>
    public static PwStatus Parse(ReadOnlySpan<byte> encoded)
    {
        if (encoded.Length < 7)
        {
            throw new ArgumentException("PW status must be at least 7 bytes.", nameof(encoded));
        }

        var policyByte = encoded[0];
        var policy = Enum.IsDefined((PinPolicy)policyByte)
            ? (PinPolicy)policyByte
            : PinPolicy.Once;

        return new PwStatus
        {
            SignaturePinPolicy = policy,
            MaxLenUser = encoded[1],
            MaxLenReset = encoded[2],
            MaxLenAdmin = encoded[3],
            AttemptsUser = encoded[4],
            AttemptsReset = encoded[5],
            AttemptsAdmin = encoded[6],
        };
    }
}