// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Text;
using Yubico.YubiKit.Core.Native;
using CFNativeMethods = Yubico.YubiKit.Core.Native.MacOS.CoreFoundation.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

internal static class CoreFoundationString
{
    private const int Utf8Encoding = 0x08000100;

    internal static nint Create(string value) =>
        Create(
            value,
            static (bytes, encoding) =>
                CFNativeMethods.CFStringCreateWithCString(IntPtr.Zero, bytes, encoding));

    internal static nint Create(string value, Func<byte[], int, nint> create)
    {
        byte[] bytes = [.. Encoding.UTF8.GetBytes(value), 0];
        var stringRef = create(bytes, Utf8Encoding);

        if (stringRef == IntPtr.Zero)
            throw new PlatformApiException(
                $"{nameof(CFNativeMethods.CFStringCreateWithCString)} failed to create a CoreFoundation string.");

        return stringRef;
    }
}
