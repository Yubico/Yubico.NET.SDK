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

using System.Buffers.Binary;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using NativeMethods = Yubico.YubiKit.Core.Native.Linux.Libc.NativeMethods;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

public class LinuxHidReportApiTests
{
    [SupportedOSPlatform("linux")]
    [Theory]
    [InlineData(17)]
    [InlineData(511)]
    [InlineData(512)]
    [InlineData(1024)]
    [InlineData(16383)]
    public void DirectIoSet_PreservesCallerLengthAndNativeZeroFraming(int length)
    {
        var native = new NativeFake();
        using var connection = new LinuxHidIOReportConnection("fake", new LinuxHidReportApi(native), new LinuxFidoHidConnectionTests.Reactor());
        byte[] payload = [.. Enumerable.Range(0, length).Select(i => (byte)(i + 1))];
        byte[] original = [.. payload];

        connection.SetReport(payload);

        Assert.Equal(length + 1, native.LastBytes.Length);
        Assert.Equal(0, native.LastBytes[0]);
        Assert.Equal(original, native.LastBytes[1..]);
        Assert.Equal(original, payload);
        Assert.Equal(3, native.Calls); // Two descriptor requests, then one write.
    }

    [SupportedOSPlatform("linux")]
    [Theory]
    [InlineData(3)]
    [InlineData(511)]
    [InlineData(512)]
    [InlineData(4096)]
    [InlineData(16382)]
    public void DirectFeatureSet_PreservesCallerLengthAndSizedIoctl(int length)
    {
        var native = new NativeFake();
        using var connection = new LinuxHidFeatureReportConnection("fake", new LinuxHidReportApi(native));
        byte[] payload = [.. Enumerable.Range(0, length).Select(i => (byte)(i + 1))];
        byte[] original = [.. payload];

        connection.SetReport(payload);

        Assert.Equal(length + 1, native.LastBytes.Length);
        Assert.Equal((nuint)0xC0004806 | ((nuint)(length + 1) << 16), native.LastRequest);
        Assert.Equal(0, native.LastBytes[0]);
        Assert.Equal(original, native.LastBytes[1..]);
        Assert.Equal(original, payload);
        Assert.Equal(1, native.Calls);
    }

    [SupportedOSPlatform("linux")]
    [Theory]
    [InlineData(32, 16, 32)]
    [InlineData(32, 16, 7)]
    [InlineData(1024, 512, 1024)]
    public void DirectIo_UsesDescriptorMetadataAndReadCapacityWithoutPaddingActualPayload(int input, int output, int actual)
    {
        byte[] descriptor = [0x75, 8, 0x96, (byte)input, (byte)(input >> 8), 0x81, 2,
            0x96, (byte)output, (byte)(output >> 8), 0x91, 2];
        var native = new NativeFake { Descriptor = descriptor, DescriptorLength = descriptor.Length, ReadCount = actual };
        var reactor = new LinuxFidoHidConnectionTests.Reactor();
        using var connection = new LinuxHidIOReportConnection("fake", new LinuxHidReportApi(native), reactor);
        reactor.Ready();

        byte[] report = connection.GetReport();

        Assert.Equal(input, connection.InputReportSize);
        Assert.Equal(output, connection.OutputReportSize);
        Assert.Equal(input, native.ReadCapacity);
        Assert.Equal(Enumerable.Range(0, actual).Select(i => (byte)(42 + i)), report);
        byte[] outgoing = [.. Enumerable.Repeat((byte)0xA5, output)];
        connection.SetReport(outgoing);
        Assert.Equal(output + 1, native.LastBytes.Length);
        Assert.Equal(outgoing, native.LastBytes[1..]);
    }

    [Theory]
    [InlineData(false, -1)]
    [InlineData(false, 512)]
    [InlineData(false, 514)]
    [InlineData(true, -1)]
    [InlineData(true, 512)]
    [InlineData(true, 514)]
    public void PooledCallerLengthSet_RejectsIncompleteTransfersWithoutRetryOrSourceMutation(bool feature, int count)
    {
        var native = new NativeFake { Count = count, Error = 5 };
        var api = new LinuxHidReportApi(native);
        using var handle = api.Open("fake", 0);
        byte[] payload = [.. Enumerable.Repeat((byte)0xA5, 512)];

        var error = Assert.Throws<PlatformApiException>(() =>
        {
            if (feature) api.SetFeature(handle, payload);
            else api.Write(handle, payload);
        });

        Assert.Contains(count < 0 ? "= 0x00000005." : "= 0x00000000.", error.Message, StringComparison.Ordinal);
        Assert.Equal(1, native.Calls);
        Assert.Equal(513, native.LastBytes.Length);
        Assert.Equal(0, native.LastBytes[0]);
        Assert.Equal(payload, native.LastBytes[1..]);
        Assert.All(payload, value => Assert.Equal(0xA5, value));
    }

