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
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.YubiHsm.Backend;
namespace Yubico.YubiKit.YubiHsm.UnitTests;

public class HsmAuthSessionByteLevelTests
{
    [Theory]
    [InlineData("9000")]
    [InlineData("790305049000")] // Broken optional TLV after successful SELECT.
    [InlineData("79030000019000")]
    public async Task CreateAsync_UnknownAppletVersionAdmitsFeatureGates(string selectHex)
    {
        using var connection = new RecordingSmartCardConnection(Convert.FromHexString(selectHex), OkResponse());
        var token = TestContext.Current.CancellationToken;
        await using var session = await HsmAuthSession.CreateAsync(connection, cancellationToken: token);
        using var password = CredentialPassword.FromUtf8("pass"u8);

        await session.GetChallengeAsync("cred", password, token);

        Assert.True(session.IsSupported(HsmAuthSession.FeaturePasswordChange));
        Assert.Contains((byte)0x73, CommandData(LastCommand(connection)).ToArray());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task CreateAsync_UsesDeviceVersionAndTreatsItsPrereleaseOptimistically(bool prerelease, bool gateOpens)
    {
        var deviceVersion = new FirmwareVersion(5, 6, 0);
        var cached = default(DeviceInfo) with
        {
            FirmwareVersion = deviceVersion,
            VersionQualifier = new VersionQualifier(deviceVersion, prerelease ? VersionQualifierType.Beta : VersionQualifierType.Final, 1)
        };
        using var connection = new RecordingSmartCardConnection([0x79, 3, 5, 8, 0, 0x90, 0]);
        await using var session = await HsmAuthSession.CreateAsync(connection, null, () => cached, TestContext.Current.CancellationToken);

        Assert.Equal(gateOpens, session.IsSupported(HsmAuthSession.FeaturePasswordChange));
    }

    [Fact]
    public async Task CreateAsync_ExplicitOverrideBypassesCacheButStillSelects()
    {
        using var connection = new RecordingSmartCardConnection(OkResponse());
        var options = new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 6, 0) };
        await using var session = await HsmAuthSession.CreateAsync(connection, options,
            () => throw new InvalidOperationException("The override must bypass the cache."), TestContext.Current.CancellationToken);

