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

using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.PlatformInterop
{
    internal class SCard156456
    {

        /*
        public uint Status(
            SCardCardHandle card,
            out string[] readerNames,
            out SCARD_STATUS status,
            out SCARD_PROTOCOL protocol,
            out byte[]? atr
            )
        {
            int readerNameLength = 0;
            int atrLength = 0;

            // Get the lengths for the reader names and ATR first
            uint result = SCardStatus(
                card,
                null,
                ref readerNameLength,
                out _,
                out _,
                null,
                ref atrLength);

            if (result == ErrorCode.SCARD_S_SUCCESS)
            {
                byte[] rawReaderNames = new byte[readerNameLength];
                byte[] atrBuffer = new byte[atrLength];

                // Now we get the actual values
                result = Status(
                    card,
                    rawReaderNames,
                    ref readerNameLength,
                    out status,
                    out protocol,
                    atrBuffer,
                    ref atrLength);

                if (result == ErrorCode.SCARD_S_SUCCESS)
                {
                    readerNames = MultiString.GetStrings(rawReaderNames, SdkPlatformInfo.Encoding);

                    atr = atrBuffer;
                    return result;
                }
            }

            readerNames = Array.Empty<string>();
            status = SCARD_STATUS.UNKNOWN;
            protocol = SCARD_PROTOCOL.Undefined;
            atr = null;
            return result;
        }
        */

    }
}
