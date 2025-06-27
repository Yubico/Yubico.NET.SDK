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

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp
{
    internal static class CommandApduExtension
    {
        private const int TotalReports = 10;

        private const int _framePayloadSize = 64;
        private const int _slotNumberOffset = 64;
        private const int _framePayloadCrcOffset = 65;
        private const int _yubiKeyFrameSize = 70;
        private const int _hidReportPayloadSize = 7;

        /// <summary>
        /// Formats an APDU as a YubiKey Frame
        /// </summary>
        /// <param name="apdu">A <see cref="CommandApdu"/>.</param>
        /// <returns>A byte array formatted as a YubiKey frame.</returns>
        /// <remarks>
        /// Normal guidance is to use span type objects for collections of bytes.
        /// However, since the main use for this is to break up the frame to send
        /// as HID usage reports, we will use a byte array.
        /// </remarks>
        public static byte[] GetYubiKeyFrame(this CommandApdu apdu)
        {
            if (apdu.Data.Length > _framePayloadSize)
            {
                throw new ArgumentException(
                    ExceptionMessages.KeyboardDataTooBig,
                    nameof(apdu));
            }

            // Constructs a new YubiKey keyboard frame. In C it is defined as:
            // struct frame_st {
            //     unsigned char payload[64];
            //     unsigned char slot; // AKA instruction
            //     unsigned short crc;
            //     unsigned char filler[3]; // Unused filler
            // };

            byte[] frame = apdu.Data.ToArray()
                .Concat(new byte[_yubiKeyFrameSize - apdu.Nc])
                .ToArray();
            frame[_slotNumberOffset] = apdu.P1;
            AddCrc(frame);

            return frame;
        }

        // This is private because it relies on the very specific layout of a
        // YubiKey frame.
        private static void AddCrc(byte[] frame)
        {
            short crc = Crc13239.Calculate(frame.AsSpan(0, _framePayloadSize));
            BinaryPrimitives.WriteInt16LittleEndian(frame.AsSpan(_framePayloadCrcOffset, sizeof(ushort)), crc);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> collection of <see cref="KeyboardReport"/>
        /// objects to send through an HID interface.
        /// </summary>
        /// <param name="apdu">A <see cref="CommandApdu"/> instance.</param>
        /// <returns>A collection of <see cref="KeyboardReport"/>.</returns>
        /// <remarks>
        /// This method adheres to the YubiKey protocol, which includes skipping frames
        /// that are not the start or end frame and have all zeros for payload.
        /// </remarks>
        public static IEnumerable<KeyboardReport> GetHidReports(this CommandApdu apdu)
        {
            byte[] frame = apdu.GetYubiKeyFrame();

            for (byte reportIndex = 0; reportIndex < TotalReports; ++reportIndex)
            {
                var report = new KeyboardReport();
                int index = reportIndex * _hidReportPayloadSize;

                IEnumerable<byte> payload = frame.Skip(index).Take(_hidReportPayloadSize).ToArray();
                if (reportIndex == 0
                    || reportIndex == TotalReports - 1
                    || payload.Any(b => b != 0))
                {
                    report.Payload = payload.ToArray().AsSpan();
                    report.SequenceNumber = reportIndex;
                    report.Flags = KeyboardReportFlags.WritePending;
                    yield return report;
                }
            }
        }
    }
}
