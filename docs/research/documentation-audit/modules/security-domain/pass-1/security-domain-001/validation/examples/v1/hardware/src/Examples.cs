using Xunit;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Devices;

namespace AuditSd001.Examples;

public sealed class HardwareExamples
{
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
public async Task MyTest(YubiKeyTestState state)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100));
    CancellationToken ct = timeout.Token;
    using var parameters = Scp03KeyParameters.Default;
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: true,
        async session =>
        {
            var keyInfo = await session.ListKeyInformationAsync(ct);
            Assert.NotEmpty(keyInfo);
        },
        scpKeyParams: parameters,
        cancellationToken: ct);
}
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
public async Task ListAfterReset_ContainsFactoryVersion(YubiKeyTestState state)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100));
    CancellationToken ct = timeout.Token;
    using var parameters = Scp03KeyParameters.Default;
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: true,
        async session =>
        {
            var keys = await session.ListKeyInformationAsync(ct);
            Assert.Contains(keys, entry => entry.KeyReference.Kvn == 0xFF);
        },
        scpKeyParams: parameters,
        cancellationToken: ct);
}
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.2")]
public async Task GeneratedKey_IsListedInNextSession(YubiKeyTestState state)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100));
    CancellationToken ct = timeout.Token;
    using var parameters = Scp03KeyParameters.Default;
    var keyRef = new KeyReference(ScpKid.SCP11b, 0x03);
    try
    {
        await state.WithSecurityDomainSessionAsync(
            resetBeforeUse: true,
            async session => { await session.GenerateKeyAsync(keyRef, cancellationToken: ct); },
            scpKeyParams: parameters,
            cancellationToken: ct);

        await state.WithSecurityDomainSessionAsync(
            resetBeforeUse: false,
            async session =>
            {
                var keys = await session.ListKeyInformationAsync(ct);
                Assert.Contains(keys, entry => entry.KeyReference.Kid == keyRef.Kid &&
                    entry.KeyReference.Kvn == keyRef.Kvn);
            },
            cancellationToken: ct);
    }
    finally
    {
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await state.ResetSecurityDomainAsync(cleanup.Token);
    }
}
}
