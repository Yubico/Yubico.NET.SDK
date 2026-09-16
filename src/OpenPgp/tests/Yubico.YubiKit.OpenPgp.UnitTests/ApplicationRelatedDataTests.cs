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

using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public class ApplicationRelatedDataTests
{
    /// <summary>
    ///     Builds a realistic Application Related Data (DO 0x6E) TLV for testing.
    /// </summary>
    private static byte[] BuildTestApplicationRelatedData()
    {
        // AID: D276000124010304000612345678 (v3.4, Yubico, serial=12345678)
        byte[] aid = [0xD2, 0x76, 0x00, 0x01, 0x24, 0x01, 0x03, 0x04, 0x00, 0x06, 0x12, 0x34, 0x56, 0x78];

        // Historical bytes
        byte[] historicalBytes = [0x00, 0x73, 0x00, 0x01, 0x80, 0x05, 0x90, 0x00];

        // Algorithm attributes: RSA 2048 for SIG, DEC, AUT
        byte[] rsaAttrs = [0x01, 0x08, 0x00, 0x00, 0x11, 0x00];

        // Extended capabilities (10 bytes)
        byte[] extCaps = [0x75, 0x00, 0x00, 0xFF, 0x04, 0x80, 0x00, 0xFF, 0x00, 0x00];

        // PW Status: Always, max=127, attempts=3/0/3
        byte[] pwStatus = [0x00, 0x7F, 0x7F, 0x7F, 0x03, 0x00, 0x03];

        // Fingerprints (3 slots x 20 bytes = 60 bytes, all zeros)
        var fingerprints = new byte[60];

        // CA fingerprints (60 bytes, all zeros)
        var caFingerprints = new byte[60];

        // Generation times (3 x 4 bytes = 12 bytes, all zeros)
        var genTimes = new byte[12];

        // Build discretionary data objects (tag 0x73)
        var discretionaryTlvs = new Tlv[]
        {
            new(0xC0, extCaps),
            new(0xC1, rsaAttrs),
            new(0xC2, rsaAttrs),
            new(0xC3, rsaAttrs),
            new(0xC4, pwStatus),
            new(0xC5, fingerprints),
            new(0xC6, caFingerprints),
            new(0xCD, genTimes),
        };

        byte[] discretionaryContent;
        try
        {
            discretionaryContent = TlvHelper.EncodeList(discretionaryTlvs).ToArray();
        }
        finally
        {
            foreach (var t in discretionaryTlvs) t.Dispose();
        }

        // Build outer 0x6E
        var outerTlvs = new Tlv[]
        {
            new(0x4F, aid),
            new(0x5F52, historicalBytes),
            new(0x73, discretionaryContent),
        };

        byte[] outerContent;
        try
        {
            outerContent = TlvHelper.EncodeList(outerTlvs).ToArray();
        }
        finally
        {
            foreach (var t in outerTlvs) t.Dispose();
        }

        using var result = new Tlv(0x6E, outerContent);
        return result.AsMemory().ToArray();
    }

    [Fact]
    public void Parse_ValidData_ExtractsAid()
    {
        var data = BuildTestApplicationRelatedData();
        var parsed = ApplicationRelatedData.Parse(data);

        Assert.Equal(3, parsed.Aid.Version.Major);
        Assert.Equal(4, parsed.Aid.Version.Minor);
        Assert.Equal(6, parsed.Aid.Manufacturer);
        Assert.Equal(12345678, parsed.Aid.Serial);
    }

    [Fact]
    public void Parse_ValidData_ExtractsHistoricalBytes()
    {
        var data = BuildTestApplicationRelatedData();
        var parsed = ApplicationRelatedData.Parse(data);

        Assert.False(parsed.HistoricalBytes.IsEmpty);
    }

    [Fact]
    public void Parse_ValidData_ExtractsDiscretionary()
    {
        var data = BuildTestApplicationRelatedData();
        var parsed = ApplicationRelatedData.Parse(data);

        // Algorithm attributes for SIG should be RSA 2048
        var sigAttrs = Assert.IsType<RsaAttributes>(parsed.Discretionary.AlgorithmAttributesSig);
        Assert.Equal(2048, sigAttrs.NLen);

        // PW Status
        Assert.Equal(PinPolicy.Always, parsed.Discretionary.PwStatus.SignaturePinPolicy);
        Assert.Equal(3, parsed.Discretionary.PwStatus.AttemptsUser);
        Assert.Equal(0, parsed.Discretionary.PwStatus.AttemptsReset);
        Assert.Equal(3, parsed.Discretionary.PwStatus.AttemptsAdmin);
    }

    [Fact]
    public void Parse_ValidData_ExtractsExtendedCapabilities()
    {
        var data = BuildTestApplicationRelatedData();
        var parsed = ApplicationRelatedData.Parse(data);

        Assert.NotNull(parsed.Discretionary.ExtendedCapabilities);
        Assert.Equal(255, parsed.Discretionary.ExtendedCapabilities.ChallengeMaxLength);
    }

    [Fact]
    public void Parse_WithoutExtendedLengthInfo_LeavesNull()
    {
        var data = BuildTestApplicationRelatedData();
        var parsed = ApplicationRelatedData.Parse(data);

        Assert.Null(parsed.ExtendedLengthInfo);
    }

    [Fact]
    public void DiscretionaryParse_KeyCollections_PreserveLookupEnumerationAndEnumValues()
    {
        byte[] fingerprint = Enumerable.Range(1, 20).Select(static value => (byte)value).ToArray();
        byte[] caFingerprint = Enumerable.Range(21, 20).Select(static value => (byte)value).ToArray();
        byte[] encoded = BuildDiscretionaryData(
            fingerprints: fingerprint,
            caFingerprints: caFingerprint,
            generationTimes: [0x01, 0x02, 0x03, 0x04],
            keyInformation: [(byte)KeyRef.Sig, 0xFE, 0x7F, (byte)KeyStatus.Generated]);

        DiscretionaryDataObjects parsed = DiscretionaryDataObjects.Parse(encoded);

        Assert.Equal(fingerprint, parsed.KeyFingerprints[KeyRef.Sig].ToArray());
        Assert.Equal(caFingerprint, parsed.CaKeyFingerprints[KeyRef.Sig].ToArray());
        Assert.Equal(0x01020304, parsed.GenerationTimes[KeyRef.Sig]);
        Assert.Equal((KeyStatus)0xFE, parsed.KeyInformation[KeyRef.Sig]);
        Assert.Equal([KeyRef.Sig], parsed.KeyInformation.Select(static entry => entry.Key));
        Assert.False(parsed.KeyInformation.ContainsKey((KeyRef)0x7F));
        Assert.Throws<KeyNotFoundException>(() => parsed.KeyInformation[KeyRef.Dec]);
    }

    [Fact]
    public void DiscretionaryParse_MissingCollectionObjects_ReturnsEmptyCollections()
    {
        byte[] encoded = BuildDiscretionaryData();

        DiscretionaryDataObjects parsed = DiscretionaryDataObjects.Parse(encoded);

        Assert.Empty(parsed.KeyFingerprints);
        Assert.Empty(parsed.CaKeyFingerprints);
        Assert.Empty(parsed.GenerationTimes);
        Assert.Empty(parsed.KeyInformation);
        Assert.False(parsed.KeyFingerprints.TryGetValue(KeyRef.Sig, out _));
    }

    [Fact]
    public void DiscretionaryParse_RepeatedParsingOwnsFingerprintStorageIndependently()
    {
        byte[] fingerprint = Enumerable.Range(1, 20).Select(static value => (byte)value).ToArray();
        byte[] encoded = BuildDiscretionaryData(fingerprints: fingerprint);

        DiscretionaryDataObjects first = DiscretionaryDataObjects.Parse(encoded);
        DiscretionaryDataObjects second = DiscretionaryDataObjects.Parse(encoded);
        encoded.AsSpan().Fill(0xFF);

        Assert.Equal(fingerprint, first.KeyFingerprints[KeyRef.Sig].ToArray());
        Assert.Equal(fingerprint, second.KeyFingerprints[KeyRef.Sig].ToArray());
        Assert.True(MemoryMarshal.TryGetArray(first.KeyFingerprints[KeyRef.Sig], out ArraySegment<byte> firstStorage));
        Assert.True(MemoryMarshal.TryGetArray(second.KeyFingerprints[KeyRef.Sig], out ArraySegment<byte> secondStorage));
        Assert.NotSame(firstStorage.Array, secondStorage.Array);
    }

    private static byte[] BuildDiscretionaryData(
        byte[]? fingerprints = null,
        byte[]? caFingerprints = null,
        byte[]? generationTimes = null,
        byte[]? keyInformation = null)
    {
        byte[] rsaAttributes = [0x01, 0x08, 0x00, 0x00, 0x11, 0x00];
        var tlvs = new List<Tlv>(capacity: 9);

        try
        {
            tlvs.Add(new Tlv(0xC0, new byte[] { 0x75, 0x00, 0x00, 0xFF, 0x04, 0x80, 0x00, 0xFF, 0x00, 0x00 }));
            tlvs.Add(new Tlv(0xC1, rsaAttributes));
            tlvs.Add(new Tlv(0xC2, rsaAttributes));
            tlvs.Add(new Tlv(0xC3, rsaAttributes));
            tlvs.Add(new Tlv(0xC4, new byte[] { 0x00, 0x7F, 0x7F, 0x7F, 0x03, 0x00, 0x03 }));
            AddOptionalTlv(tlvs, 0xC5, fingerprints);
            AddOptionalTlv(tlvs, 0xC6, caFingerprints);
            AddOptionalTlv(tlvs, 0xCD, generationTimes);
            AddOptionalTlv(tlvs, 0xDE, keyInformation);

            return TlvHelper.EncodeList(tlvs.ToArray()).ToArray();
        }
        finally
        {
            foreach (Tlv tlv in tlvs)
            {
                tlv.Dispose();
            }
        }
    }

    private static void AddOptionalTlv(List<Tlv> tlvs, int tag, byte[]? value)
    {
        if (value is not null)
        {
            tlvs.Add(new Tlv(tag, value));
        }
    }
}