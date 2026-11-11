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

/// <summary>Mechanical overlapped report access for one Windows FIDO native owner.</summary>
/// <remarks>
/// One instance retains native resources solely at its owner's direction. The owner's worker
/// calls these methods; this seam supplies no scheduler, physical claim or public completion.
/// Report arrays belong to the owner and include the native report-ID-zero byte.
/// </remarks>
internal interface IWindowsHidReportApi
{
    /// <summary>Opens the overlapped report handle and obtains its native report metadata.</summary>
    /// <remarks>
    /// Records each acquired resource before the next fallible step. The owner calls
    /// <see cref="CloseOnce"/> even after a failed open.
    /// </remarks>
    WindowsHidReportMetadata Open(string devicePath);

    /// <summary>Prepares and dispatches one request, returning acceptance or native rejection.</summary>
    /// <remarks>
    /// Installs all callback bookkeeping before dispatch; completion may precede this method's return.
    /// Default completion notifications are required: no skip-on-success mode and a zero hEvent.
    /// Immediate success and pending acceptance each require exactly one callback; rejected HID
    /// ReadFile/WriteFile requests receive none. Never synthesize immediate-success completion.
    /// Native outcomes are data; this method must not throw after accepted submission. Unexpected
    /// failure with unknown dispatch state requires quarantine, never assumed rejection or retirement.
    /// </remarks>
    WindowsHidSubmission Submit(WindowsHidOverlappedOperation operation);

    /// <summary>Requests cancellation for this exact accepted request on the owner's worker.</summary>
    /// <returns>Zero if requested, 1168 if not found, or another captured native error.</returns>
    /// <remarks>
    /// Called only after Submit returns accepted. Cancellation intent during a held submit waits
    /// for that return. Neither cancellation nor not-found proves completion: retain the request
    /// until its callback. Valid native success wins a cancellation race; native errors remain errors.
    /// </remarks>
    uint Cancel(WindowsHidOverlappedOperation operation);

    /// <summary>Retires request allocation exactly once, including partial preparation or rejection.</summary>
    /// <remarks>
    /// Requires Submit to have returned and either known rejection or native completion plus the
    /// callback's resource-last-use publication. After publication the callback must not access
    /// the operation, native pointer, report buffer or handle, even while its stack unwinds.
    /// This resource barrier is not proof of physical callback-stack return; deployed Windows
    /// JIT/AOT runtime validation is still required. Use runtime-owned overlapped retirement,
    /// not direct native free. The owner clears its array only after retirement.
    /// </remarks>
    void Retire(WindowsHidOverlappedOperation operation);

    /// <summary>Requests checked close once and returns cached native release evidence.</summary>
    /// <remarks>
    /// Drain and retire requests, dispose the binding and release native references before close.
    /// Returning, disposal and an IsClosed flag do not prove native release. Pending, failed or
    /// unexecuted release retains the owner's rooted resources and physical claim. Only proven
    /// release permits claim release; never retry close or substitute a timeout for evidence.
    /// </remarks>
    WindowsHidRelease CloseOnce();
}

/// <summary>Direction of one native HID report transfer.</summary>
internal enum WindowsHidReportDirection
{
    /// <summary>Reads an input report.</summary>
    Input,
    /// <summary>Writes an output report.</summary>
    Output
}

/// <summary>Native HID descriptor metadata; report lengths include the report-ID byte.</summary>
/// <param name="UsagePage">HID usage page.</param>
/// <param name="Usage">HID usage.</param>
/// <param name="InputReportByteLength">Native input report length.</param>
/// <param name="OutputReportByteLength">Native output report length.</param>
internal readonly record struct WindowsHidReportMetadata(
    ushort UsagePage, ushort Usage,
    ushort InputReportByteLength, ushort OutputReportByteLength);

/// <summary>Native submission outcome, independent of terminal completion.</summary>
/// <param name="ErrorCode">Zero for accepted immediate or pending I/O; otherwise the rejection error.</param>
internal readonly record struct WindowsHidSubmission(uint ErrorCode);

/// <summary>Native terminal outcome; the owner validates actual count and report framing.</summary>
/// <param name="ErrorCode">Native completion error, or zero for success.</param>
/// <param name="BytesTransferred">Actual native byte count, including the report-ID byte.</param>
internal readonly record struct WindowsHidCompletion(uint ErrorCode, uint BytesTransferred);

/// <summary>Checked native release evidence, independent of managed disposal.</summary>
/// <param name="NativeReleaseProven">Whether all acquired native resources are proven released.</param>
/// <param name="ErrorCode">Captured native release diagnostic.</param>
internal readonly record struct WindowsHidRelease(bool NativeReleaseProven, uint ErrorCode);