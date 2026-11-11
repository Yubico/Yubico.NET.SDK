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

using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>
///     Linux implementation of the FIDO IO report connection using hidraw.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxHidIOReportConnection : IHidConnection
{
    private readonly LinuxFidoHidNativeOwner _owner;

    public LinuxHidIOReportConnection(string devNode) : this(devNode, LinuxHidReportApi.Instance)
    {
    }

    internal LinuxHidIOReportConnection(string devNode, ILinuxHidReportApi api)
        : this(devNode, api, LinuxHidReadReactor.Shared)
    {
    }

    internal LinuxHidIOReportConnection(string devNode, ILinuxHidReportApi api, ILinuxHidReadReactor reactor) =>
        _owner = LinuxFidoHidNativeOwner.OpenAsync(devNode, api, reactor, CancellationToken.None).GetAwaiter().GetResult();

    public int InputReportSize => _owner.InputReportSize;
    public int OutputReportSize => _owner.OutputReportSize;

    public byte[] GetReport()
    {
        // Deliberately indefinite, as the original Linux blocking hidraw read. No macOS timeout.
        // Raw descriptor-sized reports are not restricted to typed FIDO64.
        var report = _owner.ReceiveAsync(false, CancellationToken.None).GetAwaiter().GetResult();
        // Native owner returns an owned exact-length array; transfer it without another copy.
        return System.Runtime.InteropServices.MemoryMarshal.TryGetArray(report, out var segment) && segment.Array is { } array
            ? array
            : throw new InvalidOperationException("Linux HID owner did not return an owned report.");
    }

    public void SetReport(byte[] report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _owner.SendAsync(report, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _owner.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await _owner.ShutdownAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    public ConnectionType Type { get; } = ConnectionType.Hid;

    internal static (int InputSize, int OutputSize) ParseReportSizes(ReadOnlySpan<byte> descriptor) =>
        LinuxHidReportSizes.Parse(descriptor);

    ~LinuxHidIOReportConnection() => _owner?.RequestShutdown();
}