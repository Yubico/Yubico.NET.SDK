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

namespace Yubico.YubiKey
{
    public enum YubiKeyApplication
    {
        Unknown = 0,
        Management = 1,
        Otp = 2,
        FidoU2f = 3,
        Fido2 = 4,
        Oath = 5,
        OpenPgp = 6,
        Piv = 7,
        InterIndustry = 8,
        OtpNdef = 9,
        YubiHsmAuth = 10,
        Scp03 = 11,
    }

    internal static class YubiKeyApplicationExtensions
    {
        private static readonly byte[] ManagementAppId = new byte[] { 0xa0, 0x00, 0x00, 0x05, 0x27, 0x47, 0x11, 0x17 };
        private static readonly byte[] OtpAppId = new byte[]        { 0xa0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01, 0x01 };
        private static readonly byte[] FidoU2fAppId = new byte[]    { 0xa0, 0x00, 0x00, 0x06, 0x47, 0x2f, 0x00, 0x01 };
        private static readonly byte[] Fido2AppId = new byte[]      { 0xa0, 0x00, 0x00, 0x06, 0x47, 0x2f, 0x00, 0x01 };
        private static readonly byte[] OathAppId = new byte[]       { 0xa0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x01 };
        private static readonly byte[] OpenPgpAppId = new byte[]    { 0xd2, 0x76, 0x00, 0x01, 0x24, 0x01 };
        private static readonly byte[] PivAppId = new byte[]        { 0xa0, 0x00, 0x00, 0x03, 0x08 };
        private static readonly byte[] OtpNdef = new byte[]         { 0xd2, 0x76, 0x00, 0x00, 0x85, 0x01, 0x01 };
        private static readonly byte[] YubiHsmAuthId = new byte[]   { 0xa0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x07, 0x01 };
        private static readonly byte[] Scp03AuthId = new byte[]     { 0xA0, 0x00, 0x00, 0x01, 0x51, 0x00, 0x00, 0x00 };

        public static byte[] GetIso7816ApplicationId(this YubiKeyApplication application) =>
            application switch
            {
                YubiKeyApplication.Management => ManagementAppId,
                YubiKeyApplication.Otp => OtpAppId,
                YubiKeyApplication.FidoU2f => FidoU2fAppId,
                YubiKeyApplication.Fido2 => Fido2AppId,
                YubiKeyApplication.Oath => OathAppId,
                YubiKeyApplication.OpenPgp => OpenPgpAppId,
                YubiKeyApplication.Piv => PivAppId,
                YubiKeyApplication.OtpNdef => OtpNdef,
                YubiKeyApplication.YubiHsmAuth => YubiHsmAuthId,
                YubiKeyApplication.Scp03 => Scp03AuthId,
                _ => throw new NotSupportedException(ExceptionMessages.ApplicationIdNotFound),
            };
    }
}
