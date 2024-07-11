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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Yubico.Core.Devices.Hid;

namespace Yubico.PlatformInterop
{
    // This class will use the P/Invoke udev functions to perform operations.
    // This class will be able to scan for devices and return a list indicating
    // what it finds.
    // Use this class to find all the HIDs attached.
    // If we later on need libudev to do more than just find HIDs, we can update
    // this and/or add other classes.
    // To use this class...
    //  1. Instantiate
    //  2. Call EnumerateAddMatchSubsystem (what are we looking for?)
    //  3. Call EnumerateScanDevices (Scan for all the devices that match the
    //  subsystem[s] specified in the AddMatch call)
    //  4. Call GetLinuxHidDeviceList (what was found?)
    internal class LinuxUdevScan : LinuxUdev
    {
        private readonly LinuxUdevEnumerateSafeHandle _enumerateObject;
        private bool _isDisposed;

        // Create a new instance of LinuxUdevScan, the object that will be able
        // to scan for devices. This is essentially equivalent in C to calling
        // udev_enumerate_new.
        // That is, in C, to get an object that can scan for devices, make this
        // call.
        //    struct udev_enumerate *enumerateObject = udev_enumerate_new();
        // Using this class, the first thing you do is
        //    var scatObject = new LinuxUdevScan();
        // Then you call the methods of this object to perform the operations.
        // Note that this is a subclass of LinuxUdev, so you don't need to get an
        // instance of that class. That is, in C you get a struct udev *, and use
        // it to build a struct udev_enumerate *. But here, just build the
        // LinuxUdevScan, it will build all necessary supporting objects.
        public LinuxUdevScan()
        {
            _enumerateObject = NativeMethods.udev_enumerate_new(_udevObject);
            _ = ThrowIfFailedNull(_enumerateObject);
        }

        // C# version of udev_enumerate_add_match_subsystem
        // Set the object with a subsystem. This only says, "When you scan for
        // devices, you will scan for this type of device."
        // If the operation fails, this method will throw an exception.
        public void EnumerateAddMatchSubsystem(string subsystem) =>
            _ = ThrowIfFailedNegative(
                NativeMethods.udev_enumerate_add_match_subsystem(_enumerateObject, subsystem));

        // Perform a scan, looking for devices that match the criteria based on
        // previous add_match calls. After scanning, the object will have its
        // internal list of devices.
        public void EnumerateScanDevices() =>
            _ = ThrowIfFailedNegative(
                NativeMethods.udev_enumerate_scan_devices(_enumerateObject));

        // Get a list of all the devices discovered, represented as a List of
        // LinuxHidDevice.
        public IEnumerable<LinuxHidDevice> GetLinuxHidDeviceList()
        {
            var returnValue = new List<LinuxHidDevice>();

            // Get the first entry in the list.
            // The return is a reference to an object that belongs to the
            // udevEnumerateObject. We don't destroy it.
            // A null return is valid.
            IntPtr currentEntry = NativeMethods.udev_enumerate_get_list_entry(_enumerateObject);

            while (currentEntry != IntPtr.Zero)
            {
                // Get the name associated with this entry. It is the path.
                IntPtr namePtr = NativeMethods.udev_list_entry_get_name(currentEntry);

                // Get a Device object based on the path.
                using LinuxUdevDeviceSafeHandle currentDevice =
                    NativeMethods.udev_device_new_from_syspath(_udevObject, Marshal.PtrToStringAnsi(namePtr));

                _ = ThrowIfFailedNull(currentDevice);

                var linuxHid = new LinuxHidDevice(currentDevice);
                returnValue.Add(linuxHid);

                // Get the next entry in the list. As with a link list, it is
                // possible to get the next entry from the previous.
                // The return is a reference to an object that belongs to the
                // udevEnumerateObject. We don't destroy it.
                // A null return is valid.
                currentEntry = NativeMethods.udev_list_entry_get_next(currentEntry);
            }

            return returnValue;
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _enumerateObject.Dispose();
            base.Dispose(disposing);
            _isDisposed = true;
        }
    }
}
