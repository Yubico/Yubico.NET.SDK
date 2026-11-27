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

/// <summary>Synchronous expert reports over the independently rooted Windows FIDO native owner.</summary>
/// <remarks>
/// Construction, reports and disposal wait for the owner's actual native last use. Direct reports use
/// descriptor payload sizes rather than typed FIDO64; SET must match the output descriptor exactly.
/// Returned arrays belong to the caller, including sensitive-data clearing. Disposal has no drain timeout.
/// Descriptor lengths include the report-ID byte; these payload sizes exclude it. The owner clears its
/// separate native staging after last use; that does not clear returned payloads or protocol/application
/// output. Direct opens acquire no implicit grouped SDK claim.
/// </remarks>
internal sealed class WindowsHidIOReportConnection : IHidConnection
{
    private readonly WindowsFidoHidNativeOwner _owner;

    internal WindowsHidIOReportConnection(string path)
        : this(path, new WindowsHidReportApi())
    {
    }

    /// <summary>Synchronously waits for worker-owned open and checked failed-open cleanup.</summary>
    internal WindowsHidIOReportConnection(string path, IWindowsHidReportApi reportApi) =>
        _owner = WindowsFidoHidNativeOwner.OpenAsync(path, reportApi, CancellationToken.None).GetAwaiter().GetResult();

    public int InputReportSize => _owner.Metadata.InputReportByteLength - 1;
    public int OutputReportSize => _owner.Metadata.OutputReportByteLength - 1;
    public ConnectionType Type => ConnectionType.Hid;

    public byte[] GetReport()
    {
        _owner.ThrowIfWorkerThread();
        var report = _owner.ReceiveAsync(false, CancellationToken.None).GetAwaiter().GetResult();
        // Transfer the exact independent payload array, not native report-ID framing or a second copy.
        return MemoryMarshal.TryGetArray(report, out var segment) && segment.Array is { } array &&
            segment.Offset == 0 && segment.Count == array.Length
            ? array : throw new InvalidOperationException("Windows HID owner did not return an exact owned report.");
    }

    public void SetReport(byte[] report)
    {
        _owner.ThrowIfWorkerThread();
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

    ~WindowsHidIOReportConnection() => _owner?.RequestShutdown();
}