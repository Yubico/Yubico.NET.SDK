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
using System.Diagnostics;
using System.Globalization;

namespace Yubico.YubiKey
{
    internal class KeyboardReport
    {
        private const int SequenceMask = 0b0001_1111;
        private const int FlagsMask = 0b1110_0000;

        private readonly Memory<byte> _reportBuffer;

        public Span<byte> Payload
        {
            get => PayloadSpan();
            set => value.CopyTo(PayloadSpan());
        }

        public bool IsAllZeros() => Payload.SequenceEqual(new byte[] { 0, 0, 0, 0, 0, 0, 0 });

        public KeyboardReportFlags Flags
        {
            get => (KeyboardReportFlags)(_reportBuffer.Span[^1] & FlagsMask);
            set
            {
                if (((int)value & ~FlagsMask) != 0)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.KeyboardInvalidFlag,
                            value),
                        nameof(value));
                }

                int temp = _reportBuffer.Span[^1] & SequenceMask; // Only retain the sequence number
                temp |= (int)value; // Add in the flags
                _reportBuffer.Span[^1] = (byte)temp;
            }
        }

        public bool TouchPending => (Flags & KeyboardReportFlags.TouchPending) != 0;
        public bool ReadPending => (Flags & KeyboardReportFlags.ReadPending) != 0;
        public bool WritePending => (Flags & KeyboardReportFlags.WritePending) != 0;

        public int SequenceNumber
        {
            get => _reportBuffer.Span[^1] & SequenceMask;
            set
            {
                if ((value & ~SequenceMask) != 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.KeyboardSequenceOutOfRange,
                            value));
                }

                int temp = _reportBuffer.Span[^1] & FlagsMask; // Only retain the flags
                temp |= value; // Add in the sequence number
                _reportBuffer.Span[^1] = (byte)temp;
            }
        }

        public KeyboardReport()
        {
            _reportBuffer = new byte[8];
        }

        public KeyboardReport(Memory<byte> reportBuffer)
        {
            Debug.Assert(reportBuffer.Length == 8);
            _reportBuffer = reportBuffer;
        }

        public byte[] ToArray() => _reportBuffer.ToArray();

        private Span<byte> PayloadSpan() => _reportBuffer.Slice(0, 7).Span;

        public override string ToString()
        {
            return
                $"TouchPending: {TouchPending}, " +
                $"ReadPending: {ReadPending}, " +
                $"WritePending: {WritePending}, " +
                $"SequenceNumber: {SequenceNumber}, " +
                $"Payload: {BitConverter.ToString(PayloadSpan().ToArray())}";
        }
    }
}
