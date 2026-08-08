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

using System.Reflection;
using Yubico.YubiKit.Piv.DataObjects;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.DataObjects;

/// <summary>
/// Golden-vector round-trip tests for typed PIV data objects (ISC-18..21), plus proof that typed
/// read/write operations produce the same bytes as the raw GetObjectAsync/PutObjectAsync helpers
/// (ISC-22, ISC-22.1).
/// </summary>
public class PivDataObjectsTests
{
    private static readonly byte[] FixedFascNumber =
    [
        0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83, 0x68, 0x58, 0x21, 0x08,
        0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb
    ];

    private static readonly byte[] Guid16 =
        [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10];

    // === CHUID (ISC-18) ===

    private static byte[] ChuidGoldenVector() =>
    [
        0x30, 0x19, .. FixedFascNumber,
        0x34, 0x10, .. Guid16,
        0x35, 0x08, .. "20300101"u8.ToArray(),
        0x3E, 0x00,
        0xFE, 0x00
    ];

    [Fact]
    public void Chuid_TryDecode_GoldenVector_ProducesExpectedFields()
    {
        Assert.True(PivCardholderUniqueId.TryDecode(ChuidGoldenVector(), out var chuid));

        Assert.False(chuid.IsEmpty);
        Assert.Equal(FixedFascNumber, chuid.FascNumber.ToArray());
        Assert.Equal(Guid16, chuid.GuidValue.ToArray());
        Assert.Equal(new DateOnly(2030, 1, 1), chuid.ExpirationDate);
    }

    [Fact]
    public void Chuid_Encode_RoundTripsGoldenVectorExactly()
    {
        var golden = ChuidGoldenVector();
        Assert.True(PivCardholderUniqueId.TryDecode(golden, out var chuid));

        Assert.Equal(golden, chuid.Encode().ToArray());
    }

    [Fact]
    public void Chuid_TryDecode_Empty_ReturnsEmptyInstance()
    {
        Assert.True(PivCardholderUniqueId.TryDecode(ReadOnlyMemory<byte>.Empty, out var chuid));
        Assert.True(chuid.IsEmpty);
        Assert.Equal(ReadOnlyMemory<byte>.Empty.ToArray(), chuid.Encode().ToArray());
    }

    [Fact]
    public void Chuid_TryDecode_WrongFascNumber_Fails()
    {
        var bad = ChuidGoldenVector();
        bad[2] ^= 0xFF; // corrupt first FASC-N byte
        Assert.False(PivCardholderUniqueId.TryDecode(bad, out var chuid));
        Assert.True(chuid.IsEmpty);
    }

