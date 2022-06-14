// Copyright 2021 Yubico AB
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

namespace Yubico.PlatformInterop
{
    internal static partial class NativeMethods
    {
        internal const string UdevSubsystemName = "hidraw";
        internal const string UdevMonitorName = "udev";

        // Returns a new SafeHandle. If it fails, the returnValue.IsInvalid will
        // be true.
        // The C function returns a new object that is the responsibility of the
        // caller to destroy, which is why this is returned as a SafeHandle.
        // The C signature is
        //   struct udev *udev_new(void);
        // Note that the DefaultDllImportSearchPaths attribute is a security best
        // practice on the Windows platform (and required by our analyzer
        // settings). It does not currently have any effect on platforms other
        // than Windows, but is included because of the analyzer and in the hope
        // that it will be supported by these platforms in the future.
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_new")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern LinuxUdevSafeHandle udev_new();

        // "Destroy" the object. This function always returns null, so it is
        // possible to call
        //   udevObject = udev_unref(udevObject);
        // This will be called from within the SafeHandle class, but should be
        // called by no one else.
        // The C signature is
        //   struct udev *udev_unref(struct udev *udev);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_unref")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr udev_unref(IntPtr udevObject);

        // Returns a new Handle. If it fails, the returnValue.IsInvalid will
        // be true.
        // The input is a LinuxUdevSafeHandle, which was the return value from
        // udev_new.
        // The C function returns a new object that is the responsibility of the
        // caller to destroy, which is why this is returned as a SafeHandle.
        // The C signature is
        //   struct udev_enumerate *udev_enumerate_new(struct udev *udev);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_enumerate_new")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern LinuxUdevEnumerateSafeHandle udev_enumerate_new(LinuxUdevSafeHandle udevObject);

        // "Destroy" the object. This function always returns null, so it is
        // possible to call
        //   enumerateObject = udev_enumerate_unref(enumerateObject);
        // This will be called from within the SafeHandle class, but should be
        // called by no one else.
        // The C signature is
        //   struct udev_enumerate *udev_enumerate_unref(struct udev_enumerate *udev_enumerate);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_enumerate_unref")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr udev_enumerate_unref(IntPtr enumerateObject);

        // Set the object with a subsystem. This only says, "When you scan for
        // devices, you will scan for this type of device."
        // If the result is < 0, error.
        // The C signature is
        //   int udev_enumerate_add_match_subsystem(
        //       struct udev_enumerate *udev_enumerate, const char *subsystem);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, BestFitMapping = false, EntryPoint = "udev_enumerate_add_match_subsystem")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern int udev_enumerate_add_match_subsystem(
            LinuxUdevEnumerateSafeHandle enumerateObject, string subsystem);

        // Scan for devices, namely, devices that match attributes from all
        // add_match calls.
        // After scanning, the object will have its internal list of devices.
        // If the result is < 0, error.
        // The C signature is
        //   int udev_enumerate_scan_devices(struct udev_enumerate *udev_enumerate);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_enumerate_scan_devices")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern int udev_enumerate_scan_devices(LinuxUdevEnumerateSafeHandle enumerateObject);

        // Get the first entry in the list.
        // The return is a reference to an object that belongs to the
        // enumerateObject. We don't destroy it. Hence, it is an IntPtr, not a
        // SafeHandle.
        // A null return is valid.
        // The C signature is
        //   struct udev_list_entry *udev_enumerate_get_list_entry(struct udev_enumerate *udev_enumerate);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_enumerate_get_list_entry")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr udev_enumerate_get_list_entry(LinuxUdevEnumerateSafeHandle enumerateObject);

        // Get the next entry in the list. As with a link list, it is possible to
        // get the next entry from the previous.
        // The return is a reference to an object that belongs to the
        // enumerateObject. We don't destroy it. Hence, it is an IntPtr, not a
        // SafeHandle.
        // A null return is valid.
        // The C signature is
        //   struct udev_list_entry *udev_list_entry_get_next(struct udev_list_entry *list_entry);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_list_entry_get_next")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr udev_list_entry_get_next(IntPtr previousEntry);

        // Get the name associated with this entry. It is the path.
        // The return value is a string, but in the form of a pointer to ASCII
        // bytes. Hence, to convert the result into a C# string object, use
        //   Marshal.PtrToStringAnsi
        // The return is a reference to an object that belongs to the
        // enumerateObject. We don't destroy it. Hence, it is an IntPtr, not a
        // SafeHandle.
        // The C signature is
        //   const char *udev_list_entry_get_name(struct udev_list_entry *list_entry);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_list_entry_get_name")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr udev_list_entry_get_name(IntPtr currentEntry);

