using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class DeviceInfoReaderTests
{
    [Fact]
    public async Task ReadAsync_SmartCardSinglePage_ParsesDeviceInfo()
    {
        var protocol = new FakeSmartCardProtocol(BuildPage(CreateRequiredDeviceInfoTlvs()));

        var info = await DeviceInfoReader.ReadAsync(protocol, null, TestContext.Current.CancellationToken);

        Assert.Equal(0x01020304, info.SerialNumber);
        Assert.Equal(new FirmwareVersion(5, 7, 2), info.FirmwareVersion);
        Assert.Equal([0], protocol.RequestedPages);
    }

    [Fact]
    public async Task ReadAsync_SmartCardMoreData_ReadsNextPage()
    {
        var protocol = new FakeSmartCardProtocol(
            BuildPage(new Tlv(0x10, [0x01])),
            BuildPage(CreateRequiredDeviceInfoTlvs()));

        var info = await DeviceInfoReader.ReadAsync(protocol, null, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1], protocol.RequestedPages);
        Assert.Equal(0x01020304, info.SerialNumber);
        Assert.Equal(new FirmwareVersion(5, 7, 2), info.FirmwareVersion);
    }

    [Fact]
    public async Task ReadAsync_SmartCardMoreDataCount_ReadsRemainingPages()
    {
        var protocol = new FakeSmartCardProtocol(
            BuildPage(new Tlv(0x10, [0x02])),
            BuildPage(new Tlv(0x10, [0x01])),
            BuildPage(CreateRequiredDeviceInfoTlvs()));

        var info = await DeviceInfoReader.ReadAsync(protocol, null, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1, 2], protocol.RequestedPages);
        Assert.Equal(0x01020304, info.SerialNumber);
        Assert.Equal(new FirmwareVersion(5, 7, 2), info.FirmwareVersion);
    }

    [Fact]
    public async Task ReadAsync_SmartCardInvalidPageLength_ThrowsPageAwareBadResponse()
    {
        var protocol = new FakeSmartCardProtocol([0x02, 0x01]);

        var ex = await Assert.ThrowsAsync<BadResponseException>(
            () => DeviceInfoReader.ReadAsync(protocol, null, TestContext.Current.CancellationToken));

        Assert.Contains("page 0", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("declared 2", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("actual 1", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadAsync_FidoSinglePage_ParsesDeviceInfo()
    {
        var protocol = new FakeFidoHidProtocol(BuildPage(CreateRequiredDeviceInfoTlvs()));

        var info = await DeviceInfoReader.ReadAsync(protocol, null, TestContext.Current.CancellationToken);

        Assert.Equal(0x01020304, info.SerialNumber);
        Assert.Equal(new FirmwareVersion(5, 7, 2), info.FirmwareVersion);
        Assert.Equal([0], protocol.RequestedPages);
    }

    [Fact]
    public async Task ReadAsync_OtpSinglePage_ParsesDeviceInfo()
    {
        var frame = BuildOtpFrame(BuildPage(CreateRequiredDeviceInfoTlvs()));
        var protocol = new FakeOtpHidProtocol(frame);

        var info = await DeviceInfoReader.ReadAsync(protocol, null, TestContext.Current.CancellationToken);

        Assert.Equal(0x01020304, info.SerialNumber);
        Assert.Equal(new FirmwareVersion(5, 7, 2), info.FirmwareVersion);
    }

    [Fact]
    public async Task ReadAsync_OtpInvalidCrc_ThrowsBadResponse()
    {
        // Valid frame then corrupt the trailing CRC byte.
        var frame = BuildOtpFrame(BuildPage(CreateRequiredDeviceInfoTlvs()));
        frame[^1] ^= 0xFF;
        var protocol = new FakeOtpHidProtocol(frame);

        var ex = await Assert.ThrowsAsync<BadResponseException>(
            () => DeviceInfoReader.ReadAsync(protocol, null, TestContext.Current.CancellationToken));

        Assert.Contains("CRC", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadAsync_UnsupportedProtocol_ThrowsNotSupported()
    {
        var protocol = new FakeUnsupportedProtocol();

        await Assert.ThrowsAsync<NotSupportedException>(
            () => DeviceInfoReader.ReadAsync(protocol, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadAsync_DefaultVersionProvided_OverridesFirmwareVersionTlv()
    {
        // The page carries firmware-version TLV 0x05 = 5.7.2, but a non-null defaultVersion must
        // win, proving the reader passes defaultVersion through to DeviceInfo.CreateFromTlvs.
        var protocol = new FakeSmartCardProtocol(BuildPage(CreateRequiredDeviceInfoTlvs()));
        var defaultVersion = new FirmwareVersion(5, 4, 3);

        var info = await DeviceInfoReader.ReadAsync(protocol, defaultVersion, TestContext.Current.CancellationToken);

        Assert.Equal(defaultVersion, info.FirmwareVersion);
    }

    private static byte[] BuildPage(params Tlv[] tlvs)
    {
        var encoded = TlvHelper.EncodeAndDisposeList(tlvs);
        var page = new byte[encoded.Length + 1];
        page[0] = (byte)encoded.Length;
        encoded.Span.CopyTo(page.AsSpan(1));
        return page;
    }

    private static byte[] BuildOtpFrame(byte[] page)
    {
        // YubiKey OTP appends the one's-complement CRC (FCS) in little-endian so the residue
        // over [page][FCS] equals ChecksumUtils.ValidResidue.
        var crc = ChecksumUtils.CalculateCrc(page, page.Length);
        var fcs = (ushort)~crc;
        var frame = new byte[page.Length + 2];
        page.CopyTo(frame, 0);
        frame[page.Length] = (byte)(fcs & 0xFF);
        frame[page.Length + 1] = (byte)((fcs >> 8) & 0xFF);

        // Guard: the constructed frame must satisfy the reader's CRC check.
        Assert.True(ChecksumUtils.CheckCrc(frame, frame.Length));
        return frame;
    }

    private static Tlv[] CreateRequiredDeviceInfoTlvs() =>
    [
        new(0x0A, [0x00]),
        new(0x04, [(byte)FormFactor.UsbAKeychain]),
        new(0x18, [0x00]),
        new(0x03, [0x00, 0x01]),
        new(0x01, [0x00, 0x01]),
        new(0x0E, [0x00]),
        new(0x0D, [0x00]),
        new(0x14, [0x00]),
        new(0x15, [0x00]),
        new(0x06, [0x00, 0x00]),
        new(0x07, [0x00]),
        new(0x08, [0x00]),
        new(0x05, [0x05, 0x07, 0x02]),
        new(0x02, [0x01, 0x02, 0x03, 0x04])
    ];

    private sealed class FakeSmartCardProtocol(params byte[][] pages) : ISmartCardProtocol
    {
        private readonly Queue<byte[]> _pages = new(pages);

        public List<byte> RequestedPages { get; } = [];

        public Task<ApduResponse> TransmitAndReceiveAsync(
            ApduCommand command,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            RequestedPages.Add(command.P1);
            var page = _pages.Dequeue();
            return Task.FromResult(new ApduResponse(page, unchecked((short)0x9000)));
        }

        public Task<ReadOnlyMemory<byte>> SelectAsync(
            ReadOnlyMemory<byte> applicationId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null) { }

        public void Dispose() { }
    }

    private sealed class FakeFidoHidProtocol(params byte[][] pages) : IFidoHidProtocol
    {
        private readonly Queue<byte[]> _pages = new(pages);

        public List<byte> RequestedPages { get; } = [];

        public bool IsChannelInitialized => true;

        public FirmwareVersion? FirmwareVersion => null;

        public Task<ReadOnlyMemory<byte>> SendVendorCommandAsync(
            byte command,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken = default)
        {
            RequestedPages.Add(data.Span[0]);
            return Task.FromResult<ReadOnlyMemory<byte>>(_pages.Dequeue());
        }

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ApduCommand command,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ReadOnlyMemory<byte>> SelectAsync(
            ReadOnlyMemory<byte> applicationId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null) { }

        public void Dispose() { }
    }

    private sealed class FakeOtpHidProtocol(params byte[][] frames) : IOtpHidProtocol
    {
        private readonly Queue<byte[]> _frames = new(frames);

        public FirmwareVersion? FirmwareVersion => null;

        public Task<ReadOnlyMemory<byte>> SendAndReceiveAsync(
            byte slot,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ReadOnlyMemory<byte>>(_frames.Dequeue());

        public Task<ReadOnlyMemory<byte>> ReadStatusAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null) { }

        public void Dispose() { }
    }

    private sealed class FakeUnsupportedProtocol : IProtocol
    {
        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null) { }

        public void Dispose() { }
    }
}