        Assert.False(session.IsSupported(HsmAuthSession.FeaturePasswordChange));
        Assert.Equal(0xA4, Assert.Single(connection.TransmittedCommands)[1]);
    }

    [Fact]
    public async Task CreateAsync_UnknownFirmwareStillHonorsForceShortApdus()
    {
        using var connection = new ExtendedCardConnection(OkResponse(), OkResponse());
        var options = new SessionCreationOptions { ProtocolConfiguration = new ProtocolConfiguration { ForceShortApdus = true } };
        var token = TestContext.Current.CancellationToken;
        await using var session = await HsmAuthSession.CreateAsync(connection, options, token);

        await session.PutManagementKeyAsync(new byte[16], Sequence(0x20, 16), token);

        Assert.True(session.IsSupported(HsmAuthSession.FeaturePasswordChange));
        Assert.Equal(36, connection.Recorder.TransmittedCommands[^1][4]); // Short Lc, two 18-byte TLVs.
    }
    // P-256 base point (SEC 2, section 2.4.2), encoded as 04 || X || Y.
    internal static byte[] P256Point() => Convert.FromHexString(
        "046B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296" +
        "4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5");

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPublicKeyAsync_RequiresAnOnCurvePoint(bool onCurve)
    {
        byte[] point = onCurve ? P256Point() : [0x04, .. new byte[64]];
        using var connection = CreateInitializedConnection([.. point, 0x90, 0x00]);
        await using var session = await HsmAuthSession.CreateAsync(connection,
            new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 6, 0) },
            cancellationToken: TestContext.Current.CancellationToken);

        if (onCurve)
            Assert.Equal(point, (await session.GetPublicKeyAsync("cred", TestContext.Current.CancellationToken)).ToArray());
        else
            await Assert.ThrowsAsync<BadResponseException>(() => session.GetPublicKeyAsync("cred", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CalculateSessionKeysAsymmetricAsync_RejectsOffCurveHsmKeyBeforeAnyAppletCommand()
    {
        using var connection = CreateInitializedConnection();
        await using var session = await HsmAuthSession.CreateAsync(connection,
            new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 6, 0) },
            cancellationToken: TestContext.Current.CancellationToken);
        using var password = CredentialPassword.FromUtf8("pass"u8);
        byte[] offCurve = [0x04, .. new byte[64]];

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => session.CalculateSessionKeysAsymmetricAsync(
            "cred", new byte[130], offCurve, password, new byte[8], TestContext.Current.CancellationToken));

        Assert.Equal("publicKey", exception.ParamName);
        Assert.Single(connection.TransmittedCommands);
    }
    [Fact]
    public async Task ListCredentialsAsync_PreservesUnknownAlgorithmAndTouchPolicy()
    {
        using var connection = CreateInitializedConnection([
            0x72, 0x04, 0x26, 0x01, (byte)'a', 8,
            0x70, 0x01, 0xFF, // Unknown, well-formed extension.
            0x72, 0x05, 0xFE, 0x02, 0xC3, 0xA9, 7,
            0x90, 0x00]);
        await using var session = await HsmAuthSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Collection(await session.ListCredentialsAsync(TestContext.Current.CancellationToken),
            known => Assert.Equal(new HsmAuthCredential("a", HsmAuthAlgorithm.Aes128YubicoAuthentication, 8, true), known),
            future => Assert.Equal(new HsmAuthCredential("é", (HsmAuthAlgorithm)0xFE, 7, null), future));
    }

    [Theory]
    [InlineData("72022601")]
    [InlineData("7203260108")]
    [InlineData("72042601FF08")]
    public async Task ListCredentialsAsync_DoesNotReturnPartialListWhenARecordIsMalformed(string malformedRecordHex)
    {
        using var connection = CreateInitializedConnection([
            0x72, 0x04, 0x26, 0x01, (byte)'a', 8,
            .. Convert.FromHexString(malformedRecordHex), 0x90, 0x00]);
        await using var session = await HsmAuthSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<BadResponseException>(() => session.ListCredentialsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_AppletProbeFailure_DoesNotDisposeTheBorrowedConnection()
    {
        var connection = new RecordingSmartCardConnection();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            HsmAuthSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        // The direct factory borrows this connection. Initialization failure leaves disposal to its caller.
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task PutCredentialSymmetricAsync_TransmitsOrderedCredentialTlvs()
    {
        using var password = CredentialPassword.FromUtf8("pass"u8);
        var connection = CreateInitializedConnection(OkResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 4, 3) },
            cancellationToken: TestContext.Current.CancellationToken);

        await session.PutCredentialSymmetricAsync(
            Sequence(0x10, 16),
            "cred",
            Sequence(0x20, 16),
            Sequence(0x30, 16),
            password,
            touchRequired: true,
            TestContext.Current.CancellationToken);

        var command = LastCommand(connection);
        // APDU header: INS=PUT, P1/P2 default to 0 for YubiHSM Auth credential storage.
        Assert.Equal(0x01, command[1]);
        Assert.Equal(0x00, command[2]);
        Assert.Equal(0x00, command[3]);
        // Data: management key(7B), label(71), algorithm(74), K-ENC(75), K-MAC(76), password(73), touch(7A).
        Assert.Equal([
            0x7B, 0x10, .. Sequence(0x10, 16),
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d',
            0x74, 0x01, (byte)HsmAuthAlgorithm.Aes128YubicoAuthentication,
            0x75, 0x10, .. Sequence(0x20, 16),
            0x76, 0x10, .. Sequence(0x30, 16),
            0x73, 0x10, (byte)'p', (byte)'a', (byte)'s', (byte)'s', .. new byte[12],
            0x7A, 0x01, 0x01
        ], CommandData(command).ToArray());
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_TransmitsOrderedCalculateTlvs()
    {
        using var password = CredentialPassword.FromUtf8("pass"u8);
        var connection = CreateInitializedConnection(SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 4, 3) },
            cancellationToken: TestContext.Current.CancellationToken);

        using var keys = await session.CalculateSessionKeysSymmetricAsync(
            "cred",
            Sequence(0x40, 16),
            password,
            Sequence(0x50, 8),
            TestContext.Current.CancellationToken);

        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
        var command = LastCommand(connection);
        // APDU header: INS=CALCULATE with default P1/P2.
        Assert.Equal(0x03, command[1]);
        Assert.Equal(0x00, command[2]);
        Assert.Equal(0x00, command[3]);
        // Data: label(71), context(77), card cryptogram response(78), credential password(73).
        Assert.Equal([
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d',
            0x77, 0x10, .. Sequence(0x40, 16),
            0x78, 0x08, .. Sequence(0x50, 8),
            0x73, 0x10, (byte)'p', (byte)'a', (byte)'s', (byte)'s', .. new byte[12]
        ], CommandData(command).ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(17)]
    public async Task CalculateSessionKeysSymmetricAsync_ReadsKeyPrefixAndClearsEntireFinalResponse(int suffixLength)
    {
        using var password = CredentialPassword.FromUtf8("pass"u8);
        byte[] source = [.. SessionKeyResponse()[..^2], .. Sequence(0xE0, suffixLength), 0x90, 0x00];
        var response = new ApduResponse(source);
        using var connection = CreateInitializedConnection();
        await using var session = await HsmAuthSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);
        SetCalculateBackend(session, response);

        using var keys = await session.CalculateSessionKeysSymmetricAsync("cred", new byte[16], password,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
        Assert.Equal(Sequence(0xB0, 16), keys.SMac.ToArray());
        Assert.Equal(Sequence(0xC0, 16), keys.SRmac.ToArray());
        Assert.All(response.RawData.ToArray(), value => Assert.Equal(0, value));
        Assert.Equal((byte)0x90, source[^2]);
        Assert.Equal(Sequence(0xE0, suffixLength), source[48..^2]);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_RejectsShortResponseAndClearsIt()
    {
        using var password = CredentialPassword.FromUtf8("pass"u8);
        byte[] source = [.. Sequence(0xA0, 47), 0x90, 0x00];
        var response = new ApduResponse(source);
        using var connection = CreateInitializedConnection();
        await using var session = await HsmAuthSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);
        SetCalculateBackend(session, response);

        var exception = await Assert.ThrowsAsync<BadResponseException>(() => session.CalculateSessionKeysSymmetricAsync(
            "cred", new byte[16], password, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("47 bytes", exception.Message);
        Assert.All(response.RawData.ToArray(), value => Assert.Equal(0, value));
    }

    private static void SetCalculateBackend(HsmAuthSession session, ApduResponse response)
    {
        // The raw connection cannot witness the final response: ApduResponse clones the transport array.
        var backend = typeof(HsmAuthSession).GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Missing applet backend.");
        backend.SetValue(session, new CalculateBackend(response));
    }

    [Fact]
    public async Task RejectedCredentialPassword_DoesNotEndSessionOrBlockIndependentManagementCommand()
    {
        using var connection = CreateInitializedConnection([0x63, 0xC7], OkResponse(), SessionKeyResponse());
        var token = TestContext.Current.CancellationToken;
        await using var session = await HsmAuthSession.CreateAsync(connection, cancellationToken: token);
        using var rejected = CredentialPassword.FromUtf8("wrong"u8);
        using var accepted = CredentialPassword.FromUtf8("right"u8);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.CalculateSessionKeysSymmetricAsync(
            "cred", new byte[16], rejected, cancellationToken: token));

        Assert.Equal(7, exception.RetriesRemaining);
        Assert.True(session.IsInitialized);
        await session.PutManagementKeyAsync(new byte[16], Sequence(0x10, 16), token);
        using var keys = await session.CalculateSessionKeysSymmetricAsync("cred", new byte[16], accepted, cancellationToken: token);
        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
        Assert.Equal(new byte[] { 0xA4, 0x03, 0x08, 0x03 }, connection.TransmittedCommands.Select(c => c[1]));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(17)]
    public async Task CalculateSessionKeysSymmetricAsync_InvalidContextLength_ThrowsBeforeDeviceIo(int length)
    {
        using var password = CredentialPassword.FromUtf8("pass"u8);
        var connection = CreateInitializedConnection(SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 4, 3) },
            cancellationToken: TestContext.Current.CancellationToken);
        var commandCount = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            session.CalculateSessionKeysSymmetricAsync(
                "cred",
                new byte[length],
                password,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("context", exception.ParamName);
        Assert.Equal(commandCount, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(129)]
    [InlineData(131)]
    public async Task CalculateSessionKeysAsymmetricAsync_InvalidContextLength_ThrowsBeforeDeviceIo(int length)
    {
        using var password = CredentialPassword.FromUtf8("pass"u8);
        var connection = CreateInitializedConnection(SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 6, 0) },
            cancellationToken: TestContext.Current.CancellationToken);
        var commandCount = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            session.CalculateSessionKeysAsymmetricAsync(
                "cred",
                new byte[length],
                Sequence(0x60, 65),
                password,
                Sequence(0x70, 8),
                TestContext.Current.CancellationToken));

        Assert.Equal("context", exception.ParamName);
        Assert.Equal(commandCount, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task GetChallengeAsync_Firmware560WithoutPassword_TransmitsOnlyLabel()
    {
        var connection = CreateInitializedConnection([.. Sequence(0x40, 8), 0x90, 0x00]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 6, 0) },
            cancellationToken: TestContext.Current.CancellationToken);

        var challenge = await session.GetChallengeAsync(
            "cred",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(Sequence(0x40, 8), challenge.ToArray());
        Assert.Equal([
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d'
        ], CommandData(LastCommand(connection)).ToArray());
    }

    [Fact]
    public async Task GetChallengeAsync_Firmware560WithPassword_DoesNotTransmitPassword()
    {
        using var password = CredentialPassword.FromUtf8("pass"u8);
        var connection = CreateInitializedConnection([.. Sequence(0x40, 8), 0x90, 0x00]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 6, 0) },
            cancellationToken: TestContext.Current.CancellationToken);

        await session.GetChallengeAsync(
            "cred",
            password,
            TestContext.Current.CancellationToken);

        Assert.Equal([
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d'
        ], CommandData(LastCommand(connection)).ToArray());
    }

    [Fact]
    public async Task GetChallengeAsync_Firmware571WithPassword_TransmitsPassword()
    {
        using var password = CredentialPassword.FromUtf8("pass"u8);
        var connection = CreateInitializedConnection([.. Sequence(0x40, 8), 0x90, 0x00]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 7, 1) },
            cancellationToken: TestContext.Current.CancellationToken);

        await session.GetChallengeAsync(
            "cred",
            password,
            TestContext.Current.CancellationToken);

        Assert.Equal([
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d',
            0x73, 0x10, (byte)'p', (byte)'a', (byte)'s', (byte)'s', .. new byte[12]
        ], CommandData(LastCommand(connection)).ToArray());
    }

    [Fact]
    public async Task GetChallengeAsync_WithRetryStatusKeepsGeneralApduError()
    {
        using var connection = CreateInitializedConnection([0x63, 0xC7]);
        var options = new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 7, 1) };
        var token = TestContext.Current.CancellationToken;
        await using var session = await HsmAuthSession.CreateAsync(connection, options, token);
        using var password = CredentialPassword.FromUtf8("pass"u8);

        var failure = await Assert.ThrowsAsync<ApduException>(() => session.GetChallengeAsync("cred", password, token));

        Assert.Equal(unchecked((short)0x63C7), failure.SW);
        Assert.Equal(0x04, connection.TransmittedCommands[^1][1]);
    }

    [Fact]
    public async Task DeleteCredentialAsync_WhenManagementKeyRetryFailure_ThrowsHsmAuthRetryException()
    {
        // SW 63C2 is the YubiHSM Auth retry-counter failure response with 2 attempts left.
        var connection = CreateInitializedConnection([0x63, 0xC2]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 4, 3) },
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.DeleteCredentialAsync(
            Sequence(0x10, 16),
            "cred",
            TestContext.Current.CancellationToken));

        Assert.Equal(unchecked((short)0x63C2), exception.SW);
        Assert.Equal(2, exception.RetriesRemaining);
        Assert.Equal((byte)0x02, exception.Ins.GetValueOrDefault());
        Assert.Contains("2 attempt(s) remaining", exception.Message);
        var command = LastCommand(connection);
        // APDU header: INS=DELETE; retry-aware exception proves throwOnError:false allowed SW 63Cx parsing.
        Assert.Equal(0x02, command[1]);
        // Data: management key(7B) followed by credential label(71).
        Assert.Equal([
            0x7B, 0x10, .. Sequence(0x10, 16),
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d'
        ], CommandData(command).ToArray());
    }

    /// <summary>
    ///     Pins the CHANGE credential password APDU for the password-authenticated path.
    /// </summary>
    /// <remarks>
    ///     The recorder checks the instruction, selector, header and selected TLV tag order. These
    ///     assertions help separate command-encoding regressions from device rejections. Diagnose a
    ///     hardware status such as 0x6D00 using the actual command, applet version and device evidence;
    ///     passing these checks alone does not establish why a device rejected a command.
    /// </remarks>
    [Fact]
    public async Task ChangeCredentialPasswordAsync_TransmitsChangeInstructionWithPasswordSelector()
    {
        using var newPassword = CredentialPassword.FromUtf8("newpass"u8);
        using var password = CredentialPassword.FromUtf8("pass"u8);
        var connection = CreateInitializedConnection(OkResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 8, 0) },
            cancellationToken: TestContext.Current.CancellationToken);

        await session.ChangeCredentialPasswordAsync(
            "cred",
            password,
            newPassword,
            TestContext.Current.CancellationToken);

        var command = LastCommand(connection);
        Assert.Equal(0x00, command[0]);
        Assert.Equal(0x0B, command[1]);
        Assert.Equal(0x00, command[2]); // P1=0 selects password authentication.
        Assert.Equal(0x00, command[3]);

        // Label, then current password, then new password.
        var data = CommandData(command).ToArray();
        Assert.Equal(0x71, data[0]);
        var afterLabel = 2 + data[1];
        Assert.Equal(0x73, data[afterLabel]);
        var afterCurrent = afterLabel + 2 + data[afterLabel + 1];
        Assert.Equal(0x73, data[afterCurrent]);
    }

    /// <summary>
    ///     Checks the CHANGE instruction, management-key selector, initial label tag and following
    ///     management-key tag for the administrator path.
    /// </summary>
    [Fact]
    public async Task ChangeCredentialPasswordAdminAsync_TransmitsChangeInstructionWithManagementKeySelector()
    {
        using var newPassword = CredentialPassword.FromUtf8("newpass"u8);
        var connection = CreateInitializedConnection(OkResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 8, 0) },
            cancellationToken: TestContext.Current.CancellationToken);

        await session.ChangeCredentialPasswordAdminAsync(
            Sequence(0x10, 16),
            "cred",
            newPassword,
            TestContext.Current.CancellationToken);

        var command = LastCommand(connection);
        Assert.Equal(0x0B, command[1]);
        Assert.Equal(0x01, command[2]); // P1=1 selects management-key authentication.

        var data = CommandData(command).ToArray();
        Assert.Equal(0x71, data[0]);
        var afterLabel = 2 + data[1];
        Assert.Equal(0x7B, data[afterLabel]); // Management key, not a credential password.
    }

    /// <summary>
    ///     A caller-supplied firmware override survives <c>ResetAsync</c>.
    /// </summary>
    /// <remarks>
    ///     Reset reselects using a fresh protocol. Retaining the explicit override
    ///     keeps feature policy consistent across reset, even when SELECT reports a lower applet version.
    /// </remarks>
    [Fact]
    public async Task ResetAsync_PreservesCallerSuppliedFirmwareVersionOverride()
    {
        // Re-SELECT reports 5.4.3 through the version TLV; the override must win regardless.
        byte[] reselectWithLowerVersion = [0x79, 0x03, 0x05, 0x04, 0x03, 0x90, 0x00];
        var connection = CreateInitializedConnection(OkResponse(), reselectWithLowerVersion);

        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 8, 0) },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new FirmwareVersion(5, 8, 0), session.FirmwareVersion);

        await session.ResetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new FirmwareVersion(5, 8, 0), session.FirmwareVersion);
        Assert.True(session.IsSupported(HsmAuthSession.FeaturePasswordChange));
    }

    [Fact]
    public async Task ResetAsync_RefreshesPolicyAndUsesSameSessionForNextCommand()
    {
        using var connection = new RecordingSmartCardConnection(
            [0x79, 3, 5, 4, 3, 0x90, 0], OkResponse(), [0x79, 3, 5, 8, 0, 0x90, 0], [8, 0x90, 0]);
        var token = TestContext.Current.CancellationToken;
        await using var session = await HsmAuthSession.CreateAsync(connection, cancellationToken: token);
        Assert.False(session.IsSupported(HsmAuthSession.FeaturePasswordChange));

        await session.ResetAsync(token);

        Assert.True(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
        Assert.True(session.IsSupported(HsmAuthSession.FeaturePasswordChange));
        Assert.Equal(8, await session.GetManagementKeyRetriesAsync(token));
        Assert.Equal(new byte[] { 0xA4, 0x06, 0xA4, 0x09 }, connection.TransmittedCommands.Select(c => c[1]));
        Assert.Equal(0, connection.DisposeCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResetAsync_DistinguishesRejectedResetFromFailedReselect(bool resetConfirmed)
    {
        byte[][] responses = resetConfirmed ? [OkResponse(), [0x6A, 0x82]] : [[0x69, 0x85], [8, 0x90, 0]];
        using var connection = CreateInitializedConnection(responses);
        var token = TestContext.Current.CancellationToken;
        await using var session = await HsmAuthSession.CreateAsync(connection, cancellationToken: token);

        var error = await Assert.ThrowsAsync<ApduException>(() => session.ResetAsync(token));

        Assert.Equal(resetConfirmed ? 0x6A82 : 0x6985, (ushort)error.SW.GetValueOrDefault());
        Assert.Equal(!resetConfirmed, session.IsInitialized);
        if (resetConfirmed)
            await Assert.ThrowsAsync<ObjectDisposedException>(() => session.GetManagementKeyRetriesAsync(token));
        else
            Assert.Equal(8, await session.GetManagementKeyRetriesAsync(token));
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task ResetAsync_RefreshErrorSurvivesFailedOwnedConnectionDisposal()
    {
        var connection = new ExtendedCardConnection(OkResponse(), OkResponse(), [0x6A, 0x82]) { FailDisposal = true };
        var device = new CardDevice(connection);
        var token = TestContext.Current.CancellationToken;

        var error = await Assert.ThrowsAsync<ApduException>(async () =>
        {
            await using var session = await device.CreateHsmAuthSessionAsync(cancellationToken: token);
            await session.ResetAsync(token);
        });

        Assert.Equal(0x6A82, (ushort)error.SW.GetValueOrDefault());
        Assert.Equal(1, connection.DisposeCalls);
    }

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), .. trailingResponses]);

    private static byte[] LastCommand(RecordingSmartCardConnection connection) =>
        connection.TransmittedCommands[^1];

    private static ReadOnlySpan<byte> CommandData(byte[] command) =>
        // Short APDU format: CLA INS P1 P2 Lc Data; the recorder reports SupportsExtendedApdu=false.
        command.AsSpan(5, command[4]);

    private static byte[] OkResponse() => [0x90, 0x00];

    private static byte[] Sequence(byte start, int length)
    {
        var bytes = new byte[length];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)(start + i);

        return bytes;
    }

    private static byte[] SessionKeyResponse() =>
    [
        .. Sequence(0xA0, 16),
        .. Sequence(0xB0, 16),
        .. Sequence(0xC0, 16),
        0x90, 0x00
    ];

    private sealed class CalculateBackend(ApduResponse response) : IHsmAuthBackend
    {
        public Task<ReadOnlyMemory<byte>> SelectAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The calculation must not reinitialize the applet.");

        public Task<ApduResponse> SendAsync(ApduCommand command, bool throwOnError, CancellationToken cancellationToken)
        {
            Assert.Equal(0x03, command.Ins);
            return Task.FromResult(response);
        }
    }

    private sealed class CardDevice(ISmartCardConnection connection) : IYubiKey
    {
        public string DeviceId => "scripted-hsmauth";
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;
        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken) where TConnection : class, IConnection =>
            Task.FromResult(Assert.IsAssignableFrom<TConnection>(connection));
    }

    private sealed class ExtendedCardConnection(params byte[][] responses) : ISmartCardConnection
    {
        public RecordingSmartCardConnection Recorder { get; } = new(responses);
        public bool FailDisposal { get; init; }
        public int DisposeCalls { get; private set; }
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;
        public bool SupportsExtendedApdu() => true;
        public IDisposable BeginTransaction(CancellationToken cancellationToken) => Recorder.BeginTransaction(cancellationToken);
        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken) =>
            Recorder.TransmitAndReceiveAsync(command, cancellationToken);

        public void Dispose()
        {
            DisposeCalls++;
            if (FailDisposal)
                throw new InvalidOperationException("Scripted cleanup failure");
            Recorder.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}