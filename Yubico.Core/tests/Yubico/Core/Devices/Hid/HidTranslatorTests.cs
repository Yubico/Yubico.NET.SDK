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
using Xunit;

namespace Yubico.Core.Devices.Hid.UnitTests
{
    public class HidTranslatorTests
    {
        [Theory]
        [InlineData(KeyboardLayout.ModHex)]
        [InlineData(KeyboardLayout.en_US)]
        [InlineData(KeyboardLayout.en_UK)]
        [InlineData(KeyboardLayout.de_DE)]
        [InlineData(KeyboardLayout.fr_FR)]
        [InlineData(KeyboardLayout.it_IT)]
        [InlineData(KeyboardLayout.es_US)]
        [InlineData(KeyboardLayout.sv_SE)]
        public void GetHidCodes_GivenKeyboardLayout_ReturnsCorrectInstance(KeyboardLayout layout)
        {
            HidCodeTranslator hid = HidCodeTranslator.GetInstance(layout);
            Assert.Equal(layout, hid.Layout);
        }

        [Theory]
        [InlineData(KeyboardLayout.en_US, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.en_UK, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.de_DE, new byte[] { 0x9d, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.fr_FR, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x38 })]
        [InlineData(KeyboardLayout.it_IT, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.es_US, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.sv_SE, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        public void GetHidCodes_GivenString_ReturnsCorrectCodes(KeyboardLayout layout, byte[] expected)
        {
            HidCodeTranslator hid = HidCodeTranslator.GetInstance(layout);
            string s = "Yubico!";
            byte[] actual = hid.GetHidCodes(s);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(KeyboardLayout.en_US, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.en_UK, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.de_DE, new byte[] { 0x9d, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.fr_FR, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x38 })]
        [InlineData(KeyboardLayout.it_IT, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.es_US, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.sv_SE, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        public void GetHidCodes_GivenCharArray_ReturnsCorrectCodes(KeyboardLayout layout, byte[] expected)
        {
            HidCodeTranslator hid = HidCodeTranslator.GetInstance(layout);
            char[] s = { 'Y', 'u', 'b', 'i', 'c', 'o', '!' };
            byte[] actual = hid.GetHidCodes(s);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(KeyboardLayout.en_US, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.en_UK, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.de_DE, new byte[] { 0x9d, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.fr_FR, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x38 })]
        [InlineData(KeyboardLayout.it_IT, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.es_US, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        [InlineData(KeyboardLayout.sv_SE, new byte[] { 0x9c, 0x18, 0x05, 0x0c, 0x06, 0x12, 0x9e })]
        public void GetString_GivenHidCodes_ReturnsCorrectString(KeyboardLayout layout, byte[] input)
        {
            string expected = "Yubico!";
            HidCodeTranslator hid = HidCodeTranslator.GetInstance(layout);
            string actual = hid.GetString(input);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(KeyboardLayout.en_US)]
        [InlineData(KeyboardLayout.en_UK)]
        [InlineData(KeyboardLayout.de_DE)]
        [InlineData(KeyboardLayout.fr_FR)]
        [InlineData(KeyboardLayout.it_IT)]
        [InlineData(KeyboardLayout.es_US)]
        [InlineData(KeyboardLayout.sv_SE)]
        public void TestSpecializedKeyboardSupportsModhexCodes(KeyboardLayout layout)
        {
            // Since ModHex is a subset of all supported keyboard layouts, this
            // will confirm that all ModHex HID codes are the same in non-ModHex
            // layouts.
            HidCodeTranslator testHid = HidCodeTranslator.GetInstance(layout);
            HidCodeTranslator modHexHid = HidCodeTranslator.GetInstance(KeyboardLayout.ModHex);
            foreach (byte code in modHexHid.SupportedHidCodes)
            {
                Assert.Equal(modHexHid[code], testHid[code]);
            }
        }

        [Theory]
        [InlineData(KeyboardLayout.en_US)]
        [InlineData(KeyboardLayout.en_UK)]
        [InlineData(KeyboardLayout.de_DE)]
        [InlineData(KeyboardLayout.fr_FR)]
        [InlineData(KeyboardLayout.it_IT)]
        [InlineData(KeyboardLayout.es_US)]
        [InlineData(KeyboardLayout.sv_SE)]
        public void TestSpecializedKeyboardSupportsModhexChars(KeyboardLayout layout)
        {
            // Since ModHex is a subset of all keyboard layouts, this will confirm
            // that all ModHex chars are the same in non-ModHex layouts.
            HidCodeTranslator testHid = HidCodeTranslator.GetInstance(layout);
            HidCodeTranslator modHexHid = HidCodeTranslator.GetInstance(KeyboardLayout.ModHex);
            foreach (char ch in modHexHid.SupportedCharacters)
            {
                Assert.Equal(modHexHid[ch], testHid[ch]);
            }
        }

        [Theory]
        [InlineData(KeyboardLayout.en_US)]
        [InlineData(KeyboardLayout.en_UK)]
        [InlineData(KeyboardLayout.de_DE)]
        [InlineData(KeyboardLayout.fr_FR)]
        [InlineData(KeyboardLayout.it_IT)]
        [InlineData(KeyboardLayout.es_US)]
        [InlineData(KeyboardLayout.sv_SE)]
        public void TestSpecializedKeyboardSupportsModhexString(KeyboardLayout layout)
        {
            // Since ModHex is a subset of all keyboard layouts, this will confirm
            // that all ModHex chars are the same in non-ModHex layouts.
            HidCodeTranslator hid = HidCodeTranslator.GetInstance(KeyboardLayout.ModHex);
            byte[] modHexCodes = hid.SupportedHidCodes;
            string decoded = HidCodeTranslator.GetInstance(layout).GetString(modHexCodes);
            Assert.Equal(hid.SupportedCharactersString, decoded);
        }

#if Windows
#pragma warning disable CA1825
        [Theory]
        [MemberData(nameof(GetTestData))]
        public void GetChar_GivenHidCode_ReturnsCorrectChar(KeyboardLayout layout, (char, byte)[] testData)
        {
            var hid = HidCodeTranslator.GetInstance(layout);
            foreach ((char ch, byte code) item in testData)
            {
                Assert.Equal(item.ch, hid[item.code]);
            }
        }
#pragma warning restore CA1825
#endif

        public static IEnumerable<object[]> GetTestData()
        {
            // Originally, I hard-coded these, but I decided that it should do
            // this dynamically so that newly added keyboard layouts aren't left
            // out of these tests.
            foreach (KeyboardLayout layout in (KeyboardLayout[])Enum.GetValues(typeof(KeyboardLayout)))
            {
                yield return new object[] { layout, GetDataForKeyboard(layout) };
            }
        }

        // This method gets an array of tuples with the character and HID usage
        // code for all supported keystrokes for a keyboard layout.
        private static (char, byte)[] GetDataForKeyboard(KeyboardLayout layout)
        {
            var cases = new List<(char, byte)>();
            IntPtr hLayout = GetKeyboardLayout(layout);
            byte[] codes = HidCodeTranslator.GetInstance(layout).SupportedHidCodes;
            char[] output = new char[2];
            byte[] keyboardState = GetCleanKeyboardState();

            foreach (byte code in codes)
            {
                // If it's shifted, we need to adjust.
                if ((code & 0x80) != 0)
                {
                    keyboardState[0x10] |= 0x80;
                    keyboardState[0xA0] |= 0x80;
                }
                else
                {
                    keyboardState[0x10] = (byte)(keyboardState[0x10] & ~0x80);
                    keyboardState[0xA0] = (byte)(keyboardState[0x10] & ~0x80);
                }
                // The actual HID code doesn't use 0x80 for shift.
                uint scanCode = _scanCodeByHid[(byte)(code & ~0x80)];

                // We need the VKEY to map the keystroke.
                uint vkey = NativeMethods.MapVirtualKeyEx(scanCode, 1, hLayout);

                // I don't know why ToAsciiEx needs both the VKEY and the scan
                // code, but it does.
                int result = NativeMethods.ToAsciiEx(vkey, scanCode, keyboardState, output, 0, hLayout);
                if (result != 1)
                {
                    string error = result switch
                    {
                        -1 => $"ToAscii returned -1 converting scan code to a char, which means that it is a dead key (https://bit.ly/3tZOIi0).",
                        0 => $"ToAscii returned 0 converting scan code to a char, which means that there is no mapping for the current code.",
                        2 => $"ToAscii returned 2, which means that a dead key (https://bit.ly/3tZOIi0) had state in the keyboard state buffer. Should never happen here.",
                        _ => $"ToAscii returned {result}. This is not a documented return value for ToAscii."
                    };
                    string message = error + Environment.NewLine +
                        $"HID Usage Code[{code.ToString("x2")}], PS/2 Scan Code[{scanCode.ToString("x2")}], VKey[{vkey.ToString("x2")}]";
                    throw new InvalidOperationException(message);
                }
                // Windows returns \r for the enter key, so we'll just swap.
                output[0] = output[0] == '\r' ? '\n' : output[0];
                cases.Add((output[0], code));
            }

            return cases.ToArray();
        }

        // The Windows APIs use PS/2 scan codes instead of HID usage codes. This
        // maps the usage codes to scan codes so we can look up the VKEY codes
        // and get the characters for any given usage code.
        private static readonly Dictionary<byte, byte> _scanCodeByHid =
            new Dictionary<byte, byte>
            {
                [0x04] = 0x1e, [0x05] = 0x30, [0x06] = 0x2e, [0x07] = 0x20, [0x08] = 0x12,
                [0x09] = 0x21, [0x0a] = 0x22, [0x0b] = 0x23, [0x0c] = 0x17, [0x0d] = 0x24,
                [0x0e] = 0x25, [0x0f] = 0x26, [0x10] = 0x32, [0x11] = 0x31, [0x12] = 0x18,
                [0x13] = 0x19, [0x14] = 0x10, [0x15] = 0x13, [0x16] = 0x1f, [0x17] = 0x14,
                [0x18] = 0x16, [0x19] = 0x2f, [0x1a] = 0x11, [0x1b] = 0x2d, [0x1c] = 0x15,
                [0x1d] = 0x2c, [0x1e] = 0x02, [0x1f] = 0x03, [0x20] = 0x04, [0x21] = 0x05,
                [0x22] = 0x06, [0x23] = 0x07, [0x24] = 0x08, [0x25] = 0x09, [0x26] = 0x0a,
                [0x27] = 0x0b, [0x28] = 0x1c, [0x2b] = 0x0f, [0x2c] = 0x39, [0x2d] = 0x0c,
                [0x2e] = 0x0d, [0x2f] = 0x1a, [0x30] = 0x1b, [0x31] = 0x2b, [0x32] = 0x2b,
                [0x33] = 0x27, [0x34] = 0x28, [0x35] = 0x29, [0x36] = 0x33, [0x37] = 0x34,
                [0x38] = 0x35, [0x54] = 0xe0, [0x55] = 0x37, [0x56] = 0x4a, [0x57] = 0x4e,
                [0x58] = 0xe0, [0x59] = 0x4f, [0x5a] = 0x50, [0x5b] = 0x51, [0x5c] = 0x4b,
                [0x5d] = 0x4c, [0x5e] = 0x4d, [0x5f] = 0x47, [0x60] = 0x48, [0x61] = 0x49,
                [0x62] = 0x52, [0x63] = 0x53, [0x64] = 0x56, [0x67] = 0x59, [0x85] = 0x7e,
            };

        // The API for getting a character for a scan code uses keyboard state
        // to determine if there are any modifiers like dead keys and shifts.
        // This is what my keyboard state looks like when there are not pending
        // control or dead characters.
        // Info on dead keys: https://bit.ly/3tZOIi0
        // Here's the MSDN documentation:
        //
        // Each element (byte) in the array contains the state of one key. If the
        // high-order bit of a byte is set, the key is down (pressed).
        //
        // The low bit, if set, indicates that the key is toggled on.In this
        // function, only the toggle bit of the CAPS LOCK key is relevant.The
        // toggle state of the NUM LOCK and SCROLL LOCK keys is ignored.
        private static byte[] GetCleanKeyboardState()
        {
            byte[] keyboardState = new byte[256];
            keyboardState[0x08] = 0x01;
            keyboardState[0x0d] = 0x01;
            keyboardState[0x10] = 0x01;
            keyboardState[0x12] = 0x01;
            keyboardState[0x1b] = 0x01;
            keyboardState[0x90] = 0x01;
            keyboardState[0xa1] = 0x01;
            keyboardState[0xa4] = 0x01;
            keyboardState[0xf0] = 0x01;
            keyboardState[0xf3] = 0x01;
            keyboardState[0xf6] = 0x01;
            keyboardState[0xfb] = 0x01;
            return keyboardState;
        }

        private static IntPtr GetKeyboardLayout(KeyboardLayout layout) =>
            layout switch
            {
                KeyboardLayout.en_US => NativeMethods.LoadKeyboardLayout("00000409", 0),
                KeyboardLayout.en_UK => NativeMethods.LoadKeyboardLayout("00000809", 0),
                KeyboardLayout.de_DE => NativeMethods.LoadKeyboardLayout("00000407", 0),
                KeyboardLayout.fr_FR => NativeMethods.LoadKeyboardLayout("0000040c", 0),
                KeyboardLayout.it_IT => NativeMethods.LoadKeyboardLayout("00000410", 0),
                KeyboardLayout.es_US => NativeMethods.LoadKeyboardLayout("0000540a", 0),
                KeyboardLayout.sv_SE => NativeMethods.LoadKeyboardLayout("0000410d", 0),
                // We'll use the en_US layout for ModHex.
                KeyboardLayout.ModHex => NativeMethods.LoadKeyboardLayout("00000409", 0),
                _ => throw new NotSupportedException($"Layout [{layout}] not implemented."
                    + Environment.NewLine + "Did you implement a new layout without adding it here?")
            };

        private static class NativeMethods
        {
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

            [DllImport("user32.dll")]
            public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

            [DllImport("user32.dll")]
            public static extern int ToAsciiEx(
                uint uVirtKey,
                uint uScanCode,
                byte[] lpKeyState,
                [Out] char[] lpChar,
                uint uFlags,
                IntPtr hkl);
        }
    }
}
