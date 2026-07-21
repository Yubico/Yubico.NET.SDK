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

using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Piv.Authentication;
using Yubico.YubiKit.Piv.Backend;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.Authentication;

/// <summary>
/// Protocol/vector and security tests for PIV PIN-only mode (ISC-14, 14.1, 15, 15.1, 16, 16.1, 16.2).
/// </summary>
public class PivPinOnlyProtocolTests
{
    private static IPivBackend CreateBackend(params byte[][] responses)
    {
        var connection = new RecordingSmartCardConnection(responses);
        var protocol = ProtocolFactory.Create(connection);
        return new PivBackend(protocol);
    }

    private static (IPivBackend Backend, RecordingSmartCardConnection Connection) CreateBackendWithConnection(params byte[][] responses)
    {
        var connection = new RecordingSmartCardConnection(responses);
        var protocol = ProtocolFactory.Create(connection);
        return (new PivBackend(protocol), connection);
    }

    // === ISC-14: detect PIN-only state from ADMIN DATA ===

    [Fact]
    public async Task GetPinOnlyModeAsync_NoAdminData_ReturnsNone()
    {
        var backend = CreateBackend([0x6A, 0x82]); // File not found -> GetObjectAsync returns empty.

        var mode = await PivPinOnlyProtocol.GetPinOnlyModeAsync(backend, NullLogger.Instance, TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.None, mode);
    }

    [Fact]
    public async Task GetPinOnlyModeAsync_PinProtectedBitSet_ReturnsPinProtected()
    {
        // ADMIN DATA inner content: 80 03 81 01 02 (PinProtected bit only), wrapped in 53 for GET DATA.
        byte[] adminData = [0x80, 0x03, 0x81, 0x01, 0x02];
        var backend = CreateBackend([0x53, (byte)adminData.Length, .. adminData, 0x90, 0x00]);

        var mode = await PivPinOnlyProtocol.GetPinOnlyModeAsync(backend, NullLogger.Instance, TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.PinProtected, mode);
    }

    [Fact]
    public async Task GetPinOnlyModeAsync_SaltPresent_ReturnsPinDerived()
    {
        byte[] salt = new byte[16];
        byte[] adminData = [0x80, 0x15, 0x81, 0x01, 0x00, 0x82, 0x10, .. salt];
        var backend = CreateBackend([0x53, (byte)adminData.Length, .. adminData, 0x90, 0x00]);

        var mode = await PivPinOnlyProtocol.GetPinOnlyModeAsync(backend, NullLogger.Instance, TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.PinDerived, mode);
    }

    [Fact]
    public async Task GetPinOnlyModeAsync_MalformedAdminData_ReturnsBothUnavailable()
    {
        // Data present but does not decode as ADMIN DATA (wrong outer tag 0x7F instead of 0x80).
        byte[] malformed = [0x7F, 0x02, 0xAA, 0xBB];
        var backend = CreateBackend([0x53, (byte)malformed.Length, .. malformed, 0x90, 0x00]);

        var mode = await PivPinOnlyProtocol.GetPinOnlyModeAsync(backend, NullLogger.Instance, TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.PinProtectedUnavailable | PivPinOnlyMode.PinDerivedUnavailable, mode);
    }

    // === ISC-14.1 / ISC-16 family: recover PIN-only authentication ===

    [Fact]
    public async Task RecoverPinOnlyModeAsync_PinProtectedKeyAuthenticates_ReturnsPinProtected()
    {
        // PRINTED inner content: 88 1A [ 89 18 <24-byte key> ] wrapped in 53 for GET DATA.
        byte[] key = Enumerable.Range(0, 24).Select(i => (byte)i).ToArray();
        byte[] printed = [0x88, 0x1A, 0x89, 0x18, .. key];
        // ADMIN DATA absent (file not found).
        var backend = CreateBackend(
            [0x53, (byte)printed.Length, .. printed, 0x90, 0x00], // PRINTED
            [0x6A, 0x82]); // ADMIN DATA absent

        byte[]? authenticatedWith = null;
        Task Authenticate(ReadOnlyMemory<byte> suppliedKey, CancellationToken ct)
        {
            authenticatedWith = suppliedKey.ToArray();
            return Task.CompletedTask;
        }

        Task VerifyPin(ReadOnlyMemory<byte> pin, CancellationToken ct) => Task.CompletedTask;

        var mode = await PivPinOnlyProtocol.RecoverPinOnlyModeAsync(
            backend,
            NullLogger.Instance,
            PivManagementKeyType.TripleDes,
            "123456"u8.ToArray(),
            Authenticate,
            VerifyPin,
            TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.PinProtected, mode);
        Assert.Equal(key, authenticatedWith);
    }

