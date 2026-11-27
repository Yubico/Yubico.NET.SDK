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
///     Interceptable native-lifetime and OTP feature-report operations for macOS HID connections:
///     creating the device, opening and closing it, creating and releasing CoreFoundation objects, and
///     registering or clearing IOKit callbacks.
/// </summary>
/// <remarks>
///     <para>
///         Connection code uses this seam to make acquisition failures, feature reports and cleanup
///         interceptable in no-hardware tests. The production
///         implementation forwards native calls and includes small managed compositions and status handling
///         for device lookup and creation, property reads, and open/close results.
///     </para>
///     <para>
///         Connection owners schedule and drain accepted operations before checked native cleanup and
///         physical-claim release. Injected implementations let tests check that ordering; the seam alone
///         provides neither drain nor native-release proof. FIDO report I/O uses a separate bridge; this
///         seam exposes the feature-report and callback operations alongside native-lifetime operations.
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
    /// <remarks>
    ///     This dormant registration seam requires a buffer pinned for the entire native registration and
    ///     callback drain, plus a rooted context. A call-scoped array pin is insufficient. Active FIDO input
    ///     instead uses the native bridge's persistent buffer and separate callback-state root.
    /// </remarks>
    void RegisterInputReportCallback(nint device, byte[] buffer, int bufferLength, nint callback, nint context);

    /// <summary>
    ///     Registers, or with zero <paramref name="callback" /> and <paramref name="context" /> clears, the
    ///     device removal callback.
    /// </summary>
    void RegisterRemovalCallback(nint device, nint callback, nint context);
}