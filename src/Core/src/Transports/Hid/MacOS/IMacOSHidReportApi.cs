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
using Yubico.YubiKit.Core.Native;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>
///     The IOKit and CoreFoundation calls that govern the <em>lifetime</em> of a macOS HID connection:
///     creating the device, opening and closing it, creating and releasing CoreFoundation objects, and
///     registering or clearing the IOKit callbacks, and OTP feature-report I/O.
/// </summary>
/// <remarks>
///     <para>
///         This is a seam, not an abstraction layer. It exists so the constructor-failure and disposal
///         paths of <see cref="MacOSHidFeatureReportConnection" />,
///         <see cref="MacOSHidIOReportConnection" />, and <see cref="MacOSOtpHidConnection" />
///         can be tested without macOS hardware — the same
///         reason <c>IWindowsHidReportAccess</c> exists on the Windows side. It deliberately mirrors the native calls
///         one-for-one and adds no policy.
///     </para>
///     <para>
///         OTP feature-report I/O and checked close verify that accepted operations drain before the
///         physical claim is released. FIDO report I/O uses a separate bridge; this seam also exposes
///         the existing feature-report and callback calls used by the macOS HID connections.
///     </para>
/// </remarks>
[SupportedOSPlatform("macos")]
internal interface IMacOSHidReportApi
{
    /// <summary>
    ///     Resolves an IO registry entry ID to a retained <c>IOHIDDeviceRef</c>.
    /// </summary>
    /// <remarks>
    ///     The returned handle is a CoreFoundation object owned by the caller and must be passed to
    ///     <see cref="ReleaseCFObject" /> when it is no longer needed. Closing it with
    ///     <see cref="CloseDevice" /> is not a substitute for releasing it.
    /// </remarks>
    /// <exception cref="PlatformApiException">
    ///     No registry entry matched, or the device handle could not be created.
    /// </exception>
    nint CreateDevice(long entryId);

    /// <summary>
    ///     Opens the device non-seizing (<c>kIOHIDOptionsTypeNone</c>).
    /// </summary>
    /// <exception cref="PlatformApiException">The open failed.</exception>
    void OpenDevice(nint device);

    // OTP needs the return code: exclusive access still establishes an open that must be closed.
    // Apple IOHIDDeviceClass.m sets _opened for both success and kIOReturnExclusiveAccess.
    int OpenDeviceResult(nint device)
    {
        OpenDevice(device);
        return 0;
    }

    /// <summary>
    ///     Closes the device. Does not release it; see <see cref="ReleaseCFObject" />.
    /// </summary>
    void CloseDevice(nint device);

    // OTP uses the checked result to decide whether its physical claim can be released.
    bool CloseDeviceChecked(nint device);

    int GetFeatureReport(nint device, byte[] buffer, ref long length);

    int SetFeatureReport(nint device, byte[] buffer);

    /// <summary>
    ///     Releases a CoreFoundation object previously created by <see cref="CreateDevice" /> or
    ///     <see cref="CreateRunLoopMode" />.
    /// </summary>
    void ReleaseCFObject(nint cfObject);

    /// <summary>
    ///     Creates a retained <c>CFStringRef</c> used as a run-loop mode name. The caller owns the result
    ///     and must pass it to <see cref="ReleaseCFObject" />.
    /// </summary>
    nint CreateRunLoopMode(string name);

    /// <summary>
    ///     Reads an integer-typed IOKit property from the device.
    /// </summary>
    int GetIntProperty(nint device, string propertyName);

    /// <summary>
    ///     Registers, or with zero <paramref name="callback" /> and <paramref name="context" /> clears, the
    ///     input report callback.
    /// </summary>
    void RegisterInputReportCallback(nint device, byte[] buffer, int bufferLength, nint callback, nint context);

    /// <summary>
    ///     Registers, or with zero <paramref name="callback" /> and <paramref name="context" /> clears, the
    ///     device removal callback.
    /// </summary>
    void RegisterRemovalCallback(nint device, nint callback, nint context);
}