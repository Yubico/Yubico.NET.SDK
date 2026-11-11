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

namespace Yubico.YubiKit.Core.Transports.Hid.Windows;

/// <summary>Stable identity and owner-supplied storage for one overlapped report request.</summary>
/// <remarks>
/// The native owner owns the array and physical claim. This carrier neither frees storage nor
/// schedules work. The native adapter must retain and pin the exact array through retirement.
/// </remarks>
internal sealed class WindowsHidOverlappedOperation
{
    private readonly Action<WindowsHidOverlappedOperation, WindowsHidCompletion> _completion;

    // Worker-only native bookkeeping. Fakes leave these empty. The completion callback uses only
    // the allocation's state and Complete; it never inspects these fields after publication.
    internal unsafe NativeOverlapped* NativeOverlapped { get; set; }
    internal bool RetirementStarted { get; set; }

    /// <summary>Gets or sets the allocator-only failure fact: report dispatch was never reached.</summary>
    /// <remarks>
    /// Set only when overlapped allocation throws, before ReadFile/WriteFile. This proves no report
    /// payload access or callback is pending, not that BCL allocation resources were released.
    /// A null native pointer alone proves neither fact. Retain uncertain resources even after zeroing.
    /// </remarks>
    internal bool PreparationFailedBeforeNativeDispatch { get; set; }

    /// <summary>Creates a request with its bounded internal owner completion recorder.</summary>
    public WindowsHidOverlappedOperation(
        WindowsHidReportDirection direction,
        byte[] nativeReport,
        Action<WindowsHidOverlappedOperation, WindowsHidCompletion> completion)
    {
        ArgumentNullException.ThrowIfNull(nativeReport);
        ArgumentNullException.ThrowIfNull(completion);
        Direction = direction;
        NativeReport = nativeReport;
        _completion = completion;
    }

    /// <summary>Gets the native transfer direction.</summary>
    public WindowsHidReportDirection Direction { get; }

    /// <summary>Gets the owner's native report array, including report ID zero at index zero.</summary>
    public byte[] NativeReport { get; }

    /// <summary>Invokes the bounded, nonthrowing internal owner recorder for native completion.</summary>
    /// <remarks>
    /// The recorder records completion and resource last use, then signals the owner worker.
    /// It must not run application code or access retired resources after last-use publication.
    /// This method does not itself establish retirement, settle a public task or free the buffer.
    /// </remarks>
    public void Complete(WindowsHidCompletion completion) => _completion(this, completion);
}