    [Fact]
    public void NativeFraming_UsesSyntheticZeroAndExactRequestsWithoutChangingSource()
    {
        var native = new NativeFake();
        var api = new LinuxHidReportApi(native);
        using var handle = api.Open("fake", NativeMethods.OpenFlags.O_NONBLOCK);
        byte[] fido = [.. Enumerable.Range(1, 64).Select(i => (byte)i)];
        api.Write(handle, fido);
        Assert.Equal(65, native.LastBytes.Length);
        Assert.Equal(0, native.LastBytes[0]);
        Assert.Equal(fido, native.LastBytes[1..]);
        Assert.Equal(1, fido[0]);
        api.SetFeature(handle, [1, 2, 3, 4, 5, 6, 7, 8]);
        Assert.Equal((nuint)0xC0094806, native.LastRequest);
        Assert.Equal<byte>([0, 1, 2, 3, 4, 5, 6, 7, 8], native.LastBytes);
        byte[] output = new byte[8];
        api.GetFeature(handle, output);
        Assert.Equal((nuint)0xC0094807, native.LastRequest);
        Assert.Equal<byte>([1, 2, 3, 4, 5, 6, 7, 8], output);
        Assert.True(api.CloseOnce(handle).Released);
        Assert.Equal(1, native.Closes);
    }

    [Theory]
    [InlineData("write", 64)]
    [InlineData("get", 8)]
    [InlineData("set", 8)]
    [InlineData("write", 66)]
    [InlineData("get", 10)]
    [InlineData("set", 10)]
    public void IncompleteOrOversizedNativeCount_IsRejectedWithoutSuffixRetry(string operation, int count)
    {
        var native = new NativeFake { Count = count };
        var api = new LinuxHidReportApi(native);
        using var handle = api.Open("fake", 0);
        byte[] output = [.. Enumerable.Repeat((byte)0xCC, 8)];
        Assert.Throws<PlatformApiException>(() =>
        {
            if (operation == "write") api.Write(handle, new byte[64]);
            else if (operation == "get") api.GetFeature(handle, output);
            else api.SetFeature(handle, new byte[8]);
        });
        Assert.Equal(1, native.Calls);
        Assert.All(output, b => Assert.Equal(0xCC, b));
    }

    [Theory]
    [InlineData(64, 0, LinuxReadDisposition.Data, 64)]
    [InlineData(7, 0, LinuxReadDisposition.Data, 7)]
    [InlineData(0, 0, LinuxReadDisposition.EndOfStream, 0)]
    [InlineData(-1, 11, LinuxReadDisposition.WouldBlock, 0)]
    [InlineData(-1, 4, LinuxReadDisposition.Interrupted, 0)]
    [InlineData(-1, 5, LinuxReadDisposition.Error, 0)]
    public void TryRead_PreservesActualCountAndErrno(int count, int error, object disposition, int actual)
    {
        var native = new NativeFake { Count = count, Error = error };
        var api = new LinuxHidReportApi(native);
        using var handle = api.Open("fake", 0);
        byte[] buffer = new byte[64];
        var result = api.TryRead(handle, buffer);
        Assert.Equal(new LinuxReadResult((LinuxReadDisposition)disposition, actual, error), result);
        Assert.Equal(1, native.Calls);
        if (actual > 0) Assert.Equal(42, buffer[0]);
    }

    [Fact]
    public void ReportSizes_ReadBoundedDescriptorAndUseExistingYubiKeyParser()
    {
        var native = new NativeFake();
        var api = new LinuxHidReportApi(native);
        using var handle = api.Open("fake", 0);
        Assert.Equal((64, 64), api.GetReportSizes(handle));
        Assert.Equal(2, native.Calls);
        Assert.Equal(NativeMethods.DescriptorSize, native.LastBytes.Length);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(4096)]
    [InlineData(4097)]
    public void ReportSizes_RejectUnboundedNativeDescriptorBeforeSecondIoctl(int size)
    {
        var native = new NativeFake { DescriptorLength = size };
        var api = new LinuxHidReportApi(native);
        using var handle = api.Open("fake", 0);
        Assert.Throws<InvalidDataException>(() => api.GetReportSizes(handle));
        Assert.Equal(1, native.Calls);
    }

