// Copyright 2026 Yubico AB
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

using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

/// <summary>
/// Verifies <see cref="WindowsHidDeviceListener.ReadSymbolicLink"/> never reads past the
/// event payload reported by the native notification, even when the buffer is not
/// NUL-terminated. Pure memory parsing with no P/Invoke, so it runs on all platforms.
/// </summary>
public class WindowsHidDeviceListenerTests
{
    /// <summary>
    /// Offset of the SymbolicLink field in CM_NOTIFY_EVENT_DATA. Mirrors the constant in
    /// <see cref="WindowsHidDeviceListener"/>.
    /// </summary>
    private const int SymbolicLinkOffset = 24;

    [Fact]
    public void ReadSymbolicLink_NulTerminatedWithinPayload_ReturnsPath()
    {
        const string expected = @"\\?\HID#VID_1050&PID_0407";

        RunWithEventData(expected + '\0', trailingGarbage: false, (eventData, eventDataSize) =>
        {
            var result = WindowsHidDeviceListener.ReadSymbolicLink(eventData, eventDataSize);

            Assert.Equal(expected, result);
        });
    }

    [Fact]
    public void ReadSymbolicLink_MissingNulTerminator_ReadsOnlyDeclaredPayload()
    {
        // No NUL anywhere in the declared payload; garbage characters follow it in memory.
        // The parser must stop at eventDataSize instead of scanning for a terminator.
        const string payload = @"\\?\HID#VID_1050";

        RunWithEventData(payload, trailingGarbage: true, (eventData, eventDataSize) =>
        {
            var result = WindowsHidDeviceListener.ReadSymbolicLink(eventData, eventDataSize);

            Assert.Equal(payload, result);
        });
    }

    [Fact]
    public void ReadSymbolicLink_NulBeforeEndOfPayload_TrimsAtFirstNul()
    {
        const string expected = @"\\?\HID#VID_1050";

        RunWithEventData(expected + "\0IGNORED", trailingGarbage: false, (eventData, eventDataSize) =>
        {
            var result = WindowsHidDeviceListener.ReadSymbolicLink(eventData, eventDataSize);

            Assert.Equal(expected, result);
        });
    }

    [Fact]
    public void ReadSymbolicLink_EmptyString_ReturnsNull()
    {
        RunWithEventData("\0", trailingGarbage: false, (eventData, eventDataSize) =>
        {
            var result = WindowsHidDeviceListener.ReadSymbolicLink(eventData, eventDataSize);

            Assert.Null(result);
        });
    }

    [Fact]
    public void ReadSymbolicLink_PayloadTooSmallForAnyCharacter_ReturnsNull()
    {
        RunWithEventData("X", trailingGarbage: false, (eventData, _) =>
        {
            // Declared size covers the header only - no room for even one character.
            var result = WindowsHidDeviceListener.ReadSymbolicLink(eventData, SymbolicLinkOffset);

            Assert.Null(result);
        });
    }

    [Fact]
    public void ReadSymbolicLink_ZeroPointer_ReturnsNull()
    {
        var result = WindowsHidDeviceListener.ReadSymbolicLink(IntPtr.Zero, 256);

        Assert.Null(result);
    }

    /// <summary>
    /// Allocates a native buffer laid out like CM_NOTIFY_EVENT_DATA: 24 header bytes followed
    /// by <paramref name="symbolicLink"/> as UTF-16. The declared event size covers exactly the
    /// header plus the string. When <paramref name="trailingGarbage"/> is set, non-NUL sentinel
    /// characters are written after the declared payload so an unbounded read would include them.
    /// </summary>
    private static void RunWithEventData(string symbolicLink, bool trailingGarbage, Action<IntPtr, int> assert)
    {
        var payloadChars = symbolicLink.ToCharArray();
        var eventDataSize = SymbolicLinkOffset + (payloadChars.Length * sizeof(char));
        var garbageChars = trailingGarbage ? 8 : 0;
        var allocationSize = eventDataSize + (garbageChars * sizeof(char));

        var eventData = Marshal.AllocHGlobal(allocationSize);
        try
        {
            for (var i = 0; i < SymbolicLinkOffset; i++)
            {
                Marshal.WriteByte(eventData, i, 0);
            }

            Marshal.Copy(payloadChars, 0, IntPtr.Add(eventData, SymbolicLinkOffset), payloadChars.Length);

            if (garbageChars > 0)
            {
                var garbage = new char[garbageChars];
                Array.Fill(garbage, 'Z');
                Marshal.Copy(garbage, 0, IntPtr.Add(eventData, eventDataSize), garbageChars);
            }

            assert(eventData, eventDataSize);
        }
        finally
        {
            Marshal.FreeHGlobal(eventData);
        }
    }
}