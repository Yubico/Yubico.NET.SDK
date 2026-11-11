// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>Extracts bounded unnumbered input/output payload-size metadata for the supported YubiKey descriptor shape.</summary>
internal static class LinuxHidReportSizes
{
    /// <summary>Gets the last nonzero Input/Output main-item size in bytes, defaulting each direction to 64.</summary>
    /// <param name="descriptor">Borrowed report descriptor bytes, not report payload or native transfer-count evidence.</param>
    /// <returns>Input/output metadata with widened bit arithmetic and a 4096-byte bound per recognized main item.</returns>
    /// <remarks>
    /// Not a generic HID parser: no collection aggregation or global PUSH/POP support. Numbered
    /// reports fail explicitly. Retains the descriptor reader's truncation/default behavior rather
    /// than promising complete malformed-descriptor validation. Actual reads may be shorter, and
    /// raw output uses caller length instead of requiring a match to this metadata.
    /// </remarks>
    internal static (int InputSize, int OutputSize) Parse(ReadOnlySpan<byte> descriptor)
    {
        int inputSize = 64;
        int outputSize = 64;
        uint currentReportSize = 0;
        uint currentReportCount = 0;
        int position = 0;
        while (HidReportDescriptorReader.TryReadItem(descriptor, ref position, out HidReportItem item))
        {
            if (item.Type == HidReportDescriptorReader.TypeGlobal)
            {
                ReadGlobal(item, ref currentReportSize, ref currentReportCount);
                continue;
            }
            if (item.Type != HidReportDescriptorReader.TypeMain || item.Tag is not (8 or 9))
                continue;
            int reportBytes = GetReportBytes(currentReportSize, currentReportCount);
            if (reportBytes == 0)
                continue;
            if (item.Tag == 8)
                inputSize = reportBytes;
            else
                outputSize = reportBytes;
        }
        return (inputSize, outputSize);
    }

    private static void ReadGlobal(HidReportItem item, ref uint size, ref uint count)
    {
        if (item.Tag == 8)
            throw new NotSupportedException("Numbered HID reports are not supported by the YubiKey report adapter.");
        size = item.Tag == 7 ? item.Value : size;
        count = item.Tag == 9 ? item.Value : count;
    }

    private static int GetReportBytes(uint size, uint count)
    {
        ulong bytes = ((ulong)size * count + 7) / 8;
        return bytes > 4096 ? throw new InvalidDataException("Linux HID report size exceeds the bounded report limit.") : (int)bytes;
    }
}