    [Theory]
    [InlineData(new byte[] { 0x77, 0xFF, 0xFF, 0xFF, 0xFF, 0x97, 0xFF, 0xFF, 0xFF, 0xFF, 0x81, 2 })]
    [InlineData(new byte[] { 0x75, 8, 0x97, 0, 0, 0, 0x80, 0x91, 2 })]
    public void DescriptorParser_RejectsUnboundedReportArithmeticRatherThanFallingBack(byte[] descriptor)
    {
        Assert.Throws<InvalidDataException>(() => LinuxHidReportSizes.Parse(descriptor));
    }

    [Fact]
    public void DescriptorParser_RejectsNumberedReportsOutsideSupportedYubiKeyShape()
    {
        Assert.Throws<NotSupportedException>(() => LinuxHidReportSizes.Parse([0x85, 1, 0x75, 8, 0x95, 64, 0x81, 2]));
    }

    [Theory]
    [InlineData("write")]
    [InlineData("get")]
    [InlineData("set")]
    public void NativeFailure_PreservesErrnoAndDoesNotRetry(string operation)
    {
        var native = new NativeFake { Count = -1, Error = 5 };
        var api = new LinuxHidReportApi(native);
        using var handle = api.Open("fake", 0);
        var error = Assert.Throws<PlatformApiException>(() =>
        {
            if (operation == "write") api.Write(handle, new byte[64]);
            else if (operation == "get") api.GetFeature(handle, new byte[8]);
            else api.SetFeature(handle, new byte[8]);
        });
        Assert.Contains("= 0x00000005.", error.Message, StringComparison.Ordinal);
        Assert.Equal(1, native.Calls);
    }

    [Theory]
    [InlineData(65)]
    [InlineData(-2)]
    public void NativeRead_ImpossibleCountCannotBecomePayload(int count)
    {
        var native = new NativeFake { Count = count };
        var api = new LinuxHidReportApi(native);
        using var handle = api.Open("fake", 0);
        Assert.Throws<PlatformApiException>(() => api.TryRead(handle, new byte[64]));
        Assert.Equal(1, native.Calls);
    }

    private sealed class NativeFake : ILinuxHidNativeApi
    {
        public int? Count { get; init; }
        public int Error { get; init; }
        public int DescriptorLength { get; init; } = 8;
        public byte[] Descriptor { get; init; } = [0x75, 8, 0x95, 64, 0x81, 2, 0x91, 2];
        public int? ReadCount { get; init; }
        public int ReadCapacity { get; private set; }
        public int Calls { get; private set; }
        public int Closes { get; private set; }
        public nuint LastRequest { get; private set; }
        public byte[] LastBytes { get; private set; } = [];

        public LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags)
        {
            Assert.Equal("fake", path);
            Assert.True(flags.HasFlag(NativeMethods.OpenFlags.O_CLOEXEC));
            var handle = new LinuxFileSafeHandle(_ => { Closes++; return new LinuxCloseResult(0, 0); });
            handle.Initialize(23);
            return handle;
        }

        public LinuxIoResult Read(LinuxFileSafeHandle handle, Span<byte> buffer)
        {
            Calls++;
            ReadCapacity = buffer.Length;
            for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)(42 + i);
            return new LinuxIoResult(Count ?? ReadCount ?? buffer.Length, Error);
        }

        public LinuxIoResult Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> buffer)
        {
            Calls++;
            LastBytes = [.. buffer];
            return new LinuxIoResult(Count ?? buffer.Length, Error);
        }

        public LinuxIoResult Ioctl(LinuxFileSafeHandle handle, nuint request, Span<byte> buffer)
        {
            Calls++;
            LastRequest = request;
            LastBytes = [.. buffer];
            if (request == (nuint)NativeMethods.HIDIOCGRDESCSIZE)
                BinaryPrimitives.WriteInt32LittleEndian(buffer, DescriptorLength);
            else if (request == (nuint)NativeMethods.HIDIOCGRDESC)
                Descriptor.CopyTo(buffer[4..]);
            else
            {
                Assert.Equal(0, buffer[0]);
                if (request == 0xC0094807)
                    for (int i = 1; i < buffer.Length; i++) buffer[i] = (byte)i;
            }
            return new LinuxIoResult(Count ?? (request == (nuint)NativeMethods.HIDIOCGRDESCSIZE || request == (nuint)NativeMethods.HIDIOCGRDESC ? 0 : buffer.Length), Error);
        }
    }
}