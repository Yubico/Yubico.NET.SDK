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

using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public class CoreFoundationStringTests
{
    [Fact]
    public void Create_PassesNullTerminatedUtf8BytesAndUtf8Encoding()
    {
        byte[]? capturedBytes = null;
        var capturedEncoding = 0;

        var result = CoreFoundationString.Create(
            "mode-å",
            (bytes, encoding) =>
            {
                capturedBytes = bytes;
                capturedEncoding = encoding;
                return 42;
            });

        Assert.Equal((nint)42, result);
        Assert.Equal([0x6D, 0x6F, 0x64, 0x65, 0x2D, 0xC3, 0xA5, 0x00], capturedBytes);
        Assert.Equal(0x08000100, capturedEncoding);
    }

    [Fact]
    public void Create_WhenNativeCreationFails_ThrowsPlatformApiException()
    {
        _ = Assert.Throws<PlatformApiException>(
            () => CoreFoundationString.Create("mode", static (_, _) => IntPtr.Zero));
    }
}