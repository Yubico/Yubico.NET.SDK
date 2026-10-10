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
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.UnitTests.AuditV2;

/// <summary>
///     Audit #25 (YESDK-1633) Core numeric-boundary sub-items b-f, and #20 (YESDK-1628) truncated
///     long-form TLV length headers.
/// </summary>
public class CoreNumericBoundaryAuditReproTests
{
    // ---- #25 b) DeviceInfoReader pagination --------------------------------------------------

    /// <summary>
    ///     A device that signals "more data" on every page must not keep the reader looping. The page
    ///     index is a single P1 byte (0..255); the reader must fail before requesting a page twice.
    ///     The fake stops after 600 requests and the token times out after 10 s, so this never hangs.
    /// </summary>
    [Fact]
    [Trait("Audit", "YESDK-1633")]
    public async Task YESDK1633_DeviceInfoAlwaysMoreData_StopsWithBadResponseWithinPageSpace()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var protocol = new AlwaysMoreDataProtocol(maxRequests: 600);

        var exception = await Record.ExceptionAsync(
            () => DeviceInfoReader.ReadAsync(protocol, null, timeout.Token));

        Assert.True(
            exception is BadResponseException,
            $"Expected BadResponseException, got {exception?.GetType().Name ?? "no exception"} after " +
            $"{protocol.RequestedPages.Count} page requests (first repeated page index: " +
            $"{FirstRepeatedIndex(protocol.RequestedPages)}).");
        Assert.InRange(protocol.RequestedPages.Count, 1, 256);
        Assert.Equal(protocol.RequestedPages.Count, protocol.RequestedPages.Distinct().Count());
    }

    // ---- #25 c) RandomNumberGeneratorExt.GetInt32 --------------------------------------------

    /// <summary>
    ///     For ranges wider than 2^31 the rejection loop is skipped and a constant is returned without
    ///     consulting the RNG at all (e.g. [int.MinValue, int.MaxValue) always yields -1).
    /// </summary>
    [Theory]
    [Trait("Audit", "YESDK-1633")]
    [InlineData(int.MinValue, int.MaxValue)]
    [InlineData(-1, int.MaxValue)]
    [InlineData(int.MinValue, 1)]
    public void YESDK1633_GetInt32WideRange_ConsumesRandomness(int fromInclusive, int toExclusive)
    {
        using var rng = new CountingRandomNumberGenerator();

        var value = rng.GetInt32(fromInclusive, toExclusive);

        Assert.InRange(value, fromInclusive, toExclusive - 1);
        Assert.True(rng.GetBytesCalls > 0, $"GetInt32 returned {value} without drawing any random bytes.");
    }

    // ---- #25 d) HkdfUtilities ---------------------------------------------------------------

    /// <summary>
    ///     RFC 5869 §2.3 allows L up to 255*HashLen (8160 for SHA-256). With N = 255 blocks the byte loop
    ///     counter wraps to 0 and the block offset goes negative, so valid lengths 8129..8160 throw.
    /// </summary>
    [Theory]
    [Trait("Audit", "YESDK-1633")]
    [InlineData(8129)]
    [InlineData(8160)]
    public void YESDK1633_HkdfMaximumRfcLength_MatchesBclHkdf(int length)
    {
        byte[] ikm = [.. Enumerable.Range(0, 32).Select(i => (byte)i)];
        byte[] info = "audit"u8.ToArray();

        var actual = HkdfUtilities.DeriveKey(ikm, contextInfo: info, length: length).ToArray();

        var expected = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, length, salt: null, info: info);
        Assert.Equal(expected, actual);
    }

    /// <summary>Guard: the RFC 5869 §2.3 upper bound itself is already enforced at HEAD.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1633")]
    public void YESDK1633_HkdfAboveRfcMaximum_Rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => HkdfUtilities.DeriveKey(new byte[32], length: (255 * 32) + 1));

    // ---- #25 e) and #20: Tlv long-form length decoding ----------------------------------------

    /// <summary>
    ///     A five-byte length (85 01 00 00 00 01) overflows the 32-bit accumulator to 1, so the parser
    ///     silently accepts a TLV whose declared length is 2^32 + 1.
    /// </summary>
    [Fact]
    [Trait("Audit", "YESDK-1633")]
    public void YESDK1633_TlvFiveByteLengthOverflow_Rejected()
    {
        byte[] encoded = [0x5A, 0x85, 0x01, 0x00, 0x00, 0x00, 0x01, 0xAA];

        Assert.ThrowsAny<ArgumentException>(() => Tlv.Create(encoded));
    }

    /// <summary>
    ///     Guard: 84 FF FF FF FF decodes to -1, which is rejected today via span slicing
    ///     (ArgumentOutOfRangeException, an ArgumentException subtype).
    /// </summary>
    [Fact]
    [Trait("Audit", "YESDK-1633")]
    public void YESDK1633_TlvFourByteLengthNegative_Rejected()
    {
        byte[] encoded = [0x5A, 0x84, 0xFF, 0xFF, 0xFF, 0xFF, 0xAA];

        Assert.ThrowsAny<ArgumentException>(() => Tlv.Create(encoded));
    }

    /// <summary>
    ///     Truncated long-form length headers must be rejected like every other truncation in the parser
    ///     ("Insufficient data for ..."), not with IndexOutOfRangeException.
    /// </summary>
    [Theory]
    [Trait("Audit", "YESDK-1628")]
    [InlineData(new byte[] { 0x5A, 0x81 })]
    [InlineData(new byte[] { 0x5A, 0x82, 0x01 })]
    public void YESDK1628_TlvTruncatedLongFormLength_ThrowsParseError(byte[] encoded)
    {
        Assert.ThrowsAny<ArgumentException>(() => Tlv.Create(encoded));
        Assert.ThrowsAny<ArgumentException>(() => TlvHelper.DecodeList(encoded).Dispose());
    }

    // ---- #25 f) TlvHelper.EncodeDictionary ----------------------------------------------------

    /// <summary>
    ///     The size estimate assumes 1-byte tag + 1-byte length. A 2-byte tag or a long-form length
    ///     overflows the rented buffer once the estimate lands exactly on an ArrayPool bucket size.
    /// </summary>
    [Theory]
    [Trait("Audit", "YESDK-1633")]
    [InlineData(0x7F49, 126)] // estimate 128, actual 2 + 1 + 126 = 129
    [InlineData(0x53, 254)] // estimate 256, actual 1 + 2 + 254 = 257
    public void YESDK1633_EncodeDictionaryLongTagOrLength_EncodesAllBytes(int tag, int valueLength)
    {
        var value = new byte[valueLength];
        value.AsSpan().Fill(0xA5);
        using var expected = new Tlv(tag, value);

        var actual = TlvHelper.EncodeDictionary(new Dictionary<int, byte[]?> { [tag] = value });

        Assert.Equal(expected.AsSpan().ToArray(), actual.ToArray());
    }

    private static string FirstRepeatedIndex(List<byte> pages)
    {
        var seen = new HashSet<byte>();
        for (var i = 0; i < pages.Count; i++)
        {
            if (!seen.Add(pages[i]))
            {
                return $"request #{i} asked for page {pages[i]} again";
            }
        }

        return "none";
    }

    private sealed class AlwaysMoreDataProtocol(int maxRequests) : ISmartCardProtocol
    {
        // [len=3] 10 01 01 : TLV tag 0x10 (more device info), value 0x01.
        private static readonly byte[] MoreDataPage = [0x03, 0x10, 0x01, 0x01];

        public List<byte> RequestedPages { get; } = [];

        public Task<ApduResponse> TransmitAndReceiveAsync(
            ApduCommand command,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (RequestedPages.Count >= maxRequests)
            {
                throw new InvalidOperationException($"Loop guard: reader issued {maxRequests} page requests.");
            }

            RequestedPages.Add(command.P1);
            return Task.FromResult(new ApduResponse(MoreDataPage, unchecked((short)0x9000)));
        }

        public Task<ReadOnlyMemory<byte>> SelectAsync(
            ReadOnlyMemory<byte> applicationId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null) { }

        public void Dispose() { }
    }

    private sealed class CountingRandomNumberGenerator : RandomNumberGenerator
    {
        public int GetBytesCalls { get; private set; }

        public override void GetBytes(byte[] data)
        {
            GetBytesCalls++;
            RandomNumberGenerator.Fill(data);
        }
    }
}
