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

using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public sealed class OpenPgpSessionWireTests
{
    [Fact]
    public async Task SignaturePolicy_UnknownResponseIsPreservedButNotAcceptedAsRequest()
    {
        var token = TestContext.Current.CancellationToken;
        using var connection = CreateInitializedConnection([0xA7, 0x86, 0x0C, 0x88, 3, 0, 3, 0x90, 0x00]);
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);
        var status = await session.GetPinStatusAsync(token);

        Assert.Equal(0xA7, (int)status.SignaturePinPolicy);
        Assert.Equal(6, status.MaxLenUser);
        Assert.Equal(12, status.MaxLenReset);
        Assert.Equal(8, status.MaxLenAdmin);
        Assert.True(status.UserUsesPinBlock2);
        Assert.True(status.AdminUsesPinBlock2);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.SetSignaturePinPolicyAsync(
            status.SignaturePinPolicy, token));
        Assert.Equal(4, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task UnknownRequiredCurve_AllowsSessionReadbackButCannotBeResentAsAttributes()
    {
        var token = TestContext.Current.CancellationToken;
        byte[] unknown = [0x12, 0x2B, 0x06, 0x04, 0x01, 0x7F];
        var appData = ApplicationRelatedDataResponse(decipherAttributes: AlgorithmAttributes.Parse(unknown));
        using var connection = new RecordingSmartCardConnection([OkResponse(), VersionResponse(), appData, appData]);
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);
        var parsed = await session.GetApplicationRelatedDataAsync(token);

        Assert.Equal(appData[..^2], parsed.Raw.ToArray());
        Assert.Equal(unknown, parsed.Discretionary.AlgorithmAttributesDec.ToBytes());
        await Assert.ThrowsAsync<NotSupportedException>(() => session.SetAlgorithmAttributesAsync(
            KeyRef.Dec, parsed.Discretionary.AlgorithmAttributesDec, token));
        Assert.Equal(4, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(0u, "00000000")]
    [InlineData(0x7FFF_FFFFu, "7FFFFFFF")]
    [InlineData(0x8000_0000u, "80000000")]
    [InlineData(0xFFFF_FFFFu, "FFFFFFFF")]
    public async Task GenerationTime_WriteAndReadback_UsesUnsignedBigEndianSeconds(uint seconds, string hex)
    {
        var token = TestContext.Current.CancellationToken;
        byte[] value = Convert.FromHexString(hex);
        byte[] appData = ApplicationRelatedDataResponse();
        // DO CD is the last TLV in this fixture; SIG is first of three four-byte values.
        value.CopyTo(appData.AsSpan(appData.Length - 14, 4));
        using var connection = CreateInitializedConnection(OkResponse(), appData);
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);

        await session.SetGenerationTimeAsync(KeyRef.Sig, seconds, token);
        var command = LastCommand(connection);
        Assert.Equal(new byte[] { 0x00, 0xDA, 0x00, 0xCE }, command[..4]);
        Assert.Equal(value, CommandData(command).ToArray());
        Assert.Equal(seconds, (await session.GetGenerationTimesAsync(token))[KeyRef.Sig]);
    }

    [Fact]
    public async Task CreateAsync_AppletProbeFailure_DoesNotDisposeTheBorrowedConnection()
    {
        var connection = new RecordingSmartCardConnection();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            OpenPgpSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        // The factory borrows this connection; session teardown leaves its disposal with the caller.
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
    public async Task GetSupportedAlgorithmsAsync_PreservesRepeatedSlotsAndOrder()
    {
        var connection = CreateInitializedConnection([
            0xFA, 0x10,
            0xC1, 0x06, 0x01, 0x08, 0x00, 0x00, 0x11, 0x00,
            0xC1, 0x06, 0x01, 0x0C, 0x00, 0x00, 0x11, 0x00,
            0x90, 0x00
        ]);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var algorithms = await session.GetSupportedAlgorithmsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, algorithms.Count);
        Assert.All(algorithms, algorithm => Assert.Equal(KeyRef.Sig, algorithm.KeyRef));
        Assert.Equal(2048, Assert.IsType<RsaAttributes>(algorithms[0].Attributes).NLen);
        Assert.Equal(3072, Assert.IsType<RsaAttributes>(algorithms[1].Attributes).NLen);
        var mutableView = Assert.IsAssignableFrom<IList<SupportedAlgorithm>>(algorithms);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView.Add(algorithms[0]));
    }

    [Theory]
    [InlineData(KeyRef.Sig)]
    [InlineData(KeyRef.Dec)]
    [InlineData(KeyRef.Aut)]
    [InlineData(KeyRef.Att)]
    public async Task GetCertificateAsync_EmptySuccessfulRead_ReturnsNull(KeyRef keyRef)
    {
        var connection = CreateCertificateReadConnection(keyRef, OkResponse());
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        using var certificate = await session.GetCertificateAsync(keyRef, TestContext.Current.CancellationToken);

        Assert.Null(certificate);
        AssertCertificateReadCommands(connection, keyRef);
    }

    [Theory]
    [InlineData(KeyRef.Sig)]
    [InlineData(KeyRef.Dec)]
    [InlineData(KeyRef.Aut)]
    public async Task GetCertificateAsync_CardholderDataNotFound_ReturnsNull(KeyRef keyRef)
    {
        // OpenPGP card v3.4.1 section 7.9: referenced data, reference data or DO not found.
        var connection = CreateCertificateReadConnection(keyRef, [0x6A, 0x88]);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        using var certificate = await session.GetCertificateAsync(keyRef, TestContext.Current.CancellationToken);

        Assert.Null(certificate);
        AssertCertificateReadCommands(connection, keyRef);
    }

    [Theory]
    [InlineData(KeyRef.Sig)]
    [InlineData(KeyRef.Dec)]
    [InlineData(KeyRef.Aut)]
    [InlineData(KeyRef.Att)]
    public async Task GetCertificateAsync_ValidDer_ReadsCertificate(KeyRef keyRef)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=OpenPGP certificate read", key, HashAlgorithmName.SHA256);
        using var expected = request.CreateSelfSigned(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));
        byte[] certificateData = expected.RawData;
        var connection = CreateCertificateReadConnection(keyRef, [.. certificateData, 0x90, 0x00]);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        using var actual = await session.GetCertificateAsync(keyRef, TestContext.Current.CancellationToken);

        Assert.NotNull(actual);
        Assert.Equal(certificateData, actual.RawData);
        Assert.False(actual.HasPrivateKey);
        AssertCertificateReadCommands(connection, keyRef);
    }

    [Theory]
    [InlineData(KeyRef.Sig, 0x6982)]
    [InlineData(KeyRef.Dec, 0x6985)]
    [InlineData(KeyRef.Aut, 0x6581)]
    [InlineData(KeyRef.Sig, 0x6A82)]
    [InlineData(KeyRef.Sig, 0x6F00)]
    [InlineData(KeyRef.Att, 0x6982)]
    [InlineData(KeyRef.Att, 0x6A88)]
    public async Task GetCertificateAsync_ReadFailure_PropagatesExactStatus(KeyRef keyRef, int status)
    {
        var connection = CreateCertificateReadConnection(keyRef, [(byte)(status >> 8), (byte)status]);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApduException>(() =>
            session.GetCertificateAsync(keyRef, TestContext.Current.CancellationToken));

        Assert.Equal((short)status, exception.SW);
        Assert.Equal((byte)Ins.GetData, exception.Ins);
        AssertCertificateReadCommands(connection, keyRef);
    }

    [Theory]
    [InlineData(0x6A88)]
    [InlineData(0x6982)]
    public async Task GetCertificateAsync_SelectionFailure_PropagatesWithoutReading(int status)
    {
        var connection = CreateInitializedConnection([(byte)(status >> 8), (byte)status]);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApduException>(() =>
            session.GetCertificateAsync(KeyRef.Sig, TestContext.Current.CancellationToken));

        Assert.Equal((short)status, exception.SW);
        var command = Assert.Single(connection.TransmittedCommands.Skip(3));
        Assert.Equal((byte)Ins.SelectData, command[1]);
    }

    [Theory]
    [InlineData(KeyRef.Sig)]
    [InlineData(KeyRef.Att)]
    public async Task GetCertificateAsync_MalformedCertificate_PropagatesParsingFailure(KeyRef keyRef)
    {
        var connection = CreateCertificateReadConnection(keyRef, [0x30, 0x01, 0x00, 0x90, 0x00]);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        _ = await Assert.ThrowsAsync<CryptographicException>(() =>
            session.GetCertificateAsync(keyRef, TestContext.Current.CancellationToken));

        AssertCertificateReadCommands(connection, keyRef);
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

    [Theory]
    [InlineData(0x01, 0x09)] // 4.1.9, just below the conservative policy
    [InlineData(0x03, 0x05)] // corrected 4.3.5
    public async Task GenerateKeyAsync_OutsideRocaPolicy_WritesAttributesBeforeGeneration(byte minor, byte patch)
    {
        var connection = new RecordingSmartCardConnection(
            [OkResponse(), [0x04, minor, patch, 0x90, 0x00], ApplicationRelatedDataResponse(),
                OkResponse(), OkResponse(), ApplicationRelatedDataResponse()]);
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
    [InlineData(0x02, 0x00)]
    [InlineData(0x02, 0x06)]
    [InlineData(0x03, 0x04)]
    public async Task GenerateKeyAsync_InsideRocaPolicy_SendsNoOperationApdu(byte minor, byte patch)
    {
        var connection = new RecordingSmartCardConnection(
            [OkResponse(), [0x04, minor, patch, 0x90, 0x00], ApplicationRelatedDataResponse()]);
        var token = TestContext.Current.CancellationToken;
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);
        int before = connection.TransmittedCommands.Count;

        var error = await Assert.ThrowsAsync<NotSupportedException>(() =>
            session.GenerateKeyAsync(KeyRef.Sig, RsaAttributes.Create(RsaSize.Rsa2048), token));

        Assert.Contains("ROCA", error.Message, StringComparison.Ordinal);
        Assert.Equal(before, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task NeoFixedAttributes_GeneratesWithoutAttributeWriteAndRefusesDeletion()
    {
        byte[] appData = ApplicationRelatedDataResponse(attributesChangeable: false);
        var connection = new RecordingSmartCardConnection(OkResponse(), [0x03, 0x04, 0x00, 0x90, 0x00], appData,
            OkResponse(), appData);
        var token = TestContext.Current.CancellationToken;
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);
        await Assert.ThrowsAsync<NotSupportedException>(() => session.DeleteKeyAsync(KeyRef.Sig, token));
        await Assert.ThrowsAsync<NotSupportedException>(() => session.GenerateKeyAsync(KeyRef.Sig, RsaAttributes.Create(RsaSize.Rsa4096), token));
        Assert.Equal(3, connection.TransmittedCommands.Count);

        await session.GenerateKeyAsync(KeyRef.Sig, RsaAttributes.Create(RsaSize.Rsa2048), token);

        Assert.Equal([0x47, 0xCA], connection.TransmittedCommands.Skip(3).Select(c => c[1]).ToArray());
    }

    [Fact]
    public async Task AttestationKey_ReadAndGenerate_SendNestedCrt()
    {
        var connection = CreateInitializedConnection(
            OkResponse(), OkResponse(), OkResponse(), ApplicationRelatedDataResponse());
        var token = TestContext.Current.CancellationToken;
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);

        await session.GetPublicKeyAsync(KeyRef.Att, token);
        await session.GenerateKeyAsync(KeyRef.Att, RsaAttributes.Create(RsaSize.Rsa2048), token);

        byte[][] commands = connection.TransmittedCommands.Where(command => command[1] == 0x47).ToArray();
        Assert.Collection(commands,
            command =>
            {
                Assert.Equal(0x81, command[2]);
                Assert.Equal([0xB6, 0x03, 0x84, 0x01, 0x81], CommandData(command).ToArray());
            },
            command =>
            {
                Assert.Equal(0x80, command[2]);
                Assert.Equal([0xB6, 0x03, 0x84, 0x01, 0x81], CommandData(command).ToArray());
            });
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
        Assert.Equal(UserPresenceOperations.OpenPgp.Sign, requested.Context.Operation);
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
        ReadOnlyMemory<byte> decrypted = await session.DecryptAsync(
            ciphertext,
            TestContext.Current.CancellationToken);
        _ = await session.AuthenticateAsync(
            "authenticate"u8.ToArray(),
            HashAlgorithmName.SHA256,
            TestContext.Current.CancellationToken);

        Assert.Equal(["Sig", "Dec", "Aut"], prompt.Requested.Select(entry => entry.Context.Scope));
        Assert.Equal(
            [UserPresenceOperations.OpenPgp.Sign, UserPresenceOperations.OpenPgp.Decrypt, UserPresenceOperations.OpenPgp.Authenticate],
            prompt.Requested.Select(entry => entry.Context.Operation));
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, decrypted.ToArray());
        Assert.All(prompt.Requested, entry =>
        {
            Assert.Equal("OpenPGP", entry.Context.Application);
            Assert.Equal(UserPresenceBasis.PolicyRequires, entry.Context.Basis);
        });
        Assert.All(prompt.Resolved, entry => Assert.Equal(UserPresenceOutcome.Completed, entry.Outcome));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SignAsync_WhenUifReadFails_NotifiesMayRequireAndStillSigns(bool loggerThrows)
    {
        var connection = CreateInitializedConnection([0x6A, 0x88], CryptoResponse());
        var prompt = new RecordingUserPresencePrompt(connection);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        if (loggerThrows)
        {
            // Replace this session's diagnostic logger only; don't change the process-global factory.
            var loggerField = typeof(OpenPgpSession).GetField("_logger", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Missing session logger.");
            loggerField.SetValue(session, new ThrowingUifLogger());
        }

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
        Assert.Equal(UserPresenceOperations.OpenPgp.AttestKey, requested.Context.Operation);
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
                OkResponse(), // algorithm attributes
                OkResponse(), // import, chained in two short APDUs
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
        using var rsa = RSA.Create(2048);
        using var importKey = RSAPrivateKey.CreateFromParameters(rsa.ExportParameters(includePrivateParameters: true));
        await (mutation switch
        {
            KeyMutation.Generate => session.GenerateKeyAsync(
                KeyRef.Sig,
                RsaAttributes.Create(RsaSize.Rsa2048),
                TestContext.Current.CancellationToken),
            KeyMutation.Import => session.PutKeyAsync(
                KeyRef.Sig,
                importKey,
                TestContext.Current.CancellationToken),
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

    [Theory]
    [InlineData(SignatureOperation.Sign)]
    [InlineData(SignatureOperation.Authenticate)]
    public async Task SignatureOperation_WhenResolutionFailsAfterSuccess_PropagatesResolutionException(
        SignatureOperation operation)
    {
        var expected = new InvalidOperationException("resolution failed");
        var connection = CreateInitializedConnectionWithUifs(
            Uif.On,
            dec: null,
            aut: Uif.On,
            att: null,
            CryptoResponse());
        var prompt = new RecordingUserPresencePrompt(connection, resolutionException: expected);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RunSignatureOperationAsync(session, operation, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(UserPresenceOutcome.Completed, Assert.Single(prompt.Resolved).Outcome);
    }

    [Theory]
    [InlineData(SignatureEncoding.RsaRaw)]
    [InlineData(SignatureEncoding.EdDsaRaw)]
    [InlineData(SignatureEncoding.EcdsaDer)]
    public async Task FormatResolveAndTransferSignatureAsync_OnSuccess_PreservesTransferredBuffer(
        SignatureEncoding encoding)
    {
        byte[] ownedRawResponse = [0x01, 0x02, 0x03, 0x04, 0x90, 0x00];
        byte[] originalRawResponse = ownedRawResponse.ToArray();

        ReadOnlyMemory<byte> signature = await OpenPgpSession.FormatResolveAndTransferSignatureAsync(
            CreateSignatureAttributes(KeyRef.Sig, encoding),
            ownedRawResponse,
            () => ValueTask.CompletedTask);

        byte[] expected = encoding is SignatureEncoding.EcdsaDer
            ? [0x30, 0x08, 0x02, 0x02, 0x01, 0x02, 0x02, 0x02, 0x03, 0x04]
            : [0x01, 0x02, 0x03, 0x04];
        Assert.Equal(expected, signature.ToArray());
        Assert.True(MemoryMarshal.TryGetArray(signature, out ArraySegment<byte> segment));
        byte[] backingArray = Assert.IsType<byte[]>(segment.Array);
        if (encoding is SignatureEncoding.EcdsaDer)
        {
            Assert.NotSame(ownedRawResponse, backingArray);
            Assert.All(ownedRawResponse, value => Assert.Equal(0, value));
        }
        else
        {
            Assert.Same(ownedRawResponse, backingArray);
            Assert.Equal(originalRawResponse, ownedRawResponse);
        }
    }

    [Theory]
    [InlineData(SignatureEncoding.RsaRaw)]
    [InlineData(SignatureEncoding.EdDsaRaw)]
    [InlineData(SignatureEncoding.EcdsaDer)]
    public async Task FormatResolveAndTransferSignatureAsync_WhenResolutionFails_ZeroesOwnedRawResponse(
        SignatureEncoding encoding)
    {
        byte[] ownedRawResponse = [0x01, 0x02, 0x03, 0x04, 0x90, 0x00];
        var expected = new InvalidOperationException("resolution failed");

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            OpenPgpSession.FormatResolveAndTransferSignatureAsync(
                CreateSignatureAttributes(KeyRef.Sig, encoding),
                ownedRawResponse,
                () => ValueTask.FromException(expected)));

        Assert.Same(expected, actual);
        Assert.All(ownedRawResponse, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task FormatResolveAndTransferSignatureAsync_WhenFormattingFails_ZeroesOwnedRawResponseWithoutResolving()
    {
        byte[] ownedRawResponse = [0x90, 0x00];
        var resolved = false;

        _ = await Assert.ThrowsAsync<IndexOutOfRangeException>(() =>
            OpenPgpSession.FormatResolveAndTransferSignatureAsync(
                EcAttributes.Create(KeyRef.Sig, KeyType.P256),
                ownedRawResponse,
                () =>
                {
                    resolved = true;
                    return ValueTask.CompletedTask;
                }));

        Assert.False(resolved);
        Assert.All(ownedRawResponse, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task DecryptAsync_X25519_WrapsRfc7748PeerPointInCipherDo()
    {
        // RFC 7748 §6.1 Bob public key and shared secret. The fake supplies the response;
        // the assertion qualifies SDK framing and response transfer, not device agreement.
        byte[] point = Convert.FromHexString("DE9EDB7D7B7DC1B4D35B61C2ECE435373F8343C85B78674DADFC7E146F882B4F");
        byte[] secret = Convert.FromHexString("4A5D9D5BA4CE2DE1728E3BF480350F25E07E21C947D19E3376F09B3C1E161742");
        var connection = new RecordingSmartCardConnection(
            [OkResponse(), VersionResponse(), ApplicationRelatedDataResponse(dec: Uif.Off,
                decipherAttributes: EcAttributes.Create(KeyRef.Dec, KeyType.X25519)), [.. secret, 0x90, 0x00]]);
        var token = TestContext.Current.CancellationToken;
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);

        var result = await session.DecryptAsync(point, token);

        byte[] command = Assert.Single(connection.TransmittedCommands.Skip(3));
        Assert.Equal([0x00, 0x2A, 0x80, 0x86], command[..4]);
        Assert.Equal([0xA6, 0x25, 0x7F, 0x49, 0x22, 0x86, 0x20, .. point], CommandData(command).ToArray());
        Assert.Equal(secret, result.ToArray());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResolveAndCopyDecipherResponseAsync_ClearsOwnedBytesAndNeverClearsInput(bool resolutionFails)
    {
        byte[] source = [0x34, 0x56, 0x90, 0x00];
        var response = new ApduResponse(source);
        var raw = response.RawData;
        var command = new ApduCommand(0, 0x2A, 0x80, 0x86);
        var expected = new InvalidOperationException("resolution failed");
        if (resolutionFails)
        {
            var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                OpenPgpSession.ResolveAndCopyDecipherResponseAsync(response, command,
                    () => ValueTask.FromException(expected)));
            Assert.Same(expected, actual);
        }
        else
        {
            Memory<byte> plaintext = await OpenPgpSession.ResolveAndCopyDecipherResponseAsync(response, command,
                () => ValueTask.CompletedTask);
            Assert.Equal(new byte[] { 0x34, 0x56 }, plaintext.ToArray());
            CryptographicOperations.ZeroMemory(plaintext.Span);
        }

        Assert.All(raw.ToArray(), value => Assert.Equal(0, value));
        Assert.Equal([0x34, 0x56, 0x90, 0x00], source);
    }

    [Fact]
    public async Task ResolveAndCopyDecipherResponseAsync_WhenCardRejects_ClearsOwnedBytesAndPreservesStatus()
    {
        var response = new ApduResponse(new byte[] { 0x34, 0x56, 0x69, 0x82 });
        var retainedBytes = response.RawData;
        var command = new ApduCommand(0, 0x2A, 0x80, 0x86);

        var error = await Assert.ThrowsAsync<ApduException>(() =>
            OpenPgpSession.ResolveAndCopyDecipherResponseAsync(response, command,
                () => throw new InvalidOperationException("A rejected command cannot resolve as Completed.")));

        Assert.Equal((short)0x6982, error.SW);
        Assert.All(retainedBytes.ToArray(), value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task DecryptAsync_WhenResolutionFailsAfterSuccess_PropagatesResolutionException()
    {
        var expected = new InvalidOperationException("resolution failed");
        var connection = CreateInitializedConnectionWithUifs(
            sig: null,
            dec: Uif.On,
            aut: null,
            att: null,
            CryptoResponse());
        var prompt = new RecordingUserPresencePrompt(connection, resolutionException: expected);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.DecryptAsync(new byte[] { 0x01, 0x02 }, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(UserPresenceOutcome.Completed, Assert.Single(prompt.Resolved).Outcome);
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
            [0x81, 0x01, 0x00, 0x90, 0x00], // F9 explicitly disables KDF.
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
    public async Task VerifyPinAsync_ExtendedWithDistinctKdfSalts_SendsUserHashToReference82()
    {
        using var kdf = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256,
            IterationCount = 32,
            SaltUser = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            SaltReset = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 },
        };
        var connection = CreateInitializedConnection([.. kdf.ToBytes(), 0x90, 0x00], OkResponse());
        var token = TestContext.Current.CancellationToken;
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);

        await session.VerifyPinAsync("123456"u8.ToArray(), extended: true, cancellationToken: token);

        byte[] command = LastCommand(connection);
        Assert.Equal([0x00, 0x20, 0x00, 0x82], command[..4]);
        Assert.Equal(kdf.Process(Pw.User, "123456"u8), CommandData(command).ToArray());
    }

    [Fact]
    public async Task VerifyPinAsync_WhenF9ReadIsRejected_DoesNotSendVerify()
    {
        var connection = CreateInitializedConnection([0x69, 0x82]);
        var token = TestContext.Current.CancellationToken;
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);

        var error = await Assert.ThrowsAsync<ApduException>(() => session.VerifyPinAsync("123456"u8.ToArray(), cancellationToken: token));

        Assert.Equal((short)0x6982, error.SW);
        Assert.Equal([0, (byte)Ins.GetData, 0, (byte)DataObject.Kdf], Assert.Single(connection.TransmittedCommands.Skip(3))[..4]);
    }

    [Fact]
    public async Task GetKdfAsync_ReadsAgainAndDoesNotOwnReturnedObject()
    {
        using var configuredKdf = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256,
            IterationCount = 32,
            SaltUser = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };
        var connection = CreateInitializedConnection(
            [.. configuredKdf.ToBytes(), 0x90, 0x00],
            [0x81, 0x01, 0x00, 0x90, 0x00]);
        var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        using var first = Assert.IsType<KdfIterSaltedS2k>(await session.GetKdfAsync(TestContext.Current.CancellationToken));
        using var second = Assert.IsType<KdfNone>(await session.GetKdfAsync(TestContext.Current.CancellationToken));
        ReadOnlyMemory<byte> salt = first.SaltUser;
        Assert.Contains(salt.ToArray(), value => value != 0);

        await session.DisposeAsync();

        Assert.Equal(configuredKdf.SaltUser.ToArray(), salt.ToArray());
    }

    [Fact]
    public async Task SetKdfAsync_WhenWriteFails_ClearsPayloadAndDoesNotDisposeInput()
    {
        byte[] encoded = [0x81, 0x01, 0x00];
        var connection = CreateInitializedConnection([0x69, 0x85]);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        var kdf = new TrackingKdf(encoded);
        var error = await Assert.ThrowsAsync<ApduException>(() => session.SetKdfAsync(kdf, TestContext.Current.CancellationToken));
        Assert.Equal((short)0x6985, error.SW);
        Assert.Equal([0x81, 0x01, 0x00], CommandData(LastCommand(connection)).ToArray());
        Assert.All(encoded, value => Assert.Equal(0, value));
        await session.DisposeAsync();
        Assert.False(kdf.Disposed);
    }

    [Fact]
    public async Task SetKeyFingerprintAsync_AfterDisposal_InvalidLengthThrowsObjectDisposedBeforeValidation()
    {
        var connection = CreateInitializedConnection();
        var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();
        int transmissionsBeforeCall = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.SetKeyFingerprintAsync(
                KeyRef.Sig,
                ReadOnlyMemory<byte>.Empty,
                TestContext.Current.CancellationToken));

        Assert.Equal(typeof(OpenPgpSession).FullName, exception.ObjectName);
        Assert.Equal(transmissionsBeforeCall, connection.TransmittedCommands.Count);
    }

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), VersionResponse(), ApplicationRelatedDataResponse(), .. trailingResponses]);

    [Fact]
    public async Task LegacyFirmware_UsesBuiltInAlgorithmsAndRejectsUnsupportedUifBeforeWrite()
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), [0x04, 0x04, 0x00, 0x90, 0x00], ApplicationRelatedDataResponse());
        var token = TestContext.Current.CancellationToken;
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);
        var algorithms = await session.GetSupportedAlgorithmsAsync(token);
        Assert.Equal([KeyRef.Sig, KeyRef.Dec, KeyRef.Aut], algorithms.Select(algorithm => algorithm.KeyRef).ToArray());
        Assert.All(algorithms, algorithm => Assert.Equal(2048, Assert.IsType<RsaAttributes>(algorithm.Attributes).NLen));
        await Assert.ThrowsAsync<NotSupportedException>(() => session.SetUifAsync(KeyRef.Sig, Uif.Cached, token));
        await Assert.ThrowsAsync<NotSupportedException>(() => session.SetUifAsync(KeyRef.Att, Uif.On, token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.GetChallengeAsync(256, token));
        Assert.Equal(3, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task Initialize_WithExplicitOverride_DoesNotConsultCacheOrSendGetVersion()
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), ApplicationRelatedDataResponse());
        var token = TestContext.Current.CancellationToken;
        await using var session = await OpenPgpSession.CreateWithDeviceInfoAsync(connection,
            new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(4, 3, 5) },
            () => throw new InvalidOperationException("The override bypasses cached metadata."), token);

        Assert.False(session.IsSupported(OpenPgpSession.FeatureEc));
        Assert.Equal([0xA4, 0xCA], connection.TransmittedCommands.Select(command => command[1]).ToArray());
    }

    [Fact]
    public async Task Initialize_WithUnsuccessfulVersionRead_UsesOptimisticFeaturesButStillReadsAppData()
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), [0x69, 0x85], ApplicationRelatedDataResponse());
        var token = TestContext.Current.CancellationToken;
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);

        Assert.True(session.IsSupported(OpenPgpSession.FeatureUnverify));
        Assert.Equal([0xA4, 0xF1, 0xCA], connection.TransmittedCommands.Select(command => command[1]).ToArray());
    }

    [Theory]
    [InlineData(0x20, 1)] // unrelated VERIFY error
    [InlineData(0xE6, 3)] // TERMINATE refusal
    [InlineData(0x44, 4)] // ACTIVATE refusal
    public async Task ResetAsync_WhenCommandIsRejected_StopsAtThatCommand(byte instruction, int precedingSuccesses)
    {
        byte[][] responses = [PwStatusResponse(), [0x69, 0x83], [0x69, 0x83], OkResponse(), OkResponse()];
        var connection = CreateInitializedConnection([.. responses.Take(precedingSuccesses), [0x6A, 0x80]]);
        var token = TestContext.Current.CancellationToken;
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);

        var error = await Assert.ThrowsAsync<ApduException>(() => session.ResetAsync(token));

        Assert.Equal((short)0x6A80, error.SW);
        Assert.Equal(4 + precedingSuccesses, connection.TransmittedCommands.Count);
        Assert.Equal(instruction, LastCommand(connection)[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetAsync_ConfirmedReset_ReselectsAndRequiresSuccessfulRefresh(bool refreshFails)
    {
        var connection = CreateInitializedConnection(PwStatusResponse(), [0x69, 0x83], [0x69, 0x83],
            OkResponse(), OkResponse(), OkResponse(), VersionResponse(),
            refreshFails ? [0x6A, 0x80] : ApplicationRelatedDataResponse(), PwStatusResponse());
        var token = TestContext.Current.CancellationToken;
        await using var session = await OpenPgpSession.CreateAsync(connection, cancellationToken: token);

        if (refreshFails)
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ResetAsync(token));
            Assert.Equal((short)0x6A80, Assert.IsType<ApduException>(error.InnerException).SW);
            Assert.False(session.IsInitialized);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => session.GetPinStatusAsync(token));
        }
        else
        {
            await session.ResetAsync(token);
            Assert.True(session.IsInitialized);
            Assert.False(session.IsAuthenticated);
            Assert.Equal(3, (await session.GetPinStatusAsync(token)).AttemptsUser);
        }

        Assert.Equal([0xCA, 0x20, 0x20, 0xE6, 0x44, 0xA4, 0xF1, 0xCA],
            connection.TransmittedCommands.Skip(3).Take(8).Select(command => command[1]).ToArray());
    }

    private static RecordingSmartCardConnection CreateCertificateReadConnection(KeyRef keyRef, byte[] response) =>
        keyRef is KeyRef.Att
            ? CreateInitializedConnection(response)
            : CreateInitializedConnection(OkResponse(), response);

    private static void AssertCertificateReadCommands(RecordingSmartCardConnection connection, KeyRef keyRef)
    {
        byte[][] commands = connection.TransmittedCommands.Skip(3).ToArray();
        Assert.Equal(keyRef is KeyRef.Att ? 1 : 2, commands.Length);
        if (keyRef is not KeyRef.Att)
        {
            Assert.Equal([0x00, 0xA5, (byte)(3 - (byte)keyRef), 0x04], commands[0][..4]);
            Assert.Equal([0x60, 0x04, 0x5C, 0x02, 0x7F, 0x21], CommandData(commands[0]).ToArray());
        }

        byte[] expectedReadHeader = keyRef is KeyRef.Att
            ? [0x00, 0xCA, 0x00, 0xFC]
            : [0x00, 0xCA, 0x7F, 0x21];
        Assert.Equal(expectedReadHeader, commands[^1][..4]);
    }

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

    private static Task<ReadOnlyMemory<byte>> RunSignatureOperationAsync(
        OpenPgpSession session,
        SignatureOperation operation,
        CancellationToken cancellationToken) =>
        operation switch
        {
            SignatureOperation.Sign => session.SignAsync(
                "message"u8.ToArray(),
                HashAlgorithmName.SHA256,
                cancellationToken),
            SignatureOperation.Authenticate => session.AuthenticateAsync(
                "message"u8.ToArray(),
                HashAlgorithmName.SHA256,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

    private static AlgorithmAttributes CreateSignatureAttributes(KeyRef keyRef, SignatureEncoding encoding) =>
        encoding switch
        {
            SignatureEncoding.RsaRaw => RsaAttributes.Create(RsaSize.Rsa2048),
            SignatureEncoding.EdDsaRaw => EcAttributes.Create(keyRef, KeyType.Ed25519),
            SignatureEncoding.EcdsaDer => EcAttributes.Create(keyRef, KeyType.P256),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };

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

    public enum SignatureOperation
    {
        Sign,
        Authenticate
    }

    public enum SignatureEncoding
    {
        RsaRaw,
        EdDsaRaw,
        EcdsaDer
    }

    private sealed class TrackingKdf(byte[] encoding) : Kdf
    {
        public bool Disposed { get; private set; }
        public override int Algorithm => 0;

        public override byte[] Process(Pw pw, ReadOnlySpan<byte> pin) =>
            pin.ToArray();

        public override byte[] ToBytes() => encoding;

        public override void Dispose() => Disposed = true;
    }

    private sealed class ThrowingUifLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (exception is ApduException)
                throw new InvalidOperationException("UIF diagnostics failed.");
        }
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
        Uif? att = null,
        AlgorithmAttributes? decipherAttributes = null,
        bool attributesChangeable = true) =>
        [.. BuildApplicationRelatedData(sig, dec, aut, att, decipherAttributes, attributesChangeable), 0x90, 0x00];

    private static byte[] BuildApplicationRelatedData(Uif? sig, Uif? dec, Uif? aut, Uif? att,
        AlgorithmAttributes? decipherAttributes, bool attributesChangeable)
    {
        // AID: D276000124010304000612345678 (OpenPGP v3.4, Yubico, serial 12345678).
        byte[] aid = [0xD2, 0x76, 0x00, 0x01, 0x24, 0x01, 0x03, 0x04, 0x00, 0x06, 0x12, 0x34, 0x56, 0x78];
        byte[] historicalBytes = [0x00, 0x73, 0x00, 0x01, 0x80, 0x05, 0x90, 0x00];
        byte[] rsa2048Attributes = [0x01, 0x08, 0x00, 0x00, 0x11, 0x00];
        byte[] extendedCapabilities = [attributesChangeable ? (byte)0x75 : (byte)0x71, 0x00, 0x00, 0xFF, 0x04, 0x80, 0x00, 0xFF, 0x00, 0x00];
        byte[] pwStatus = [0x00, 0x7F, 0x7F, 0x7F, 0x03, 0x00, 0x03];
        var fingerprints = new byte[60];
        var caFingerprints = new byte[60];
        var generationTimes = new byte[12];

        var discretionaryTlvs = new List<Tlv>
        {
            new(0xC0, extendedCapabilities),
            new(0xC1, rsa2048Attributes),
            new(0xC2, decipherAttributes?.ToBytes() ?? rsa2048Attributes),
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
