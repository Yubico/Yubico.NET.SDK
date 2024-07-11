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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class HidCodeTablePlugin : PluginBase
    {
        private static readonly byte[] _modHexBytes =
        {
            0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x11, 0x15, 0x17,
            0x18, 0x19, 0x28, 0x2b
        };

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

        private IEnumerable<string> _keyboards = Array.Empty<string>();

        private bool _printLayouts;

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

            Converters[typeof(IEnumerable<string>)] = s => ParseStringCollection(s);
        }

        public override string Name => "HidCodeTableGenerator";

        public override string Description =>
            "Generates lookup tables for Yubico.YubiKey.Otp.HidCodeTranslator. " +
            "Use this to add support for an additional keyboard layout.";

        private static IEnumerable<HidInfo> _modHexHidInfo => HidInfo.Codes
            .Where(c => _modHexBytes.Contains(c.HidCode));

        public override void HandleParameters()
        {
            // If none were specified, print them all.
            var keyboards =
                (IEnumerable<string>)(Parameters["keyboardids"].Value ?? Array.Empty<string>());
            _keyboards = keyboards.Any()
                ? keyboards
                : _keyboardLayouts.Keys;
            _printLayouts = (string)(Parameters["command"].Value ?? string.Empty) == "info";
        }

        private static string GetSourceFile([CallerFilePath] string path = "")
        {
            return path;
        }

        // I put this here instead of StaticConverters because we're using it
        // in a specific way. A generic implementation might want to account for
        // there being spaces in the strings, or some other thing. We're just
        // looking for anything that might be separating LCID ID strings.
        public static IEnumerable<string> ParseStringCollection(string s)
        {
            return s.Split(' ', ',', ':', ';', '/', '+')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.ToUpper());
        }

        public override bool Execute()
        {
            if (!_printLayouts)
            {
                return PrintTables();
            }

            Output.WriteLine("LCID IDs and Bit Values:");
            foreach (var (ID, LCID) in _keyboardLayouts.Select(kvp => (kvp.Key, kvp.Value.LCID)))
            {
                Output.WriteLine($"[{ID}] => [{LCID}]");
            }

            return true;
        }

        public bool PrintTables()
        {
            foreach (var name in _keyboards)
            {
                var layout = NativeMethods.LoadKeyboardLayout(_keyboardLayouts[name].LCID, Flags: 0);

                var codes = _keyboardLayouts[name].Codes ?? HidInfo.Codes;

                Output.WriteLine(
                    "// Copyright (c) Yubico AB" + Eol + Eol +
                    "using System.Collections.Generic;" + Eol +
                    "using Yubico.Core.Devices.Hid;" + Eol + Eol +
                    "namespace Yubico.YubiKey.Otp" + Eol + "{" + Eol +
                    $"    // Keyboard Mapping for {name}." + Eol +
                    "    public sealed partial class HidCodeTranslator" + Eol +
                    "    {" + Eol +
                    $"        private static HidCodeTranslator Get{(name == "ModHex" ? name : name.ToUpper())}()" +
                    Eol +
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
                    var linePosition = maxwidth + 1;
                    foreach (var code in codes)
                    {
                        var results = GetChar(layout, code.PS2MakeCode);
                        foreach (var (ch, shifted) in results)
                        {
                            var codeStr = shifted
                                ? code.HidCode.ToString("x2") + " | _shift"
                                : code.HidCode.ToString("x2");
                            var item = string.Format(template, ch, codeStr);
                            var prefix = " ";
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

        private static byte[] GetCleanKeyboardState()
        {
            var keyboardState = new byte[256];
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
            var vkey = NativeMethods.MapVirtualKeyEx(scanCode, uMapType: 1, layout);
            var keyboardState = GetCleanKeyboardState();
            var output = new char[4];

            var results = new List<(string ch, bool shifted)>();
            foreach (var shifted in new[] { false, true })
            {
                if (shifted)
                {
                    keyboardState[0x10] |= _shift;
                    keyboardState[NativeMethods.VK_LSHIFT] |= _shift;
                }

                var result = NativeMethods.ToAsciiEx(vkey, scanCode, keyboardState, output, uFlags: 0, layout);
                if (result == 1)
                {
                    var st = output[0] switch
                    {
                        '\'' => @"\'",
                        '\t' => @"\t",
                        '\\' => @"\\",
                        '\r' => @"\n", // Windows uses CR instead of LF for [Enter].
                        _ => output[0].ToString()
                    };
                    if (results.Count == 0 || results[index: 0].ch != st)
                    {
                        results.Add((st, shifted));
                    }
                }

                if (result != 1)
                {
                    Debug.WriteLine(
                        $"Got [{result}] from [0x{scanCode.ToString("x2")}] Shift [{shifted}]");
                }
            }

            return results.ToArray();
        }

        private static class NativeMethods
        {
            public const int VK_LSHIFT = 0xA0;

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
        }

        public class HidInfo
        {
            public static IEnumerable<HidInfo> Codes = new[]
            {
                new HidInfo(@"a A", hidCode: 0x04, ps2Make: 0x1e, ps2Break: 0x9e),
                new HidInfo(@"b B", hidCode: 0x05, ps2Make: 0x30, ps2Break: 0xb0),
                new HidInfo(@"c C", hidCode: 0x06, ps2Make: 0x2e, ps2Break: 0xae),
                new HidInfo(@"d D", hidCode: 0x07, ps2Make: 0x20, ps2Break: 0xa0),
                new HidInfo(@"e E", hidCode: 0x08, ps2Make: 0x12, ps2Break: 0x92),
                new HidInfo(@"f F", hidCode: 0x09, ps2Make: 0x21, ps2Break: 0xa1),
                new HidInfo(@"g G", hidCode: 0x0a, ps2Make: 0x22, ps2Break: 0xa2),
                new HidInfo(@"h H", hidCode: 0x0b, ps2Make: 0x23, ps2Break: 0xa3),
                new HidInfo(@"i I", hidCode: 0x0c, ps2Make: 0x17, ps2Break: 0x97),
                new HidInfo(@"j J", hidCode: 0x0d, ps2Make: 0x24, ps2Break: 0xa4),
                new HidInfo(@"k K", hidCode: 0x0e, ps2Make: 0x25, ps2Break: 0xa5),
                new HidInfo(@"l L", hidCode: 0x0f, ps2Make: 0x26, ps2Break: 0xa6),
                new HidInfo(@"m M", hidCode: 0x10, ps2Make: 0x32, ps2Break: 0xb2),
                new HidInfo(@"n N", hidCode: 0x11, ps2Make: 0x31, ps2Break: 0xb1),
                new HidInfo(@"o O", hidCode: 0x12, ps2Make: 0x18, ps2Break: 0x98),
                new HidInfo(@"p P", hidCode: 0x13, ps2Make: 0x19, ps2Break: 0x99),
                new HidInfo(@"q Q", hidCode: 0x14, ps2Make: 0x10, ps2Break: 0x90),
                new HidInfo(@"r R", hidCode: 0x15, ps2Make: 0x13, ps2Break: 0x93),
                new HidInfo(@"s S", hidCode: 0x16, ps2Make: 0x1f, ps2Break: 0x9f),
                new HidInfo(@"t T", hidCode: 0x17, ps2Make: 0x14, ps2Break: 0x94),
                new HidInfo(@"u U", hidCode: 0x18, ps2Make: 0x16, ps2Break: 0x96),
                new HidInfo(@"v V", hidCode: 0x19, ps2Make: 0x2f, ps2Break: 0xaf),
                new HidInfo(@"w W", hidCode: 0x1a, ps2Make: 0x11, ps2Break: 0x91),
                new HidInfo(@"x X", hidCode: 0x1b, ps2Make: 0x2d, ps2Break: 0xad),
                new HidInfo(@"y Y", hidCode: 0x1c, ps2Make: 0x15, ps2Break: 0x95),
                new HidInfo(@"z Z", hidCode: 0x1d, ps2Make: 0x2c, ps2Break: 0xac),
                new HidInfo(@"1 !", hidCode: 0x1e, ps2Make: 0x02, ps2Break: 0x82),
                new HidInfo(@"2 @", hidCode: 0x1f, ps2Make: 0x03, ps2Break: 0x83),
                new HidInfo(@"3 #", hidCode: 0x20, ps2Make: 0x04, ps2Break: 0x84),
                new HidInfo(@"4 $", hidCode: 0x21, ps2Make: 0x05, ps2Break: 0x85),
                new HidInfo(@"5 %", hidCode: 0x22, ps2Make: 0x06, ps2Break: 0x86),
                new HidInfo(@"6 ^", hidCode: 0x23, ps2Make: 0x07, ps2Break: 0x87),
                new HidInfo(@"7 &", hidCode: 0x24, ps2Make: 0x08, ps2Break: 0x88),
                new HidInfo(@"8 *", hidCode: 0x25, ps2Make: 0x09, ps2Break: 0x89),
                new HidInfo(@"9 (", hidCode: 0x26, ps2Make: 0x0a, ps2Break: 0x8a),
                new HidInfo(@"0 )", hidCode: 0x27, ps2Make: 0x0b, ps2Break: 0x8b),
                new HidInfo(@"Return", hidCode: 0x28, ps2Make: 0x1c, ps2Break: 0x9c),
                new HidInfo(@"Tab", hidCode: 0x2b, ps2Make: 0x0f, ps2Break: 0x8f),
                new HidInfo(@"Space", hidCode: 0x2c, ps2Make: 0x39, ps2Break: 0xb9),
                new HidInfo(@"- _", hidCode: 0x2d, ps2Make: 0x0c, ps2Break: 0x8c),
                new HidInfo(@"= +", hidCode: 0x2e, ps2Make: 0x0d, ps2Break: 0x8d),
                new HidInfo(@"[ {", hidCode: 0x2f, ps2Make: 0x1a, ps2Break: 0x9a),
                new HidInfo(@"] }", hidCode: 0x30, ps2Make: 0x1b, ps2Break: 0x9b),
                new HidInfo(@"\ |", hidCode: 0x31, ps2Make: 0x2b, ps2Break: 0xab),
                new HidInfo(@"Europe 1", hidCode: 0x32, ps2Make: 0x2b, ps2Break: 0xab),
                new HidInfo(@"; :", hidCode: 0x33, ps2Make: 0x27, ps2Break: 0xa7),
                new HidInfo(@"' """, hidCode: 0x34, ps2Make: 0x28, ps2Break: 0xa8),
                new HidInfo(@"` ~", hidCode: 0x35, ps2Make: 0x29, ps2Break: 0xa9),
                new HidInfo(@", <", hidCode: 0x36, ps2Make: 0x33, ps2Break: 0xb3),
                new HidInfo(@". >", hidCode: 0x37, ps2Make: 0x34, ps2Break: 0xb4),
                new HidInfo(@"/ ?", hidCode: 0x38, ps2Make: 0x35, ps2Break: 0xb5),
                new HidInfo(@"Europe 2", hidCode: 0x64, ps2Make: 0x56, ps2Break: 0xd6)
            };

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
        }

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

        #region Constants

        private const byte _shift = 0x80;
        private const int spacecount = 13;
        private const int maxwidth = 90;
        private const int indent = 4;
        private static readonly string lineStart = new string(c: ' ', spacecount);
        private static readonly string itemLineStart = Eol + new string(c: ' ', spacecount + indent);

        #endregion
    }
}
