using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.OpenPgp.UnitTests;

/// <summary>
///     PutKeyAsync names the slot once; the SDK derives the algorithm attributes and the private key
///     template from the key, as ykman put_key and yubikit-android putKey do.
/// </summary>
public sealed class OpenPgpPutKeyWireTests
{
    private const byte InsPutData = 0xDA;
    private const byte InsPutDataOdd = 0xDB;
    private const byte AlgorithmAttributesChangeable = 0x04;
    private const byte NoChangeableAttributes = 0x71; // 0x75 without bit 2
    private static readonly byte[] CrtSig = [0xB6, 0x00];
    private static readonly byte[] CrtDec = [0xB8, 0x00];
    private static readonly byte[] CrtAut = [0xA4, 0x00];

    /// <summary>A 2048-bit RSA key generated with public exponent 3 (openssl genpkey -pkeyopt rsa_keygen_pubexp:3).</summary>
    private const string RsaExponent3Pkcs8Base64 =
        "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQDrqvKad/i78FG+g7HXMAg6Vi35It3n2HMebkIxznATvEx5I/ZN" +
        "d7RDUF/sXmnA4WMPgqPVyImKnY3gkrk0O/46JJG6cxrltwR54fhqxoV0E42tjAGlpqFkXNqxom1xMTIQA4u6Xg0m1NELpshO4rXt" +
        "OkDJVAWkkrg9XqL76vJFD9kJSG1n2Fv/hlKzRNZsxi1viV8rhKEI4SH2eiurNTzj5Nug3sAiIqVB2fS95grmcEm0gFhlmN0xeSiY" +
        "1uGDB0T5gBRAzhPC+VUS0LCEFMRgYZV/XARBjZ9pPoQZy8+7hZKFLnOvlW3y3dO4DLgqBarYTOFOToTLgmW+N0nId7DxAgEDAoIB" +
        "AQCdHKG8T/sn9YvUV8vkyrAm5B6mFz6akEy+9CwhNEq30t2mF/mI+ngs4D/y6ZvV65dfrG05MFuxvl6VtyYi0ql8GGEm92dDz1hR" +
        "QVBHLwOit7PJCAEZGcDtkzx2bEj2IMwKrQfRlAjEjeCybzA0lyPzfCsw4q5ttyV+PxdSnKGDXp5rk76NM0z6L4JjK1MbJO6jkf7l" +
        "ssJTYmBr/nazCM9MQy+zNKcEZd1q3puRDe0xtmV/ZIm7JpqHjlrZQedEz/KhVOQ2C4mAi3R/o6j7sY6shzrO0jKIB9yOIkugdcaG" +
        "WaADA2kjO0Bh1FjGM5qYMeTNgffgQ3n4h4fen03yweCjAoGBAPvypdn4lh2dHQ3cq18DAHt/Csp8d2KPq2h/tuPGKHa4snm3vUi0" +
        "5SUZ9SkqAeO+1p59OI2VUlCEZ0Tmf8YVvUWG/tz+GtvkOuLIULVfvZVnFwVW/Y1TQ3cPmZWWtRaL0+yDWqlAvJhZVynGnXkuSS9Q" +
        "h7f6pOUCiTbhskkL2L6bAoGBAO91RPWbdUrrIgFB2HrBDkv7I5ZWgRr8IijUxZXYf484zZpcUnzmpLQHluI6T0NdCBL4MPw3jKTh" +
        "vFtsdECGEhOAgeDxoemd7UOLCn2qzNj2f7fyIysiPl2EcX0SZg9mKzX9Tqy5//UGyCTIIdcXcaRTgjWDRGjULeMOlgvQfCFjAoGB" +
        "AKf3GTv7Dr5ovgk9x5Ssqvz/XIb9pOxfx5r/z0KEGvnQdvvP04XN7hi7+MYcAUJ/Ob7+JbO44YsC74NEVS65KNkEqeipZz1C0eyF" +
        "iyOVKQ5EugOPU7OM16S1EQ5keLmyjUhXkcYrKGWQ5MaEaPt0MMo1r8/8be4BsM9BIYYH5dRnAoGBAJ+jg05no4dHbADWkFHWCYf8" +
        "wmQ5q2dSwXCN2Q6QVQol3mboNv3vGHgFD0F8NNeTWrdQIKglCG3r0uedotWutrerAUChFpu+ni0HXFPHMztO/8/2whzBfukC9lNh" +
        "mV+ZciP+NHMmqqNZ2sMwFo9k9m2NAXkCLZs4HpdfDrKK/WuXAoGBAMwpRF94w7pJUaAdW4aO4l4mSVneGOeLrNQwnJGebGHeLSNW" +
        "ruTRvr5NnDfCSnCBSbdoJwsVkU+JLdOgT78I8mtY/913rwBrGN43yLgkbmFFarIEufrWpsVt9KM91NLZnVVgZQGia2yeIdP7j93H" +
        "r84HURL/9xoD8WsDeHRqG/tx";

