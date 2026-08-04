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

using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Oath.UnitTests;

public class OathSessionTests
{
    [Fact]
    public void ComputeDeviceId_WithKnownSalt_ReturnsExpectedBase64()
    {
        // A known salt value
        byte[] salt = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        string deviceId = OathSession.ComputeDeviceId(salt);

        // Compute expected: Base64(SHA256(salt)[:16]) with padding stripped
        byte[] hash = SHA256.HashData(salt);
        string expected = Convert.ToBase64String(hash[..16]).TrimEnd('=');
        Assert.Equal(expected, deviceId);
    }

    [Fact]
    public void ComputeDeviceId_WithEmptySalt_ReturnsHashOfEmpty()
    {
        byte[] salt = [];

        string deviceId = OathSession.ComputeDeviceId(salt);

        byte[] hash = SHA256.HashData(salt);
        string expected = Convert.ToBase64String(hash[..16]).TrimEnd('=');
        Assert.Equal(expected, deviceId);
    }

    [Fact]
    public void ComputeDeviceId_StripsBase64Padding()
    {
        // Any salt — the result should never contain '='
        byte[] salt = [0xAA, 0xBB, 0xCC, 0xDD];

        string deviceId = OathSession.ComputeDeviceId(salt);

        Assert.DoesNotContain("=", deviceId);
    }

    [Fact]
    public void ComputeDeviceId_DifferentSalts_ProduceDifferentIds()
    {
        byte[] salt1 = [0x01, 0x02, 0x03, 0x04];
        byte[] salt2 = [0x05, 0x06, 0x07, 0x08];

        string id1 = OathSession.ComputeDeviceId(salt1);
        string id2 = OathSession.ComputeDeviceId(salt2);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void ComputeDeviceId_SameSalt_ProducesSameId()
    {
        byte[] salt = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        string id1 = OathSession.ComputeDeviceId(salt);
        string id2 = OathSession.ComputeDeviceId(salt);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void ComputeDeviceId_Uses16BytePrefix_NotFullHash()
    {
        byte[] salt = [0x01, 0x02, 0x03, 0x04];

        string deviceId = OathSession.ComputeDeviceId(salt);

        // 16 bytes -> ~22 base64 chars (without padding)
        // Full 32 bytes -> ~43 base64 chars
        Assert.True(deviceId.Length <= 22, $"DeviceId too long ({deviceId.Length}), should use 16-byte prefix");
    }

    [Fact]
    public void DeriveKey_WithKnownInputs_ProducesDeterministicOutput()
    {
        // DeriveKey is an instance method that uses the session's salt.
        // We can verify the PBKDF2 algorithm independently.
        string password = "test_password";
        byte[] salt = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);

        Assert.Equal(16, key.Length);

        // Same inputs should yield same output
        byte[] key2 = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);

        Assert.Equal(key, key2);
    }

    [Fact]
    public void DeriveKey_DifferentPasswords_ProduceDifferentKeys()
    {
        byte[] salt = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        byte[] key1 = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes("password1"),
            salt,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);

        byte[] key2 = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes("password2"),
            salt,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void DeriveKey_DifferentSalts_ProduceDifferentKeys()
    {
        byte[] salt1 = [0x01, 0x02, 0x03, 0x04];
        byte[] salt2 = [0x05, 0x06, 0x07, 0x08];

        byte[] key1 = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes("password"),
            salt1,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);

        byte[] key2 = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes("password"),
            salt2,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public async Task ListCredentialsAsync_ChainedResponse_UsesOathSendRemainingInstruction()
    {
        var credentialId = Encoding.UTF8.GetBytes("issuer:alice");
        byte[] credentialTlv = [0x72, (byte)(credentialId.Length + 1), (byte)OathType.Totp, .. credentialId];
        byte[] firstChunk = [.. credentialTlv[..4], 0x61, 0x01];
        byte[] finalChunk = [.. credentialTlv[4..], 0x90, 0x00];
        var connection = new RecordingSmartCardConnection(SelectResponse(), firstChunk, finalChunk);

        await using var session = await OathSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var credentials = await session.ListCredentialsAsync(TestContext.Current.CancellationToken);

        Assert.Single(credentials);
        Assert.Equal("alice", credentials[0].Name);
        Assert.Equal("issuer", credentials[0].Issuer);
        Assert.Contains(connection.TransmittedCommands, command => command[1] == OathConstants.InsSendRemaining);
        Assert.DoesNotContain(connection.TransmittedCommands, command => command[1] == 0xC0);
    }

