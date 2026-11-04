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
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.SecurityDomain.UnitTests;

/// <summary>
///     Checks selected SCP03 configuration failures during CreateAsync and ResetAsync reinitialization
///     surface as <see cref="SecureChannelException" />
///     with the original failure preserved as <see cref="Exception.InnerException" /> and, when
///     available, the ISO 7816 status word preserved on <see cref="SecureChannelException.StatusWord" />.
///     Also checks an unauthenticated malformed key-information response retains its original exception type.
/// </summary>
public class SecureChannelExceptionTests
{
    private static byte[] OkResponse() => [0x90, 0x00];

    [Fact]
    public async Task RawSmartCardSession_CreateWithScp_MarksSessionReadyAfterHandshake()
    {
        using var connection = new Scp03HandshakeFakeConnection();
        using var scpKeyParams = Scp03KeyParameters.Default;

        using (var raw = await RawSmartCardSession.CreateAsync(connection, TestContext.Current.CancellationToken))
        {
            Assert.True(raw.IsInitialized);
            await raw.SelectAsync(ApplicationIds.SecurityDomain, TestContext.Current.CancellationToken);
        }

        using var secured = await RawSmartCardSession.CreateAsync(
            connection,
            scpKeyParams,
            new FirmwareVersion(5, 7, 2),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(secured.IsInitialized);
        Assert.True(secured.IsAuthenticated);
        Assert.Equal(3, connection.TransmittedCommands.Count);
    }

    // APDU-level rejection supplied by the recording connection.

    [Fact]
    public async Task CreateAsync_Scp03InitializeUpdateRejectedByDevice_ThrowsSecureChannelExceptionWithStatusWordAndInnerApduException()
    {
        // Simulate INITIALIZE UPDATE rejection with SW=0x6982 (Security status not satisfied).
        var connection = new RecordingSmartCardConnection(
            OkResponse(), // SELECT
            [0x69, 0x82]); // INITIALIZE UPDATE rejected

        using var scpKeyParams = Scp03KeyParameters.Default;

        var ex = await Assert.ThrowsAsync<SecureChannelException>(() =>
            SecurityDomainSession.CreateAsync(
                connection,
                new SessionCreationOptions { ScpKeyParameters = scpKeyParams },
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(unchecked((short)0x6982), ex.StatusWord);
        var inner = Assert.IsType<ApduException>(ex.InnerException);
        Assert.Equal(unchecked((short)0x6982), inner.SW);
        Assert.Contains("INITIALIZE UPDATE", inner.Message);
    }

    // Local card-cryptogram verification failure.

    [Fact]
    public async Task CreateAsync_Scp03CardCryptogramMismatch_ThrowsSecureChannelExceptionWithInnerBadResponseException()
    {
        // A well-formed INITIALIZE UPDATE response (SW=9000) whose card cryptogram cannot match the
        // default static keys' derivation. This is the "wrong SCP03 key set" failure mode, detected
        // locally by comparing derived vs. returned cryptograms rather than via a bad status word.
        byte[] initializeUpdateResponse =
        [
            // 10 bytes diversification data
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A,
            // 3 bytes key info
            0xFF, 0x02, 0x00,
            // 8 bytes card challenge
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
            // 8 bytes card cryptogram (will not match the derived value for the default keys)
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
            0x90, 0x00
        ];
        var connection = new RecordingSmartCardConnection(OkResponse(), initializeUpdateResponse);

        using var scpKeyParams = Scp03KeyParameters.Default;

        var ex = await Assert.ThrowsAsync<SecureChannelException>(() =>
            SecurityDomainSession.CreateAsync(
                connection,
                new SessionCreationOptions { ScpKeyParameters = scpKeyParams },
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(ex.StatusWord);
        var inner = Assert.IsType<BadResponseException>(ex.InnerException);
        Assert.Contains("Wrong SCP03 key set", inner.Message);
        Assert.Same(inner, ex.InnerException);
    }

    // Unsupported supplied firmware version.

    [Fact]
    public async Task CreateAsync_Scp03UnsupportedFirmware_ThrowsSecureChannelExceptionWithInnerNotSupportedException()
    {
        // Firmware below 5.3.0 fails the SCP03 feature check before any INITIALIZE UPDATE APDU is
        // transmitted, so only the SELECT response needs to be queued.
        var connection = new RecordingSmartCardConnection(OkResponse());

        using var scpKeyParams = Scp03KeyParameters.Default;

        var ex = await Assert.ThrowsAsync<SecureChannelException>(() =>
            SecurityDomainSession.CreateAsync(
                connection,
                new SessionCreationOptions
                {
                    ScpKeyParameters = scpKeyParams,
                    FirmwareVersionOverride = new FirmwareVersion(5, 2, 9)
                },
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(ex.StatusWord);
        Assert.IsType<NotSupportedException>(ex.InnerException);
        Assert.Contains("SCP03", ex.InnerException!.Message);
    }

    // Per-operation parsing failure without SCP.

    [Fact]
    public async Task ListKeyInformationAsync_MalformedResponse_DoesNotWrapAsSecureChannelException()
    {
        // No SCP parameters: assert that parsing the later malformed entry throws BadResponseException
        // directly, outside initialization's SecureChannelException catch.
        var connection = new RecordingSmartCardConnection(
            OkResponse(),
            [0xC0, 0x00, 0x90, 0x00]); // Key information entry advertises 0 bytes for its value
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<BadResponseException>(() =>
            session.ListKeyInformationAsync(TestContext.Current.CancellationToken));

        Assert.IsNotType<SecureChannelException>(ex);
    }

    // Reset reinitialization reaches the same selected configuration-error wrapper as creation.

    [Fact]
    public async Task ResetAsync_ReinitializationRejectedAfterKeyBlocked_ThrowsSecureChannelExceptionWithInnerApduException()
    {
        // The fixture derives responses for initial SCP03 establishment and encrypted key enumeration,
        // reports a blocked status on the raw reset attempt, then rejects reinitialization. Assertions
        // check the wrapper, immediate status/inner exception and failed-session flags, not device reset
        // or complete SCP03 protocol verification.
        var connection = new Scp03HandshakeFakeConnection();
        using var scpKeyParams = Scp03KeyParameters.Default;

        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            new SessionCreationOptions { ScpKeyParameters = scpKeyParams },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(session.IsAuthenticated);

        var ex = await Assert.ThrowsAsync<SecureChannelException>(() =>
            session.ResetAsync(TestContext.Current.CancellationToken));

        Assert.Equal(connection.ReinitRejectionStatusWord, ex.StatusWord);
        var inner = Assert.IsType<ApduException>(ex.InnerException);
        Assert.Equal(connection.ReinitRejectionStatusWord, inner.SW);
        Assert.Contains("INITIALIZE UPDATE", inner.Message);
        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
    }
}