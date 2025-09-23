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

using Yubico.YubiKit.Core.Iso7816;

namespace Yubico.YubiKit.Core;

public static class ProductAtrs
{
    // YubiKey NEO
    public static readonly AnswerToReset YubiKeyNeoUsb =
        new(
            new byte[]
            {
                0x3B, 0xFC, 0x13, 0x00, 0x00, 0x81, 0x31, 0xFE, 0x15, 0x59, 0x75, 0x62, 0x69, 0x6B, 0x65, 0x79,
                0x4E, 0x45, 0x4F, 0x72, 0x33, 0xE1
            }
        );

    public static readonly AnswerToReset YubiKeyNeoNfc =
        new(
            new byte[]
            {
                0x3B, 0x8C, 0x80, 0x01, 0x59, 0x75, 0x62, 0x69, 0x6B, 0x65, 0x79, 0x4E, 0x45, 0x4F, 0x72, 0x33, 0x58
            }
        );

    // YubiKey 4 Series
    public static readonly AnswerToReset YubiKey4Usb =
        new(
            new byte[]
            {
                0x3B, 0xF8, 0x13, 0x00, 0x00, 0x81, 0x31, 0xFE, 0x15, 0x59, 0x75, 0x62, 0x69, 0x6B, 0x65, 0x79,
                0x34, 0xD4
            }
        );

    // YubiKey 5 Series
    public static readonly AnswerToReset YubiKey5Usb =
        new(
            new byte[]
            {
                0x3B, 0xFD, 0x13, 0x00, 0x00, 0x81, 0x31, 0xFE, 0x15, 0x80, 0x73, 0xC0, 0x21, 0xC0, 0x57, 0x59,
                0x75, 0x62, 0x69, 0x4B, 0x65, 0x79, 0x40
            }
        );

    public static readonly AnswerToReset YubiKey5Nfc =
        new(
            new byte[]
            {
                0x3B, 0x8D, 0x80, 0x01, 0x80, 0x73, 0xC0, 0x21, 0xC0, 0x57, 0x59, 0x75, 0x62, 0x69, 0x4B, 0x65,
                0x79, 0xF9
            }
        );

    public static IList<AnswerToReset> UsbYubiKeys => new List<AnswerToReset>
    {
        YubiKeyNeoUsb, YubiKey4Usb, YubiKey5Usb
    };

    public static IList<AnswerToReset> NfcYubiKeys => new List<AnswerToReset> { YubiKeyNeoNfc, YubiKey5Nfc };

    public static IList<AnswerToReset> AllYubiKeys => new List<AnswerToReset>
    {
        YubiKeyNeoNfc,
        YubiKeyNeoUsb,
        YubiKey4Usb,
        YubiKey5Nfc,
        YubiKey5Usb
    };
}