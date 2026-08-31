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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.OpenPgp.UnitTests;

/// <summary>
///     Proves ISC-44: OpenPGP PIN verification failures — previously a plain
///     <see cref="ApduException" /> with the retry count only embedded in
///     <see cref="Exception.Message" /> text — now surface a typed, structured
///     <see cref="OpenPgpInvalidPinException.RetriesRemaining" /> for every status word the card
///     can use to report the failure (0x63Cx, 0x6982, and 0x6983).
/// </summary>
public sealed class OpenPgpInvalidPinExceptionTests
{
    [Fact]
    public void OpenPgpInvalidPinException_IsApduException_ForBackwardCompatibleCatchSites()
    {
        var exception = new OpenPgpInvalidPinException(3, "test");

        Assert.IsAssignableFrom<ApduException>(exception);
    }

    [Fact]
    public async Task VerifyPinAsync_WhenWrongPin_ExposesRetriesRemainingWithoutMessageParsing()
    {
        // SW 63C4: standard wrong-PIN status word, 4 attempts remaining.
        var connection = CreateInitializedConnection(KdfNotFoundResponse(), [0x63, 0xC4]);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<OpenPgpInvalidPinException>(() => session.VerifyPinAsync(
            "000000"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(4, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x63C4), exception.SW);
    }

    [Fact]
    public async Task VerifyAdminAsync_WhenWrongPin_ExposesRetriesRemaining()
    {
        // SW 63C2: standard wrong-PIN status word, 2 attempts remaining.
        var connection = CreateInitializedConnection(KdfNotFoundResponse(), [0x63, 0xC2]);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<OpenPgpInvalidPinException>(() => session.VerifyAdminAsync(
            "00000000"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(2, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x63C2), exception.SW);
    }

    [Fact]
    public async Task VerifyPinAsync_WhenPinAlreadyBlocked_ExposesZeroRetriesRemainingWithoutStatusQuery()
    {
        // SW 6983 (Authentication Method Blocked): the PIN is already permanently blocked.
        // Unlike 0x6982, this status word unambiguously means zero retries remain, so no
        // extra GET DATA status query should be issued.
        var connection = CreateInitializedConnection(KdfNotFoundResponse(), [0x69, 0x83]);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var commandCountBeforeVerify = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<OpenPgpInvalidPinException>(() => session.VerifyPinAsync(
            "000000"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(0, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x6983), exception.SW);
        // Exactly two commands (KDF lookup, then VERIFY) were transmitted after session
        // creation: no additional PW status query.
        Assert.Equal(commandCountBeforeVerify + 2, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task VerifyPinAsync_WhenSecurityStatusNotSatisfied_FallsBackToPinStatusQuery()
    {
        // SW 6982 (Security Status Not Satisfied): some 5.8.0-alpha firmware reports wrong PIN
        // this way instead of 0x63Cx. The session must fall back to GET DATA PW_STATUS_BYTES.
        var connection = CreateInitializedConnection(
            KdfNotFoundResponse(),
            [0x69, 0x82],
            PwStatusResponse(attemptsUser: 5));
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<OpenPgpInvalidPinException>(() => session.VerifyPinAsync(
            "000000"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(5, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x6982), exception.SW);
    }

    [Fact]
    public async Task VerifyAdminAsync_WhenSecurityStatusNotSatisfied_FallsBackToAdminAttemptsFromPinStatusQuery()
    {
        // SW 6982 (Security Status Not Satisfied) for an Admin PIN verify: the fallback must
        // resolve AttemptsAdmin, not AttemptsUser or AttemptsReset. Distinct attempt counts
        // in PwStatusResponse make a wrong arm mapping fail loudly.
        var connection = CreateInitializedConnection(
            KdfNotFoundResponse(),
            [0x69, 0x82],
            PwStatusResponse(attemptsUser: 5, attemptsReset: 6, attemptsAdmin: 7));
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<OpenPgpInvalidPinException>(() => session.VerifyAdminAsync(
            "00000000"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(7, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x6982), exception.SW);
    }

    [Fact]
    public async Task VerifyPinAsync_ExtendedWhenSecurityStatusNotSatisfied_FallsBackToResetAttemptsFromPinStatusQuery()
    {
        // SW 6982 (Security Status Not Satisfied) for an extended-mode (Pw.Reset) verify: the
        // fallback must resolve AttemptsReset, not AttemptsUser or AttemptsAdmin. The only route
        // that reaches VerifyPwAsync with Pw.Reset is VerifyPinAsync(pinUtf8, extended: true, ...).
        // Distinct attempt counts in PwStatusResponse make a wrong arm mapping fail loudly.
        var connection = CreateInitializedConnection(
            KdfNotFoundResponse(),
            [0x69, 0x82],
            PwStatusResponse(attemptsUser: 5, attemptsReset: 6, attemptsAdmin: 7));
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<OpenPgpInvalidPinException>(() => session.VerifyPinAsync(
            "000000"u8.ToArray(),
            extended: true,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(6, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x6982), exception.SW);
    }

    [Fact]
    public async Task VerifyPinAsync_WhenStatusQueryAlsoFails_ExposesNullRetriesRemaining()
    {
        // SW 6982 followed by a failing PW_STATUS_BYTES query: the retry count is genuinely
        // unknown and must not be reported as a fabricated number.
        var connection = CreateInitializedConnection(
            KdfNotFoundResponse(),
            [0x69, 0x82],
            [0x6A, 0x82]);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<OpenPgpInvalidPinException>(() => session.VerifyPinAsync(
            "000000"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x6982), exception.SW);
    }

    [Fact]
    public async Task VerifyPinAsync_WhenPinStatusLookupIsCanceled_PropagatesCancellation()
    {
        var recordingConnection = CreateInitializedConnection(
            KdfNotFoundResponse(),
            [0x69, 0x82],
            PwStatusResponse(attemptsUser: 5));
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        var connection = new CancelOnPwStatusLookupConnection(recordingConnection, cancellationSource);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.VerifyPinAsync(
            "000000"u8.ToArray(),
            cancellationToken: cancellationSource.Token));

        Assert.True(connection.PinStatusLookupWasCanceled);
        Assert.Equal(0x20, recordingConnection.TransmittedCommands[^1][1]); // VERIFY
    }

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), VersionResponse(), ApplicationRelatedDataResponse(), .. trailingResponses]);

    // SW 9000: successful APDU response with no data.
    private static byte[] OkResponse() => [0x90, 0x00];

    // OpenPGP version response: BCD 5.8.0 followed by SW 9000.
    private static byte[] VersionResponse() => [0x05, 0x08, 0x00, 0x90, 0x00];

    // GET DATA for the KDF object returns "Referenced data not found" — no KDF configured,
    // so VerifyPwAsync falls back to sending raw PIN bytes through KdfNone.
    private static byte[] KdfNotFoundResponse() => [0x6A, 0x82];

    private static byte[] PwStatusResponse(int attemptsUser, int attemptsReset = 0, int attemptsAdmin = 3) =>
        [0x00, 0x7F, 0x7F, 0x7F, (byte)attemptsUser, (byte)attemptsReset, (byte)attemptsAdmin, 0x90, 0x00];

    private sealed class CancelOnPwStatusLookupConnection(
        RecordingSmartCardConnection inner,
        CancellationTokenSource cancellationSource) : ISmartCardConnection
    {
        public bool PinStatusLookupWasCanceled { get; private set; }

        public Transport Transport => inner.Transport;

        public ConnectionType Type => inner.Type;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            if (IsPwStatusLookup(command))
            {
                PinStatusLookupWasCanceled = true;
                cancellationSource.Cancel();
                return Task.FromCanceled<ReadOnlyMemory<byte>>(cancellationSource.Token);
            }

            return inner.TransmitAndReceiveAsync(command, cancellationToken);
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            inner.BeginTransaction(cancellationToken);

        public bool SupportsExtendedApdu() => inner.SupportsExtendedApdu();

        public void Dispose() => inner.Dispose();

        public ValueTask DisposeAsync() => inner.DisposeAsync();

        private static bool IsPwStatusLookup(ReadOnlyMemory<byte> command) =>
            command.Length >= 4 &&
            command.Span[1] == 0xCA && // GET DATA
            command.Span[2] == 0x00 &&
            command.Span[3] == 0xC4; // PW Status Bytes DO
    }

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