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

using System;
using System.Collections.Generic;
using Yubico.Core.Buffers;

namespace Yubico.YubiKey;

public enum YubiKeyApplication
{
    Unknown = 0,
    Management = 1,
    Otp = 2,
    FidoU2f = 3,
    Fido2 = 4,
    Oath = 5,
    OpenPgp = 6,
    Piv = 7,
    InterIndustry = 8,
    OtpNdef = 9,
    YubiHsmAuth = 10,

    [Obsolete("Use SecurityDomain instead")]
    Scp03 = 11,
    SecurityDomain = 12
}

internal static class YubiKeyApplicationExtensions
{
    private static readonly byte[] ManagementAppId = { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x47, 0x11, 0x17 };
    private static readonly byte[] OtpAppId = { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01, 0x01 };
    private static readonly byte[] FidoU2fAppId = { 0xA0, 0x00, 0x00, 0x06, 0x47, 0x2F, 0x00, 0x01 };
    private static readonly byte[] Fido2AppId = { 0xA0, 0x00, 0x00, 0x06, 0x47, 0x2F, 0x00, 0x01 };
    private static readonly byte[] OathAppId = { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x01 };
    private static readonly byte[] OpenPgpAppId = { 0xD2, 0x76, 0x00, 0x01, 0x24, 0x01 };
    private static readonly byte[] PivAppId = { 0xA0, 0x00, 0x00, 0x03, 0x08 };
    private static readonly byte[] OtpNdef = { 0xD2, 0x76, 0x00, 0x00, 0x85, 0x01, 0x01 };
    private static readonly byte[] YubiHsmAuthId = { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x07, 0x01 };
    private static readonly byte[] SecurityDomainAppId = { 0xA0, 0x00, 0x00, 0x01, 0x51, 0x00, 0x00, 0x00 };

    public static IReadOnlyDictionary<YubiKeyApplication, ReadOnlyMemory<byte>> Iso7816ApplicationIds =>
        new Dictionary<YubiKeyApplication, ReadOnlyMemory<byte>>
        {
            { YubiKeyApplication.Management, ManagementAppId },
            { YubiKeyApplication.Otp, OtpAppId },
            { YubiKeyApplication.FidoU2f, FidoU2fAppId },
            { YubiKeyApplication.Fido2, Fido2AppId },
            { YubiKeyApplication.Oath, OathAppId },
            { YubiKeyApplication.OpenPgp, OpenPgpAppId },
            { YubiKeyApplication.Piv, PivAppId },
            { YubiKeyApplication.OtpNdef, OtpNdef },
            { YubiKeyApplication.YubiHsmAuth, YubiHsmAuthId },
            #pragma warning disable CS0618 // Type or member is obsolete // Remove in next major release
            { YubiKeyApplication.Scp03, SecurityDomainAppId },
            #pragma warning restore CS0618 // Type or member is obsolete // Remove in next major release
            { YubiKeyApplication.SecurityDomain, SecurityDomainAppId }
        };

    public static byte[] GetIso7816ApplicationId(this YubiKeyApplication application) =>
        Iso7816ApplicationIds.ContainsKey(application)
            ? Iso7816ApplicationIds[application].ToArray()
            : throw new NotSupportedException(ExceptionMessages.ApplicationIdNotFound);

    /// <summary>
    ///     Gets the <see cref="YubiKeyApplication" /> associated with the given applicationId.
    /// </summary>
    /// <param name="applicationId">The application id as a byte array.</param>
    /// <returns>The associated <see cref="YubiKeyApplication" />.</returns>
    /// <exception cref="ArgumentException">No YubiKey application found with the given application id.</exception>
    public static YubiKeyApplication GetYubiKeyApplication(this ReadOnlySpan<byte> applicationId)
    {
        foreach (var kvp in Iso7816ApplicationIds)
        {
            if (kvp.Value.Span.SequenceEqual(applicationId))
            {
                return kvp.Key;
            }
        }

        throw new ArgumentException(
            $"No YubiKey application found with application id: {Base16.EncodeBytes(applicationId)}",
            nameof(applicationId));
    }

    /// <inheritdoc cref="GetYubiKeyApplication(ReadOnlySpan{byte})" />
    public static YubiKeyApplication GetYubiKeyApplication(this byte[] applicationId) =>
        GetYubiKeyApplication(applicationId.AsSpan());
}
