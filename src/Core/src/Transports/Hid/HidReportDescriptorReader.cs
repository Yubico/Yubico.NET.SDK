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

namespace Yubico.YubiKit.Core.Transports.Hid;

/// <summary>
/// One decoded HID report-descriptor short item.
/// </summary>
/// <param name="Type">The item type: <see cref="HidReportDescriptorReader.TypeMain"/>,
/// <see cref="HidReportDescriptorReader.TypeGlobal"/>, or
/// <see cref="HidReportDescriptorReader.TypeLocal"/> (or the reserved value 3).</param>
/// <param name="Tag">The item tag, distinguishing items within a type (for example, Usage Page,
/// Report Size, Input).</param>
/// <param name="Value">The little-endian item value. Zero for a zero-size item.</param>
internal readonly record struct HidReportItem(int Type, int Tag, uint Value);

/// <summary>
/// Platform-neutral walker over a HID report descriptor's short-item byte stream, per the HID
/// specification's short-item encoding (prefix byte: bits 0-1 = size, bits 2-3 = type, bits 4-7
/// = tag).
/// </summary>
/// <remarks>
/// This does not implement HID long items (prefix 0xFE). A long-item prefix decodes through this
/// short-item layout as size=2/type=3/tag=15, consuming its two length/tag bytes as an ordinary
/// item value and desynchronizing the cursor for the remainder of the descriptor. YubiKeys do not
/// emit long items in practice. This is a deliberate, pinned limitation inherited from the two
/// original hand-rolled walkers this type replaces; do not add long-item handling here without a
/// dedicated, reviewed change.
/// </remarks>
internal static class HidReportDescriptorReader
{
    /// <summary>
    /// Main item type (bits 2-3 of the prefix byte == 0). Examples: Input, Output, Collection.
    /// </summary>
    internal const int TypeMain = 0;

    /// <summary>
    /// Global item type (bits 2-3 of the prefix byte == 1). Examples: Usage Page, Report Size,
    /// Report Count.
    /// </summary>
    internal const int TypeGlobal = 1;

    /// <summary>
    /// Local item type (bits 2-3 of the prefix byte == 2). Examples: Usage.
    /// </summary>
    internal const int TypeLocal = 2;

    /// <summary>
    /// Reads the next short item from <paramref name="descriptor"/>, advancing
    /// <paramref name="position"/> past it.
    /// </summary>
    /// <param name="descriptor">The raw HID report descriptor bytes.</param>
    /// <param name="position">The read cursor. Advanced past the item just read on success;
    /// left unspecified once this method returns <see langword="false"/>.</param>
    /// <param name="item">The decoded item, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if an item was read; <see langword="false"/> once the
    /// descriptor is exhausted or the trailing item is truncated.</returns>
    /// <remarks>
    /// A truncated item (fewer bytes remaining than its prefix declares) terminates the walk
    /// rather than being clamped or skipped, matching the two hand-rolled walkers this replaces.
    /// An item declaring a size of zero still advances the cursor by its prefix byte and yields a
    /// <see cref="HidReportItem.Value"/> of zero.
    /// </remarks>
    internal static bool TryReadItem(ReadOnlySpan<byte> descriptor, ref int position, out HidReportItem item)
    {
        item = default;

        if (position >= descriptor.Length)
        {
            return false;
        }

        byte prefix = descriptor[position];
        int size = prefix & 0x03;
        if (size == 3)
        {
            size = 4; // Size encoding: 0=0, 1=1, 2=2, 3=4
        }

        int type = (prefix >> 2) & 0x03;
        int tag = (prefix >> 4) & 0x0F;

        int cursor = position + 1; // Move past prefix

        if (cursor + size > descriptor.Length)
        {
            return false;
        }

        uint value = 0;
        for (int j = 0; j < size; j++)
        {
            value |= (uint)descriptor[cursor + j] << (8 * j);
        }

        item = new HidReportItem(type, tag, value);
        position = cursor + size;
        return true;
    }
}