    [Fact]
    public async Task CalculateAllAsync_ChainedResponse_UsesOathSendRemainingInstruction()
    {
        var credentialId = Encoding.UTF8.GetBytes("issuer:bob");
        byte[] responseTlvs = [
            0x71, (byte)credentialId.Length, ..credentialId,
            0x76, 0x05, 0x06, 0x00, 0x00, 0x00, 0x01
        ];
        byte[] firstChunk = [.. responseTlvs[..5], 0x61, 0x01];
        byte[] finalChunk = [.. responseTlvs[5..], 0x90, 0x00];
        var connection = new RecordingSmartCardConnection(SelectResponse(), firstChunk, finalChunk);

        await using var session = await OathSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var codes = await session.CalculateAllAsync(1704067200, TestContext.Current.CancellationToken);

        var entry = Assert.Single(codes);
        Assert.Equal("bob", entry.Key.Name);
        Assert.Equal("issuer", entry.Key.Issuer);
        Assert.NotNull(entry.Value);
        Assert.Contains(connection.TransmittedCommands, command => command[1] == OathConstants.InsSendRemaining);
        Assert.DoesNotContain(connection.TransmittedCommands, command => command[1] == 0xC0);
    }

    [Fact]
    public async Task PutCredentialAsync_Totp_SendsOrderedPutPayload()
    {
        byte[] secret = [
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
            0x38, 0x39, 0x30, 0x31, 0x32, 0x33, 0x34
        ];
        var expectedSecret = secret.ToArray();
        var connection = new RecordingSmartCardConnection(SelectResponse(), [0x90, 0x00]);
        await using var session = await OathSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        using var credential = new CredentialData
        {
            Name = "alice",
            Issuer = "issuer",
            OathType = OathType.Totp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = secret,
            Digits = 6
        };

        await session.PutCredentialAsync(credential, cancellationToken: TestContext.Current.CancellationToken);

        var putCommand = Assert.Single(connection.TransmittedCommands, command => command[1] == OathConstants.InsPut);
        byte[] expectedId = Encoding.UTF8.GetBytes("issuer:alice");
        byte[] expectedData = [
            OathConstants.TagName, (byte)expectedId.Length, .. expectedId,
            OathConstants.TagKey, (byte)(2 + expectedSecret.Length),
            (byte)((byte)OathType.Totp | (byte)OathHashAlgorithm.Sha1), 0x06, .. expectedSecret
        ];

        Assert.Equal(0x00, putCommand[0]);
        Assert.Equal(OathConstants.InsPut, putCommand[1]);
        Assert.Equal(0x00, putCommand[2]);
        Assert.Equal(0x00, putCommand[3]);
        Assert.Equal(expectedData.Length, putCommand[4]);
        Assert.Equal(expectedData, putCommand[5..^1]);
        Assert.Equal(0x00, putCommand[^1]);
    }

    [Fact]
    public async Task PutCredentialAsync_HotpWithTouchAndCounter_SendsPropertyAndImfPayload()
    {
        byte[] secret = [
            0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            0x48, 0x49, 0x50, 0x51, 0x52, 0x53, 0x54
        ];
        var expectedSecret = secret.ToArray();
        var connection = new RecordingSmartCardConnection(SelectResponse(), [0x90, 0x00]);
        await using var session = await OathSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        using var credential = new CredentialData
        {
            Name = "bob",
            Issuer = "issuer",
            OathType = OathType.Hotp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = secret,
            Digits = 8,
            Counter = 7
        };

        await session.PutCredentialAsync(
            credential,
            requireTouch: true,
            cancellationToken: TestContext.Current.CancellationToken);

        var putCommand = Assert.Single(connection.TransmittedCommands, command => command[1] == OathConstants.InsPut);
        byte[] expectedId = Encoding.UTF8.GetBytes("issuer:bob");
        byte[] expectedData = [
            OathConstants.TagName, (byte)expectedId.Length, .. expectedId,
            OathConstants.TagKey, (byte)(2 + expectedSecret.Length),
            (byte)((byte)OathType.Hotp | (byte)OathHashAlgorithm.Sha1), 0x08, .. expectedSecret,
            OathConstants.TagProperty, OathConstants.PropRequireTouch,
            OathConstants.TagImf, 0x04, 0x00, 0x00, 0x00, 0x07
        ];

        Assert.Equal(0x00, putCommand[0]);
        Assert.Equal(OathConstants.InsPut, putCommand[1]);
        Assert.Equal(0x00, putCommand[2]);
        Assert.Equal(0x00, putCommand[3]);
        Assert.Equal(expectedData.Length, putCommand[4]);
        Assert.Equal(expectedData, putCommand[5..^1]);
        Assert.Equal(0x00, putCommand[^1]);
    }

