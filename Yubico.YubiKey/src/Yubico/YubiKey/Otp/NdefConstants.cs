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

namespace Yubico.YubiKey.Otp;

internal static class NdefConstants
{
    public static string[] supportedUriPrefixes = new[]
    {
        "",
        "http://www.",
        "https://www.",
        "http://",
        "https://",

        // 5
        "tel:",
        "mailto:",
        "ftp://anonymous:anonymous@",
        "ftp://ftp.",
        "ftps://",

        // 10
        "sftp://",
        "smb://",
        "nfs://",
        "ftp://",
        "dav://",

        // 15
        "news:",
        "telnet://",
        "imap:",
        "rtsp://",
        "urn:",

        // 20
        "pop:",
        "sip:",
        "sips:",
        "tftp:",
        "btspp://",

        // 25
        "btl2cap://",
        "btgoep://",
        "tcpobex://",
        "irdaobex://",
        "file://",

        // 30
        "urn:epc:id:",
        "urn:epc:tag:",
        "urn:epc:pat:",
        "urn:epc:raw:",
        "urn:epc:",

        // 35
        "urn:nfc:"
    };
}
