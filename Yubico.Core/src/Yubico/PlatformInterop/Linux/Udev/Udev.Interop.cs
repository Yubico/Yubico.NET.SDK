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
        // Returns a new SafeHandle. If it fails, the returnValue.IsInvalid will
        // be true.
        // The C function returns a new object that is the responsibility of the
        // caller to destroy, which is why this is returned as a SafeHandle.
        // The C signature is
        //   struct udev *udev_new(void);
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

        // Returns a new Ptr. If it fails, the returnValue.IsInvalid will
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

        // "Destroy" the object.This function always returns null, so it is
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
    }
}
