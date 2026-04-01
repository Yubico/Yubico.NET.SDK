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

using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Parsed Application Related Data (DO 0x6E) -- the top-level card state returned after SELECT.
/// </summary>
/// <remarks>
///     The 0x6E TLV contains:
///     <list type="bullet">
///         <item>0x4F -- Application Identifier (AID)</item>
///         <item>0x5F52 -- Historical Bytes</item>
///         <item>0x7F66 -- Extended Length Info (optional)</item>
///         <item>0x7F74 -- General Feature Management (optional)</item>
///         <item>0x73 -- Discretionary Data Objects (algorithm attrs, PIN status, etc.)</item>
///     </list>
///     On older keys, discretionary data may be at the outer level instead of nested under 0x73.
/// </remarks>
public sealed class ApplicationRelatedData
{
    /// <summary>
    ///     The parsed Application Identifier.
    /// </summary>
    public OpenPgpAid Aid { get; init; } = null!;

    /// <summary>
    ///     Historical bytes from ATR.
    /// </summary>
    public ReadOnlyMemory<byte> HistoricalBytes { get; init; }

    /// <summary>
    ///     Extended length information (maximum APDU sizes). Null if not supported.
    /// </summary>
    public ExtendedLengthInfo? ExtendedLengthInfo { get; init; }

    /// <summary>
    ///     General feature management flags. Null if not reported.
    /// </summary>
    public GeneralFeatureManagement? GeneralFeatures { get; init; }

    /// <summary>
    ///     Discretionary data objects containing algorithm attributes, PIN status, fingerprints, etc.
    /// </summary>
    public DiscretionaryDataObjects Discretionary { get; init; } = null!;

    /// <summary>
    ///     Parses Application Related Data from the raw TLV response of GET DATA(0x6E).
    /// </summary>
    /// <param name="encoded">The full TLV-encoded 0x6E response including the outer tag.</param>
    public static ApplicationRelatedData Parse(ReadOnlySpan<byte> encoded)
    {
        // Unwrap the outer 0x6E tag
        using var outerTlv = Tlv.Create(encoded);
        var outerValue = outerTlv.Value.Span;

        var data = TlvHelper.DecodeDictionary(outerValue);

        // Parse optional Extended Length Info
        ExtendedLengthInfo? extLenInfo = null;
        if (data.TryGetValue((int)DataObject.ExtendedLengthInfo, out var extLenData))
        {
            extLenInfo = ExtendedLengthInfo.Parse(extLenData.Span);
        }

        // Parse optional General Feature Management
        GeneralFeatureManagement? generalFeatures = null;
        if (data.TryGetValue((int)DataObject.GeneralFeatureManagement, out var gfmData))
        {
            // GFM contains a nested TLV with tag 0x81, the first byte of which is the flags
            var gfmDict = TlvHelper.DecodeDictionary(gfmData.Span);
            if (gfmDict.TryGetValue(0x81, out var gfmValue) && gfmValue.Length > 0)
            {
                generalFeatures = (GeneralFeatureManagement)gfmValue.Span[0];
            }
        }

        // Parse Discretionary Data Objects -- may be under tag 0x73 or at the outer level
        ReadOnlySpan<byte> discretionaryBytes;
        if (data.TryGetValue((int)DataObject.DiscretionaryDataObjects, out var discData))
        {
            discretionaryBytes = discData.Span;
        }
        else
        {
            // Older keys: discretionary data is at the outer level
            discretionaryBytes = outerValue;
        }

        return new ApplicationRelatedData
        {
            Aid = OpenPgpAid.Parse(data[(int)DataObject.Aid].Span),
            HistoricalBytes = data.TryGetValue((int)DataObject.HistoricalBytes, out var hb) ? hb : ReadOnlyMemory<byte>.Empty,
            ExtendedLengthInfo = extLenInfo,
            GeneralFeatures = generalFeatures,
            Discretionary = DiscretionaryDataObjects.Parse(discretionaryBytes),
        };
    }
}
