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

using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Parsed discretionary data objects (tag 0x73) from the application related data.
///     Contains algorithm attributes, PIN status, fingerprints, key information, and UIF values.
/// </summary>
public sealed class DiscretionaryDataObjects
{
    /// <summary>
    ///     Extended capabilities of the card.
    /// </summary>
    public ExtendedCapabilities ExtendedCapabilities { get; init; } = null!;

    /// <summary>
    ///     Algorithm attributes for the Signature key slot.
    /// </summary>
    public AlgorithmAttributes AlgorithmAttributesSig { get; init; } = null!;

    /// <summary>
    ///     Algorithm attributes for the Decryption key slot.
    /// </summary>
    public AlgorithmAttributes AlgorithmAttributesDec { get; init; } = null!;

    /// <summary>
    ///     Algorithm attributes for the Authentication key slot.
    /// </summary>
    public AlgorithmAttributes AlgorithmAttributesAut { get; init; } = null!;

    /// <summary>
    ///     Algorithm attributes for the Attestation key slot. Null if not supported.
    /// </summary>
    public AlgorithmAttributes? AlgorithmAttributesAtt { get; init; }

    /// <summary>
    ///     PIN status bytes (policy, max lengths, remaining attempts).
    /// </summary>
    public PwStatus PwStatus { get; init; } = null!;

    /// <summary>
    ///     Fingerprints for each key slot (20 bytes each).
    /// </summary>
    public KeyFingerprints KeyFingerprints { get; init; } = new([]);

    /// <summary>
    ///     CA fingerprints for each key slot (20 bytes each).
    /// </summary>
    public KeyFingerprints CaKeyFingerprints { get; init; } = new([]);

    /// <summary>
    ///     Key generation timestamps for each key slot (Unix epoch seconds).
    /// </summary>
    public GenerationTimes GenerationTimes { get; init; } = new([]);

    /// <summary>
    ///     Key status information (none, generated, imported) for each slot.
    /// </summary>
    public KeyInformation KeyInformation { get; init; } = new([]);

    /// <summary>
    ///     User Interaction Flag for the Signature key. Null if not supported.
    /// </summary>
    public Uif? UifSig { get; init; }

    /// <summary>
    ///     User Interaction Flag for the Decryption key. Null if not supported.
    /// </summary>
    public Uif? UifDec { get; init; }

    /// <summary>
    ///     User Interaction Flag for the Authentication key. Null if not supported.
    /// </summary>
    public Uif? UifAut { get; init; }

    /// <summary>
    ///     User Interaction Flag for the Attestation key. Null if not supported.
    /// </summary>
    public Uif? UifAtt { get; init; }

    /// <summary>
    ///     Parses discretionary data objects from the TLV-encoded data.
    /// </summary>
    /// <param name="encoded">The inner bytes of the 0x73 TLV, or the outer data for older keys.</param>
    public static DiscretionaryDataObjects Parse(ReadOnlySpan<byte> encoded)
    {
        var data = TlvHelper.DecodeDictionary(encoded);

        return new DiscretionaryDataObjects
        {
            ExtendedCapabilities = ExtendedCapabilities.Parse(
                data[(int)DataObject.ExtendedCapabilitiesTag].Span),
            AlgorithmAttributesSig = AlgorithmAttributes.Parse(
                data[(int)DataObject.AlgorithmAttributesSig].Span),
            AlgorithmAttributesDec = AlgorithmAttributes.Parse(
                data[(int)DataObject.AlgorithmAttributesDec].Span),
            AlgorithmAttributesAut = AlgorithmAttributes.Parse(
                data[(int)DataObject.AlgorithmAttributesAut].Span),
            AlgorithmAttributesAtt = data.TryGetValue((int)DataObject.AlgorithmAttributesAtt, out var attData)
                ? AlgorithmAttributes.Parse(attData.Span)
                : null,
            PwStatus = PwStatus.Parse(data[(int)DataObject.PwStatusBytes].Span),
            KeyFingerprints = ParseKeyFingerprints(data.TryGetValue((int)DataObject.Fingerprints, out var fp)
                ? fp.Span
                : ReadOnlySpan<byte>.Empty),
            CaKeyFingerprints = ParseKeyFingerprints(data.TryGetValue((int)DataObject.CaFingerprints, out var caFp)
                ? caFp.Span
                : ReadOnlySpan<byte>.Empty),
            GenerationTimes = ParseGenerationTimes(data.TryGetValue((int)DataObject.GenerationTimes, out var gt)
                ? gt.Span
                : ReadOnlySpan<byte>.Empty),
            KeyInformation = ParseKeyInformation(data.TryGetValue((int)DataObject.KeyInformation, out var ki)
                ? ki.Span
                : ReadOnlySpan<byte>.Empty),
            UifSig = data.TryGetValue((int)DataObject.UifSig, out var uifSig) ? UifExtensions.ParseUif(uifSig.Span) : null,
            UifDec = data.TryGetValue((int)DataObject.UifDec, out var uifDec) ? UifExtensions.ParseUif(uifDec.Span) : null,
            UifAut = data.TryGetValue((int)DataObject.UifAut, out var uifAut) ? UifExtensions.ParseUif(uifAut.Span) : null,
            UifAtt = data.TryGetValue((int)DataObject.UifAtt, out var uifAtt) ? UifExtensions.ParseUif(uifAtt.Span) : null,
        };
    }

    private static readonly KeyRef[] KeySlots = [KeyRef.Sig, KeyRef.Dec, KeyRef.Aut, KeyRef.Att];

    private static KeyFingerprints ParseKeyFingerprints(ReadOnlySpan<byte> encoded)
    {
        var entries = new Dictionary<KeyRef, ReadOnlyMemory<byte>>();
        for (var i = 0; i < KeySlots.Length && (i + 1) * 20 <= encoded.Length; i++)
        {
            entries[KeySlots[i]] = encoded.Slice(i * 20, 20).ToArray();
        }

        return new KeyFingerprints(entries);
    }

    private static GenerationTimes ParseGenerationTimes(ReadOnlySpan<byte> encoded)
    {
        var entries = new Dictionary<KeyRef, int>();
        for (var i = 0; i < KeySlots.Length && (i + 1) * 4 <= encoded.Length; i++)
        {
            var offset = i * 4;
            var timestamp = (encoded[offset] << 24)
                            | (encoded[offset + 1] << 16)
                            | (encoded[offset + 2] << 8)
                            | encoded[offset + 3];
            entries[KeySlots[i]] = timestamp;
        }

        return new GenerationTimes(entries);
    }

    private static KeyInformation ParseKeyInformation(ReadOnlySpan<byte> encoded)
    {
        var entries = new Dictionary<KeyRef, KeyStatus>();
        for (var i = 0; i + 1 < encoded.Length; i += 2)
        {
            if (Enum.IsDefined((KeyRef)encoded[i]))
            {
                entries[(KeyRef)encoded[i]] = (KeyStatus)encoded[i + 1];
            }
        }

        return new KeyInformation(entries);
    }
}