        // Build a new Device object from the path (the path to pass in is the
        // return value from udev_list_entry_get_name).
        // This is a new object and the caller must destroy it.
        // The input is a LinuxUdevSafeHandle, which was the return value from
        // udev_new.
        // The C function returns a new object that is the responsibility of the
        // caller to destroy, which is why this is returned as a SafeHandle.
        // The C signature is
        //   struct udev_device *udev_device_new_from_syspath(
        //       struct udev *udev, const char *syspath);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, BestFitMapping = false, EntryPoint = "udev_device_new_from_syspath")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern LinuxUdevDeviceSafeHandle udev_device_new_from_syspath(
            LinuxUdevSafeHandle udevObject, string path);

        // "Destroy" the object. Returns null, so it is possible to call
        //   deviceObject = udev_device_unref(deviceObject);
        // This will be called from within the SafeHandle class, but should be
        // called by no one else.
        // The C signature is
        //   struct udev_device *udev_device_unref(struct udev_device *udev_device);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_device_unref")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr udev_device_unref(IntPtr deviceObject);

        // Get the devnode from the device.
        // This is what will be used by the HIDRAW library.
        // The return value is a string, but in the form of a pointer to ASCII
        // bytes. Hence, to convert the result into a C# string object, use
        //   Marshal.PtrToStringAnsi
        // The return is a reference to an object that belongs to the
        // deviceObject. We don't destroy it. Hence, it is an IntPtr, not a
        // SafeHandle.
        // The C signature is
        //   const char *udev_device_get_devnode(struct udev_device *udev_device);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_device_get_devnode")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr udev_device_get_devnode(LinuxUdevDeviceSafeHandle deviceObject);

        // Get the parent device from the current device.
        // The return value is another UDEV device handle which is ref counted,
        // hence this function returns a disposable safe handle so that we
        // are careful to reliably release this resource when we are done with it.
        // The C signature is
        //   struct udev_device *udev_device_get_parent(struct udev_device *udev_device);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_device_get_parent")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern LinuxUdevDeviceSafeHandle udev_device_get_parent(LinuxUdevDeviceSafeHandle deviceObject);

        // Get the path from the device.
        // This is what will be used by the HIDRAW library.
        // The return value is a string, but in the form of a pointer to ASCII
        // bytes. Hence, to convert the result into a C# string object, use
        //   Marshal.PtrToStringAnsi
        // The return is a reference to an object that belongs to the
        // deviceObject. We don't destroy it. Hence, it is an IntPtr, not a
        // SafeHandle.
        // The C signature is
        //   const char *udev_device_get_syspath(struct udev_device *udev_device);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_device_get_syspath")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr udev_device_get_syspath(LinuxUdevDeviceSafeHandle deviceObject);

        // Gets a string specifying what the latest action was: "add", "remove",
        // and others.
        // The C signature is
        //   const char *udev_device_get_action(struct udev_device *udev_device);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, BestFitMapping = false, EntryPoint = "udev_device_get_action")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr udev_device_get_action(LinuxUdevDeviceSafeHandle deviceObject);

        // Returns a new Handle. If it fails, the returnValue.IsInvalid will
        // be true.
        // The input is a LinuxUdevSafeHandle, which was the return value from
        // udev_new.
        // The C function returns a new object that is the responsibility of the
        // caller to destroy, which is why this is returned as a SafeHandle.
        // The C signature is
        //   struct udev_monitor * udev_monitor_new_from_netlink(
        //       struct udev *udev, const char *name);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, BestFitMapping = false, EntryPoint = "udev_monitor_new_from_netlink")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern LinuxUdevMonitorSafeHandle udev_monitor_new_from_netlink(
            LinuxUdevSafeHandle udevObject, string name);

        // "Destroy" the object.
        // This will be called from within the SafeHandle class, but should be
        // called by no one else.
        // The C signature is
        //   void udev_monitor_unref(struct udev_monitor *udev_monitor);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_monitor_unref")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void udev_monitor_unref(IntPtr monitorObject);

        // Set the object to monitor devices of the given subsystem and devtype
        // (devtype can be NULL). This only says, "When you monitor for devices,
        // you will monitor for this type of device."
        // If the result is < 0, error.
        // The C signature is
        //   int udev_monitor_filter_add_match_subsystem_devtype(
        //       struct udev_monitor *udev_monitor, const char *subsystem, const char *devtype);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, BestFitMapping = false, EntryPoint = "udev_monitor_filter_add_match_subsystem_devtype")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern int udev_monitor_filter_add_match_subsystem_devtype(
            LinuxUdevMonitorSafeHandle monitorObject, string subsystem, string? devtype);

        // Set the monitor object to be able to receive reports.
        // If the result is < 0, error.
        // The C signature is
        //   int udev_monitor_enable_receiving(struct udev_monitor *udev_monitor);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_monitor_enable_receiving")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern int udev_monitor_enable_receiving(LinuxUdevMonitorSafeHandle monitorObject);

        // Get the latest report. If there has been a change, the function will
        // return a new Device, the device that has changed. If there has been no
        // change, this will return NULL.
        // The C signature is
        //   struct udev_device *udev_monitor_receive_device(struct udev_monitor *udev_monitor);
        [DllImport(Libraries.LinuxUdevLib, CharSet = CharSet.Ansi, EntryPoint = "udev_monitor_receive_device")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern LinuxUdevDeviceSafeHandle udev_monitor_receive_device (LinuxUdevMonitorSafeHandle monitorObject);
    }
}