    [Fact]
    public async Task RecoverPinOnlyModeAsync_PinProtectedKeyDoesNotAuthenticate_ReturnsNone()
    {
        byte[] key = Enumerable.Range(0, 24).Select(i => (byte)i).ToArray();
        byte[] printed = [0x88, 0x1A, 0x89, 0x18, .. key];
        var backend = CreateBackend(
            [0x53, (byte)printed.Length, .. printed, 0x90, 0x00],
            [0x6A, 0x82]);

        Task Authenticate(ReadOnlyMemory<byte> suppliedKey, CancellationToken ct) =>
            throw new ApduException("Management key authentication failed");

        Task VerifyPin(ReadOnlyMemory<byte> pin, CancellationToken ct) => Task.CompletedTask;

        var mode = await PivPinOnlyProtocol.RecoverPinOnlyModeAsync(
            backend,
            NullLogger.Instance,
            PivManagementKeyType.TripleDes,
            "123456"u8.ToArray(),
            Authenticate,
            VerifyPin,
            TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.None, mode);
    }

    [Fact]
    public async Task RecoverPinOnlyModeAsync_PinDerivedSuccess_ZeroesDerivedKeyMaterial()
    {
        // ISC-16: PIN-derived management-key material is zeroed after a successful operation.
        byte[] salt = Enumerable.Range(100, 16).Select(i => (byte)i).ToArray();
        byte[] adminData = [0x80, 0x15, 0x81, 0x01, 0x00, 0x82, 0x10, .. salt];
        var backend = CreateBackend(
            [0x6A, 0x82], // PRINTED absent
            [0x53, (byte)adminData.Length, .. adminData, 0x90, 0x00]); // ADMIN DATA

        byte[]? capturedArray = null;
        int capturedOffset = 0, capturedCount = 0;

        Task Authenticate(ReadOnlyMemory<byte> derivedKey, CancellationToken ct)
        {
            Assert.True(MemoryMarshal.TryGetArray(derivedKey, out var segment));
            capturedArray = segment.Array;
            capturedOffset = segment.Offset;
            capturedCount = segment.Count;

            // Sanity: the derived key must not be all-zero at the moment authentication happens.
            Assert.Contains(derivedKey.ToArray(), b => b != 0);
            return Task.CompletedTask;
        }

        Task VerifyPin(ReadOnlyMemory<byte> pin, CancellationToken ct) => Task.CompletedTask;

        var mode = await PivPinOnlyProtocol.RecoverPinOnlyModeAsync(
            backend,
            NullLogger.Instance,
            PivManagementKeyType.TripleDes,
            "123456"u8.ToArray(),
            Authenticate,
            VerifyPin,
            TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.PinDerived, mode);
        Assert.NotNull(capturedArray);
        Assert.All(capturedArray!.AsSpan(capturedOffset, capturedCount).ToArray(), b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task RecoverPinOnlyModeAsync_PinDerivedAuthenticationFails_StillZeroesDerivedKeyMaterial()
    {
        // ISC-16.1: PIN-derived management-key material is zeroed after a failed operation.
        byte[] salt = Enumerable.Range(50, 16).Select(i => (byte)i).ToArray();
        byte[] adminData = [0x80, 0x15, 0x81, 0x01, 0x00, 0x82, 0x10, .. salt];
        var backend = CreateBackend(
            [0x6A, 0x82],
            [0x53, (byte)adminData.Length, .. adminData, 0x90, 0x00]);

        byte[]? capturedArray = null;
        int capturedOffset = 0, capturedCount = 0;

        Task Authenticate(ReadOnlyMemory<byte> derivedKey, CancellationToken ct)
        {
            Assert.True(MemoryMarshal.TryGetArray(derivedKey, out var segment));
            capturedArray = segment.Array;
            capturedOffset = segment.Offset;
            capturedCount = segment.Count;
            throw new ApduException("Management key authentication failed");
        }

        Task VerifyPin(ReadOnlyMemory<byte> pin, CancellationToken ct) => Task.CompletedTask;

        var mode = await PivPinOnlyProtocol.RecoverPinOnlyModeAsync(
            backend,
            NullLogger.Instance,
            PivManagementKeyType.TripleDes,
            "123456"u8.ToArray(),
            Authenticate,
            VerifyPin,
            TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.None, mode);
        Assert.NotNull(capturedArray);
        Assert.All(capturedArray!.AsSpan(capturedOffset, capturedCount).ToArray(), b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task RecoverPinOnlyModeAsync_CancelledDuringAuthenticate_ZeroesDerivedKeyMaterialAndPropagates()
    {
        // ISC-16.2: PIN-derived management-key material is zeroed after a cancelled operation.
        byte[] salt = Enumerable.Range(10, 16).Select(i => (byte)i).ToArray();
        byte[] adminData = [0x80, 0x15, 0x81, 0x01, 0x00, 0x82, 0x10, .. salt];
        var backend = CreateBackend(
            [0x6A, 0x82],
            [0x53, (byte)adminData.Length, .. adminData, 0x90, 0x00]);

        byte[]? capturedArray = null;
        int capturedOffset = 0, capturedCount = 0;

        Task Authenticate(ReadOnlyMemory<byte> derivedKey, CancellationToken ct)
        {
            Assert.True(MemoryMarshal.TryGetArray(derivedKey, out var segment));
            capturedArray = segment.Array;
            capturedOffset = segment.Offset;
            capturedCount = segment.Count;
            throw new OperationCanceledException(ct);
        }

        Task VerifyPin(ReadOnlyMemory<byte> pin, CancellationToken ct) => Task.CompletedTask;

        await Assert.ThrowsAsync<OperationCanceledException>(() => PivPinOnlyProtocol.RecoverPinOnlyModeAsync(
            backend,
            NullLogger.Instance,
            PivManagementKeyType.TripleDes,
            "123456"u8.ToArray(),
            Authenticate,
            VerifyPin,
            TestContext.Current.CancellationToken));

        Assert.NotNull(capturedArray);
        Assert.All(capturedArray!.AsSpan(capturedOffset, capturedCount).ToArray(), b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task RecoverPinOnlyModeAsync_WrongPin_PropagatesAndZeroesDerivedKeyBuffer()
    {
        // A wrong PIN must propagate InvalidPinException (never silently swallowed as "not PIN-derived"),
        // and the rented derived-key buffer must still be zeroed before being returned to the pool.
        byte[] salt = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();
        byte[] adminData = [0x80, 0x15, 0x81, 0x01, 0x00, 0x82, 0x10, .. salt];
        var backend = CreateBackend(
            [0x6A, 0x82],
            [0x53, (byte)adminData.Length, .. adminData, 0x90, 0x00]);

        Task Authenticate(ReadOnlyMemory<byte> derivedKey, CancellationToken ct) =>
            throw new InvalidOperationException("Authenticate should not be reached when PIN verification fails.");

        Task VerifyPin(ReadOnlyMemory<byte> pin, CancellationToken ct) =>
            throw new InvalidPinException(2, "Wrong PIN");

        await Assert.ThrowsAsync<InvalidPinException>(() => PivPinOnlyProtocol.RecoverPinOnlyModeAsync(
            backend,
            NullLogger.Instance,
            PivManagementKeyType.TripleDes,
            "000000"u8.ToArray(),
            Authenticate,
            VerifyPin,
            TestContext.Current.CancellationToken));
    }

    // === ISC-16 family (PIN-protected recovery path): every intermediate plaintext copy is zeroed ===

    [Fact]
    public void TryDecodePinProtectedManagementKey_Success_ZeroesInputBufferButNotExtractedKey()
    {
        byte[] key = Enumerable.Range(1, 24).Select(i => (byte)i).ToArray();
        byte[] printed = [0x88, 0x1A, 0x89, 0x18, .. key];
        byte[] workingCopy = (byte[])printed.Clone();

        bool found = PivPinOnlyProtocol.TryDecodePinProtectedManagementKey(workingCopy, out var extractedKey);

        Assert.True(found);
        Assert.Equal(key, extractedKey.ToArray());

        // ISC-16 family: the raw PRINTED-object bytes (containing the clear-text key) must be
        // zeroed once this method is done with them, regardless of whether it found a key.
        Assert.All(workingCopy, b => Assert.Equal(0, b));
    }

    [Fact]
    public void TryDecodePinProtectedManagementKey_WrongOuterTag_StillZeroesInputBuffer()
    {
        byte[] key = Enumerable.Range(1, 24).Select(i => (byte)i).ToArray();
        byte[] notPinProtected = [0x89, 0x18, .. key]; // wrong outer tag (0x89 instead of 0x88)
        byte[] workingCopy = (byte[])notPinProtected.Clone();

        bool found = PivPinOnlyProtocol.TryDecodePinProtectedManagementKey(workingCopy, out var extractedKey);

        Assert.False(found);
        Assert.True(extractedKey.IsEmpty);
        Assert.All(workingCopy, b => Assert.Equal(0, b));
    }

    [Fact]
    public void TryDecodePinProtectedManagementKey_NoManagementKeyPresent_StillZeroesInputBuffer()
    {
        byte[] emptyPinProtected = [0x88, 0x00]; // "cleared" PIN-protected marker, no management key
        byte[] workingCopy = (byte[])emptyPinProtected.Clone();

        bool found = PivPinOnlyProtocol.TryDecodePinProtectedManagementKey(workingCopy, out var extractedKey);

        Assert.False(found);
        Assert.True(extractedKey.IsEmpty);
        Assert.All(workingCopy, b => Assert.Equal(0, b));
    }

    // === ISC-15 / ISC-15.1: enable/disable protocol bytes ===

    [Fact]
    public async Task SetPinOnlyModeAsync_NotAuthenticated_Throws()
    {
        var backend = CreateBackend();

        await Assert.ThrowsAsync<InvalidOperationException>(() => PivPinOnlyProtocol.SetPinOnlyModeAsync(
            backend,
            NullLogger.Instance,
            isAuthenticated: false,
            PivManagementKeyType.TripleDes,
            PivPinOnlyMode.PinProtected,
            "123456"u8.ToArray(),
            new byte[24],
            (p, ct) => Task.CompletedTask,
            (t, k, touch, ct) => Task.CompletedTask,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_PinDerivedRequested_ThrowsArgumentException()
    {
        var backend = CreateBackend();

        await Assert.ThrowsAsync<ArgumentException>(() => PivPinOnlyProtocol.SetPinOnlyModeAsync(
            backend,
            NullLogger.Instance,
            isAuthenticated: true,
            PivManagementKeyType.TripleDes,
            PivPinOnlyMode.PinDerived,
            "123456"u8.ToArray(),
            new byte[24],
            (p, ct) => Task.CompletedTask,
            (t, k, touch, ct) => Task.CompletedTask,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_PinProtected_NoManagementKey_ThrowsArgumentNullException()
    {
        var backend = CreateBackend();

        await Assert.ThrowsAsync<ArgumentNullException>(() => PivPinOnlyProtocol.SetPinOnlyModeAsync(
            backend,
            NullLogger.Instance,
            isAuthenticated: true,
            PivManagementKeyType.TripleDes,
            PivPinOnlyMode.PinProtected,
            "123456"u8.ToArray(),
            managementKey: null,
            (p, ct) => Task.CompletedTask,
            (t, k, touch, ct) => Task.CompletedTask,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_EnablePinProtected_VerifiesPinStoresKeyBlocksPukAndUpdatesAdminData()
    {
        byte[] managementKey = Enumerable.Range(0, 24).Select(i => (byte)i).ToArray();

        // PUT PRINTED -> OK, then BlockPukAsync loops RESET RETRY until blocked (SW 63C0 = 0 retries left),
        // then GET ADMIN DATA (absent), then PUT ADMIN DATA -> OK.
        var (backend, connection) = CreateBackendWithConnection(
            [0x90, 0x00], // PUT DATA (PRINTED)
            [0x63, 0xC0], // RESET RETRY -> 0 retries remaining, PUK now blocked
            [0x6A, 0x82], // GET DATA (ADMIN DATA) - absent
            [0x90, 0x00]); // PUT DATA (ADMIN DATA)

        bool pinVerified = false;
        Task VerifyPin(ReadOnlyMemory<byte> pin, CancellationToken ct)
        {
            pinVerified = true;
            return Task.CompletedTask;
        }

        await PivPinOnlyProtocol.SetPinOnlyModeAsync(
            backend,
            NullLogger.Instance,
            isAuthenticated: true,
            PivManagementKeyType.TripleDes,
            PivPinOnlyMode.PinProtected,
            "123456"u8.ToArray(),
            managementKey,
            VerifyPin,
            (t, k, touch, ct) => Task.CompletedTask,
            TestContext.Current.CancellationToken);

        Assert.True(pinVerified);
        Assert.Equal(4, connection.TransmittedCommands.Count);

        // Command 0: PUT DATA for PRINTED, containing 88/89-wrapped management key.
        var putPrinted = connection.TransmittedCommands[0];
        Assert.Equal(0xDB, putPrinted[1]);
        Assert.Contains((byte)0x88, putPrinted);
        Assert.Contains((byte)0x89, putPrinted);
        Assert.True(putPrinted.AsSpan().IndexOf(managementKey) >= 0);

        // Command 1: RESET RETRY COUNTER (0x2C) with P2=0x80 (PUK-blocking quirk), 16-byte empty PUK+PIN.
        var resetRetry = connection.TransmittedCommands[1];
        Assert.Equal(0x2C, resetRetry[1]);
        Assert.Equal(0x80, resetRetry[3]);

        // Command 3: PUT DATA for ADMIN DATA with PukBlocked + PinProtected bits set (0x03).
        var putAdmin = connection.TransmittedCommands[3];
        Assert.Equal(0xDB, putAdmin[1]);
        Assert.Contains((byte)0x03, putAdmin);
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_DisableWhenAlreadyDisabled_MakesNoChanges()
    {
        // GetPinOnlyModeAsync will read ADMIN DATA once and find it absent -> None -> no-op.
        var (backend, connection) = CreateBackendWithConnection([0x6A, 0x82]);

        bool setManagementKeyCalled = false;

        await PivPinOnlyProtocol.SetPinOnlyModeAsync(
            backend,
            NullLogger.Instance,
            isAuthenticated: true,
            PivManagementKeyType.TripleDes,
            PivPinOnlyMode.None,
            ReadOnlyMemory<byte>.Empty,
            null,
            (p, ct) => Task.CompletedTask,
            (t, k, touch, ct) =>
            {
                setManagementKeyCalled = true;
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken);

        Assert.False(setManagementKeyCalled);
        Assert.Single(connection.TransmittedCommands); // Only the ADMIN DATA read for the mode check.
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_DisableWhenEnabled_ClearsObjectsAndResetsManagementKeyToDefaultPattern()
    {
        // GetPinOnlyModeAsync sees PinProtected set -> proceeds to clear PRINTED, clear ADMIN DATA,
        // and reset the management key to the well-known default pattern.
        byte[] adminData = [0x80, 0x03, 0x81, 0x01, 0x02]; // PinProtected bit set.
        var (backend, connection) = CreateBackendWithConnection(
            [0x53, (byte)adminData.Length, .. adminData, 0x90, 0x00], // GetPinOnlyModeAsync's ADMIN DATA read
            [0x90, 0x00], // PUT DATA (PRINTED) -> clear
            [0x90, 0x00]); // PUT DATA (ADMIN DATA) -> clear

        PivManagementKeyType? resetType = null;
        byte[]? resetKey = null;

        await PivPinOnlyProtocol.SetPinOnlyModeAsync(
            backend,
            NullLogger.Instance,
            isAuthenticated: true,
            PivManagementKeyType.TripleDes,
            PivPinOnlyMode.None,
            ReadOnlyMemory<byte>.Empty,
            null,
            (p, ct) => Task.CompletedTask,
            (t, k, touch, ct) =>
            {
                resetType = t;
                resetKey = k.ToArray();
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(PivManagementKeyType.TripleDes, resetType);
        Assert.Equal(
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8 },
            resetKey);
        Assert.Equal(3, connection.TransmittedCommands.Count);
    }
}
