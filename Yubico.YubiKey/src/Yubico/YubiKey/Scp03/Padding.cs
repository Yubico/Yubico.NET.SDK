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
using System.Linq;

namespace Yubico.YubiKey.Scp03
{
    internal static class Padding
    {
        public static byte[] PadToBlockSize(byte[] payload)
        {
            if (payload is null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            int paddedLen = (payload.Length / 16 + 1) * 16;
            byte[] padded = new byte[paddedLen];
            payload.CopyTo(padded, index: 0);
            padded[payload.Length] = 0x80;
            return padded;
        }

        public static byte[] RemovePadding(byte[] paddedPayload)
        {
            if (paddedPayload is null)
            {
                throw new ArgumentNullException(nameof(paddedPayload));
            }

            for (int i = paddedPayload.Length - 1; i >= 0; i--)
            {
                if (paddedPayload[i] == 0x80)
                {
                    return paddedPayload.Take(i).ToArray();
                }
                else if (paddedPayload[i] != 0x00)
                {
                    throw new SecureChannelException(ExceptionMessages.InvalidPadding);
                }
            }

            throw new SecureChannelException(ExceptionMessages.InvalidPadding);
        }
    }
}
