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

using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Piv.Authentication;
using Yubico.YubiKit.Piv.Backends;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.Authentication;

/// <summary>
/// Protocol/vector and security tests for PIV PIN-only mode (ISC-14, 14.1, 15, 15.1, 16, 16.1, 16.2).
/// </summary>
public class PinOnlyProtocolTests
{
    private static IBackend CreateBackend(params byte[][] responses)
    {
        var connection = new RecordingSmartCardConnection(responses);
        var protocol = ProtocolFactory.Create(connection);
        return new SmartCardBackend(protocol);
    }

    private static (IBackend Backend, RecordingSmartCardConnection Connection) CreateBackendWithConnection(params byte[][] responses)
    {
        var connection = new RecordingSmartCardConnection(responses);
        var protocol = ProtocolFactory.Create(connection);
        return (new SmartCardBackend(protocol), connection);
    }

    [Fact]
    public async Task GetPinOnlyModeAsync_NoAdminData_ReturnsNone()
    {
        var backend = CreateBackend([0x6A, 0x82]); // File not found -> GetObjectAsync returns empty.

        var mode = await PinOnlyProtocol.GetPinOnlyModeAsync(backend, TestContext.Current.CancellationToken);

        Assert.Equal(PinOnlyMode.None, mode);
    }

    [Fact]
    public async Task GetPinOnlyModeAsync_PinProtectedBitWithOpaqueField_ReturnsPinProtected()
    {
        byte[] adminData = [0x80, 0x06, 0x84, 0x01, 0xAA, 0x81, 0x01, 0x02];
        var backend = CreateBackend([0x53, (byte)adminData.Length, .. adminData, 0x90, 0x00]);

        var mode = await PinOnlyProtocol.GetPinOnlyModeAsync(backend, TestContext.Current.CancellationToken);

        Assert.Equal(PinOnlyMode.PinProtected, mode);
    }

    [Fact]
    public async Task GetPinOnlyModeAsync_SaltPresent_ReturnsPinDerived()
    {
        byte[] salt = new byte[16];
        byte[] adminData = [0x80, 0x15, 0x81, 0x01, 0x00, 0x82, 0x10, .. salt];
        var backend = CreateBackend([0x53, (byte)adminData.Length, .. adminData, 0x90, 0x00]);

        var mode = await PinOnlyProtocol.GetPinOnlyModeAsync(backend, TestContext.Current.CancellationToken);

        Assert.Equal(PinOnlyMode.PinDerived, mode);
    }

    [Fact]
    public async Task GetPinOnlyModeAsync_MalformedAdminData_ReturnsBothUnavailable()
    {
        // Data present but does not decode as ADMIN DATA (wrong outer tag 0x7F instead of 0x80).
        byte[] malformed = [0x7F, 0x02, 0xAA, 0xBB];
        var backend = CreateBackend([0x53, (byte)malformed.Length, .. malformed, 0x90, 0x00]);

        var mode = await PinOnlyProtocol.GetPinOnlyModeAsync(backend, TestContext.Current.CancellationToken);

        Assert.Equal(PinOnlyMode.PinProtectedUnavailable | PinOnlyMode.PinDerivedUnavailable, mode);
    }

    [Theory]
    [InlineData("8800", false)]
    [InlineData("8900", true)]
    [InlineData("883489180102030405060708111213141516171821222324252627288918010203040506070811121314151617182122232425262728", true)]
    public async Task ReadPrintedForRecovery_DoesNotUseEmptyOrMalformedCandidate(string hex, bool malformed)
    {
        var backend = CreateBackend([.. Convert.FromHexString(hex), 0x90, 0x00]);
        using var metadata = await PinOnlyMetadata.ReadForRecoveryAsync(backend, DataObject.PrintedInformation,
            TestContext.Current.CancellationToken);
        if (malformed)
            Assert.Null(metadata);
        else
            Assert.True(Assert.IsType<PinOnlyMetadata>(metadata).StoredKey.IsEmpty);
    }
}