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

using System.Collections.Generic;

namespace Yubico.Core.Devices.Hid
{
    // Keyboard Mapping for ModHex.
    public sealed partial class HidCodeTranslator
    {
        private static HidCodeTranslator GetModHex()
        {
            var byChar = new Dictionary<char, byte>
            {
                ['b'] = 0x05, ['B'] = 0x05 | _shift, ['c'] = 0x06, ['C'] = 0x06 | _shift,
                ['d'] = 0x07, ['D'] = 0x07 | _shift, ['e'] = 0x08, ['E'] = 0x08 | _shift,
                ['f'] = 0x09, ['F'] = 0x09 | _shift, ['g'] = 0x0a, ['G'] = 0x0a | _shift,
                ['h'] = 0x0b, ['H'] = 0x0b | _shift, ['i'] = 0x0c, ['I'] = 0x0c | _shift,
                ['j'] = 0x0d, ['J'] = 0x0d | _shift, ['k'] = 0x0e, ['K'] = 0x0e | _shift,
                ['l'] = 0x0f, ['L'] = 0x0f | _shift, ['n'] = 0x11, ['N'] = 0x11 | _shift,
                ['r'] = 0x15, ['R'] = 0x15 | _shift, ['t'] = 0x17, ['T'] = 0x17 | _shift,
                ['u'] = 0x18, ['U'] = 0x18 | _shift, ['v'] = 0x19, ['V'] = 0x19 | _shift,
                ['\n'] = 0x28, ['\t'] = 0x2b
            };

            var byCode = new Dictionary<byte, char>
            {
                [0x05] = 'b', [0x05 | _shift] = 'B', [0x06] = 'c', [0x06 | _shift] = 'C',
                [0x07] = 'd', [0x07 | _shift] = 'D', [0x08] = 'e', [0x08 | _shift] = 'E',
                [0x09] = 'f', [0x09 | _shift] = 'F', [0x0a] = 'g', [0x0a | _shift] = 'G',
                [0x0b] = 'h', [0x0b | _shift] = 'H', [0x0c] = 'i', [0x0c | _shift] = 'I',
                [0x0d] = 'j', [0x0d | _shift] = 'J', [0x0e] = 'k', [0x0e | _shift] = 'K',
                [0x0f] = 'l', [0x0f | _shift] = 'L', [0x11] = 'n', [0x11 | _shift] = 'N',
                [0x15] = 'r', [0x15 | _shift] = 'R', [0x17] = 't', [0x17 | _shift] = 'T',
                [0x18] = 'u', [0x18 | _shift] = 'U', [0x19] = 'v', [0x19 | _shift] = 'V',
                [0x28] = '\n', [0x2b] = '\t'
            };

            return new HidCodeTranslator(byChar, byCode, KeyboardLayout.ModHex);
        }
    }
}
