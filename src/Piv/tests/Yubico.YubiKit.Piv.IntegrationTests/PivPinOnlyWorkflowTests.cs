using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

[Trait(TestCategories.Category, TestCategories.RequiresHardware)]
public class PivPinOnlyWorkflowTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task PinProtectedMode_ReopensRecoversAndDisablesWithDefaultManagementKey(YubiKeyTestState state)
    {
        byte[] defaultKey = PivSession.DefaultManagementKey.ToArray();
        byte[] activeKey = RandomNumberGenerator.GetBytes(24);
        byte[] pin = "123456"u8.ToArray();
        try
        {
            await using (var session = await state.Device.CreatePivSessionAsync())
            {
                await session.ResetAsync();
                await session.AuthenticateAsync(defaultKey);
                await session.SetManagementKeyAsync(session.ManagementKeyType, activeKey);

                await session.SetPinOnlyModeAsync(PivPinOnlyMode.PinProtected, pin, activeKey);

                Assert.Equal(PivPinOnlyMode.PinProtected, await session.GetPinOnlyModeAsync());
                Assert.Equal(0, (await session.GetPukMetadataAsync()).RetriesRemaining);
            }

            await using (var recovered = await state.Device.CreatePivSessionAsync())
            {
                Assert.False(recovered.IsManagementKeyAuthenticated);
                Assert.Equal(PivPinOnlyMode.PinProtected, await recovered.RecoverPinOnlyModeAsync(pin));
                Assert.True(recovered.IsManagementKeyAuthenticated);

                await recovered.SetPinOnlyModeAsync(PivPinOnlyMode.None, pin);

                Assert.True(recovered.IsManagementKeyAuthenticated);
                Assert.Equal(PivPinOnlyMode.None, await recovered.GetPinOnlyModeAsync());
                Assert.True((await recovered.GetObjectAsync(PivDataObject.PrintedInformation)).IsEmpty);
                Assert.True((await recovered.GetObjectAsync(PivDataObject.AdminData)).IsEmpty);
                await recovered.AuthenticateAsync(defaultKey);
                Assert.True((await recovered.GetManagementKeyMetadataAsync()).IsDefault);
            }
        }
        finally
        {
            try
            {
                // Persistent setup must be reset even after an assertion or operation fails.
                // Cleanup has an independent finite budget rather than borrowing a cancelled test token.
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await using var session = await state.Device.CreatePivSessionAsync(cancellationToken: cleanup.Token);
                await session.ResetAsync(cleanup.Token);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(defaultKey);
                CryptographicOperations.ZeroMemory(activeKey);
                CryptographicOperations.ZeroMemory(pin);
            }
        }
    }
}