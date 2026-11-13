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

using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.Windows;

/// <summary>Synchronous expert features over the independently rooted Windows OTP native owner.</summary>
/// <remarks>
/// Construction, GET/SET and disposal wait for the owner's actual native last use. Direct reports use
/// descriptor payload sizes rather than typed OTP8; SET must match the feature descriptor exactly.
/// Returned arrays belong to the caller, including sensitive-data clearing. Disposal has no drain timeout.
/// </remarks>
internal sealed class WindowsHidFeatureReportConnection : IHidConnection
{
    private readonly WindowsOtpHidNativeOwner _owner;

    internal WindowsHidFeatureReportConnection(string path)
        : this(path, new WindowsHidFeatureReportApi())
    {
    }

    /// <summary>Synchronously waits for worker-owned zero-access open and checked failed-open cleanup.</summary>
    internal WindowsHidFeatureReportConnection(string path, IWindowsHidFeatureReportApi reportApi) =>
        _owner = WindowsOtpHidNativeOwner.OpenAsync(path, reportApi, CancellationToken.None).GetAwaiter().GetResult();

    public int InputReportSize => _owner.Metadata.FeatureReportByteLength - 1;
    public int OutputReportSize => _owner.Metadata.FeatureReportByteLength - 1;
    public ConnectionType Type => ConnectionType.Hid;

    public byte[] GetReport()
    {
        var report = _owner.ReceiveAsync(CancellationToken.None).GetAwaiter().GetResult();
        // HidD GET has no actual count; the owner returns an independent descriptor-sized payload.
        return MemoryMarshal.TryGetArray(report, out var segment) && segment.Array is { } array &&
            segment.Offset == 0 && segment.Count == array.Length
            ? array : throw new InvalidOperationException("Windows OTP owner did not return an exact owned report.");
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

    public ValueTask DisposeAsync()
    {
        var closed = _owner.ShutdownAsync();
        GC.SuppressFinalize(this); // Shutdown retains its independently rooted owner through native last use.
        return new ValueTask(closed);
    }

    ~WindowsHidFeatureReportConnection() => _owner?.RequestShutdown();
}