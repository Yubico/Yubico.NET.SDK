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
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Yubico.PlatformInterop
{
    // This class represents the C libudev "struct udev_device *" class.
    internal class LinuxUdevDeviceSafeHandle : SafeHandle
    {
        public override bool IsInvalid => handle == IntPtr.Zero;

        public LinuxUdevDeviceSafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle()
        {
            _ = NativeMethods.udev_device_unref(handle);
            return true;
        }
    }
}
