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

namespace Yubico.YubiKey.Scp
{
    internal static class Padding
    {
        /// <summary>
        /// Pad the given <paramref name="payload"/> to the next multiple of 16 bytes by adding a 0x80 byte followed by zero bytes.
        /// </summary>
        /// <param name="payload">The payload to pad.</param>
        /// <returns>The padded payload.</returns>
        public static Memory<byte> PadToBlockSize(ReadOnlySpan<byte> payload)
        {
            if(payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            
            int paddedLen = ((payload.Length / 16) + 1) * 16;

            Span<byte> padded = stackalloc byte[paddedLen];
            payload.CopyTo(padded);
            padded[payload.Length] = 0x80;
            
            return padded.ToArray();
        }
        
        /// <summary>
        /// Remove the padding from the given <paramref name="paddedPayload"/>.
        /// </summary>
        /// <param name="paddedPayload">The padded payload.</param>
        /// <returns>The unpadded payload.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="paddedPayload"/> is <c>null</c>.</exception>
        /// <exception cref="SecureChannelException">The padding is invalid.</exception>
        public static Memory<byte> RemovePadding(ReadOnlySpan<byte> paddedPayload)
        {
            if (paddedPayload == null)
            {
                throw new ArgumentNullException(nameof(paddedPayload));
            }

            for (int i = paddedPayload.Length - 1; i >= 0; i--)
            {
                if (paddedPayload[i] == 0x80)
                {
                    return paddedPayload[..i].ToArray();
                }
                
                if (paddedPayload[i] != 0x00)
                {
                    throw new SecureChannelException(ExceptionMessages.InvalidPadding);
                }
            }

            throw new SecureChannelException(ExceptionMessages.InvalidPadding);
        }
    }
}
