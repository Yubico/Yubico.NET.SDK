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

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Configures a slot for Yubico OTP mode.
/// </summary>
/// <remarks>
/// Wire format layout:
/// <list type="bullet">
/// <item><c>fixed[0..publicId.Length]</c> — modhex-encoded public identity (up to 16 bytes)</item>
/// <item><c>uid</c> — 6-byte private identity</item>
/// <item><c>key</c> — 16-byte AES-128 key</item>
/// </list>
/// </remarks>
public sealed class YubiOtpSlotConfiguration : KeyboardSlotConfiguration
{
    /// <summary>
    /// Initializes a new Yubico OTP slot configuration.
    /// </summary>
    /// <param name="publicId">
    /// The public identity prefix (modhex-encoded, up to 16 bytes).
    /// </param>
    /// <param name="privateId">The 6-byte private identity.</param>
    /// <param name="aesKey">The 16-byte AES-128 secret key.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="publicId"/> exceeds 16 bytes,
    /// <paramref name="privateId"/> is not 6 bytes, or
    /// <paramref name="aesKey"/> is not 16 bytes.
    /// </exception>
    public YubiOtpSlotConfiguration(
        ReadOnlySpan<byte> publicId,
        ReadOnlySpan<byte> privateId,
        ReadOnlySpan<byte> aesKey)
    {
        if (publicId.Length > YubiOtpConstants.FixedSize)
        {
            throw new ArgumentException(
                $"Public ID must be at most {YubiOtpConstants.FixedSize} bytes.",
                nameof(publicId));
        }

        if (privateId.Length != YubiOtpConstants.UidSize)
        {
            throw new ArgumentException(
                $"Private ID must be exactly {YubiOtpConstants.UidSize} bytes.",
                nameof(privateId));
        }

        if (aesKey.Length != YubiOtpConstants.KeySize)
        {
            throw new ArgumentException(
                $"AES key must be exactly {YubiOtpConstants.KeySize} bytes.",
                nameof(aesKey));
        }

        publicId.CopyTo(_fixed);
        privateId.CopyTo(_uid);
        aesKey.CopyTo(_key);
        _fixedSize = (byte)publicId.Length;
    }
}
