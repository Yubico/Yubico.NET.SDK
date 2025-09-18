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

using System;
using System.Collections.Generic;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid;

/// <summary>
///     Base class for all HID devices represented by Yubico .NET-based SDKs.
/// </summary>
/// <remarks>
///     Yubico products, such as the YubiKey, can expose Human Interface Devices (HID) through their USB interfaces. The
///     most common types of HIDs are keyboards and mice, but other types of devices exist as well. At this time, Keyboards
///     and the custom "FIDO" type are the most interesting to this SDK. Implementations of this class should support
///     finding all HID devices.
/// </remarks>
public abstract class HidDevice : IHidDevice
{
    /// <summary>
    ///     Constructs a <see cref="HidDevice" />.
    /// </summary>
    protected HidDevice(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        Path = path;
    }

    #region IHidDevice Members

    /// <inheritdoc />
    public string Path { get; }

    /// <inheritdoc />
    public string? ParentDeviceId { get; protected set; }

    /// <summary>
    ///     A hardware identifier indicating the vendor of this device.
    /// </summary>
    /// <remarks>
    ///     The Vendor ID is an identifier defined by the USB specification. Compliant USB devices, such as the YubiKey,
    ///     will have a unique vendor identifier assigned to them. In Yubico's case, this value is `0x1050`. Other
    ///     manufacturers should have different numbers. No other manufacturer should create a device with a vendor ID
    ///     of `0x1050`, however nothing prevents a malicious or otherwise non-compliant device from existing. So, while
    ///     you can make _some_ assumptions about a device that matches your expected vendor ID, other steps should be
    ///     taken to verify that you are communicating with a genuine device. These steps will be device dependent and
    ///     are out of the scope of this document.
    /// </remarks>
    public short VendorId { get; protected set; }

    /// <summary>
    ///     A hardware identifier indicating the product that this device represents.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Product ID is an identifier defined by the USB specification. The product ID is a 16-bit number that can
    ///         be used to identify and differentiate different products produced by a given vendor. Values for the IDs are
    ///         determined by the vendor. This means that different manufacturers may produce devices that have the same ID.
    ///         Product IDs are only expected to be unique within the scope of a vendor.
    ///     </para>
    ///     <para>
    ///         Product IDs also don't guarantee that devices are the same physical model or product revision - only that
    ///         the devices behave the same as far as USB communication is concerned. A single device may also expose different
    ///         product IDs based on its configuration. An example of this behavior is with the YubiKey. The YubiKey is a
    ///         composite USB device, meaning that it exposes multiple child devices (keyboard, FIDO, and smart card reader).
    ///         It is possible to configure the YubiKey so that only a subset of those child devices are present. The product
    ///         ID enumerated by USB will change, depending on which child devices are present.
    ///     </para>
    /// </remarks>
    public short ProductId { get; protected set; }

    /// <summary>
    ///     A HID-specific hardware identifier indicating the type of device this is.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The HID specification defines a pair of values called the Usage. Usages are part of the HID report descriptor
    ///         and are used to inform an application developer what the HID device is supposed to represent. If a device
    ///         has more than one usage, the value here is considered to be the "primary" usage.
    ///     </para>
    ///     <para>
    ///         This property defines the "Usage ID" portion of the usage field. It is a 16-bit unsigned value. The usage ID
    ///         is used to select individual usage on a <see cref="UsagePage" />. That is, it helps further specify the type
    ///         of device, or device interaction, that this object represents. Note: This field is of type <c>short</c>, but
    ///         is intended to be treated as unsigned. Casting to a <c>ushort</c> is permitted.
    ///     </para>
    ///     <para>
    ///         The value of this property is determined by the appropriate HID specification for the device in question.
    ///         Knowing this value for something other than a generic HID type (such as keyboard, mouse, or joystick) may
    ///         require locating additional specifications, as vendor defined values are allowed.
    ///     </para>
    /// </remarks>
    public short Usage { get; protected set; }

    /// <summary>
    ///     A HID-specific hardware identifier indicating the type of device this is.
    /// </summary>
    /// <para>
    ///     The HID specification defines a pair of values called the Usage. Usages are part of the HID report descriptor
    ///     and are used to inform an application developer what the HID device is supposed to represent. If a device
    ///     has more than one usage, the value here is considered to be the "primary" usage.
    /// </para>
    /// <para>
    ///     This property defines the "Usage Page" portion of the usage field. It is a 16-bit unsigned value. The usage
    ///     page is used to indicate what kind of device or device interaction that this object represents. Note: this
    ///     field is represented by the <see cref="HidUsagePage" /> enumeration type. This enumeration is not exhaustive;
    ///     it only contains the usage pages that this SDK is most interested in. Code reading from this property should
    ///     be aware of the high likelihood of reading an out of range value.
    /// </para>
    /// <para>
    ///     The value of this property is determined by the appropriate HID specification for the device in question.
    ///     Knowing this value for something other than a generic HID type (such as a keyboard, mouse, or joystick) may
    ///     require locating additional specifications, as vendor defined values are allowed.
    /// </para>
    public HidUsagePage UsagePage { get; protected set; }

    /// <inheritdoc />
    public DateTime LastAccessed { get; protected set; } = DateTime.MinValue;

    /// <summary>
    ///     Opens a connection to the human interface device's feature reports.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The HID specification defines three report types that can be used to interact directly with the device: input,
    ///         output, and feature. This method connects to the feature report mechanism.
    ///     </para>
    ///     <para>
    ///         Feature reports are often used to describe device configuration that can be sent to the device. In the context
    ///         of the YubiKey SDK, feature reports are used to interact with the YubiKey's OTP application.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     An active connection to the device. <see cref="IHidConnection" /> implements <see cref="IDisposable" />, so
    ///     please call <see cref="IDisposable.Dispose" /> when you are finished with this object.
    /// </returns>
    public abstract IHidConnection ConnectToFeatureReports();

    /// <summary>
    ///     Opens a connection to the human interface device's input/output reports.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The HID specification defines three report types that can be used to interact directly with the device: input,
    ///         output, and feature. This method connects to the input and output report mechanisms.
    ///     </para>
    ///     <para>
    ///         Input reports are often used to describe information on the current state of a HID device. Output reports
    ///         are used to send data to the device, and can be used to control its state. In the context of the YubiKey SDK,
    ///         input and output reports are used to interact with the YubiKey's FIDO application.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     An active connection to the device. <see cref="IHidConnection" /> implements <see cref="IDisposable" />, so
    ///     please call <see cref="IDisposable.Dispose" /> when you are finished with this object.
    /// </returns>
    public abstract IHidConnection ConnectToIOReports();

    #endregion

    /// <summary>
    ///     Get a list of all the HIDs present on this computer.
    /// </summary>
    /// <returns>
    ///     An enumeration of all HIDs found on this computer. No connection to these devices has been established yet.
    /// </returns>
    /// <exception cref="PlatformNotSupportedException">
    ///     Support for this operating system platform has not been added to the SDK yet.
    /// </exception>
    public static IEnumerable<HidDevice> GetHidDevices() =>
        SdkPlatformInfo.OperatingSystem switch
        {
            SdkPlatform.Windows => WindowsHidDevice.GetList(),
            SdkPlatform.MacOS => MacOSHidDevice.GetList(),
            SdkPlatform.Linux => LinuxHidDevice.GetList(),
            _ => throw new PlatformNotSupportedException()
        };

    public override string ToString() => $"HID:{VendorId:X4}:{ProductId:X4} Usage:{UsagePage} {Path}";
}
