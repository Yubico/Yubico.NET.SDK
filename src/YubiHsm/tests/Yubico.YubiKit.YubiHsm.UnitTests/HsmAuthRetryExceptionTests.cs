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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.YubiHsm.UnitTests;

/// <summary>
///     Proves ISC-31/31.1/31.2: every retry-counted YubiHSM Auth operation (credential
///     management-key operations, the management-key change operation, and credential-password
///     session-key operations) surfaces retry information through the typed
///     <see cref="HsmAuthRetryException.RetriesRemaining" /> property, not through parsing
///     <see cref="Exception.Message" />.
/// </summary>
public class HsmAuthRetryExceptionTests
{
    [Fact]
    public void HsmAuthRetryException_IsApduException_ForBackwardCompatibleCatchSites()
    {
        var exception = new HsmAuthRetryException(3, "test");

        Assert.IsAssignableFrom<ApduException>(exception);
    }

    [Fact]
    public async Task PutCredentialSymmetricAsync_WhenManagementKeyRetryFailure_ExposesRetriesRemainingWithoutMessageParsing()
    {
        // SW 63C4: management-key verification failed for a credential-operation (ISC-31.1),
        // 4 attempts remaining.
        var connection = CreateInitializedConnection([0x63, 0xC4]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.PutCredentialSymmetricAsync(
            Sequence(0x10, 16),
            "cred",
            Sequence(0x20, 16),
            Sequence(0x30, 16),
            "pass"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(4, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x63C4), exception.SW);
    }

    [Fact]
    public async Task PutManagementKeyAsync_WhenCurrentManagementKeyRetryFailure_ExposesRetriesRemaining()
    {
        // SW 63C1: management-key operation itself (ISC-31.1's other named case), 1 attempt left.
        var connection = CreateInitializedConnection([0x63, 0xC1]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.PutManagementKeyAsync(
            Sequence(0x10, 16),
            Sequence(0x20, 16),
            TestContext.Current.CancellationToken));

        Assert.Equal(1, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x63C1), exception.SW);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCredentialPasswordRetryFailure_ExposesRetriesRemaining()
    {
        // SW 63C6: credential-password verification failed calculating session keys (ISC-31.2),
        // 6 attempts remaining.
        var connection = CreateInitializedConnection([0x63, 0xC6]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.CalculateSessionKeysSymmetricAsync(
            "cred",
            Sequence(0x40, 16),
            "pass"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(6, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x63C6), exception.SW);
    }

    [Fact]
    public async Task ChangeCredentialPasswordAsync_WhenCurrentPasswordRetryFailure_ExposesRetriesRemaining()
    {
        // SW 63C0: credential-operation password change (ISC-31), 0 attempts remaining.
        var connection = CreateInitializedConnection([0x63, 0xC0]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 8, 0),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.ChangeCredentialPasswordAsync(
            "cred",
            "oldpass"u8.ToArray(),
            "newpass"u8.ToArray(),
            TestContext.Current.CancellationToken));

        Assert.Equal(0, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x63C0), exception.SW);
    }

    [Fact]
    public async Task PutCredentialAsymmetricAsync_WhenManagementKeyRetryFailure_ExposesRetriesRemaining()
    {
        // SW 63C3: management-key verification failed for an asymmetric PUT (ISC-31.1), 3 attempts remaining.
        var connection = CreateInitializedConnection([0x63, 0xC3]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 6, 0),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.PutCredentialAsymmetricAsync(
            Sequence(0x10, 16),
            "cred",
            Sequence(0x20, 32),
            "pass"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(3, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x63C3), exception.SW);
        Assert.Contains("Management key verification failed", exception.Message);
    }

    [Fact]
    public async Task GenerateCredentialAsymmetricAsync_WhenManagementKeyRetryFailure_ExposesRetriesRemaining()
    {
        // SW 63C7: management-key verification failed for asymmetric GENERATE (ISC-31.1), 7 attempts remaining.
        var connection = CreateInitializedConnection([0x63, 0xC7]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 6, 0),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.GenerateCredentialAsymmetricAsync(
            Sequence(0x10, 16),
            "cred",
            "pass"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(7, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x63C7), exception.SW);
        Assert.Contains("Management key verification failed", exception.Message);
    }

    [Fact]
    public async Task ChangeCredentialPasswordAdminAsync_WhenManagementKeyRetryFailure_ExposesRetriesRemaining()
    {
        // SW 63C2: management-key verification failed for admin password change (ISC-31.1), 2 attempts remaining.
        var connection = CreateInitializedConnection([0x63, 0xC2]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 8, 0),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.ChangeCredentialPasswordAdminAsync(
            Sequence(0x10, 16),
            "cred",
            "newpass"u8.ToArray(),
            TestContext.Current.CancellationToken));

        Assert.Equal(2, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x63C2), exception.SW);
        Assert.Contains("Management key verification failed", exception.Message);
    }

    [Fact]
    public async Task CalculateSessionKeysAsymmetricAsync_WhenCredentialPasswordRetryFailure_ExposesRetriesRemaining()
    {
        // SW 63C5: credential-password verification failed calculating asymmetric session keys
        // (ISC-31.2), 5 attempts remaining.
        var connection = CreateInitializedConnection([0x63, 0xC5]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 6, 0),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.CalculateSessionKeysAsymmetricAsync(
            "cred",
            Sequence(0x40, 16),
            Sequence(0x60, 65),
            "pass"u8.ToArray(),
            Sequence(0x70, 8),
            TestContext.Current.CancellationToken));

        Assert.Equal(5, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x63C5), exception.SW);
        Assert.Contains("Invalid credential password", exception.Message);
    }

    [Fact]
    public async Task PutCredentialDerivedAsync_WhenManagementKeyRetryFailure_ExposesRetriesRemaining()
    {
        // SW 63C9: management-key verification failed for a derived-key PUT (ISC-31.1), 9 attempts
        // remaining. PutCredentialDerivedAsync delegates to PutCredentialSymmetricAsync's PUT
        // command after deriving keys, rather than having its own TransmitWithRetryCheckAsync call
        // site; this test guards that delegation continuing to surface retry information.
        var connection = CreateInitializedConnection([0x63, 0xC9]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.PutCredentialDerivedAsync(
            Sequence(0x10, 16),
            "cred",
            "derivationPassword"u8.ToArray(),
            "pass"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(9, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x63C9), exception.SW);
        Assert.Contains("Management key verification failed", exception.Message);
    }

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([[0x90, 0x00], .. trailingResponses]);

    private static byte[] Sequence(byte start, int length)
    {
        var bytes = new byte[length];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)(start + i);

        return bytes;
    }
}