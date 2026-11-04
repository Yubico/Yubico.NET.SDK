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
using Yubico.YubiKit.Core.Protocols;
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
}