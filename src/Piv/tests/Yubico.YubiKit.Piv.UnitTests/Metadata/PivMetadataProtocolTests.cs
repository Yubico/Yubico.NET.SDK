// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Piv.Backend;
using Yubico.YubiKit.Piv.Metadata;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.Metadata;

public class PivMetadataProtocolTests
{
    [Fact]
    public async Task BlockPukAsync_UnexpectedStatus_ThrowsApduException()
    {
        var (backend, connection) = CreateBackend([0x6A, 0x80]);

        var exception = await Assert.ThrowsAsync<ApduException>(() => PivMetadataProtocol.BlockPukAsync(
            backend,
            NullLogger.Instance,
            TestContext.Current.CancellationToken));

        Assert.True(exception.SW == 0x6A80);
        Assert.Single(connection.TransmittedCommands);
    }

    [Fact]
    public async Task BlockPukAsync_RetryStatusesUntilZero_CompletesAfterExactSequence()
    {
        var (backend, connection) = CreateBackend(
            [0x63, 0xC2],
            [0x63, 0xC1],
            [0x63, 0xC0]);

        await PivMetadataProtocol.BlockPukAsync(
            backend,
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        Assert.Equal(3, connection.TransmittedCommands.Count);
        Assert.All(connection.TransmittedCommands, command =>
        {
            Assert.Equal(0x2C, command[1]);
            Assert.Equal(0x80, command[3]);
        });
    }

    [Fact]
    public async Task BlockPukAsync_AlreadyBlocked_CompletesAfterOneCommand()
    {
        var (backend, connection) = CreateBackend([0x69, 0x83]);

        await PivMetadataProtocol.BlockPukAsync(
            backend,
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        Assert.Single(connection.TransmittedCommands);
    }

    private static (IPivBackend Backend, RecordingSmartCardConnection Connection) CreateBackend(params byte[][] responses)
    {
        var connection = new RecordingSmartCardConnection(responses);
        var protocol = ProtocolFactory.Create(connection);
        return (new PivBackend(protocol), connection);
    }
}