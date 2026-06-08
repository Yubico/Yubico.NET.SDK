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

using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public sealed class OpenPgpSessionWireTests
{
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
            CommandData(command).SequenceEqual(ApplicationIds.OpenPgp.AsSpan()));
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

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), VersionResponse(), ApplicationRelatedDataResponse(), .. trailingResponses]);

    private static byte[] LastCommand(RecordingSmartCardConnection connection) =>
        connection.TransmittedCommands[^1];

    private static ReadOnlySpan<byte> CommandData(byte[] command) =>
        // Short APDU format: CLA INS P1 P2 Lc Data; the recorder reports SupportsExtendedApdu=false.
        command.AsSpan(5, command[4]);

    // SW 9000: successful APDU response with no data.
    private static byte[] OkResponse() => [0x90, 0x00];

    // OpenPGP version response: BCD 5.8.0 followed by SW 9000.
    private static byte[] VersionResponse() => [0x05, 0x08, 0x00, 0x90, 0x00];

    private static byte[] PwStatusResponse() => [0x00, 0x7F, 0x7F, 0x7F, 0x03, 0x00, 0x03, 0x90, 0x00];

    private static byte[] ApplicationRelatedDataResponse() => [.. BuildApplicationRelatedData(), 0x90, 0x00];

    private static byte[] BuildApplicationRelatedData()
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

        var discretionaryTlvs = new Tlv[]
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

        byte[] discretionaryContent;
        try
        {
            discretionaryContent = TlvHelper.EncodeList(discretionaryTlvs).ToArray();
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
}