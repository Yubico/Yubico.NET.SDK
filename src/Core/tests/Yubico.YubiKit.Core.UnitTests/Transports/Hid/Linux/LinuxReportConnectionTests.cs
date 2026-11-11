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

using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Transports.Hid.Linux;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

public class LinuxReportConnectionTests
{
    // Platform annotations describe the production transport; this test uses only fake calls.
    [SupportedOSPlatform("linux")]
    [Fact]
    public async Task FidoConnection_UsesInjectedPayloadOperationsAndPreservesShortReadCount()
    {
        var api = new ReportFake();
        var reactor = new LinuxFidoHidConnectionTests.Reactor();
        using var connection = new LinuxHidIOReportConnection("fake", api, reactor);
        Assert.Equal(64, connection.InputReportSize);
        reactor.Ready();
        Assert.Equal<byte>([42, 43, 44], connection.GetReport());
        connection.SetReport(new byte[64]);
        await connection.DisposeAsync();
        Assert.Equal<string>(["open", "sizes", "read", "write", "close"], api.Operations);
    }

    [SupportedOSPlatform("linux")]
    [Fact]
    public void OtpConnection_UsesInjectedFeaturePayloadOperations()
    {
        var api = new ReportFake();
        using var connection = new LinuxHidFeatureReportConnection("fake", api);
        Assert.Equal(8, connection.InputReportSize);
        Assert.Equal<byte>([1, 2, 3, 4, 5, 6, 7, 8], connection.GetReport());
        connection.SetReport(new byte[8]);
        connection.Dispose();
        Assert.Equal<string>(["open", "get-feature", "set-feature", "close"], api.Operations);
    }

    private sealed class ReportFake : ILinuxHidReportApi
    {
        private bool _read;
        public List<string> Operations { get; } = [];
        public LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags)
        {
            Assert.Equal("fake", path);
            Operations.Add("open");
            var handle = new LinuxFileSafeHandle(_ => new(0, 0));
            handle.Initialize(42);
            return handle;
        }
        public (int InputSize, int OutputSize) GetReportSizes(LinuxFileSafeHandle handle)
        {
            Operations.Add("sizes");
            return (64, 64);
        }
        public LinuxReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> payload)
        {
            if (_read) return new(LinuxReadDisposition.WouldBlock, 0, 11);
            _read = true;
            Operations.Add("read");
            Assert.Equal(64, payload.Length);
            "*+,"u8.ToArray().CopyTo(payload);
            return new LinuxReadResult(LinuxReadDisposition.Data, 3, 0);
        }
        public void Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload)
        {
            Operations.Add("write");
            Assert.Equal(64, payload.Length);
        }
        public void GetFeature(LinuxFileSafeHandle handle, Span<byte> payload)
        {
            Operations.Add("get-feature");
            Assert.Equal(8, payload.Length);
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }.CopyTo(payload);
        }
        public void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload)
        {
            Operations.Add("set-feature");
            Assert.Equal(8, payload.Length);
        }
        public LinuxReleaseOutcome CloseOnce(LinuxFileSafeHandle handle)
        {
            Operations.Add("close");
            handle.Dispose();
            return new LinuxReleaseOutcome(true, true, 0, 0);
        }
    }
}