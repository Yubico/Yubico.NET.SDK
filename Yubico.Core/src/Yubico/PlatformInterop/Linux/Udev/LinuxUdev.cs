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
using System.Globalization;
using System.Runtime.InteropServices;
using Yubico.Core;

namespace Yubico.PlatformInterop
{
    // This class will use the P/Invoke udev functions to perform operations.
    // Use this class just as you would use any C# class. Under the covers it
    // will call to the native libraries.
    // This is the base class for all supported libudev operations.
    // You will likely nevr use this class directly, but ise one of the
    // subclasses.
    internal class LinuxUdev : IDisposable
    {
        protected LinuxUdevSafeHandle _udevObject;
        private bool _isDisposed;

        // Create a new instance of LinuxUdev. This is essentially equivalent in
        // C to calling udev_new to get the initial object.
        // That is, in C, the first thing you do is call
        //    struct udev *udevObject = udev_new();
        public LinuxUdev()
        {
            _udevObject = (LinuxUdevSafeHandle)ThrowIfFailedNull(NativeMethods.udev_new());
        }

        // Throw the PlatformApiException(LinuxUdevError) if the value is NULL.
        // Otherwise, just return value.
        protected static SafeHandle ThrowIfFailedNull(SafeHandle value)
        {
            if (!value.IsInvalid)
            {
                return value;
            }

            throw new PlatformApiException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.LinuxUdevError));
        }

        // Throw the PlatformApiException(LinuxUdevError) if the value is < 0.
        // Otherwise, just return.
        protected static int ThrowIfFailedNegative(int value)
        {
            if (value >= 0)
            {
                return value;
            }

            throw new PlatformApiException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.LinuxUdevError));
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _udevObject.Dispose();
            _isDisposed = true;
        }
    }
}
