using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Xunit.Abstractions;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public sealed class PivCredentialPromptTests(ITestOutputHelper output)
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task CredentialPrompt_OnceAndAlways_VerifiesSignaturesAndPinState(YubiKeyTestState state)
    {
        await using var physicalConnection = await state.Device.ConnectAsync<ISmartCardConnection>();
        using var connection = new StatusRecordingConnection(physicalConnection);
        await using (var reset = await PivSession.CreateAsync(connection))
            await reset.ResetAsync();

        var prompt = new DefaultCredentialPrompt();
        Exception? primaryFailure = null;
        try
        {
            await using var session = await PivSession.CreateAsync(connection,
                new SessionCreationOptions { CredentialPrompt = prompt });
            IPublicKey once = await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256,
                new PivKeyCreationOptions { PinPolicy = PivPinPolicy.Once, TouchPolicy = PivTouchPolicy.Never });
            IPublicKey always = await session.GenerateKeyAsync(PivSlot.Signature, PivAlgorithm.EccP256,
                new PivKeyCreationOptions { PinPolicy = PivPinPolicy.Always, TouchPolicy = PivTouchPolicy.Never });
            Assert.Equal(2, prompt.ManagementRequests); // Wrong host key was rejected at challenge response.
            Assert.Equal((short)0x6982, connection.RejectedChallengeStatus);
            Assert.Null(connection.RejectedWitnessStatus);
            output.WriteLine($"Rejected management host challenge SW={connection.RejectedChallengeStatus:X4}; witness rejection={connection.RejectedWitnessStatus?.ToString("X4") ?? "none"}");

            var digest = SHA256.HashData("prompted signing"u8);
            await VerifyAsync(session, once, PivSlot.Authentication, digest);
            Assert.Equal(2, prompt.PinRequests); // Wrong PIN once, then the accepted PIN.
            Assert.Equal(2, prompt.RetryPinCount);
            Assert.Equal(3, (await session.GetPinMetadataAsync()).RetriesRemaining);

            // Once authorization remains reusable in the same session.
            await VerifyAsync(session, once, PivSlot.Authentication, digest);
            Assert.Equal(2, prompt.PinRequests);

            ReadOnlyMemory<byte> priorStatus = await connection.TransmitAndReceiveAsync(new byte[] { 0, 0x20, 0, 0x80, 0 });
            output.WriteLine($"Empty VERIFY after Once: {Convert.ToHexString(priorStatus.Span)}");
            Assert.Equal(new byte[] { 0x90, 0x00 }, priorStatus.ToArray());

            // The status query says verified after Once, but that state cannot authorize
            // an Always-protected signature: it needs a fresh VERIFY even here.
            await VerifyAsync(session, always, PivSlot.Signature, digest);
            Assert.Equal(3, prompt.PinRequests);

            // Match the SDK's short APDU formatter: case 2, empty data plus Le=00.
            // The card can report 9000 even after an Always signature. That status
            // cannot authorize another Always signature: the next two independent
            // operations must still acquire fresh verification.
            ReadOnlyMemory<byte> status = await connection.TransmitAndReceiveAsync(new byte[] { 0, 0x20, 0, 0x80, 0 });
            output.WriteLine($"Empty VERIFY after first Always: {Convert.ToHexString(status.Span)}");
            Assert.Equal(new byte[] { 0x90, 0x00 }, status.ToArray());
            await VerifyAsync(session, always, PivSlot.Signature, digest);
            Assert.Equal(4, prompt.PinRequests);
            status = await connection.TransmitAndReceiveAsync(new byte[] { 0, 0x20, 0, 0x80, 0 });
            output.WriteLine($"Empty VERIFY after second Always: {Convert.ToHexString(status.Span)}");
            Assert.Equal(new byte[] { 0x90, 0x00 }, status.ToArray());
            await VerifyAsync(session, always, PivSlot.Signature, digest);
            Assert.Equal(5, prompt.PinRequests);
            Assert.Equal(3, (await session.GetPinMetadataAsync()).RetriesRemaining);
        }
        catch (Exception error)
        {
            primaryFailure = error;
            throw;
        }
        finally
        {
            try
            {
                await using var restore = await PivSession.CreateAsync(connection);
                await restore.ResetAsync();
                PivPinMetadata restored = await restore.GetPinMetadataAsync();
                Assert.True(restored.IsDefault);
                Assert.Equal(3, restored.RetriesRemaining);
                output.WriteLine($"PIV restoration: default PIN metadata={restored.IsDefault}, retries={restored.RetriesRemaining}/{restored.TotalRetries}");
            }
            catch (Exception restorationFailure) when (primaryFailure is not null)
            {
                output.WriteLine($"PIV restoration failed while preserving {primaryFailure.GetType().Name}: {restorationFailure}");
            }
        }
    }

    private sealed class StatusRecordingConnection(ISmartCardConnection inner) : ISmartCardConnection
    {
        public short? RejectedChallengeStatus { get; private set; }
        public short? RejectedWitnessStatus { get; private set; }
        public Transport Transport => inner.Transport;
        public ConnectionType Type => inner.Type;
        public bool SupportsExtendedApdu() => inner.SupportsExtendedApdu();
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            inner.BeginTransaction(cancellationToken);

        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            ReadOnlyMemory<byte> response = await inner.TransmitAndReceiveAsync(command, cancellationToken);
            ReadOnlySpan<byte> raw = response.Span;
            if (command.Span.Length > 9 && command.Span[1] == 0x87 && command.Span[3] == 0x9B && raw.Length >= 2)
            {
                short status = (short)((raw[^2] << 8) | raw[^1]);
                if (status != unchecked((short)0x9000))
                {
                    if (command.Span[8] == 0)
                        RejectedWitnessStatus = status;
                    else
                        RejectedChallengeStatus = status;
                }
            }
            return response;
        }

        // The test's outer using owns the physical connection, not PIV sessions.
        public void Dispose() { }
        public ValueTask DisposeAsync() => default;
    }

    private static async Task VerifyAsync(PivSession session, IPublicKey publicKey, PivSlot slot, byte[] digest)
    {
        ReadOnlyMemory<byte> signature = await session.SignOrDecryptAsync(slot, digest);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(ecdsa.VerifyHash(digest, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
    }

    private sealed class DefaultCredentialPrompt : ICredentialPrompt
    {
        public int PinRequests { get; private set; }
        public int ManagementRequests { get; private set; }
        public int? RetryPinCount { get; private set; }

        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context, CancellationToken cancellationToken)
        {
            ReadOnlySpan<byte> key = PivSession.DefaultManagementKey;
            ReadOnlySpan<byte> pin = "123456"u8;
            IMemoryOwner<byte> owner;
            if (context.Kind == CredentialKind.ManagementKey)
            {
                ManagementRequests++;
                Assert.Equal(ManagementRequests > 1, context.IsRetry);
                if (ManagementRequests == 1)
                {
                    Span<byte> wrong = stackalloc byte[24];
                    for (int i = 0; i < wrong.Length; i++) wrong[i] = (byte)(i + 1);
                    owner = DisposableArrayPoolBuffer.CreateFromSpan(wrong);
                    CryptographicOperations.ZeroMemory(wrong);
                }
                else
                    owner = DisposableArrayPoolBuffer.CreateFromSpan(key);
            }
            else
            {
                Assert.Equal(CredentialKind.Pin, context.Kind);
                PinRequests++;
                if (PinRequests == 2)
                {
                    Assert.True(context.IsRetry);
                    RetryPinCount = context.RetriesRemaining;
                }
                owner = DisposableArrayPoolBuffer.CreateFromSpan(PinRequests == 1 ? "000000"u8 : pin);
            }
            return ValueTask.FromResult<IMemoryOwner<byte>?>(owner);
        }
    }
}
