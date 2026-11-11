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

internal static class LinuxHidReportSizes
{
    // Existing last-main-item parser for the current YubiKey descriptors. Not a generic
    // HID parser: no collection aggregation, report IDs, or global PUSH/POP support.
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