    [Fact]
    public void Chuid_Create_WrongGuidLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => PivCardholderUniqueId.Create(new byte[15]));
    }

    // === CCC (ISC-19) ===

    private static readonly byte[] CardId14 =
        [0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE];

    private static byte[] CccGoldenVector() =>
    [
        0xF0, 0x15, 0xA0, 0x00, 0x00, 0x01, 0x16, 0xFF, 0x02, .. CardId14,
        0xF1, 0x01, 0x21,
        0xF2, 0x01, 0x21,
        0xF3, 0x00,
        0xF4, 0x01, 0x00,
        0xF5, 0x01, 0x10,
        0xF6, 0x00,
        0xF7, 0x00,
        0xFA, 0x00,
        0xFB, 0x00,
        0xFC, 0x00,
        0xFD, 0x00,
        0xFE, 0x00
    ];

    [Fact]
    public void Ccc_TryDecode_GoldenVector_ProducesExpectedFields()
    {
        Assert.True(PivCardCapabilityContainer.TryDecode(CccGoldenVector(), out var ccc));

        Assert.False(ccc.IsEmpty);
        Assert.Equal(CardId14, ccc.CardIdentifier.ToArray());
        Assert.Equal([0xA0, 0x00, 0x00, 0x01, 0x16, 0xFF, 0x02], ccc.ApplicationIdentifier.ToArray());
        Assert.Equal(0x21, ccc.ContainerVersionNumber);
        Assert.Equal(0x21, ccc.GrammarVersionNumber);
        Assert.Equal(0x00, ccc.Pkcs15Version);
        Assert.Equal(0x10, ccc.DataModelNumber);
    }

    [Fact]
    public void Ccc_Encode_RoundTripsGoldenVectorExactly()
    {
        var golden = CccGoldenVector();
        Assert.True(PivCardCapabilityContainer.TryDecode(golden, out var ccc));

        Assert.Equal(golden, ccc.Encode().ToArray());
    }

    [Fact]
    public void Ccc_TryDecode_WrongApplicationIdentifier_Fails()
    {
        var bad = CccGoldenVector();
        bad[2] = 0xFF; // corrupt GSC-RID byte inside the fixed AID
        Assert.False(PivCardCapabilityContainer.TryDecode(bad, out var ccc));
        Assert.True(ccc.IsEmpty);
    }

    [Fact]
    public void Ccc_Create_WrongCardIdLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => PivCardCapabilityContainer.Create(new byte[13]));
    }

    // === AdminData (ISC-20) ===

    private static readonly byte[] Salt16 =
        [0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF, 0xC0];

    [Fact]
    public void AdminData_TryDecode_BitFieldOnly_GoldenVector_ProducesExpectedFields()
    {
        // 80 03 [81 01 03] -- PukBlocked (bit 0) and PinProtected (bit 1) both set, no salt/date.
        byte[] golden = [0x80, 0x03, 0x81, 0x01, 0x03];

        Assert.True(PivAdminData.TryDecode(golden, out var adminData));

        Assert.False(adminData.IsEmpty);
        Assert.True(adminData.PukBlocked);
        Assert.True(adminData.PinProtected);
        Assert.Null(adminData.Salt);
        Assert.Null(adminData.PinLastUpdated);
    }

    [Fact]
    public void AdminData_Encode_BitFieldOnly_RoundTripsGoldenVectorExactly()
    {
        byte[] golden = [0x80, 0x03, 0x81, 0x01, 0x03];
        Assert.True(PivAdminData.TryDecode(golden, out var adminData));

        Assert.Equal(golden, adminData.Encode().ToArray());
    }

    [Fact]
    public void AdminData_WithSalt_RoundTripsExactly()
    {
        var adminData = PivAdminData.Create(pukBlocked: true, pinProtected: false, salt: Salt16);

        var encoded = adminData.Encode().ToArray();

        // 80 15 [81 01 01] [82 10 <16-byte salt>]
        byte[] expected = [0x80, 0x15, 0x81, 0x01, 0x01, 0x82, 0x10, .. Salt16];
        Assert.Equal(expected, encoded);

        Assert.True(PivAdminData.TryDecode(encoded, out var decoded));
        Assert.True(decoded.PukBlocked);
        Assert.False(decoded.PinProtected);
        Assert.Equal(Salt16, decoded.Salt!.Value.ToArray());
    }

    [Fact]
    public void AdminData_WithPinLastUpdated_RoundTripsExactly()
    {
        var updated = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var adminData = PivAdminData.Create(pukBlocked: false, pinProtected: false, pinLastUpdated: updated);

        var encoded = adminData.Encode();
        Assert.True(PivAdminData.TryDecode(encoded, out var decoded));

        Assert.Equal(updated, decoded.PinLastUpdated);
    }

    [Fact]
    public void AdminData_TryDecode_Empty_ReturnsEmptyInstance()
    {
        Assert.True(PivAdminData.TryDecode(ReadOnlyMemory<byte>.Empty, out var adminData));
        Assert.True(adminData.IsEmpty);
    }

    [Fact]
    public void AdminData_TryDecode_WrongOuterTag_Fails()
    {
        byte[] bad = [0x81, 0x03, 0x81, 0x01, 0x03];
        Assert.False(PivAdminData.TryDecode(bad, out var adminData));
        Assert.True(adminData.IsEmpty);
    }

    [Fact]
    public void AdminData_Create_WrongSaltLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => PivAdminData.Create(false, false, new byte[10]));
    }

    [Fact]
    public void AdminData_TryDecode_DuplicateBitFieldTag_Fails()
    {
        // 80 06 [81 01 03] [81 01 00] -- BitFieldTag appears twice. v1 rejects duplicate elements
        // (see AdminData.ReadBitField's elementsRead XOR check); TlvHelper.DecodeDictionary alone
        // would silently keep only the last occurrence, which is a strictness regression.
        byte[] duplicateBitField = [0x80, 0x06, 0x81, 0x01, 0x03, 0x81, 0x01, 0x00];

        Assert.False(PivAdminData.TryDecode(duplicateBitField, out var adminData));
        Assert.True(adminData.IsEmpty);
    }

    [Fact]
    public void AdminData_TryDecode_DuplicateSaltTag_Fails()
    {
        byte[] duplicateSalt =
        [
            0x80, 0x24, // 2 x (82 10 <16-byte salt>) = 36 (0x24) bytes
            0x82, 0x10, .. Salt16,
            0x82, 0x10, .. Salt16
        ];

        Assert.False(PivAdminData.TryDecode(duplicateSalt, out var adminData));
        Assert.True(adminData.IsEmpty);
    }

    [Fact]
    public void AdminData_TryDecode_UnrecognizedTopLevelTag_Fails()
    {
        // 80 05 [81 01 03] [84 00] -- tag 0x84 is not one of the three ADMIN DATA elements (bit
        // field/salt/date). v1's TryDecode has a `_ => false` default case for unknown tags.
        byte[] unrecognizedTag = [0x80, 0x05, 0x81, 0x01, 0x03, 0x84, 0x00];

        Assert.False(PivAdminData.TryDecode(unrecognizedTag, out var adminData));
        Assert.True(adminData.IsEmpty);
    }

    // === KeyHistory (ISC-21) ===

    [Fact]
    public void KeyHistory_TryDecode_NoUrl_GoldenVector_ProducesExpectedFields()
    {
        byte[] golden = [0xC1, 0x01, 0x02, 0xC2, 0x01, 0x00, 0xF3, 0x00, 0xFE, 0x00];

        Assert.True(PivKeyHistory.TryDecode(golden, out var keyHistory));

        Assert.False(keyHistory.IsEmpty);
        Assert.Equal(2, keyHistory.OnCardCertificates);
        Assert.Equal(0, keyHistory.OffCardCertificates);
        Assert.Null(keyHistory.OffCardCertificateUrl);
    }

    [Fact]
    public void KeyHistory_Encode_NoUrl_RoundTripsGoldenVectorExactly()
    {
        byte[] golden = [0xC1, 0x01, 0x02, 0xC2, 0x01, 0x00, 0xF3, 0x00, 0xFE, 0x00];
        Assert.True(PivKeyHistory.TryDecode(golden, out var keyHistory));

        Assert.Equal(golden, keyHistory.Encode().ToArray());
    }

    [Fact]
    public void KeyHistory_WithUrl_RoundTripsExactly()
    {
        var url = new Uri("https://example.com/certs");
        var keyHistory = PivKeyHistory.Create(1, 2, url);

        var encoded = keyHistory.Encode();
        Assert.True(PivKeyHistory.TryDecode(encoded, out var decoded));

        Assert.Equal(1, decoded.OnCardCertificates);
        Assert.Equal(2, decoded.OffCardCertificates);
        Assert.Equal(url, decoded.OffCardCertificateUrl);
    }

    [Fact]
    public void KeyHistory_TryDecode_Empty_ReturnsEmptyInstance()
    {
        Assert.True(PivKeyHistory.TryDecode(ReadOnlyMemory<byte>.Empty, out var keyHistory));
        Assert.True(keyHistory.IsEmpty);
    }

    [Fact]
    public void KeyHistory_Create_UrlTooLong_Throws()
    {
        string longUrl = "https://example.com/" + new string('a', 118);
        Assert.Throws<ArgumentException>(() => PivKeyHistory.Create(0, 0, new Uri(longUrl)));
    }

    // === ISC-22 / ISC-22.1: typed read/write match raw GetObjectAsync/PutObjectAsync ===

    [Fact]
    public async Task GetCardholderUniqueIdAsync_MatchesRawGetObjectAsync()
    {
        var golden = ChuidGoldenVector();
        var connection = CreateInitializedConnection([0x53, (byte)golden.Length, .. golden, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var typed = await session.GetCardholderUniqueIdAsync(TestContext.Current.CancellationToken);

        Assert.Equal(golden, typed.Encode().ToArray());
    }

    [Fact]
    public async Task SetCardholderUniqueIdAsync_TransmitsSameDataAsRawPutObjectAsync()
    {
        var golden = ChuidGoldenVector();
        Assert.True(PivCardholderUniqueId.TryDecode(golden, out var chuid));

        var typedConnection = CreateInitializedConnection([0x90, 0x00]);
        await using var typedSession = await PivSession.CreateAsync(typedConnection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(typedSession);
        await typedSession.SetCardholderUniqueIdAsync(chuid, TestContext.Current.CancellationToken);

        var rawConnection = CreateInitializedConnection([0x90, 0x00]);
        await using var rawSession = await PivSession.CreateAsync(rawConnection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(rawSession);
        await rawSession.PutObjectAsync(PivDataObject.Chuid, golden, TestContext.Current.CancellationToken);

        Assert.Equal(rawConnection.TransmittedCommands[^1], typedConnection.TransmittedCommands[^1]);
    }

    [Fact]
    public async Task GetAdminDataAsync_MatchesRawGetObjectAsync()
    {
        byte[] golden = [0x80, 0x03, 0x81, 0x01, 0x03];
        var connection = CreateInitializedConnection([0x53, (byte)golden.Length, .. golden, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var typed = await session.GetAdminDataAsync(TestContext.Current.CancellationToken);

        Assert.Equal(golden, typed.Encode().ToArray());
    }

    [Fact]
    public async Task GetKeyHistoryAsync_MatchesRawGetObjectAsync()
    {
        byte[] golden = [0xC1, 0x01, 0x02, 0xC2, 0x01, 0x00, 0xF3, 0x00, 0xFE, 0x00];
        var connection = CreateInitializedConnection([0x53, (byte)golden.Length, .. golden, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var typed = await session.GetKeyHistoryAsync(TestContext.Current.CancellationToken);

        Assert.Equal(golden, typed.Encode().ToArray());
    }

    [Fact]
    public async Task GetCardCapabilityContainerAsync_MatchesRawGetObjectAsync()
    {
        var golden = CccGoldenVector();
        var connection = CreateInitializedConnection([0x53, (byte)golden.Length, .. golden, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var typed = await session.GetCardCapabilityContainerAsync(TestContext.Current.CancellationToken);

        Assert.Equal(golden, typed.Encode().ToArray());
    }

    // === Shared session-initialization scaffolding (mirrors PivSessionTests) ===

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), VersionResponse(), ManagementKeyMetadataResponse(), .. trailingResponses]);

    private static void MarkAuthenticated(PivSession session) =>
        typeof(PivSession)
            .GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, true);

    private static byte[] OkResponse() => [0x90, 0x00];

    private static byte[] VersionResponse() => [0x00, 0x00, 0x01, 0x90, 0x00];

    private static byte[] ManagementKeyMetadataResponse() =>
    [
        0x01, 0x01, (byte)PivManagementKeyType.TripleDes,
        0x02, 0x02, 0x00, (byte)PivTouchPolicy.Default,
        0x05, 0x01, 0x01,
        0x90, 0x00
    ];
}