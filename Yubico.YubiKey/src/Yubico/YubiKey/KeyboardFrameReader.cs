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
using System.Collections.Generic;

namespace Yubico.YubiKey
{
    internal class KeyboardFrameReader
    {
        private readonly List<byte> _data = new List<byte>();

        private int _previousReportNumber = -1;

        public bool IsEndOfReadChain { get; private set; }
        public bool WaitingForTouch { get; private set; }
        public bool UnexpectedEOR { get; private set; }

        public KeyboardFrameReader()
        {
        }

        public byte[] GetData() => _data.ToArray();

        public bool TryAddFeatureReport(KeyboardReport report)
        {
            WaitingForTouch = report.TouchPending;

            if (report.SequenceNumber == 0 && _previousReportNumber != -1 || !report.ReadPending)
            {
                // If this is the second time we're seeing zero as the report number, we are at the
                // end of a read chain and should not process this record.
                IsEndOfReadChain = true;
                return false;
            }

            _previousReportNumber = report.SequenceNumber;
            _data.AddRange(report.Payload.ToArray());
            UnexpectedEOR = !report.ReadPending;

            return !UnexpectedEOR;
        }

        /// <summary>
        /// Reads a status report.
        /// </summary>
        /// <remarks>
        /// Querying for status is different between CCID and HID. CCID gets the trimmed down
        /// structure that only contains the fields defined in ykdef.h (FW version, sequence,
        /// touch level). The HID version contains some extra stuff which is HID specific and
        /// there likely for backward compatability reasons. It's a two-byte difference... there's
        /// a leading byte which is discarded (not really used by anything anymore), and
        /// the HID report sequence number which is also discarded (as usual). Since all data
        /// is expected in a single Report, this must be the first and only Report read by this
        /// Reader.
        /// </remarks>
        /// <param name="report">
        /// Report which contains the status data. The status data is 6 bytes long, and is located
        /// at an offset of 1 in <see cref="KeyboardReport.Payload"/>. The
        /// <see cref="KeyboardReport.ReadPending"/> ReadPending flag is not set.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The status report must be the first report read by the KeyboardFrameReader.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// The status report must be the first and only report returned by the YubiKey.
        /// </exception>
        public void AddStatusReport(KeyboardReport report)
        {
            if (_previousReportNumber != -1)
            {
                throw new InvalidOperationException(ExceptionMessages.StatusReportNotFirstReportRead);
            }

            if (report.SequenceNumber != 0 || report.ReadPending)
            {
                throw new MalformedYubiKeyResponseException(ExceptionMessages.InvalidStatusReport);
            }

            _previousReportNumber = report.SequenceNumber;
            _data.AddRange(report.Payload[1..].ToArray());
            IsEndOfReadChain = true;
        }
    }
}
