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
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.UnitTests.Cryptography;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.AuditV2;

/// <summary>
///     Audit #23 (YESDK-1631): an SCP response whose data field is shorter than the 8-byte R-MAC
///     (GlobalPlatform SCP03 Amd D §6.2.5, S8 mode) must be rejected with a well-typed SDK error.
/// </summary>
[Collection(CryptographyProvidersCollection.Name)]
public class CoreScpAuditReproTests
{
    /// <summary>
    ///     Data lengths 1..5 make <c>msgLength = data.Length - 8 + 2</c> negative, which reaches
    ///     <c>stackalloc</c> and terminates the process. Runs in a child process.
    /// </summary>
    [Fact]
    [Trait("Audit", "YESDK-1631")]
    public async Task YESDK1631_ShortResponseDataBelowRmac_DoesNotTerminateProcess()
    {
        var (exitCode, output) = await ChildProcessTestRunner.RunAsync(
            typeof(CoreScpAuditReproTests),
            nameof(YESDK1631_ChildBody_OneToFiveByteResponse_ThrowsBadResponse),
            TestContext.Current.CancellationToken);

        Assert.True(
            exitCode == 0,
            $"Child test process exited with {exitCode} (fatal runtime error on Unix is 134/SIGABRT). Output head:\n{Head(output)}");
        Assert.Contains("succeeded: 1", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Audit", "YESDK-1631")]
    public async Task YESDK1631_ChildBody_OneToFiveByteResponse_ThrowsBadResponse()
    {
        ChildProcessTestRunner.SkipUnlessChild(nameof(YESDK1631_ChildBody_OneToFiveByteResponse_ThrowsBadResponse));

        for (var length = 1; length <= 5; length++)
        {
            await AssertShortResponseRejected(length);
        }
    }

    /// <summary>
    ///     Data lengths 6 and 7 do not crash, but are reported as a cryptography-provider problem
    ///     (NotSupportedException "does not support AESCMAC") instead of a bad device response.
    /// </summary>
    [Theory]
    [Trait("Audit", "YESDK-1631")]
    [InlineData(6)]
    [InlineData(7)]
    public Task YESDK1631_SixOrSevenByteResponse_ThrowsBadResponse(int length) =>
        AssertShortResponseRejected(length);

    /// <summary>
    ///     After a short protected response, the protocol latches "recovery required": the next
    ///     exchange is refused with the original failure and nothing is replayed to the card.
    /// </summary>
    [Theory]
    [Trait("Audit", "YESDK-1631")]
    [InlineData(1)]
    [InlineData(7)]
    public async Task YESDK1631_ShortResponse_RefusesNextExchangeWithoutReplay(int dataLength)
    {
        var connection = new FakeSmartCardConnection();
        var response = new byte[dataLength + 2];
        response[^2] = 0x90;
        connection.EnqueueResponse(response);
        var baseProtocol = new PcscProtocol(connection);
        using var state = new ScpState(new SessionKeys(new byte[16], new byte[16], new byte[16]), new byte[16]);
        using var wrapped = new PcscProtocolScp(baseProtocol,
            ScpInitializer.CreateSecureProcessor(baseProtocol.GetBaseCommandProcessor(), state,
                new FirmwareVersion(5, 7, 2), 0xC0, baseProtocol.MarkRecoveryRequired),
            data => data.ToArray());

        var failure = await Assert.ThrowsAsync<BadResponseException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCA, 0, 0),
                cancellationToken: TestContext.Current.CancellationToken));
        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCB, 0, 0),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Same(failure, refusal.InnerException);
        Assert.Single(connection.TransmittedCommands);
    }

    private static async Task AssertShortResponseRejected(int dataLength)
    {
        var raw = new FakeApduProcessor { Formatter = new ApduFormatterShort() };
        raw.EnqueueResponse(0x90, 0x00, new byte[dataLength]);

        using var state = new ScpState(new SessionKeys(new byte[16], new byte[16], new byte[16]), new byte[16]);
        using var processor = new ScpProcessor(raw, state);

        await Assert.ThrowsAsync<BadResponseException>(() => processor.TransmitAsync(
            new ApduCommand(0x00, 0xCA, 0x00, 0x00, new byte[] { 0x01 }),
            useScp: true,
            encrypt: false,
            TestContext.Current.CancellationToken));
    }

    private static string Head(string text) => text.Length <= 300 ? text : text[..300];
}