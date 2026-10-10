using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Oath.IntegrationTests.AuditV2;

public sealed class OathAuditReproTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    [Trait("Audit", "YESDK-1636")]
    public async Task YESDK1636_RawPutZeroPeriodDoesNotAbortNormalBatch(YubiKeyTestState state)
    {
        byte[] malformed = "0/audit-v2-invalid"u8.ToArray();
        byte[] normal = "audit-v2-valid"u8.ToArray();
        byte[] secret = "12345678901234567890"u8.ToArray();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = timeout.Token;
        await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>(token);
        await using (var preflight = await OathSession.CreateAsync(connection, cancellationToken: token))
        {
            var existing = await preflight.ListCredentialsAsync(token);
            Assert.DoesNotContain(existing, credential => credential.Id.Span.SequenceEqual(malformed));
            Assert.DoesNotContain(existing, credential => credential.Id.Span.SequenceEqual(normal));
        }

        try
        {
            await using (var raw = await RawSmartCardSession.CreateAsync(connection, token))
            {
                await raw.SelectAsync(ApplicationIds.Oath, token);
                await PutAsync(raw, malformed, secret, token);
                await PutAsync(raw, normal, secret, token);
            }

            await using (var session = await OathSession.CreateAsync(connection, cancellationToken: token))
            {
                var results = await session.CalculateAllAsync(1_704_067_200, token);
                Assert.Contains(results, item => item.Key.Name == "audit-v2-valid" && item.Value is not null);
            }
        }
        finally
        {
            // Raw DELETE avoids parsing the invalid period; clean up even if the batch throws.
            await using var raw = await RawSmartCardSession.CreateAsync(connection, CancellationToken.None);
            await raw.SelectAsync(ApplicationIds.Oath, CancellationToken.None);
            await DeleteAsync(raw, malformed);
            await DeleteAsync(raw, normal);
        }
    }

    private static async Task PutAsync(RawSmartCardSession raw, byte[] id, byte[] secret, CancellationToken token)
    {
        byte[] payload = [0x71, (byte)id.Length, .. id, 0x73, (byte)(secret.Length + 2),
            0x21, 0x06, .. secret]; // YKOATH type/alg: 0x20 TOTP | 0x01 HMAC-SHA1
        await raw.TransmitAndReceiveAsync(new ApduCommand(0, 0x01, 0, 0, payload), cancellationToken: token);
    }

    private static async Task DeleteAsync(RawSmartCardSession raw, byte[] id)
    {
        byte[] payload = [0x71, (byte)id.Length, .. id];
        await raw.TransmitAndReceiveAsync(new ApduCommand(0, 0x02, 0, 0, payload),
            throwOnError: false, cancellationToken: CancellationToken.None);
    }
}
