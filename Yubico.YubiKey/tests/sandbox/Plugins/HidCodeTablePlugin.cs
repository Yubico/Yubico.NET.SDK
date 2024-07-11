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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class HidCodeTablePlugin : PluginBase
    {
        public override string Name => "HidCodeTableGenerator";

        public override string Description =>
            "Generates lookup tables for Yubico.YubiKey.Otp.HidCodeTranslator. " +
            "Use this to add support for an additional keyboard layout.";

        // If you add a new keyboard, first look this up and add it here.
        // Here's a reference to find it: https://bit.ly/3u55gFt.
        #region LCID Codes
        public static string en_US = "00000409";
        public static string en_UK = "00000809";
        public static string de_DE = "00000407";
        public static string fr_FR = "0000040c";
        public static string it_IT = "00000410";
        public static string es_US = "0000540a";
        public static string sv_SE = "0000410d";
        #endregion

        // This is where the keyboard layout actually gets loaded from.
        // Important: Even though the identifiers are presented with differing
        // cases, we're going to use all upper-case for the key and make the
        // matching not case-sensitive to avoid confusion.
        #region Keyboard Layouts
        private readonly Dictionary<string, (string LCID, IEnumerable<HidInfo>? Codes)> _keyboardLayouts =
            new Dictionary<string, (string LCID, IEnumerable<HidInfo>? Codes)>
            {
                ["EN_US"] = (en_US, null),
                ["EN_UK"] = (en_UK, null),
                ["DE_DE"] = (de_DE, null),
                ["FR_FR"] = (fr_FR, null),
                ["IT_IT"] = (it_IT, null),
                ["ES_US"] = (es_US, null),
                ["SV_SE"] = (sv_SE, null),
                ["MODHEX"] = (en_US, _modHexHidInfo)
            };
        #endregion

        public HidCodeTablePlugin(IOutput output) : base(output)
        {
            Parameters["keyboardids"] = new Parameter
            {
                Name = "KeyboardIds",
                Shortcut = "k",
                Type = typeof(IEnumerable<string>),
                Description =
                        "A list (or single one of) keyboard IDs to print lookup tables for. " +
                        $"If you're adding a new layout, you'll need to edit {Path.GetFileName(GetSourceFile())}. " +
                        "If nothing is specified, all current supported layouts are printed."
            };
            Parameters["command"].Description =
                "Possible values are 'info' and 'print'. The 'info' command prints a list of LCID IDs and bit values. " +
                "The 'print' command prints the lookup tables (generated C# code).";

            Converters[typeof(IEnumerable<string>)] = (s) => ParseStringCollection(s);
        }

        private bool _printLayouts;
        public override void HandleParameters()
        {
            // If none were specified, print them all.
            IEnumerable<string> keyboards = (IEnumerable<string>)(Parameters["keyboardids"].Value ?? Array.Empty<string>());
            _keyboards = keyboards.Any()
                ? keyboards
                : _keyboardLayouts.Keys;
            _printLayouts = (string)(Parameters["command"].Value ?? string.Empty) == "info";
        }

        private static string GetSourceFile([CallerFilePath] string path = "") => path;

        private IEnumerable<string> _keyboards = Array.Empty<string>();

        // I put this here instead of StaticConverters because we're using it
        // in a specific way. A generic implementation might want to account for
        // there being spaces in the strings, or some other thing. We're just
        // looking for anything that might be separating LCID ID strings.
        public static IEnumerable<string> ParseStringCollection(string s)
            =>
            s.Split(
                new[] { ' ', ',', ':', ';', '/', '+' })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.ToUpper());

        public override bool Execute()
        {
            if (!_printLayouts)
            {
                return PrintTables();
            }

            Output.WriteLine("LCID IDs and Bit Values:");
            foreach ((string ID, string LCID) in _keyboardLayouts.Select(kvp => (kvp.Key, kvp.Value.LCID)))
            {
                Output.WriteLine($"[{ID}] => [{LCID}]");
            }

            return true;
        }

        public bool PrintTables()
        {
            foreach (string name in _keyboards)
            {
                IntPtr layout = NativeMethods.LoadKeyboardLayout(_keyboardLayouts[name].LCID, 0);

                IEnumerable<HidInfo> codes = _keyboardLayouts[name].Codes ?? HidInfo.Codes;

                Output.WriteLine(
                    "// Copyright (c) Yubico AB" + Eol + Eol +
                    "using System.Collections.Generic;" + Eol +
                    "using Yubico.Core.Devices.Hid;" + Eol + Eol +
                    "namespace Yubico.YubiKey.Otp" + Eol + "{" + Eol +
                    $"    // Keyboard Mapping for {name}." + Eol +
                    "    public sealed partial class HidCodeTranslator" + Eol +
                    "    {" + Eol +
                    $"        private static HidCodeTranslator Get{(name == "ModHex" ? name : name.ToUpper())}()" + Eol +
                    "        {" + Eol +
                    "        var byChar = new Dictionary<char, byte>");
                PrintItems("['{0}'] = 0x{1},");

                Output.WriteLine(lineStart
                    + "var byCode = new Dictionary<byte, char>");
                PrintItems("[0x{1}] = '{0}',");

                Output.WriteLine(Eol + lineStart +
                    $"return new HidCodeTranslator(byChar, byCode, KeyboardLayout.{name});" + Eol +
                    "        }" + Eol +
                    "    }" + Eol + "}" + Eol);

                void PrintItems(string template)
                {
                    Output.Write(lineStart + "{");
                    int linePosition = maxwidth + 1;
                    foreach (HidInfo? code in codes)
                    {
                        (string ch, bool shifted)[] results = GetChar(layout, code.PS2MakeCode);
                        foreach ((string ch, bool shifted) in results)
                        {
                            string codeStr = shifted
                                ? code.HidCode.ToString("x2") + " | _shift"
                                : code.HidCode.ToString("x2");
                            string item = string.Format(template, ch, codeStr);
                            string prefix = " ";
                            linePosition += item.Length;

                            if (linePosition > maxwidth)
                            {
                                prefix = itemLineStart;
                                linePosition = item.Length + prefix.Length;
                            }
                            Output.Write($"{prefix}{item}");
                        }
                    }
                    Output.WriteLine(Eol + lineStart + "};");
                }
            }
            return true;
        }

        static IEnumerable<HidInfo> _modHexHidInfo => HidInfo.Codes
            .Where(c => _modHexBytes.Contains(c.HidCode));

        private static readonly byte[] _modHexBytes =
        {
            0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x11, 0x15, 0x17,
            0x18, 0x19, 0x28, 0x2b,
        };

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

        private static (string ch, bool shifted)[] GetChar(IntPtr layout, uint scanCode)
        {
            uint vkey = NativeMethods.MapVirtualKeyEx(scanCode, 1, layout);
            byte[] keyboardState = GetCleanKeyboardState();
            char[] output = new char[4];

            var results = new List<(string ch, bool shifted)>();
            foreach (bool shifted in new[] { false, true })
            {
                if (shifted)
                {
                    keyboardState[0x10] |= _shift;
                    keyboardState[NativeMethods.VK_LSHIFT] |= _shift;
                }
                int result = NativeMethods.ToAsciiEx(vkey, scanCode, keyboardState, output, 0, layout);
                if (result == 1)
                {
                    string st = output[0] switch
                    {
                        '\'' => @"\'",
                        '\t' => @"\t",
                        '\\' => @"\\",
                        '\r' => @"\n", // Windows uses CR instead of LF for [Enter].
                        _ => output[0].ToString(),
                    };
                    if (results.Count == 0 || results[0].ch != st)
                    {
                        results.Add((st, shifted));
                    }
                }
                if (result != 1)
                {
                    System.Diagnostics.Debug.WriteLine($"Got [{result}] from [0x{scanCode.ToString("x2")}] Shift [{shifted}]");
                }
            }
            return results.ToArray();
        }

        #region Constants
        const byte _shift = 0x80;
        const int spacecount = 13;
        const int maxwidth = 90;
        const int indent = 4;
        static readonly string lineStart = new string(' ', spacecount);
        static readonly string itemLineStart = Eol + new string(' ', spacecount + indent);
        #endregion

        private static class NativeMethods
        {
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

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

            public const int VK_LSHIFT = 0xA0;
        }

        public class HidInfo
        {
            public HidInfo(string name, byte hidCode, byte ps2Make, byte ps2Break)
            {
                Name = name;
                HidCode = hidCode;
                PS2MakeCode = ps2Make;
                PS2BreakCode = ps2Break;
            }
            public string Name { get; set; }
            public byte HidCode { get; }
            public byte PS2MakeCode { get; }
            public byte PS2BreakCode { get; }

            public static IEnumerable<HidInfo> Codes = new HidInfo[]
            {
            new HidInfo(@"a A", 0x04, 0x1e, 0x9e),
            new HidInfo(@"b B", 0x05, 0x30, 0xb0),
            new HidInfo(@"c C", 0x06, 0x2e, 0xae),
            new HidInfo(@"d D", 0x07, 0x20, 0xa0),
            new HidInfo(@"e E", 0x08, 0x12, 0x92),
            new HidInfo(@"f F", 0x09, 0x21, 0xa1),
            new HidInfo(@"g G", 0x0a, 0x22, 0xa2),
            new HidInfo(@"h H", 0x0b, 0x23, 0xa3),
            new HidInfo(@"i I", 0x0c, 0x17, 0x97),
            new HidInfo(@"j J", 0x0d, 0x24, 0xa4),
            new HidInfo(@"k K", 0x0e, 0x25, 0xa5),
            new HidInfo(@"l L", 0x0f, 0x26, 0xa6),
            new HidInfo(@"m M", 0x10, 0x32, 0xb2),
            new HidInfo(@"n N", 0x11, 0x31, 0xb1),
            new HidInfo(@"o O", 0x12, 0x18, 0x98),
            new HidInfo(@"p P", 0x13, 0x19, 0x99),
            new HidInfo(@"q Q", 0x14, 0x10, 0x90),
            new HidInfo(@"r R", 0x15, 0x13, 0x93),
            new HidInfo(@"s S", 0x16, 0x1f, 0x9f),
            new HidInfo(@"t T", 0x17, 0x14, 0x94),
            new HidInfo(@"u U", 0x18, 0x16, 0x96),
            new HidInfo(@"v V", 0x19, 0x2f, 0xaf),
            new HidInfo(@"w W", 0x1a, 0x11, 0x91),
            new HidInfo(@"x X", 0x1b, 0x2d, 0xad),
            new HidInfo(@"y Y", 0x1c, 0x15, 0x95),
            new HidInfo(@"z Z", 0x1d, 0x2c, 0xac),
            new HidInfo(@"1 !", 0x1e, 0x02, 0x82),
            new HidInfo(@"2 @", 0x1f, 0x03, 0x83),
            new HidInfo(@"3 #", 0x20, 0x04, 0x84),
            new HidInfo(@"4 $", 0x21, 0x05, 0x85),
            new HidInfo(@"5 %", 0x22, 0x06, 0x86),
            new HidInfo(@"6 ^", 0x23, 0x07, 0x87),
            new HidInfo(@"7 &", 0x24, 0x08, 0x88),
            new HidInfo(@"8 *", 0x25, 0x09, 0x89),
            new HidInfo(@"9 (", 0x26, 0x0a, 0x8a),
            new HidInfo(@"0 )", 0x27, 0x0b, 0x8b),
            new HidInfo(@"Return", 0x28, 0x1c, 0x9c),
            new HidInfo(@"Tab", 0x2b, 0x0f, 0x8f),
            new HidInfo(@"Space", 0x2c, 0x39, 0xb9),
            new HidInfo(@"- _", 0x2d, 0x0c, 0x8c),
            new HidInfo(@"= +", 0x2e, 0x0d, 0x8d),
            new HidInfo(@"[ {", 0x2f, 0x1a, 0x9a),
            new HidInfo(@"] }", 0x30, 0x1b, 0x9b),
            new HidInfo(@"\ |", 0x31, 0x2b, 0xab),
            new HidInfo(@"Europe 1", 0x32, 0x2b, 0xab),
            new HidInfo(@"; :", 0x33, 0x27, 0xa7),
            new HidInfo(@"' """, 0x34, 0x28, 0xa8),
            new HidInfo(@"` ~", 0x35, 0x29, 0xa9),
            new HidInfo(@", <", 0x36, 0x33, 0xb3),
            new HidInfo(@". >", 0x37, 0x34, 0xb4),
            new HidInfo(@"/ ?", 0x38, 0x35, 0xb5),
            new HidInfo(@"Europe 2", 0x64, 0x56, 0xd6)
            };
        }
    }
}
