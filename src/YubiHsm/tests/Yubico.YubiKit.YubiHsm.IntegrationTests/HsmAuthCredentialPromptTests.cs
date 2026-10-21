// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.YubiHsm.IntegrationTests;

/// <summary>Applet-only known-answer derivation and rejected-command retry safety, not connector authentication.</summary>
public class HsmAuthCredentialPromptTests
{
    private const string Label = "prompt-retry-vector";

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task WrongThenCorrect_SameContextDerivesKnownKeysAndRejectedDeletePreservesCredential(YubiKeyTestState state)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await state.WithConnectionAsync(async inner =>
        {
            var recorder = new RetryRecordingConnection(inner);
            var prompt = new VectorPrompt();
            await using var session = await HsmAuthSession.CreateAsync(recorder,
                new SessionCreationOptions { CredentialPrompt = prompt }, cancellation.Token);
            byte[] keyEnc = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
            byte[] keyMac = Enumerable.Range(16, 16).Select(i => (byte)i).ToArray();
            byte[] context = Enumerable.Range(32, 16).Select(i => (byte)i).ToArray();
            byte[] managementKey = new byte[16];
            byte[] password = "password"u8.ToArray();
            byte[] cardCryptogram = Convert.FromHexString("58218FA005323443");
            // Independent OpenSSL CMAC vectors: 11 zero bytes || type || 00 || 0080 || 01 || context.
            // Types 04/06/07 use K-ENC/K-MAC/K-MAC respectively. No SDK derivation helper is used.
            byte[] expected = Convert.FromHexString("92BB81CA0C0D91B8E2F578019AC31033934E1A129849A5BB296A0C5185FF0988E4B370EDDF13ECE23CA316E6934B46BD");
            Exception? primary = null;
            try
            {
                await session.ResetAsync(cancellation.Token);
                await session.PutCredentialSymmetricAsync(managementKey, Label, keyEnc, keyMac, password,
                    cancellationToken: cancellation.Token);
                using var keys = await session.CalculateSessionKeysSymmetricWithPromptAsync(Label, context, cardCryptogram, cancellation.Token);
                Assert.True(CryptographicOperations.FixedTimeEquals(expected.AsSpan(0, 16), keys.SEnc));
                Assert.True(CryptographicOperations.FixedTimeEquals(expected.AsSpan(16, 16), keys.SMac));
                Assert.True(CryptographicOperations.FixedTimeEquals(expected.AsSpan(32, 16), keys.SRmac));
                Assert.Equal(2, prompt.PasswordRequests);
                Assert.Equal(7, prompt.PasswordRetryCount);
                Assert.Equal(2, recorder.Calculations);
                Assert.True(recorder.PeerInputsUnchanged);
                Assert.Equal((short)0x63C7, recorder.CalculationRejection);
                Assert.Equal(8, Assert.Single(await session.ListCredentialsAsync(cancellation.Token)).RetriesRemaining);

                await session.DeleteCredentialWithPromptAsync(Label, cancellation.Token);
                Assert.Equal(2, prompt.ManagementRequests);
                Assert.Equal(7, prompt.ManagementRetryCount);
                Assert.Equal((short)0x63C7, recorder.DeleteRejection);
                Assert.True(recorder.RejectedDeletePreservedCredential);
                Assert.Equal(2, recorder.Deletions);
                Assert.Empty(await session.ListCredentialsAsync(cancellation.Token));
                Assert.Equal(8, await session.GetManagementKeyRetriesAsync(cancellation.Token));
            }
            catch (Exception error) { primary = error; throw; }
            finally
            {
                try
                {
                    await session.ResetAsync(CancellationToken.None);
                    Assert.Empty(await session.ListCredentialsAsync(CancellationToken.None));
                    Assert.Equal(8, await session.GetManagementKeyRetriesAsync(CancellationToken.None));
                }
                catch (Exception) when (primary is not null)
                {
                    // Preserve the primary failure but make failed fixture restoration visible without secrets.
                    Console.Error.WriteLine("YubiHSM Auth fixture restoration failed after a primary test failure.");
                }
                finally
                {
                    byte[][] buffers = [keyEnc, keyMac, context, managementKey, password, cardCryptogram, expected];
                    foreach (var bytes in buffers)
                        CryptographicOperations.ZeroMemory(bytes);
                    recorder.Clear();
                }
            }
        }, cancellation.Token);
    }

    private sealed class VectorPrompt : ICredentialPrompt
    {
        public int PasswordRequests { get; private set; }
        public int ManagementRequests { get; private set; }
        public int? PasswordRetryCount { get; private set; }
        public int? ManagementRetryCount { get; private set; }
        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context, CancellationToken token)
        {
            Assert.Equal(Label, Assert.IsType<HsmAuthCredentialPromptContext>(context).CredentialLabel);
            IMemoryOwner<byte> owner;
            if (context.Kind == CredentialKind.Password)
            {
                PasswordRequests++;
                Assert.Equal(PasswordRequests > 1, context.IsRetry);
                if (context.IsRetry) PasswordRetryCount = context.RetriesRemaining;
                owner = DisposableArrayPoolBuffer.CreateFromSpan(context.IsRetry ? "password"u8 : "wrong"u8);
            }
            else
            {
                Assert.Equal(CredentialKind.ManagementKey, context.Kind);
                ManagementRequests++;
                Assert.Equal(ManagementRequests > 1, context.IsRetry);
                if (context.IsRetry) ManagementRetryCount = context.RetriesRemaining;
                Span<byte> key = stackalloc byte[16];
                key.Fill(context.IsRetry ? (byte)0 : (byte)1);
                try { owner = DisposableArrayPoolBuffer.CreateFromSpan(key); }
                finally { CryptographicOperations.ZeroMemory(key); }
            }
            return ValueTask.FromResult<IMemoryOwner<byte>?>(owner);
        }
    }

    private sealed class RetryRecordingConnection(ISmartCardConnection inner) : ISmartCardConnection
    {
        private byte[]? _peerInputs;
        public int Calculations { get; private set; }
        public int Deletions { get; private set; }
        public short? CalculationRejection { get; private set; }
        public short? DeleteRejection { get; private set; }
        public bool PeerInputsUnchanged { get; private set; } = true;
        public bool RejectedDeletePreservedCredential { get; private set; }
        public Transport Transport => inner.Transport;
        public ConnectionType Type => inner.Type;
        public bool SupportsExtendedApdu() => inner.SupportsExtendedApdu();
        public IDisposable BeginTransaction(CancellationToken token = default) => inner.BeginTransaction(token);
        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken token = default)
        {
            byte instruction = command.Span[1];
            if (instruction == 3)
            {
                Calculations++;
                // Retain only peer fields, never password bytes; accept either formatter's framing.
                bool extended = command.Span[4] == 0 && command.Length > 7;
                int length = extended ? (command.Span[5] << 8) | command.Span[6] : command.Span[4];
                using var fields = TlvHelper.DecodeList(command.Span.Slice(extended ? 7 : 5, length));
                byte[] peer = new byte[24];
                int offset = 0;
                foreach (var field in fields)
                {
                    if (field.Tag is not (0x77 or 0x78)) continue;
                    field.Value.Span.CopyTo(peer.AsSpan(offset));
                    offset += field.Value.Length;
                }
                Assert.Equal(24, offset);
                if (_peerInputs is null) _peerInputs = peer;
                else { PeerInputsUnchanged &= CryptographicOperations.FixedTimeEquals(_peerInputs, peer); CryptographicOperations.ZeroMemory(peer); }
            }
            if (instruction == 2) Deletions++;
            var response = await inner.TransmitAndReceiveAsync(command, token);
            short status = (short)((response.Span[^2] << 8) | response.Span[^1]);
            if ((status & 0xFFF0) == 0x63C0)
            {
                if (instruction == 3) CalculationRejection = status;
                if (instruction == 2)
                {
                    DeleteRejection = status;
                    // Same physical transaction: observe LIST after rejection, before returning to the prompt loop.
                    var snapshot = await inner.TransmitAndReceiveAsync((byte[])[0, 5, 0, 0, 0], token);
                    Assert.Equal((byte)0x90, snapshot.Span[^2]);
                    Assert.Equal((byte)0, snapshot.Span[^1]);
                    using var entries = TlvHelper.DecodeList(snapshot.Span[..^2]);
                    RejectedDeletePreservedCredential = entries.Any(t => t.Tag == 0x72 &&
                        System.Text.Encoding.UTF8.GetString(t.Value.Span[2..^1]) == Label);
                }
            }
            return response;
        }
        public void Clear() { if (_peerInputs is not null) CryptographicOperations.ZeroMemory(_peerInputs); }
        public void Dispose() { }
        public ValueTask DisposeAsync() => default;
    }
}