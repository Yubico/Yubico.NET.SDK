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
using System.Runtime.InteropServices;

namespace Yubico.PlatformInterop;

internal static partial class NativeMethods
{
    #region Delegates

    public delegate void IOHIDCallback(IntPtr context, int result, IntPtr sender);

    public delegate void IOHIDDeviceCallback(IntPtr context, int result, IntPtr sender, IntPtr device);

    /*! @typedef IOHIDReportCallback
        @discussion Type and arguments of callout C function that is used when a HID report completion routine is called.
        @param context void * pointer to your data, often a pointer to an object.
        @param result Completion result of desired operation.
        @param sender Interface instance sending the completion routine.
        @param type The type of the report that was completed.
        @param reportID The ID of the report that was completed.
        @param report Pointer to the buffer containing the contents of the report.
        @param reportLength Size of the buffer received upon completion.
    */
    public delegate void IOHIDReportCallback(
        IntPtr context,
        int result,
        IntPtr sender,
        int type,
        int reportId,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 6)]
        byte[] report,
        long reportLength);

    #endregion

    /*!
        @function   IOHIDManagerCreate
        @abstract   Creates an IOHIDManager object.
        @discussion The IOHIDManager object is meant as a global management system
                    for communicating with HID devices.
        @param      allocator Allocator to be used during creation.
        @param      options Supply @link kIOHIDManagerOptionUsePersistentProperties @/link to load
                    properties from the default persistent property store. Otherwise supply
                    @link kIOHIDManagerOptionNone @/link (or 0).
        @result     Returns a new IOHIDManagerRef.
    */
    // Note that the DefaultDllImportSearchPaths attribute is a security best
    // practice on the Windows platform (and required by our analyzer
    // settings). It does not currently have any effect on platforms other
    // than Windows, but is included because of the analyzer and in the hope
    // that it will be supported by these platforms in the future.
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern IntPtr IOHIDManagerCreate(IntPtr allocator, int options); /* OS >= 10.5 */

    /*! @function   IOHIDManagerSetDeviceMatching
        @abstract   Sets matching criteria for device enumeration.
        @discussion Matching keys are prefixed by kIOHIDDevice and declared in
                    <IOKit/hid/IOHIDKeys.h>.  Passing a NULL dictionary will result
                    in all devices being enumerated. Any subsequent calls will cause
                    the hid manager to release previously enumerated devices and
                    restart the enumerate process using the revised criteria.  If
                    interested in multiple, specific device classes, please defer to
                    using IOHIDManagerSetDeviceMatchingMultiple.
                    If a dispatch queue is set, this call must occur before activation.
        @param      manager Reference to an IOHIDManager.
        @param      matching CFDictionaryRef containing device matching criteria.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern void IOHIDManagerSetDeviceMatching(IntPtr manager, IntPtr matching);

    /*! @function   IOHIDManagerCopyDevices
        @abstract   Obtains currently enumerated devices.
        @param      manager Reference to an IOHIDManager.
        @result     CFSetRef containing IOHIDDeviceRefs.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern IntPtr IOHIDManagerCopyDevices(IntPtr manager); /* OS >= 10.5 */

    /*! @function   IOHIDManagerRegisterDeviceMatchingCallback
        @abstract   Registers a callback to be used a device is enumerated.
        @discussion Only device matching the set criteria will be enumerated.
                    If a dispatch queue is set, this call must occur before activation.
                    Devices provided in the callback will be scheduled with the same
                    runloop/dispatch queue as the IOHIDManagerRef, and should not be
                    rescheduled.
        @param      manager Reference to an IOHIDManagerRef.
        @param      callback Pointer to a callback method of type
                    IOHIDDeviceCallback.
        @param      context Pointer to data to be passed to the callback.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern void IOHIDManagerRegisterDeviceMatchingCallback(
        IntPtr manager,
        IOHIDDeviceCallback callback,
        IntPtr context); /* OS >= 10.5 */

    /*! @function   IOHIDManagerRegisterDeviceRemovalCallback
        @abstract   Registers a callback to be used when any enumerated device is
                    removed.
        @discussion In most cases this occurs when a device is unplugged.
                    If a dispatch queue is set, this call must occur before activation.
        @param      manager Reference to an IOHIDManagerRef.
        @param      callback Pointer to a callback method of type
                    IOHIDDeviceCallback.
        @param      context Pointer to data to be passed to the callback.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern void IOHIDManagerRegisterDeviceRemovalCallback(
        IntPtr manager,
        IOHIDDeviceCallback callback,
        IntPtr context); /* OS >= 10.5 */

    /*! @function   IOHIDManagerScheduleWithRunLoop
        @abstract   Schedules HID manager with run loop.
        @discussion Formally associates manager with client's run loop. Scheduling
                    this device with the run loop is necessary before making use of
                    any asynchronous APIs.  This will propagate to current and
                    future devices that are enumerated.
        @param      manager Reference to an IOHIDManager.
        @param      runLoop RunLoop to be used when scheduling any asynchronous
                    activity.
        @param      runLoopMode Run loop mode to be used when scheduling any
                    asynchronous activity.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern void IOHIDManagerScheduleWithRunLoop(
        IntPtr manager,
        IntPtr runLoop,
        IntPtr runLoopMode); /* OS >= 10.5 */

    /*! @function   IOHIDManagerUnscheduleFromRunLoop
        @abstract   Unschedules HID manager with run loop.
        @discussion Formally disassociates device with client's run loop. This will
                    propagate to current devices that are enumerated.
        @param      manager Reference to an IOHIDManager.
        @param      runLoop RunLoop to be used when unscheduling any asynchronous
                    activity.
        @param      runLoopMode Run loop mode to be used when unscheduling any
                    asynchronous activity.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern void IOHIDManagerUnscheduleFromRunLoop(
        IntPtr manager,
        IntPtr runLoop,
        IntPtr runLoopMode); /* OS >= 10.5 */

    /*!
        @function   IOHIDDeviceCreate
        @abstract   Creates an element from an io_service_t.
        @discussion The io_service_t passed in this method must reference an object
                    in the kernel of type IOHIDDevice.
        @param      allocator Allocator to be used during creation.
        @param      service Reference to service object in the kernel.
        @result     Returns a new IOHIDDeviceRef.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern IntPtr IOHIDDeviceCreate(IntPtr allocator, int service); /* OS >= 10.5 */

    /*!
        @function   IOHIDDeviceOpen
        @abstract   Opens a HID device for communication.
        @discussion Before the client can issue commands that change the state of
                    the device, it must have succeeded in opening the device. This
                    establishes a link between the client's task and the actual
                    device.  To establish an exclusive link use the
                    kIOHIDOptionsTypeSeizeDevice option.
        @param      device Reference to an IOHIDDevice.
        @param      options Option bits to be sent down to the device.
        @result     Returns kIOReturnSuccess if successful.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern int IOHIDDeviceOpen(IntPtr device, int options); /* OS >= 10.5 */

    /*!
        @function   IOHIDDeviceClose
        @abstract   Closes communication with a HID device.
        @discussion This closes a link between the client's task and the actual
                    device.
        @param      device Reference to an IOHIDDevice.
        @param      options Option bits to be sent down to the device.
        @result     Returns kIOReturnSuccess if successful.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern int IOHIDDeviceClose(IntPtr device, int options); /* OS >= 10.5 */

    /*!
        @function   IOHIDDeviceGetProperty
        @abstract   Obtains a property from an IOHIDDevice.
        @discussion Property keys are prefixed by kIOHIDDevice and declared in
                    <IOKit/hid/IOHIDKeys.h>.
        @param      device Reference to an IOHIDDevice.
        @param      key CFStringRef containing key to be used when querying the
                    device.
        @result     Returns CFTypeRef containing the property.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern IntPtr IOHIDDeviceGetProperty(IntPtr device, IntPtr key); /* OS >= 10.5 */

    /*! @function   IOHIDDeviceGetReport
        @abstract   Obtains a report from the device.
        @discussion This method behaves synchronously and will block until the
                    report has been received from the device.  This is only intended
                    for feature reports because of sporadic device support for
                    polling input reports.  Please defer to using
                    IOHIDDeviceRegisterInputReportCallback for obtaining input
                    reports.
        @param      device Reference to an IOHIDDevice.
        @param      reportType Type of report being requested.
        @param      reportID ID of the report being requested.
        @param      report Pointer to pre-allocated buffer in which to copy inbound
                    report data.
        @param      pReportLength Pointer to length of pre-allocated buffer.  This
                    value will be modified to reflect the length of the returned
                    report.
        @result     Returns kIOReturnSuccess if successful.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern int IOHIDDeviceGetReport(
        IntPtr device,
        int reportType,
        long reportID,
        byte[] report,
        ref long pReportLength); /* OS >= 10.5 */

    /*! @function   IOHIDDeviceSetReport
        @abstract   Sends a report to the device.
        @discussion This method behaves synchronously and will block until the
                    report has been issued to the device.  It is only relevant for
                    either output or feature type reports.
        @param      device Reference to an IOHIDDevice.
        @param      reportType Type of report being sent.
        @param      reportID ID of the report being sent.  If the device supports
                    multiple reports, this should also be set in the first byte of
                    the report.
        @param      report The report bytes to be sent to the device.
        @param      reportLength The length of the report to be sent to the device.
        @result     Returns kIOReturnSuccess if successful.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern int IOHIDDeviceSetReport(
        IntPtr device,
        int reportType,
        long reportID,
        byte[] report,
        long reportLength); /* OS >= 10.5 */

    /*!
        @function   IOHIDDeviceGetService
        @abstract   Returns the io_service_t for an IOHIDDevice, if it has one.
        @discussion If the IOHIDDevice references an object in the kernel, this is
                    used to get the io_service_t for that object.
        @param      device Reference to an IOHIDDevice.
        @result     Returns the io_service_t if the IOHIDDevice has one, or
                    MACH_PORT_NULL if it does not.
     */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static extern int IOHIDDeviceGetService(IntPtr device);

    /*! @function   IOHIDDeviceRegisterInputReportCallback
        @abstract   Registers a callback to be used when an input report is issued
                    by the device.
        @discussion An input report is an interrupt driver report issued by the
                    device.
                    If a dispatch queue is set, this call must occur before activation.
        @param      device Reference to an IOHIDDevice.
        @param      report Pointer to pre-allocated buffer in which to copy inbound
                    report data.
        @param      reportLength Length of pre-allocated buffer.
        @param      callback Pointer to a callback method of type
                    IOHIDReportCallback.
        @param      context Pointer to data to be passed to the callback.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static extern void IOHIDDeviceRegisterInputReportCallback(
        IntPtr device, // IOHIDDeviceRef
        byte[] report, // uint8_t*
        long reportLength, // CFIndex (size_t)
        IntPtr
            callback, // IOHIDReportCallback (void*, IOReturn, void*, IOHIDReportType, uint32_t, uint8_t*, CFIndex) -> void
        IntPtr context); // void* (optional)

    /*! @function   IOHIDDeviceScheduleWithRunLoop
        @abstract   Schedules HID device with run loop.
        @discussion Formally associates device with client's run loop. Scheduling
                    this device with the run loop is necessary before making use of
                    any asynchronous APIs.
        @param      device Reference to an IOHIDDevice.
        @param      runLoop RunLoop to be used when scheduling any asynchronous
                    activity.
        @param      runLoopMode Run loop mode to be used when scheduling any
                    asynchronous activity.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static extern void IOHIDDeviceScheduleWithRunLoop(
        IntPtr device, // IOHIDDeviceRef
        IntPtr runLoop, // CFRunLoopRef (struct *)
        IntPtr runLoopMode); // CFStringRef (struct *)

    /*! @function   IOHIDDeviceUnscheduleFromRunLoop
        @abstract   Unschedules HID device with run loop.
        @discussion Formally disassociates device with client's run loop.
        @param      device Reference to an IOHIDDevice.
        @param      runLoop RunLoop to be used when unscheduling any asynchronous
                    activity.
        @param      runLoopMode Run loop mode to be used when unscheduling any
                    asynchronous activity.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static extern void IOHIDDeviceUnscheduleFromRunLoop(
        IntPtr device,
        IntPtr runLoop,
        IntPtr runLoopMode);

    /*! @function   IOHIDDeviceRegisterRemovalCallback
        @abstract   Registers a callback to be used when a IOHIDDevice is removed.
        @discussion In most cases this occurs when a device is unplugged.
                    If a dispatch queue is set, this call must occur before activation.
        @param      device Reference to an IOHIDDevice.
        @param      callback Pointer to a callback method of type IOHIDCallback.
        @param      context Pointer to data to be passed to the callback.
    */
    [DllImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static extern void IOHIDDeviceRegisterRemovalCallback(
        IntPtr device, // IOHIDDeviceRef
        IntPtr callback, // IOHIDCallback (void*, IOResult, void*) -> void
        IntPtr context); // void* (optional)
}
