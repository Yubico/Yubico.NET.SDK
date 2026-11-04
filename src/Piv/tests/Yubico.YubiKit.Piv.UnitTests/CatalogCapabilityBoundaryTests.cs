using System.Reflection;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests;

public class CatalogCapabilityBoundaryTests
{
    [Theory]
    [InlineData(KeyType.Secp256k1)]
    [InlineData(KeyType.BrainpoolP256r1)]
    [InlineData(KeyType.BrainpoolP384r1)]
    [InlineData(KeyType.BrainpoolP512r1)]
    public async Task ModeledCurve_DoesNotAdmitPivGenerationImportOrAgreement(KeyType keyType)
    {
        var catalogKey = new CatalogKey(keyType);
        Assert.True(catalogKey.KeyInfo.IsShortWeierstrass);
        Assert.NotEmpty(CryptographicDefinitions.Algorithms.FindForKey(catalogKey.KeyInfo));
        var connection = new RecordingSmartCardConnection(
            [0x90, 0x00], [5, 8, 0, 0x90, 0x00],
            [0x01, 0x01, 0x0A, 0x02, 0x02, 0x00, 0x00, 0x05, 0x01, 0x01, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        typeof(PivSession).GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, true);
        int initializationCommands = connection.TransmittedCommands.Count;

        await Assert.ThrowsAsync<NotSupportedException>(() => session.GenerateKeyAsync(
            Slot.Authentication, keyType, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(() => session.ImportKeyAsync(
            Slot.Authentication, catalogKey, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(() => session.CalculateSecretAsync(
            Slot.KeyManagement, catalogKey, TestContext.Current.CancellationToken));

        Assert.Equal(initializationCommands, connection.TransmittedCommands.Count);
        Assert.Equal(0, catalogKey.ExportCalls);
        Assert.Equal(0, catalogKey.ClearCalls);
    }

    // Catalog-backed carriers avoid a host-provider dependency: protocol policy must reject these
    // identities before requesting either public/private encoding or touching caller-owned material.
    private sealed class CatalogKey(KeyType keyType) : IPrivateKey, IPublicKey
    {
        public KeyInfo KeyInfo => keyType.GetKeyInfo();
        public KeyType KeyType => keyType;
        public int ExportCalls { get; private set; }
        public int ClearCalls { get; private set; }
        public byte[] ExportPkcs8PrivateKey() => UnexpectedExport();
        public byte[] ExportSubjectPublicKeyInfo() => UnexpectedExport();
        public void Clear() => ClearCalls++;

        private byte[] UnexpectedExport()
        {
            ExportCalls++;
            throw new InvalidOperationException("Unsupported PIV curves must be rejected before encoding.");
        }
    }
}