    [Theory]
    [InlineData(KeyRef.Sig)]
    [InlineData(KeyRef.Dec)]
    [InlineData(KeyRef.Aut)]
    public async Task Rsa2048_OnFirmware5_SetsStandardAttributesThenImportsEPQIntoTheRequestedSlot(KeyRef keyRef)
    {
        using var rsa = RSA.Create(2048);
        using var key = RSAPrivateKey.CreateFromParameters(rsa.ExportParameters(includePrivateParameters: true));
        var connection = CreateConnection(afterAttributeWrite: true, importChunks: 2);
        await using var session = await OpenPgpSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.PutKeyAsync(keyRef, key, TestContext.Current.CancellationToken);

        byte[] attributes = Assert.Single(connection.TransmittedCommands, c => c[1] == InsPutData);
        Assert.Equal((byte)keyRef.AlgorithmAttributesDo(), attributes[3]);
        Assert.Equal(RsaAttributes.Create(RsaSize.Rsa2048).ToBytes(), ApduData(attributes).ToArray());

        var (crt, headers, values) = ParseImport(connection);
        Assert.Equal(ExpectedCrt(keyRef), crt);
        Assert.Equal([0x91, 0x03, 0x92, 0x81, 0x80, 0x93, 0x81, 0x80], headers);
        Assert.Equal(3 + 128 + 128, values.Length);
        Assert.Equal([0x01, 0x00, 0x01], values[..3]);
        Assert.Equal(PadLeft(rsa.ExportParameters(true).P!, 128), values[3..131]);
        Assert.Equal(PadLeft(rsa.ExportParameters(true).Q!, 128), values[131..]);
    }

