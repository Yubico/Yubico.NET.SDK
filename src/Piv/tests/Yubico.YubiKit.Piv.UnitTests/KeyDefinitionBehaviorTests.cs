using System.Reflection;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests;

public class KeyDefinitionBehaviorTests
{
    [Theory]
    [InlineData(KeyType.TripleDES, 0x03, 24)]
    [InlineData(KeyType.AES128, 0x08, 16)]
    [InlineData(KeyType.AES192, 0x0A, 24)]
    [InlineData(KeyType.AES256, 0x0C, 32)]
    public async Task ManagementKey_UsesCanonicalSizeAndWireId_AndMetadataDecodesSameType(KeyType keyType, int algorithmId, int length)
    {
        byte[] raw = [0x01, 0x01, (byte)algorithmId, 0x02, 0x02, 0x00, 0x01, 0x05, 0x01, 0x00];
        var connection = Connection([0x90, 0x00], [.. raw, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);
        byte[] key = RandomNumberGenerator.GetBytes(keyType.GetKeySizeBytes());
        byte[] original = [.. key];
        try
        {
            await session.SetManagementKeyAsync(keyType, key, cancellationToken: TestContext.Current.CancellationToken);
            byte[] command = connection.TransmittedCommands[^1];
            Assert.Equal(0xFF, command[1]);
            byte[] expectedHeader = [(byte)algorithmId, 0x9B, (byte)length];
            Assert.Equal(expectedHeader, command.AsSpan(5, expectedHeader.Length).ToArray());
            Assert.True(CryptographicOperations.FixedTimeEquals(original, command.AsSpan(5 + expectedHeader.Length, length)));
            Assert.True(CryptographicOperations.FixedTimeEquals(original, key));

            var metadata = await session.GetManagementKeyMetadataAsync(TestContext.Current.CancellationToken);
            Assert.Equal(keyType, metadata.KeyType);
            Assert.Equal(algorithmId, metadata.RawAlgorithmId);
            Assert.Equal(raw, metadata.RawData.ToArray());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(original);
        }
    }

    [Theory]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP521)]
    [InlineData(KeyType.RSA2048)]
    public async Task ManagementKey_RejectsAsymmetricTypesBeforeMutation(KeyType keyType)
    {
        var connection = Connection();
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);

        await Assert.ThrowsAsync<NotSupportedException>(() => session.SetManagementKeyAsync(keyType, new byte[32], cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(3, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task UnknownManagementAlgorithm_IsInspectable_AndDoesNotBecomeAFactoryDefault()
    {
        byte[] raw = [0x01, 0x01, 0xF1, 0x02, 0x02, 0x00, 0x01, 0x05, 0x01, 0x00];
        var connection = new RecordingSmartCardConnection([0x90, 0x00], [5, 8, 0, 0x90, 0x00], [.. raw, 0x90, 0x00], [.. raw, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var metadata = await session.GetManagementKeyMetadataAsync(TestContext.Current.CancellationToken);

        Assert.Null(session.ManagementKeyType);
        Assert.Null(metadata.KeyType);
        Assert.Equal(0xF1, metadata.RawAlgorithmId);
        Assert.Equal(raw, metadata.RawData.ToArray());
        await Assert.ThrowsAsync<NotSupportedException>(() => session.AuthenticateAsync(new byte[24], TestContext.Current.CancellationToken));
        Assert.Equal(4, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    public void MetadataExtraction_UsesCanonicalCurveAndCoordinateSize(KeyType keyType)
    {
        var definition = keyType.GetKeyDefinition();
        using var expected = ECDsa.Create(ECCurve.CreateFromOid(new Oid(definition.CurveOid ?? throw new InvalidOperationException("Curve OID missing."))));
        var parameters = expected.ExportParameters(false);
        byte[] point = [0x04, .. parameters.Q.X ?? throw new InvalidOperationException(), .. parameters.Q.Y ?? throw new InvalidOperationException()];
        var metadata = new SlotMetadata(keyType, PinPolicy.Default, TouchPolicy.Default, true, (byte[])[0x86, (byte)point.Length, .. point]);

        using var actual = metadata.GetECDsaPublicKey();

        Assert.Equal(definition.LengthInBits, actual.KeySize);
        Assert.Equal(expected.ExportSubjectPublicKeyInfo(), actual.ExportSubjectPublicKeyInfo());
    }

    private static void MarkAuthenticated(PivSession session) =>
        typeof(PivSession).GetField("_isAuthenticated", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(session, true);

    private static RecordingSmartCardConnection Connection(params byte[][] responses) => new(
        [new byte[] { 0x90, 0x00 }, new byte[] { 5, 8, 0, 0x90, 0x00 },
            new byte[] { 0x01, 0x01, 0x0A, 0x02, 0x02, 0x00, 0x00, 0x05, 0x01, 0x01, 0x90, 0x00 }, .. responses]);
}