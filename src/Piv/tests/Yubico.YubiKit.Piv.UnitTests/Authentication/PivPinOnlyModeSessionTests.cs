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
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.Authentication;

/// <summary>
/// End-to-end (public API) coverage proving <see cref="PivSession"/> wires PIN-only mode through
/// to the underlying protocol helpers (ISC-14, 14.1, 15, 15.1).
/// </summary>
public class PivPinOnlyModeSessionTests
{
    [Fact]
    public async Task GetPinOnlyModeAsync_NoAdminData_ReturnsNone()
    {
        var connection = CreateInitializedConnection([0x6A, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var mode = await session.GetPinOnlyModeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.None, mode);
        Assert.Contains(connection.TransmittedCommands, c => c[1] == 0xCB); // GET DATA
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_NotAuthenticated_ThrowsInvalidOperationException()
    {
        var connection = CreateInitializedConnection();
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SetPinOnlyModeAsync(
            PivPinOnlyMode.PinProtected,
            "123456"u8.ToArray(),
            new byte[24],
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_PinDerivedRequested_ThrowsArgumentException()
    {
        var connection = CreateInitializedConnection();
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);

        await Assert.ThrowsAsync<ArgumentException>(() => session.SetPinOnlyModeAsync(
            PivPinOnlyMode.PinDerived,
            "123456"u8.ToArray(),
            new byte[24],
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RecoverPinOnlyModeAsync_NoPinOnlyDataPresent_ReturnsNoneWithoutAuthenticating()
    {
        // Neither PRINTED nor ADMIN DATA present -> Recover should short-circuit before ever
        // attempting management-key authentication (no GENERAL AUTHENTICATE APDU transmitted).
        var connection = CreateInitializedConnection([0x6A, 0x82], [0x6A, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var mode = await session.RecoverPinOnlyModeAsync("123456"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.None, mode);
        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == 0x87); // no GENERAL AUTHENTICATE
        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == 0x20); // no VERIFY
    }

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), VersionResponse(), ManagementKeyMetadataResponse(), .. trailingResponses]);

    private static void MarkAuthenticated(PivSession session) =>
        typeof(PivSession)
            .GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, true);

    private static byte[] OkResponse() => [0x90, 0x00];

    private static byte[] VersionResponse() => [0x00, 0x00, 0x01, 0x90, 0x00];

    private static byte[] ManagementKeyMetadataResponse() =>
    [
        0x01, 0x01, (byte)PivManagementKeyType.TripleDes,
        0x02, 0x02, 0x00, (byte)PivTouchPolicy.Default,
        0x05, 0x01, 0x01,
        0x90, 0x00
    ];
}
