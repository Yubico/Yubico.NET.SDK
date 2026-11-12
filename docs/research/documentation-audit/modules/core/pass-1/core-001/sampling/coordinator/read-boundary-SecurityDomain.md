PROPOSED SAMPLE — no verdict: core-001:unit:d7528501229916fe / SecurityDomain / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/SecurityDomain/src/SecurityDomainSession.cs:37–39
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>Security Domain application descriptor, with minimum firmware 5.3.0.</summary>
    public static readonly Feature FeatureSecurityDomain = new("Security Domain", 5, 3, 0);


PROPOSED SAMPLE — no verdict: core-001:unit:75d424217a4e94ef / SecurityDomain / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/SecurityDomain/src/SecurityDomainSession.cs:39–41
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>SCP03 descriptor, with minimum firmware 5.3.0.</summary>
    public static readonly Feature FeatureScp03 = new("SCP03", 5, 3, 0);


PROPOSED SAMPLE — no verdict: core-001:unit:394639d39386bc76 / SecurityDomain / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/SecurityDomain/src/SecurityDomainSession.cs:43–45
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>Key-attestation descriptor at firmware 5.7.0; this session exposes no attestation operation.</summary>
    public static readonly Feature FeatureKeyAttestation = new("Key Attestation", 5, 7, 0);


PROPOSED SAMPLE — no verdict: core-001:unit:03e44d501350ff8b / SecurityDomain / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/SecurityDomain/src/Backend/SecurityDomainBackend.cs:27–30
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public Task<ReadOnlyMemory<byte>> SelectAsync(CancellationToken cancellationToken = default) =>
        _protocol.SelectAsync(ApplicationIds.SecurityDomain, cancellationToken);


PROPOSED SAMPLE — no verdict: core-001:unit:ed3e115f81fd126c / SecurityDomain / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/SecurityDomain/CLAUDE.md:217–221
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Related Modules

- **Core.SmartCard.Scp**: Base SCP protocol implementations
- **Core.Cryptography**: ECPrivateKey, ECPublicKey, StaticKeys
- **Tests.Shared**: YubiKeyTestState, test infrastructure



PROPOSED SAMPLE — no verdict: core-001:unit:e1121aa69ba90312 / SecurityDomain / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/SecurityDomain/README.md:90–125
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Rotate the SCP03 keys

```csharp
byte[] keyMaterial = RandomNumberGenerator.GetBytes(48);
try
{
    // Escrow keyMaterial before you send it: the device will not give it back.
    using var newKeys = new StaticKeys(keyMaterial.AsSpan(0, 16), keyMaterial.AsSpan(16, 16), keyMaterial.AsSpan(32, 16));
    await session.PutKeyAsync(new KeyReference(ScpKid.SCP03, Kvn: 0x02), newKeys, replaceKvn: 0);
}
finally
{
    CryptographicOperations.ZeroMemory(keyMaterial);
}
```

Use three AES-128 keys and a custom KVN from 0x01 to 0xFE; 0xFF is reserved for the factory set. Yubico
documents three distinct cases:

- On a factory device, the first import replaces the factory set, even with `replaceKvn: 0`. There is no
  retained factory version to delete afterward.
- With custom sets already installed, `replaceKvn: 0` adds a version alongside them.
- A nonzero `replaceKvn` requests replacement of that existing version. The overwritten secrets cannot be
  recovered from the device.

Before removing any retained old set, dispose the importing session and verify a fresh session with
`Scp03KeyParameters` built from the new reference and escrowed keys. Only then delete the old set, if it is
still present. `DeleteKeyAsync` takes a filter: zero KID or KVN omits that condition, so multiple keys can
match. SCP03 component KIDs 0x01/0x02/0x03 become a **KVN-only filter**; always supply a nonzero KVN and
review all matching keys. The SDK does not reject the empty filter produced by SCP03 with KVN 0; do not
use that combination. `deleteLast: true` permits last-key deletion on the device; it is not a host safety
check. Yubico documents deletion of the last SCP03 set as restoring factory defaults, not leaving the
device protected without keys.

The SDK does not enforce every device range: integer `replaceKvn` values are currently cast to one byte.




PROPOSED SAMPLE — no verdict: core-001:unit:350a0f3bb0448eae / SecurityDomain / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/AuditV2/SecurityDomainAuditReproTests.cs:7–21
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_EmptyAllowListDoesNotIssueRestrictionRemovingStoreData()
    {
        var connection = new RecordingSmartCardConnection([0x90, 0x00], [0x90, 0x00]);
        await using var session = await SecurityDomainSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);
        int before = connection.TransmittedCommands.Count;

        await Assert.ThrowsAsync<ArgumentException>(() => session.StoreAllowListAsync(
            new KeyReference(0x11, 0x01), [], TestContext.Current.CancellationToken));

        Assert.Equal(before, connection.TransmittedCommands.Count);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6ef39b1f61d0b65d / SecurityDomain / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnection.cs:179–190
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private void AdvanceMacChain(byte[] wire)
    {
        if (_smac is null)
            throw new InvalidOperationException("SCP03 handshake state not established before a MACed command.");

        // This scenario assumes short commands with an 8-byte MAC and one-byte Le at the end.
        // Advance the chain over the preceding bytes; this is not command-MAC verification.
        var apduToMac = wire[..^9];
        _macChain = AesCmac(_smac, Concat(_macChain, apduToMac));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:1c4edc65a24bed3a / SecurityDomain / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnection.cs:193–195
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static byte[] Ok() => StatusWordBytes(SWConstants.Success);


PROPOSED SAMPLE — no verdict: core-001:unit:8ed4d3276ff806e4 / SecurityDomain / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs:116–132
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task DisposeAsync_ClearsManagedSessionState()
    {
        var connection = CreateMockConnection();
        var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(session.IsInitialized);

        await session.DisposeAsync();

        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c7e80dbb3894556d / SecurityDomain / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs:163–180
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task ListKeyInformationAsync_AfterDisposal_ThrowsObjectDisposedException()
    {
        var connection = CreateMockConnection();
        var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();
        int transmissionsBeforeCall = connection.ReceivedCalls().Count();

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.ListKeyInformationAsync(TestContext.Current.CancellationToken));

        Assert.Equal(typeof(SecurityDomainSession).FullName, exception.ObjectName);
        Assert.Equal(transmissionsBeforeCall, connection.ReceivedCalls().Count());
    }


PROPOSED SAMPLE — no verdict: core-001:unit:dcd6c17901ddf340 / SecurityDomain / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs:197–214
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task GetDataAsync_AfterDisposal_InvalidExpectedLengthThrowsObjectDisposedBeforeValidation()
    {
        var connection = new RecordingSmartCardConnection(OkResponse());
        var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();
        int transmissionsBeforeCall = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.GetDataAsync(0x66, expectedResponseLength: -1, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(typeof(SecurityDomainSession).FullName, exception.ObjectName);
        Assert.Equal(transmissionsBeforeCall, connection.TransmittedCommands.Count);
    }
