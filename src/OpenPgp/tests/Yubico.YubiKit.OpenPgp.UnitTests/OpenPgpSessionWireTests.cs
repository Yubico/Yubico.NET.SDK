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
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public sealed class OpenPgpSessionWireTests
{
    [Fact]
    public async Task CreateAsync_AppletProbeFailure_DoesNotDisposeTheBorrowedConnection()
    {
        var connection = new RecordingSmartCardConnection();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            OpenPgpSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        // Borrowed: the session did not create this connection, so disposal is the caller's.
        // Upstream asserted 1 here because its protocols disposed the connection; this branch
        // deliberately removed that (see ProtocolConnectionOwnershipTests).
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task CreateAsync_TransmitsSelectVersionAndApplicationRelatedData()
    {
        var connection = CreateInitializedConnection();

        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(connection.TransmittedCommands.Count >= 3);
        Assert.Contains(connection.TransmittedCommands, command =>
            command.Length >= 5 &&
            command[1] == 0xA4 && // SELECT
            command[2] == 0x04 &&
            command[3] == 0x00 &&
            CommandData(command).SequenceEqual(ApplicationIds.OpenPgp.Span));
        Assert.Contains(connection.TransmittedCommands, command =>
            command.Length >= 4 &&
            command[1] == 0xF1); // GET VERSION
        Assert.Contains(connection.TransmittedCommands, command =>
            command.Length >= 4 &&
            command[1] == 0xCA && // GET DATA
            command[2] == 0x00 &&
            command[3] == 0x6E); // Application Related Data DO
    }

    [Fact]
    public async Task GetDataAsync_TransmitsGetDataForRequestedObject()
    {
        var connection = CreateInitializedConnection(PwStatusResponse());
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var data = await session.GetDataAsync(
            DataObject.PwStatusBytes,
            TestContext.Current.CancellationToken);

        Assert.Equal([0x00, 0x7F, 0x7F, 0x7F, 0x03, 0x00, 0x03], data.ToArray());
        var command = LastCommand(connection);
        Assert.Equal(0x00, command[0]);
        Assert.Equal(0xCA, command[1]); // GET DATA
        Assert.Equal(0x00, command[2]);
        Assert.Equal(0xC4, command[3]); // PW Status Bytes DO
    }

    [Fact]
    public async Task SetSignaturePinPolicyAsync_TransmitsPutDataForPwStatusBytes()
    {
        var connection = CreateInitializedConnection(OkResponse());
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.SetSignaturePinPolicyAsync(
            PinPolicy.Once,
            TestContext.Current.CancellationToken);

        var command = LastCommand(connection);
        Assert.Equal(0x00, command[0]);
        Assert.Equal(0xDA, command[1]); // PUT DATA
        Assert.Equal(0x00, command[2]);
        Assert.Equal(0xC4, command[3]); // PW Status Bytes DO
        Assert.Equal([(byte)PinPolicy.Once], CommandData(command).ToArray());
    }

    [Fact]
    public async Task GenerateKeyAsync_SetsAlgorithmAttributesBeforeGeneratingKeyPair()
    {
        var connection = CreateInitializedConnection(
            OkResponse(),
            [0x90, 0x00],
            ApplicationRelatedDataResponse());
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.GenerateKeyAsync(
            KeyRef.Sig,
            RsaAttributes.Create(RsaSize.Rsa2048),
            TestContext.Current.CancellationToken);

        byte[][] operationCommands = connection.TransmittedCommands.Skip(3).ToArray();
        Assert.Equal(3, operationCommands.Length);
        Assert.Equal(0xDA, operationCommands[0][1]);
        Assert.Equal(0xC1, operationCommands[0][3]);
        Assert.Equal(0x47, operationCommands[1][1]);
        Assert.Equal(0x80, operationCommands[1][2]);
        Assert.Equal(0xCA, operationCommands[2][1]);
        Assert.Equal(0x6E, operationCommands[2][3]);
    }

    [Theory]
    [InlineData(Uif.On, UserPresenceBasis.PolicyRequires)]
    [InlineData(Uif.Fixed, UserPresenceBasis.PolicyRequires)]
    [InlineData(Uif.Cached, UserPresenceBasis.PolicyMayRequire)]
    [InlineData(Uif.CachedFixed, UserPresenceBasis.PolicyMayRequire)]
    [InlineData(Uif.Off, null)]
    public async Task SignAsync_MapsSignatureUifToNotification(
        Uif uif,
        UserPresenceBasis? expectedBasis)
    {
        var connection = CreateInitializedConnectionWithUifs(
            sig: uif,
            dec: null,
            aut: null,
            att: null,
            CryptoResponse());
        var prompt = new RecordingUserPresencePrompt(connection);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);
        int commandsBeforeOperation = connection.TransmittedCommands.Count;

        _ = await session.SignAsync(
            "message"u8.ToArray(),
            HashAlgorithmName.SHA256,
            TestContext.Current.CancellationToken);

        Assert.Equal(0, connection.TransmittedCommands.Count(command =>
            command[1] == 0xCA && command[2] == 0x00 && command[3] == (byte)DataObject.UifSig));

        if (expectedBasis is null)
        {
            Assert.Empty(prompt.Requested);
            Assert.Empty(prompt.Resolved);
            return;
        }

        var requested = Assert.Single(prompt.Requested);
        Assert.Equal("OpenPGP", requested.Context.Application);
        Assert.Equal("Sig", requested.Context.Scope);
        Assert.Equal(expectedBasis, requested.Context.Basis);
        Assert.Equal(commandsBeforeOperation, requested.CommandCount);

        var resolved = Assert.Single(prompt.Resolved);
        Assert.Same(requested.Context, resolved.Context);
        Assert.Equal(UserPresenceOutcome.Completed, resolved.Outcome);
        Assert.Equal(CancellationToken.None, resolved.CancellationToken);
        Assert.Equal(commandsBeforeOperation + 1, resolved.CommandCount);
    }

    [Fact]
    public async Task CryptoOperations_UseTheirRelevantKeyRefScopes()
    {
        var connection = CreateInitializedConnectionWithUifs(
            Uif.On,
            Uif.On,
            Uif.On,
            att: null,
            CryptoResponse(),
            CryptoResponse(),
            CryptoResponse());
        var prompt = new RecordingUserPresencePrompt(connection);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        _ = await session.SignAsync(
            "sign"u8.ToArray(),
            HashAlgorithmName.SHA256,
            TestContext.Current.CancellationToken);
        byte[] ciphertext = [0x01, 0x02];
        _ = await session.DecryptAsync(
            ciphertext,
            TestContext.Current.CancellationToken);
        _ = await session.AuthenticateAsync(
            "authenticate"u8.ToArray(),
            HashAlgorithmName.SHA256,
            TestContext.Current.CancellationToken);

        Assert.Equal(["Sig", "Dec", "Aut"], prompt.Requested.Select(entry => entry.Context.Scope));
        Assert.All(prompt.Requested, entry =>
        {
            Assert.Equal("OpenPGP", entry.Context.Application);
            Assert.Equal(UserPresenceBasis.PolicyRequires, entry.Context.Basis);
        });
        Assert.All(prompt.Resolved, entry => Assert.Equal(UserPresenceOutcome.Completed, entry.Outcome));
    }

    [Fact]
    public async Task SignAsync_WhenUifReadFails_NotifiesMayRequireAndStillSigns()
    {
        var connection = CreateInitializedConnection([0x6A, 0x88], CryptoResponse());
        var prompt = new RecordingUserPresencePrompt(connection);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        var signature = await session.SignAsync(
            "message"u8.ToArray(),
            HashAlgorithmName.SHA256,
            TestContext.Current.CancellationToken);

        Assert.Equal([0x01, 0x02, 0x03], signature.ToArray());
        Assert.Equal(UserPresenceBasis.PolicyMayRequire, Assert.Single(prompt.Requested).Context.Basis);
        Assert.Equal(UserPresenceOutcome.Completed, Assert.Single(prompt.Resolved).Outcome);
        Assert.Contains(connection.TransmittedCommands, command =>
            command[1] == (byte)Ins.Pso && command[2] == 0x9E && command[3] == 0x9A);
    }

    [Fact]
    public async Task SignAsync_WhenRequestCallbackFails_DoesNotTransmitCryptoApduOrResolve()
    {
        var expected = new InvalidOperationException("prompt failed");
        var connection = CreateInitializedConnectionWithUifs(
            Uif.On,
            dec: null,
            aut: null,
            att: null,
            CryptoResponse());
        var prompt = new RecordingUserPresencePrompt(connection, expected);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => session.SignAsync(
            "message"u8.ToArray(),
            HashAlgorithmName.SHA256,
            TestContext.Current.CancellationToken));

        Assert.Same(expected, exception);
        Assert.DoesNotContain(connection.TransmittedCommands, command =>
            command[1] == (byte)Ins.Pso && command[2] == 0x9E && command[3] == 0x9A);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task SignAsync_WhenCancelledAfterRequest_ResolvesAsCancelledWithoutTransmittingCryptoApdu()
    {
        using var cancellationSource = new CancellationTokenSource();
        var connection = CreateInitializedConnectionWithUifs(
            Uif.On,
            dec: null,
            aut: null,
            att: null,
            CryptoResponse());
        var prompt = new RecordingUserPresencePrompt(
            connection,
            cancelAfterRequest: cancellationSource);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.SignAsync(
            "message"u8.ToArray(),
            HashAlgorithmName.SHA256,
            cancellationSource.Token));

        Assert.Equal(UserPresenceOutcome.Cancelled, Assert.Single(prompt.Resolved).Outcome);
        Assert.Equal(CancellationToken.None, prompt.Resolved[0].CancellationToken);
        Assert.DoesNotContain(connection.TransmittedCommands, command =>
            command[1] == (byte)Ins.Pso && command[2] == 0x9E && command[3] == 0x9A);
    }

    [Fact]
    public async Task AttestKeyAsync_UsesAttestationUifAndResolvesFailure()
    {
        var connection = CreateInitializedConnectionWithUifs(
            sig: null,
            dec: null,
            aut: null,
            att: Uif.On,
            [0x6A, 0x80]);
        var prompt = new RecordingUserPresencePrompt(connection);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        _ = await Assert.ThrowsAsync<ApduException>(() =>
            session.AttestKeyAsync(KeyRef.Sig, TestContext.Current.CancellationToken));

        var requested = Assert.Single(prompt.Requested);
        Assert.Equal("Att", requested.Context.Scope);
        Assert.Equal(UserPresenceBasis.PolicyRequires, requested.Context.Basis);
        Assert.Equal(UserPresenceOutcome.Failed, Assert.Single(prompt.Resolved).Outcome);
        Assert.DoesNotContain(connection.TransmittedCommands, command =>
            command[1] == 0xCA && command[3] == (byte)DataObject.UifAtt);
        Assert.Contains(connection.TransmittedCommands, command => command[1] == (byte)Ins.GetAttestation);
    }

    [Fact]
    public async Task SetUifAsync_UpdatesPolicyUsedBySubsequentOperation()
    {
        var connection = CreateInitializedConnectionWithUifs(
            Uif.Off,
            dec: null,
            aut: null,
            att: null,
            OkResponse(),
            CryptoResponse());
        var prompt = new RecordingUserPresencePrompt(connection);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        await session.SetUifAsync(KeyRef.Sig, Uif.On, TestContext.Current.CancellationToken);
        _ = await session.SignAsync(
            "message"u8.ToArray(),
            HashAlgorithmName.SHA256,
            TestContext.Current.CancellationToken);

        Assert.Equal(UserPresenceBasis.PolicyRequires, Assert.Single(prompt.Requested).Context.Basis);
        Assert.DoesNotContain(connection.TransmittedCommands.Skip(3), command =>
            command[1] == 0xCA && command[3] == (byte)DataObject.UifSig);
    }

    [Theory]
    [InlineData(KeyMutation.Generate)]
    [InlineData(KeyMutation.Import)]
    [InlineData(KeyMutation.Delete)]
    public async Task KeyMutationRefresh_ClearsSetUifPolicyOverride(KeyMutation mutation)
    {
        byte[][] trailingResponses = mutation switch
        {
            KeyMutation.Generate =>
            [
                OkResponse(),
                OkResponse(),
                OkResponse(),
                ApplicationRelatedDataResponse(Uif.Off),
                CryptoResponse()
            ],
            KeyMutation.Import =>
            [
                OkResponse(),
                OkResponse(),
                ApplicationRelatedDataResponse(Uif.Off),
                CryptoResponse()
            ],
            KeyMutation.Delete =>
            [
                OkResponse(),
                OkResponse(),
                OkResponse(),
                ApplicationRelatedDataResponse(Uif.Off),
                CryptoResponse()
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        };
        var connection = CreateInitializedConnectionWithUifs(
            Uif.Off,
            dec: null,
            aut: null,
            att: null,
            trailingResponses);
        var prompt = new RecordingUserPresencePrompt(connection);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        await session.SetUifAsync(KeyRef.Sig, Uif.On, TestContext.Current.CancellationToken);
        byte[] exponent = [0x01];
        byte[] primeP = [0x02];
        byte[] primeQ = [0x03];
        await (mutation switch
        {
            KeyMutation.Generate => session.GenerateKeyAsync(
                KeyRef.Sig,
                RsaAttributes.Create(RsaSize.Rsa2048),
                TestContext.Current.CancellationToken),
            KeyMutation.Import => session.PutKeyAsync(
                KeyRef.Sig,
                new RsaKeyTemplate(KeyRef.Sig, exponent, primeP, primeQ),
                cancellationToken: TestContext.Current.CancellationToken),
            KeyMutation.Delete => session.DeleteKeyAsync(KeyRef.Sig, TestContext.Current.CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        });
        _ = await session.SignAsync(
            "message"u8.ToArray(),
            HashAlgorithmName.SHA256,
            TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task SignAsync_WhenResolutionFailsAfterSuccess_PropagatesResolutionException()
    {
        var expected = new InvalidOperationException("resolution failed");
        var connection = CreateInitializedConnectionWithUifs(
            Uif.On,
            dec: null,
            aut: null,
            att: null,
            CryptoResponse());
        var prompt = new RecordingUserPresencePrompt(connection, resolutionException: expected);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => session.SignAsync(
            "message"u8.ToArray(),
            HashAlgorithmName.SHA256,
            TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task SignAsync_WhenOperationAndResolutionFail_PreservesOperationException()
    {
        var connection = CreateInitializedConnectionWithUifs(
            Uif.On,
            dec: null,
            aut: null,
            att: null,
            [0x6A, 0x80]);
        var prompt = new RecordingUserPresencePrompt(
            connection,
            resolutionException: new InvalidOperationException("resolution failed"));
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        ApduException actual = await Assert.ThrowsAsync<ApduException>(() => session.SignAsync(
            "message"u8.ToArray(),
            HashAlgorithmName.SHA256,
            TestContext.Current.CancellationToken));

        Assert.Equal(unchecked((short)0x6A80), actual.SW);
        Assert.Single(prompt.Resolved);
    }

    [Fact]
    public async Task VerifyPinAsync_TransmitsVerifyWithUserPinPayload()
    {
        var connection = CreateInitializedConnection(
            // KDF DO absent: session falls back to raw PIN bytes through KdfNone.
            [0x6A, 0x82],
            OkResponse());
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.VerifyPinAsync(
            "123456"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        var command = LastCommand(connection);
        Assert.Equal(0x00, command[0]);
        Assert.Equal(0x20, command[1]); // VERIFY
        Assert.Equal(0x00, command[2]);
        Assert.Equal(0x81, command[3]); // User PIN for signing
        Assert.Equal("123456"u8.ToArray(), CommandData(command).ToArray());
    }

    [Fact]
    public async Task DisposeAsync_ZeroesCachedKdfSalt()
    {
        using var configuredKdf = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256,
            IterationCount = 32,
            SaltUser = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };
        var connection = CreateInitializedConnection(
            [.. configuredKdf.ToBytes(), 0x90, 0x00],
            OkResponse());
        var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.VerifyPinAsync(
            "123456"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        var cachedKdf = Assert.IsType<KdfIterSaltedS2k>(
            typeof(OpenPgpSession)
                .GetField("_kdf", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(session));
        ReadOnlyMemory<byte> salt = cachedKdf.SaltUser;
        Assert.Contains(salt.ToArray(), value => value != 0);

        await session.DisposeAsync();

        Assert.All(salt.ToArray(), value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task Dispose_ThrowingKdfStillRunsBaseTeardownAndDetachesConnection()
    {
        var expected = new InvalidOperationException("kdf dispose failed");
        var connection = new RecordingSmartCardConnection(
            OkResponse(),
            VersionResponse(),
            ApplicationRelatedDataResponse(),
            OkResponse(),
            OkResponse(),
            VersionResponse(),
            ApplicationRelatedDataResponse());
        var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.SetKdfAsync(
            new ThrowingKdf(expected),
            TestContext.Current.CancellationToken);

        Exception? exception = Record.Exception(session.Dispose);

        Assert.Same(expected, exception);
        await using var subsequent = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(subsequent);
    }

    [Fact]
    public async Task SetFingerprintAsync_AfterDisposal_InvalidLengthThrowsObjectDisposedBeforeValidation()
    {
        var connection = CreateInitializedConnection();
        var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();
        int transmissionsBeforeCall = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.SetFingerprintAsync(
                KeyRef.Sig,
                ReadOnlyMemory<byte>.Empty,
                TestContext.Current.CancellationToken));

        Assert.Equal(typeof(OpenPgpSession).FullName, exception.ObjectName);
        Assert.Equal(transmissionsBeforeCall, connection.TransmittedCommands.Count);
    }

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), VersionResponse(), ApplicationRelatedDataResponse(), .. trailingResponses]);

    private static RecordingSmartCardConnection CreateInitializedConnectionWithUifs(
        Uif? sig,
        Uif? dec,
        Uif? aut,
        Uif? att,
        params byte[][] trailingResponses) =>
        new([
            OkResponse(),
            VersionResponse(),
            ApplicationRelatedDataResponse(sig, dec, aut, att),
            .. trailingResponses
        ]);

    private static byte[] LastCommand(RecordingSmartCardConnection connection) =>
        connection.TransmittedCommands[^1];

    private static ReadOnlySpan<byte> CommandData(byte[] command) =>
        // Short APDU format: CLA INS P1 P2 Lc Data; the recorder reports SupportsExtendedApdu=false.
        command.AsSpan(5, command[4]);

    public enum KeyMutation
    {
        Generate,
        Import,
        Delete
    }

    private sealed class ThrowingKdf(Exception exception) : Kdf
    {
        public override int Algorithm => 0;

        public override byte[] Process(Pw pw, ReadOnlySpan<byte> pin) =>
            pin.ToArray();

        public override byte[] ToBytes() => [];

        public override void Dispose() => throw exception;
    }

    private sealed class RecordingUserPresencePrompt(
        RecordingSmartCardConnection connection,
        Exception? requestException = null,
        CancellationTokenSource? cancelAfterRequest = null,
        Exception? resolutionException = null) : IUserPresencePrompt
    {
        public List<(UserPresenceContext Context, int CommandCount)> Requested { get; } = [];

        public List<(
            UserPresenceContext Context,
            UserPresenceOutcome Outcome,
            CancellationToken CancellationToken,
            int CommandCount)> Resolved
        { get; } = [];

        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken)
        {
            Requested.Add((context, connection.TransmittedCommands.Count));
            cancelAfterRequest?.Cancel();
            return requestException is null
                ? ValueTask.CompletedTask
                : ValueTask.FromException(requestException);
        }

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolved.Add((context, outcome, cancellationToken, connection.TransmittedCommands.Count));
            return resolutionException is null
                ? ValueTask.CompletedTask
                : ValueTask.FromException(resolutionException);
        }
    }

    // SW 9000: successful APDU response with no data.
    private static byte[] OkResponse() => [0x90, 0x00];

    // OpenPGP version response: BCD 5.8.0 followed by SW 9000.
    private static byte[] VersionResponse() => [0x05, 0x08, 0x00, 0x90, 0x00];

    private static byte[] PwStatusResponse() => [0x00, 0x7F, 0x7F, 0x7F, 0x03, 0x00, 0x03, 0x90, 0x00];

    private static byte[] UifResponse(Uif uif) => [(byte)uif, (byte)GeneralFeatureManagement.Button, 0x90, 0x00];

    private static byte[] CryptoResponse() => [0x01, 0x02, 0x03, 0x90, 0x00];

    private static byte[] ApplicationRelatedDataResponse(
        Uif? sig = null,
        Uif? dec = null,
        Uif? aut = null,
        Uif? att = null) =>
        [.. BuildApplicationRelatedData(sig, dec, aut, att), 0x90, 0x00];

    private static byte[] BuildApplicationRelatedData(Uif? sig, Uif? dec, Uif? aut, Uif? att)
    {
        // AID: D276000124010304000612345678 (OpenPGP v3.4, Yubico, serial 12345678).
        byte[] aid = [0xD2, 0x76, 0x00, 0x01, 0x24, 0x01, 0x03, 0x04, 0x00, 0x06, 0x12, 0x34, 0x56, 0x78];
        byte[] historicalBytes = [0x00, 0x73, 0x00, 0x01, 0x80, 0x05, 0x90, 0x00];
        byte[] rsa2048Attributes = [0x01, 0x08, 0x00, 0x00, 0x11, 0x00];
        byte[] extendedCapabilities = [0x75, 0x00, 0x00, 0xFF, 0x04, 0x80, 0x00, 0xFF, 0x00, 0x00];
        byte[] pwStatus = [0x00, 0x7F, 0x7F, 0x7F, 0x03, 0x00, 0x03];
        var fingerprints = new byte[60];
        var caFingerprints = new byte[60];
        var generationTimes = new byte[12];

        var discretionaryTlvs = new List<Tlv>
        {
            new(0xC0, extendedCapabilities),
            new(0xC1, rsa2048Attributes),
            new(0xC2, rsa2048Attributes),
            new(0xC3, rsa2048Attributes),
            new(0xC4, pwStatus),
            new(0xC5, fingerprints),
            new(0xC6, caFingerprints),
            new(0xCD, generationTimes),
        };
        AddUif(discretionaryTlvs, DataObject.UifSig, sig);
        AddUif(discretionaryTlvs, DataObject.UifDec, dec);
        AddUif(discretionaryTlvs, DataObject.UifAut, aut);
        AddUif(discretionaryTlvs, DataObject.UifAtt, att);

        byte[] discretionaryContent;
        try
        {
            discretionaryContent = TlvHelper.EncodeList(discretionaryTlvs.ToArray()).ToArray();
        }
        finally
        {
            foreach (var tlv in discretionaryTlvs)
            {
                tlv.Dispose();
            }
        }

        var outerTlvs = new Tlv[]
        {
            new(0x4F, aid),
            new(0x5F52, historicalBytes),
            new(0x73, discretionaryContent),
        };

        byte[] outerContent;
        try
        {
            outerContent = TlvHelper.EncodeList(outerTlvs).ToArray();
        }
        finally
        {
            foreach (var tlv in outerTlvs)
            {
                tlv.Dispose();
            }
        }

        using var result = new Tlv(0x6E, outerContent);
        return result.AsMemory().ToArray();
    }

    private static void AddUif(List<Tlv> tlvs, DataObject dataObject, Uif? uif)
    {
        if (uif is { } value)
            tlvs.Add(new Tlv((int)dataObject, value.ToBytes()));
    }
}