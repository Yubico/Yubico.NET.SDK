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

namespace Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

/// <summary>
/// Assertions over PIN/UV auth token buffers that a test hands to production code and then
/// inspects afterwards.
/// </summary>
/// <remarks>
/// A sentinel fill is used instead of random bytes so that "still holds the token" and "was
/// zeroed" are unambiguous: a randomly generated buffer can contain zero bytes by chance, which
/// would make a per-byte assertion flaky.
/// </remarks>
internal static class TokenBufferAssert
{
    /// <summary>The CTAP PIN/UV auth token length for protocol two.</summary>
    private const int TokenLength = 32;

    private const byte Sentinel = 0xA5;

    /// <summary>
    /// Creates a token-sized buffer filled with a recognisable non-zero pattern.
    /// </summary>
    public static byte[] CreateSentinelToken()
    {
        var token = new byte[TokenLength];
        Array.Fill(token, Sentinel);
        return token;
    }

    /// <summary>
    /// Asserts every byte was cleared.
    /// </summary>
    /// <remarks>
    /// An empty buffer is rejected rather than accepted: "no byte is non-zero" is trivially true
    /// of a zero-length array, so passing one would make this assertion prove nothing.
    /// </remarks>
    public static void Zeroed(byte[] buffer, string? because = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        Assert.True(
            buffer.Length > 0,
            "expected a token-sized buffer to inspect, but it was empty, which would make the zeroing assertion vacuous");

        var firstLiveByte = Array.FindIndex(buffer, b => b != 0);
        Assert.True(
            firstLiveByte < 0,
            because is null
                ? $"expected the buffer to be zeroed, but byte {firstLiveByte} is 0x{buffer[Math.Max(firstLiveByte, 0)]:X2}"
                : $"{because} (byte {firstLiveByte} is still 0x{buffer[Math.Max(firstLiveByte, 0)]:X2})");
    }

    /// <summary>
    /// Asserts the buffer still holds its sentinel pattern, so a later <see cref="Zeroed"/>
    /// assertion is measuring a real transition rather than a buffer that was never populated.
    /// </summary>
    public static void NotZeroed(byte[] buffer, string? because = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        Assert.True(
            Array.Exists(buffer, b => b != 0),
            because ?? "expected the buffer to still hold token bytes");
    }
}