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
using System.Text;
using Yubico.PlatformInterop;
using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.Core.Devices.Hid
{
    /// <summary>
    ///     Utility methods for interacting with macOS IOKit framework.
    /// </summary>
    internal static class IOKitHelpers
    {
        /// <summary>
        ///     Gets an integer-typed property value from a device.
        /// </summary>
        /// <param name="device">
        ///     The previously opened device against which the property should be queried.
        /// </param>
        /// <param name="propertyName">
        ///     The name of the property to query for.
        /// </param>
        /// <returns>
        ///     The value of the property.
        /// </returns>
        /// <exception cref="PlatformApiException">
        ///     The type requested and the type returned by IOKit do not match.
        /// </exception>
        /// <exception cref="NullReferenceException">
        ///     An attempt was made to dereference the property, even though it was null.
        /// </exception>
        public static int GetIntPropertyValue(IntPtr device, string propertyName)
        {
            int? propertyValue = GetNullableIntPropertyValue(device, propertyName);

            // We want to rely on Nullable<T>'s null checking and subsequent exception.
            // Rather than duplicate the messaging and exception ourselves, let's just
            // use theirs.
            #pragma warning disable CS8629
            return propertyValue.Value;
            #pragma warning restore CS8629
        }

        /// <summary>
        ///     Gets an nullable integer-typed property value from a device.
        /// </summary>
        /// <param name="device">
        ///     The previously opened device against which the property should be queried.
        /// </param>
        /// <param name="propertyName">
        ///     The name of the property to query for.
        /// </param>
        /// <returns>
        ///     The value of the property.
        /// </returns>
        /// <exception cref="PlatformApiException">
        ///     The type requested and the type returned by IOKit do not match.
        /// </exception>
        public static int? GetNullableIntPropertyValue(IntPtr device, string propertyName)
        {
            const int kCFNumberTypeSignedInt = 3;

            IntPtr stringRef = IntPtr.Zero;

            try
            {
                byte[] cstr = Encoding.UTF8.GetBytes(propertyName);
                stringRef = CFStringCreateWithCString(IntPtr.Zero, cstr, encoding: 0);

                IntPtr propertyRef = IOHIDDeviceGetProperty(device, stringRef);

                if (propertyRef == IntPtr.Zero)
                {
                    return null;
                }

                ulong propertyType = CFGetTypeID(propertyRef);
                ulong numberType = CFNumberGetTypeID();
                if (propertyType != numberType)
                {
                    throw new PlatformApiException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.IOKitTypeMismatch,
                            numberType,
                            propertyType));
                }

                byte[] numberBytes = new byte[sizeof(int)];
                if (!CFNumberGetValue(propertyRef, kCFNumberTypeSignedInt, numberBytes))
                {
                    return 0;
                }

                return BitConverter.ToInt32(numberBytes, startIndex: 0);
            }
            finally
            {
                if (stringRef != IntPtr.Zero)
                {
                    CFRelease(stringRef);
                }
            }
        }
    }
}
