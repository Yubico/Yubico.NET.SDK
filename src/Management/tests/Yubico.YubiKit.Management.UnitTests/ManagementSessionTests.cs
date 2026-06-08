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

namespace Yubico.YubiKit.Management.UnitTests;

using System.Reflection;
using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Core.YubiKey;

public class ManagementSessionTests
{
    [Fact]
    public void IManagementSession_InheritsIAsyncDisposable()
    {
        // Verify that IManagementSession inherits from IAsyncDisposable
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(IManagementSession)));
    }

    [Fact]
    public void ManagementSession_ImplementsIAsyncDisposable()
    {
        // Verify that ManagementSession implements IAsyncDisposable
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(ManagementSession)));
    }

    [Fact]
    public async Task SetDeviceConfigAsync_ZeroesEncodedConfigAfterBackendWrite()
    {
        var backend = new CapturingBackend();
        var session = CreateSessionForBackend(backend);
        var lockCode = Enumerable.Repeat((byte)0xA5, 16).ToArray();
        var config = new DeviceConfig
        {
            EnabledCapabilities = new Dictionary<Transport, int>
            {
                [Transport.Usb] = 1
            }
        };

        await session.SetDeviceConfigAsync(
            config,
            reboot: false,
            currentLockCode: lockCode,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(backend.SawNonZeroConfig);
        Assert.True(backend.CapturedConfig.Span.ToArray().All(static b => b == 0));
    }

    [Fact]
    public async Task GetDeviceInfoAsync_MoreDataIndicator_ReadsNextPage()
    {
        var backend = new CapturingBackend(
            CreateDeviceInfoPage(new Tlv(0x10, [0x01])),
            CreateDeviceInfoPage(CreateRequiredDeviceInfoTlvs()));
        var session = CreateSessionForBackend(backend);

        var info = await session.GetDeviceInfoAsync(TestContext.Current.CancellationToken);

        Assert.Equal([0, 1], backend.ReadPages);
        Assert.Equal(0x01020304, info.SerialNumber);
        Assert.Equal(new FirmwareVersion(5, 7, 2), info.FirmwareVersion);
    }

    [Fact]
    public async Task GetDeviceInfoAsync_InvalidPageLength_ThrowsPageAwareBadResponse()
    {
        var backend = new CapturingBackend([0x02, 0x01]);
        var session = CreateSessionForBackend(backend);

        var ex = await Assert.ThrowsAsync<BadResponseException>(
            () => session.GetDeviceInfoAsync(TestContext.Current.CancellationToken));

        Assert.Contains("page 0", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("declared 2", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("actual 1", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ManagementSession CreateSessionForBackend(IManagementBackend backend)
    {
        var session = (ManagementSession)RuntimeHelpers.GetUninitializedObject(typeof(ManagementSession));

        typeof(ManagementSession)
            .GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, backend);

        typeof(ApplicationSession)
            .GetProperty(nameof(ApplicationSession.FirmwareVersion))!
            .SetValue(session, new FirmwareVersion(5, 0, 0));

        return session;
    }

    private static byte[] CreateDeviceInfoPage(params Tlv[] tlvs)
    {
        var encodedTlvs = TlvHelper.EncodeAndDisposeList(tlvs);
        var page = new byte[encodedTlvs.Length + 1];
        page[0] = (byte)encodedTlvs.Length;
        encodedTlvs.Span.CopyTo(page.AsSpan(1));
        return page;
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

    private sealed class CapturingBackend : IManagementBackend
    {
        private readonly Queue<byte[]> _readResponses = new();

        public CapturingBackend(params byte[][] readResponses)
        {
            foreach (var response in readResponses)
            {
                _readResponses.Enqueue(response);
            }
        }

        public ReadOnlyMemory<byte> CapturedConfig { get; private set; }
        public bool SawNonZeroConfig { get; private set; }
        public List<int> ReadPages { get; } = [];

        public ValueTask<byte[]> ReadConfigAsync(int page, CancellationToken cancellationToken = default)
        {
            ReadPages.Add(page);
            return _readResponses.TryDequeue(out var response)
                ? ValueTask.FromResult(response)
                : throw new NotSupportedException();
        }

        public ValueTask WriteConfigAsync(ReadOnlyMemory<byte> config, CancellationToken cancellationToken = default)
        {
            CapturedConfig = config;
            SawNonZeroConfig = config.Span.ContainsAnyExcept((byte)0);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetModeAsync(byte[] data, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask DeviceResetAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}