    [Fact]
    public async Task Rsa_WithExponentOtherThan65537_IsRejectedBeforeAnythingIsSent()
    {
        using var key = RSAPrivateKey.CreateFromPkcs8(Convert.FromBase64String(RsaExponent3Pkcs8Base64));
        var connection = CreateConnection(afterAttributeWrite: true);
        await using var session = await OpenPgpSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);
        int before = connection.TransmittedCommands.Count;

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            session.PutKeyAsync(KeyRef.Sig, key, TestContext.Current.CancellationToken));

        Assert.Equal(before, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(KeyRef.Sig, 0x13)] // ECDSA
    [InlineData(KeyRef.Aut, 0x13)]
    [InlineData(KeyRef.Dec, 0x12)] // ECDH: the slot decides for NIST curves
    public async Task EcP256_SetsCurveAttributesForTheSlotAndImportsThePaddedScalar(KeyRef keyRef, byte algorithmId)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var key = ECPrivateKey.CreateFromParameters(ecdsa.ExportParameters(includePrivateParameters: true));
        var connection = CreateConnection(afterAttributeWrite: true);
        await using var session = await OpenPgpSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.PutKeyAsync(keyRef, key, TestContext.Current.CancellationToken);

        byte[] attributes = Assert.Single(connection.TransmittedCommands, c => c[1] == InsPutData);
        Assert.Equal(algorithmId, attributes[5]);
        Assert.Equal(EcAttributes.Create(keyRef, CurveOid.Secp256R1).ToBytes(), ApduData(attributes).ToArray());

        var (crt, headers, values) = ParseImport(connection);
        Assert.Equal(ExpectedCrt(keyRef), crt);
        Assert.Equal([0x92, 0x20], headers);
        Assert.Equal(PadLeft(ecdsa.ExportParameters(true).D!, 32), values);
    }

    [Fact]
    public async Task Ed25519_SetsEddsaAttributesAndImportsTheRawScalar()
    {
        byte[] scalar = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        using var key = Curve25519PrivateKey.CreateFromValue(scalar, KeyType.Ed25519);
        var connection = CreateConnection(afterAttributeWrite: true);
        await using var session = await OpenPgpSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.PutKeyAsync(KeyRef.Sig, key, TestContext.Current.CancellationToken);

        byte[] attributes = Assert.Single(connection.TransmittedCommands, c => c[1] == InsPutData);
        Assert.Equal(0x16, attributes[5]);
        Assert.Equal(EcAttributes.Create(KeyRef.Sig, CurveOid.Ed25519).ToBytes(), ApduData(attributes).ToArray());

        var (crt, headers, values) = ParseImport(connection);
        Assert.Equal(CrtSig, crt);
        Assert.Equal([0x92, 0x20], headers);
        Assert.Equal(scalar, values);
    }

    /// <summary>The card takes the X25519 scalar big-endian; RFC 7748 keys are little-endian (ykman reverses too).</summary>
    [Fact]
    public async Task X25519_SetsEcdhAttributesAndImportsTheScalarByteReversed()
    {
        byte[] scalar = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        using var key = Curve25519PrivateKey.CreateFromValue(scalar, KeyType.X25519);
        var connection = CreateConnection(afterAttributeWrite: true);
        await using var session = await OpenPgpSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.PutKeyAsync(KeyRef.Dec, key, TestContext.Current.CancellationToken);

        byte[] attributes = Assert.Single(connection.TransmittedCommands, c => c[1] == InsPutData);
        Assert.Equal(0x12, attributes[5]);
        Assert.Equal(EcAttributes.Create(KeyRef.Dec, CurveOid.X25519).ToBytes(), ApduData(attributes).ToArray());

        var (crt, headers, values) = ParseImport(connection);
        Assert.Equal(CrtDec, crt);
        Assert.Equal([0x92, 0x20], headers);
        Assert.Equal(scalar.Reverse().ToArray(), values);
    }

    [Fact]
    public async Task WhenAttributesAreNotChangeable_Rsa2048IsImportedWithoutAnAttributeWrite()
    {
        using var rsa = RSA.Create(2048);
        using var key = RSAPrivateKey.CreateFromParameters(rsa.ExportParameters(includePrivateParameters: true));
        var connection = CreateConnection(afterAttributeWrite: false, importChunks: 2, capabilityFlags: NoChangeableAttributes);
        await using var session = await OpenPgpSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.PutKeyAsync(KeyRef.Sig, key, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == InsPutData);
        var (crt, _, _) = ParseImport(connection);
        Assert.Equal(CrtSig, crt);
    }

    [Fact]
    public async Task WhenAttributesAreNotChangeable_OtherKeysAreRejectedBeforeAnythingIsSent()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var key = ECPrivateKey.CreateFromParameters(ecdsa.ExportParameters(includePrivateParameters: true));
        var connection = CreateConnection(afterAttributeWrite: false, capabilityFlags: NoChangeableAttributes);
        await using var session = await OpenPgpSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);
        int before = connection.TransmittedCommands.Count;

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            session.PutKeyAsync(KeyRef.Sig, key, TestContext.Current.CancellationToken));

        Assert.Equal(before, connection.TransmittedCommands.Count);
    }

    private static byte[] ExpectedCrt(KeyRef keyRef) => keyRef switch
    {
        KeyRef.Sig => CrtSig,
        KeyRef.Dec => CrtDec,
        _ => CrtAut,
    };

    /// <summary>
    ///     Returns (CRT bytes, 7F48 header bytes, 5F48 concatenated values) of the PUT DATA ODD, reassembled
    ///     from its command-chained APDUs.
    /// </summary>
    private static (byte[] Crt, byte[] Headers, byte[] Values) ParseImport(RecordingSmartCardConnection connection)
    {
        var imports = connection.TransmittedCommands.Where(c => c[1] == InsPutDataOdd).ToList();
        Assert.NotEmpty(imports);
        Assert.All(imports, import => Assert.Equal([0x3F, 0xFF], import[2..4]));
        byte[] data = [.. imports.SelectMany(import => ApduData(import).ToArray())];
        using var outer = Tlv.Create(data);
        Assert.Equal(0x4D, outer.Tag);
        var inner = outer.Value.Span;
        byte[] crt = inner[..2].ToArray();
        var dict = TlvHelper.DecodeDictionary(inner[2..]);
        return (crt, dict[0x7F48].ToArray(), dict[0x5F48].ToArray());
    }

    private static ReadOnlyMemory<byte> ApduData(byte[] apdu) =>
        apdu[4] == 0x00 ? apdu.AsMemory(7, (apdu[5] << 8) | apdu[6]) : apdu.AsMemory(5, apdu[4]);

    private static byte[] PadLeft(byte[] value, int length)
    {
        int skip = value.Length - length;
        if (skip >= 0) return value[skip..];
        return [.. new byte[-skip], .. value];
    }

    /// <summary>SELECT, version, application data; then one OK per attribute write and per import chunk; then the refresh.</summary>
    private static RecordingSmartCardConnection CreateConnection(bool afterAttributeWrite, int importChunks = 1, byte capabilityFlags = 0x75)
    {
        byte[] appData = ApplicationData(capabilityFlags);
        List<byte[]> responses = [[0x90, 0x00], [0x05, 0x08, 0x00, 0x90, 0x00], appData];
        if (afterAttributeWrite) responses.Add([0x90, 0x00]);
        for (int i = 0; i < importChunks; i++) responses.Add([0x90, 0x00]);
        responses.Add(appData);
        return new RecordingSmartCardConnection([.. responses]);
    }

    private static byte[] ApplicationData(byte capabilityFlags)
    {
        using var aid = new Tlv(0x4F, [0xD2, 0x76, 0, 1, 0x24, 1, 3, 4, 0, 6, 0x12, 0x34, 0x56, 0x78]);
        using var capabilities = new Tlv(0xC0, [capabilityFlags, 0, 0, 0xFF, 4, 0x80, 0, 0xFF, 0, 0]);
        byte[] rsa = [1, 8, 0, 0, 0x11, 0];
        using var sig = new Tlv(0xC1, rsa);
        using var dec = new Tlv(0xC2, rsa);
        using var aut = new Tlv(0xC3, rsa);
        using var pw = new Tlv(0xC4, [0, 0x7F, 0x7F, 0x7F, 3, 0, 3]);
        using var fingerprints = new Tlv(0xC5, new byte[60]);
        using var caFingerprints = new Tlv(0xC6, new byte[60]);
        using var generationTimes = new Tlv(0xCD, new byte[12]);
        using var discretionary = new Tlv(0x73,
            [.. capabilities.AsSpan(), .. sig.AsSpan(), .. dec.AsSpan(), .. aut.AsSpan(),
                .. pw.AsSpan(), .. fingerprints.AsSpan(), .. caFingerprints.AsSpan(), .. generationTimes.AsSpan()]);
        using var outer = new Tlv(0x6E, [.. aid.AsSpan(), .. discretionary.AsSpan()]);
        return [.. outer.AsSpan(), 0x90, 0x00];
    }
}