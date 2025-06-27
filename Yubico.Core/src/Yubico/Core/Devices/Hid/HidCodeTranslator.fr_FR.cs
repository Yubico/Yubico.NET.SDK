// Copyright 2025 Yubico AB
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

using System.Collections.Generic;

namespace Yubico.Core.Devices.Hid
{
    // Keyboard Mapping for fr_FR.
    public sealed partial class HidCodeTranslator
    {
        private static HidCodeTranslator GetFR_FR()
        {
            var byChar = new Dictionary<char, byte>
            {
                ['q'] = 0x04, ['Q'] = 0x04 | _shift, ['b'] = 0x05, ['B'] = 0x05 | _shift,
                ['c'] = 0x06, ['C'] = 0x06 | _shift, ['d'] = 0x07, ['D'] = 0x07 | _shift,
                ['e'] = 0x08, ['E'] = 0x08 | _shift, ['f'] = 0x09, ['F'] = 0x09 | _shift,
                ['g'] = 0x0a, ['G'] = 0x0a | _shift, ['h'] = 0x0b, ['H'] = 0x0b | _shift,
                ['i'] = 0x0c, ['I'] = 0x0c | _shift, ['j'] = 0x0d, ['J'] = 0x0d | _shift,
                ['k'] = 0x0e, ['K'] = 0x0e | _shift, ['l'] = 0x0f, ['L'] = 0x0f | _shift,
                [','] = 0x10, ['?'] = 0x10 | _shift, ['n'] = 0x11, ['N'] = 0x11 | _shift,
                ['o'] = 0x12, ['O'] = 0x12 | _shift, ['p'] = 0x13, ['P'] = 0x13 | _shift,
                ['a'] = 0x14, ['A'] = 0x14 | _shift, ['r'] = 0x15, ['R'] = 0x15 | _shift,
                ['s'] = 0x16, ['S'] = 0x16 | _shift, ['t'] = 0x17, ['T'] = 0x17 | _shift,
                ['u'] = 0x18, ['U'] = 0x18 | _shift, ['v'] = 0x19, ['V'] = 0x19 | _shift,
                ['z'] = 0x1a, ['Z'] = 0x1a | _shift, ['x'] = 0x1b, ['X'] = 0x1b | _shift,
                ['y'] = 0x1c, ['Y'] = 0x1c | _shift, ['w'] = 0x1d, ['W'] = 0x1d | _shift,
                ['&'] = 0x1e, ['1'] = 0x1e | _shift, ['é'] = 0x1f, ['2'] = 0x1f | _shift,
                ['"'] = 0x20, ['3'] = 0x20 | _shift, ['\''] = 0x21, ['4'] = 0x21 | _shift,
                ['('] = 0x22, ['5'] = 0x22 | _shift, ['-'] = 0x23, ['6'] = 0x23 | _shift,
                ['è'] = 0x24, ['7'] = 0x24 | _shift, ['_'] = 0x25, ['8'] = 0x25 | _shift,
                ['ç'] = 0x26, ['9'] = 0x26 | _shift, ['à'] = 0x27, ['0'] = 0x27 | _shift,
                ['\n'] = 0x28, ['\t'] = 0x2b, [' '] = 0x2c, [')'] = 0x2d,
                ['°'] = 0x2d | _shift, ['='] = 0x2e, ['+'] = 0x2e | _shift, ['$'] = 0x30,
                ['£'] = 0x30 | _shift, ['*'] = 0x31, ['µ'] = 0x31 | _shift, ['*'] = 0x32,
                ['µ'] = 0x32 | _shift, ['m'] = 0x33, ['M'] = 0x33 | _shift, ['ù'] = 0x34,
                ['%'] = 0x34 | _shift, ['²'] = 0x35, [';'] = 0x36, ['.'] = 0x36 | _shift,
                [':'] = 0x37, ['/'] = 0x37 | _shift, ['!'] = 0x38, ['§'] = 0x38 | _shift,
                ['<'] = 0x64, ['>'] = 0x64 | _shift,
            };
            var byCode = new Dictionary<byte, char>
            {
                [0x04] = 'q', [0x04 | _shift] = 'Q', [0x05] = 'b', [0x05 | _shift] = 'B',
                [0x06] = 'c', [0x06 | _shift] = 'C', [0x07] = 'd', [0x07 | _shift] = 'D',
                [0x08] = 'e', [0x08 | _shift] = 'E', [0x09] = 'f', [0x09 | _shift] = 'F',
                [0x0a] = 'g', [0x0a | _shift] = 'G', [0x0b] = 'h', [0x0b | _shift] = 'H',
                [0x0c] = 'i', [0x0c | _shift] = 'I', [0x0d] = 'j', [0x0d | _shift] = 'J',
                [0x0e] = 'k', [0x0e | _shift] = 'K', [0x0f] = 'l', [0x0f | _shift] = 'L',
                [0x10] = ',', [0x10 | _shift] = '?', [0x11] = 'n', [0x11 | _shift] = 'N',
                [0x12] = 'o', [0x12 | _shift] = 'O', [0x13] = 'p', [0x13 | _shift] = 'P',
                [0x14] = 'a', [0x14 | _shift] = 'A', [0x15] = 'r', [0x15 | _shift] = 'R',
                [0x16] = 's', [0x16 | _shift] = 'S', [0x17] = 't', [0x17 | _shift] = 'T',
                [0x18] = 'u', [0x18 | _shift] = 'U', [0x19] = 'v', [0x19 | _shift] = 'V',
                [0x1a] = 'z', [0x1a | _shift] = 'Z', [0x1b] = 'x', [0x1b | _shift] = 'X',
                [0x1c] = 'y', [0x1c | _shift] = 'Y', [0x1d] = 'w', [0x1d | _shift] = 'W',
                [0x1e] = '&', [0x1e | _shift] = '1', [0x1f] = 'é', [0x1f | _shift] = '2',
                [0x20] = '"', [0x20 | _shift] = '3', [0x21] = '\'', [0x21 | _shift] = '4',
                [0x22] = '(', [0x22 | _shift] = '5', [0x23] = '-', [0x23 | _shift] = '6',
                [0x24] = 'è', [0x24 | _shift] = '7', [0x25] = '_', [0x25 | _shift] = '8',
                [0x26] = 'ç', [0x26 | _shift] = '9', [0x27] = 'à', [0x27 | _shift] = '0',
                [0x28] = '\n', [0x2b] = '\t', [0x2c] = ' ', [0x2d] = ')',
                [0x2d | _shift] = '°', [0x2e] = '=', [0x2e | _shift] = '+', [0x30] = '$',
                [0x30 | _shift] = '£', [0x31] = '*', [0x31 | _shift] = 'µ', [0x32] = '*',
                [0x32 | _shift] = 'µ', [0x33] = 'm', [0x33 | _shift] = 'M', [0x34] = 'ù',
                [0x34 | _shift] = '%', [0x35] = '²', [0x36] = ';', [0x36 | _shift] = '.',
                [0x37] = ':', [0x37 | _shift] = '/', [0x38] = '!', [0x38 | _shift] = '§',
                [0x64] = '<', [0x64 | _shift] = '>',
            };

            return new HidCodeTranslator(byChar, byCode, KeyboardLayout.fr_FR);
        }
    }
}