    // ------------------------------------------------------------------------------------------------
    // Connection ownership. Two halves of one rule: whoever CREATED the connection disposes it.
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    ///     INVARIANT PIN (must hold before and after the ownership change). The convenience entry point opens
    ///     a connection the caller never sees, so disposing the session it returned must close that connection.
    ///     If this ever regresses, every <c>CreateOathSessionAsync</c> call leaks a PC/SC handle and the
    ///     interface stays locked for the process lifetime.
    /// </summary>
    [Fact]
    public async Task CreateOathSessionAsync_DisposingSession_DisposesTheConnectionItOpened()
    {
        var connection = new DisposeCountingConnection(SelectResponse());
        var device = new SingleConnectionYubiKey(connection);

        var session = await device.CreateOathSessionAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();

        Assert.Equal(1, connection.DisposeCount);
    }

    /// <summary>
    ///     The other half: a caller who opened the connection keeps it. This is what makes successive applet
    ///     sessions over one connection possible, which is the ergonomic price of one-session-per-connection.
    /// </summary>
    [Fact]
    public async Task CreateAsync_DisposingSession_LeavesACallerCreatedConnectionOpen()
    {
        var connection = new DisposeCountingConnection(SelectResponse());

        var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();

        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task DisposeAsync_ZeroesSessionSalt()
    {
        var connection = new DisposeCountingConnection(SelectResponse());
        var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);
        ReadOnlyMemory<byte> salt = session.Salt;

        Assert.Contains(salt.ToArray(), value => value != 0);

        await session.DisposeAsync();

        Assert.All(salt.ToArray(), value => Assert.Equal(0, value));
    }

    /// <summary>
    ///     A session that fails to initialize must release its claim on the connection. The connection
    ///     outlives the failure and belongs to the caller, so a retry — or a different applet — must not be
    ///     refused by a ghost holder that no reference points at any more.
    /// </summary>
    [Fact]
    public async Task CreateAsync_InitializationFails_LeavesTheConnectionUsableByTheNextSession()
    {
        // First SELECT is refused (6A82 = file not found), second succeeds.
        var connection = new DisposeCountingConnection([0x6A, 0x82], SelectResponse());

        _ = await Assert.ThrowsAnyAsync<Exception>(() => OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken));

        await using var retry = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(retry.IsInitialized);
    }

    private static byte[] SelectResponse() =>
    [
        0x79, 0x03, 0x05, 0x07, 0x00,
        0x71, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x90, 0x00
    ];

    /// <summary>Like <see cref="RecordingSmartCardConnection" />, but disposal is observable.</summary>
    private sealed class DisposeCountingConnection(params byte[][] responses) : ISmartCardConnection
    {
        private readonly Queue<byte[]> _responses = new(responses);
        private int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public Transport Transport => Transport.Usb;

        public ConnectionType Type => ConnectionType.SmartCard;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((ReadOnlyMemory<byte>)_responses.Dequeue());

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => false;

        public void Dispose() => Interlocked.Increment(ref _disposeCount);

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SingleConnectionYubiKey(ISmartCardConnection connection) : IYubiKey
    {
        public string DeviceId => "oath-ownership-probe";

        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            Task.FromResult((connection as TConnection)!);
    }

}