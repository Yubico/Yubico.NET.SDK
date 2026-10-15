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
using Yubico.YubiKit.Core.Cryptography;
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

    // --- N2 (no Jira key): SCP initialization and response parsing must fail with BadResponseException ---

    /// <summary>
    ///     GP Amd D §7.1.1.3: INITIALIZE UPDATE returns 10 + 3 + 8 + 8 = 29 bytes. Anything shorter is malformed.
    /// </summary>
    [Theory]
    [Trait("Audit", "N2")]
    [InlineData(0)]
    [InlineData(28)]
    public async Task N2_Scp03InitializeUpdateShortResponse_ThrowsBadResponse(int dataLength)
    {
        var raw = new FakeApduProcessor { Formatter = new ApduFormatterShort() };
        raw.EnqueueResponse(0x90, 0x00, new byte[dataLength]);
        using var keyParams = Scp03KeyParameters.Default;

        await Assert.ThrowsAsync<BadResponseException>(() =>
            ScpState.Scp03InitAsync(raw, keyParams, cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>
    ///     GP Amd F §7.1.3: the SCP11 authenticate response is ePK.SD.ECKA (5F49) followed by the receipt (86).
    /// </summary>
    [Theory]
    [Trait("Audit", "N2")]
    [InlineData(new byte[] { 0x86, 0x01, 0xAA })] // one TLV
    [InlineData(new byte[] { 0x86, 0x01, 0xAA, 0x5F, 0x49, 0x01, 0xBB })] // swapped
    [InlineData(new byte[] { 0x5F, 0x49, 0x01, 0xBB, 0x87, 0x01, 0xAA })] // wrong receipt tag
    public async Task N2_Scp11AuthenticateMalformedTlvs_ThrowsBadResponse(byte[] responseData)
    {
        var raw = new FakeApduProcessor { Formatter = new ApduFormatterShort() };
        raw.EnqueueResponse(0x90, 0x00, responseData);
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using var keyParams = new Scp11KeyParameters(
            new KeyReference(ScpKid.SCP11b, 0x01),
            ECPublicKey.CreateFromParameters(ecdh.PublicKey.ExportParameters()));

        await Assert.ThrowsAsync<BadResponseException>(() =>
            ScpState.Scp11InitAsync(raw, keyParams, TestContext.Current.CancellationToken));
    }

    /// <summary>GP Amd D §6.2.7: response ciphertext is whole AES blocks, never empty.</summary>
    [Theory]
    [Trait("Audit", "N2")]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    public void N2_DecryptNonBlockLength_ThrowsBadResponse(int length)
    {
        using var state = new ScpState(new SessionKeys(new byte[16], new byte[16], new byte[16]), new byte[16]);
        Assert.Throws<BadResponseException>(() => state.Decrypt(new byte[length]));
    }

    /// <summary>
    ///     ISO/IEC 9797-1 Padding Method 2 covers the whole block: 80 00..00 is an empty message
    ///     (Python scp.py L238-242 at 4ca60f7 accepts it), while all zeros has no 0x80 and is bad padding.
    /// </summary>
    [Fact]
    [Trait("Audit", "N2")]
    public void N2_DecryptPaddingOnlyBlock_IsEmptyMessage_AndAllZerosIsBadPadding()
    {
        using var state = new ScpState(new SessionKeys(new byte[16], new byte[16], new byte[16]), new byte[16]);
        _ = state.Encrypt([]); // advance the counter so Decrypt has a command to answer

        Assert.Empty(state.Decrypt(EncryptBlockForResponse(state, [0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0])));
        Assert.Throws<BadResponseException>(() => state.Decrypt(EncryptBlockForResponse(state, new byte[16])));
    }

    /// <summary>Produces the ciphertext a card would send for <paramref name="plaintextBlock" /> at the current counter.</summary>
    private static byte[] EncryptBlockForResponse(ScpState state, byte[] plaintextBlock)
    {
        // Mirror the card side: IV = AES-ECB(S-ENC, 0x80 || 0..0 || counter), S-ENC is all zeros here.
        using var aes = Aes.Create();
        aes.Key = new byte[16];
        Span<byte> ivData = stackalloc byte[16];
        ivData[0] = 0x80;
        ivData[15] = 1;
        var iv = aes.EncryptEcb(ivData, PaddingMode.None);
        return aes.EncryptCbc(plaintextBlock, iv, PaddingMode.None);
    }

    private static string Head(string text) => text.Length <= 300 ? text : text[..300];
}