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

    // ---- #25 e) and #20: Tlv long-form length decoding ----------------------------------------

    /// <summary>
    ///     Regression for a declared length of 2^32 + 1 that formerly wrapped a 32-bit accumulator.
    ///     The parser now rejects values above Int32.MaxValue before slicing the value.
    /// </summary>
    [Fact]
    [Trait("Audit", "YESDK-1633")]
    public void YESDK1633_TlvFiveByteLengthOverflow_Rejected()
    {
        byte[] encoded = [0x5A, 0x85, 0x01, 0x00, 0x00, 0x00, 0x01, 0xAA];

        Assert.ThrowsAny<ArgumentException>(() => Tlv.Create(encoded));
    }

    /// <summary>
    ///     The unsigned length FF FF FF FF exceeds Int32.MaxValue and reaches the explicit range
    ///     guard. The assertion allows any ArgumentException subtype rather than a slicing failure.
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

    /// <summary>X.690 §8.1.3.5 c): the initial length octet 0xFF is reserved.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1633")]
    public void YESDK1633_TlvReservedLengthOctetFF_Rejected()
    {
        byte[] encoded = [0x5A, 0xFF, .. new byte[127]];

        Assert.ThrowsAny<ArgumentException>(() => Tlv.Create(encoded));
    }

    /// <summary>X.690 §8.1.3.5 Note 2: redundant leading zero length octets are valid BER.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1633")]
    public void YESDK1633_TlvLeadingZeroLengthOctets_Accepted()
    {
        byte[] encoded = [0x5A, 0x85, 0x00, 0x00, 0x00, 0x00, 0x01, 0xAA];

        using var tlv = Tlv.Create(encoded);

        Assert.Equal(0x5A, tlv.Tag);
        Assert.Equal(new byte[] { 0xAA }, tlv.Value.ToArray());
    }

    // ---- #25 f) TlvHelper.EncodeDictionary ----------------------------------------------------

    /// <summary>
    ///     Regression inputs exceed the former one-byte-tag/one-byte-length estimate. Encoding now
    ///     sizes from each TLV's exact TotalLength; the assertion compares the complete encoded bytes.
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
}