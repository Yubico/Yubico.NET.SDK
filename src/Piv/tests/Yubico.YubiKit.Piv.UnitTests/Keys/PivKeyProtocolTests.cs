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

using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Keys;

namespace Yubico.YubiKit.Piv.UnitTests.Keys;

/// <summary>
/// Security regression tests for <see cref="PivKeyProtocol.ImportKeyAsync"/>'s intermediate
/// key-data buffers. When PIN/touch policy TLVs are appended, the previous <c>keyData</c> array
/// (holding raw encoded private-key material - RSA CRT components, EC private scalar D, or
/// Curve25519 private scalar) is reassigned to a new concatenated array; the orphaned previous
/// array must be zeroed rather than left for the GC unzeroed.
/// </summary>
public class PivKeyProtocolTests
{
    [Fact]
    public void AppendTlvZeroingPrevious_ReturnsCorrectConcatenation()
    {
        byte[] previous = Enumerable.Range(1, 24).Select(i => (byte)i).ToArray();
        byte[] previousSnapshot = (byte[])previous.Clone();
        using var tlv = new Tlv(0xAA, [0x02]);

        var result = PivKeyProtocol.AppendTlvZeroingPrevious(previous, tlv);

        byte[] expected = [.. previousSnapshot, .. tlv.AsSpan()];
        Assert.Equal(expected, result);
    }

    [Fact]
    public void AppendTlvZeroingPrevious_ZeroesThePreviousArray()
    {
        byte[] previous = Enumerable.Range(1, 24).Select(i => (byte)i).ToArray();
        using var tlv = new Tlv(0xAA, [0x02]);

        _ = PivKeyProtocol.AppendTlvZeroingPrevious(previous, tlv);

        Assert.All(previous, b => Assert.Equal(0, b));
    }

    [Fact]
    public void AppendTlvZeroingPrevious_ChainedForBothPolicies_ZeroesEveryIntermediateArray()
    {
        // Mirrors ImportKeyAsync's real usage: append pin-policy TLV, then touch-policy TLV.
        // Both the original raw key-encoding array AND the first (pin-policy-appended)
        // concatenation must end up zeroed - only the final array should remain live.
        byte[] originalKeyData = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        byte[] originalSnapshot = (byte[])originalKeyData.Clone();

        using var pinTlv = new Tlv(0xAA, [(byte)PivPinPolicy.Once]);
        using var touchTlv = new Tlv(0xAB, [(byte)PivTouchPolicy.Always]);

        var afterPin = PivKeyProtocol.AppendTlvZeroingPrevious(originalKeyData, pinTlv);

        // The original array is now orphaned by the reassignment pattern in ImportKeyAsync -
        // it must already be zeroed at this point, not just eventually by some later cleanup.
        Assert.All(originalKeyData, b => Assert.Equal(0, b));

        var afterTouch = PivKeyProtocol.AppendTlvZeroingPrevious(afterPin, touchTlv);

        // The first intermediate concatenation is now itself orphaned and must be zeroed too.
        Assert.All(afterPin, b => Assert.Equal(0, b));

        byte[] expectedFinal = [.. originalSnapshot, .. pinTlv.AsSpan(), .. touchTlv.AsSpan()];
        Assert.Equal(expectedFinal, afterTouch);
    }
}