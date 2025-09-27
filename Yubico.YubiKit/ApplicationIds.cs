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

namespace Yubico.YubiKit;

public record ApplicationIds
{
    public static readonly byte[] Management = { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x47, 0x11, 0x17 };
    public static readonly byte[] Otp = { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01, 0x01 };
    public static readonly byte[] FidoU2f = { 0xA0, 0x00, 0x00, 0x06, 0x47, 0x2F, 0x00, 0x01 };
    public static readonly byte[] Fido2 = { 0xA0, 0x00, 0x00, 0x06, 0x47, 0x2F, 0x00, 0x01 };
    public static readonly byte[] Oath = { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x01 };
    public static readonly byte[] OpenPgp = { 0xD2, 0x76, 0x00, 0x01, 0x24, 0x01 };
    public static readonly byte[] Piv = { 0xA0, 0x00, 0x00, 0x03, 0x08 };
    public static readonly byte[] YubiHsmAuth = { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x07, 0x01 };
    public static readonly byte[] SecurityDomain = { 0xA0, 0x00, 0x00, 0x01, 0x51, 0x00, 0x00, 0x00 };
}