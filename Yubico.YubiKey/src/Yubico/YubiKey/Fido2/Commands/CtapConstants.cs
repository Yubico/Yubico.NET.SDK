// Copyright 2022 Yubico AB
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

namespace Yubico.YubiKey.Fido2.Commands
{
    internal static class CtapConstants
    {
        public const byte CtapHidMsg = 0x03;
        public const byte CtapHidCbor = 0x10;
        public const byte CtapHidInit = 0x06;
        public const byte CtapHidPing = 0x01;
        public const byte CtapHidCancel = 0x11;
        public const byte CtapHidError = 0x3F;
        public const byte CtapHidKeepAlive = 0x3B;
    }
}
