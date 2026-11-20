// Copyright 2025 Yubico AB
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

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Ctap2.Backend;

/// <summary>
/// FIDO backend that communicates over SmartCard (CCID) transport using ISO 7816-4 APDUs.
/// </summary>
/// <remarks>Uses a borrowed protocol. With no in-flight presence signal, an operation's supplied
/// policy notification is requested before transmission. Resolution uses CTAP status before typed
/// decoding, not verified UP/UV or RP success. The returned status-stripped view shares ApduResponse's
/// copied Data storage; other transport, SCP and decoded copies have separate lifetimes. Failure cleanup
/// clears the received, untransferred Data view after resolution. It cannot reach an APDU error response
/// that Core never returns, and a logging provider throwing while a secondary resolution failure is
/// logged can bypass the later clearing statement.</remarks>
internal sealed class SmartCardBackend : ICtap2Backend
{
    private const byte NfcCtapMsg = 0x10;  // Used as APDU INS

    private readonly ISmartCardProtocol _protocol;
    private readonly ILogger _logger;

    public SmartCardBackend(ISmartCardProtocol protocol)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        _protocol = protocol;
        _logger = YubiKitLogging.LoggerFactory.CreateLogger(nameof(SmartCardBackend));
    }

    /// <summary>
    /// Selects the FIDO2 application.
    /// </summary>
    /// <remarks>Any SELECT ApduException is currently wrapped as NotSupportedException with its
    /// original inner exception. The wrapper alone does not prove the FIDO AID was absent.</remarks>
    public async Task SelectApplicationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Selecting FIDO2 application via SmartCard");
            await _protocol.SelectAsync(ApplicationIds.Fido, cancellationToken).ConfigureAwait(false);
        }
        catch (ApduException ex)
        {
            throw new NotSupportedException(
                "FIDO2 over SmartCard is not supported because the authenticator did not expose the FIDO2 AID.",
                ex);
        }
    }

    public async Task<ReadOnlyMemory<byte>> SendCborAsync(
        ReadOnlyMemory<byte> request,
        UserPresenceNotification userPresenceNotification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userPresenceNotification);

        if (request.Length < 1)
        {
            throw new ArgumentException("Request must contain at least the command byte.", nameof(request));
        }

        _logger.LogDebug(
            "Sending CTAP CBOR via SmartCard: command=0x{Command:X2}, payload={Length} bytes",
            request.Span[0], request.Length - 1);

        Exception? primaryException = null;
        var outcome = UserPresenceOutcome.Failed;
        ReadOnlyMemory<byte> responseData = default;
        var responseTransferred = false;
        try
        {
            await userPresenceNotification.RequestAsync(cancellationToken).ConfigureAwait(false);

            // For SmartCard transport, the CTAP request is wrapped in an APDU:
            // CLA=0x80, INS=0x10, P1=0x00, P2=0x00, Data=CTAP request
            var apdu = new ApduCommand
            {
                Cla = 0x80,
                Ins = NfcCtapMsg,
                P1 = 0x00,
                P2 = 0x00,
                Data = request,
                Le = 0  // Formatter emits wire Le=0 (maximum for the selected format), not an absent Le.
            };

            var apduResponse = await _protocol.TransmitAndReceiveAsync(apdu, cancellationToken: cancellationToken).ConfigureAwait(false);
            responseData = apduResponse.Data;

            // The first payload byte is CTAP status; missing status produces synthetic Other.
            if (responseData.Length < 1)
            {
                throw new CtapException(CtapStatus.Other, "Empty response from authenticator");
            }

            var status = (CtapStatus)responseData.Span[0];
            outcome = status switch
            {
                CtapStatus.Success => UserPresenceOutcome.Completed,
                CtapStatus.KeepAliveCancel => UserPresenceOutcome.Cancelled,
                CtapStatus.UserActionTimeout => UserPresenceOutcome.TimedOut,
                _ => UserPresenceOutcome.Failed
            };
            CtapException.ThrowIfError(status);

            _logger.LogDebug("CTAP CBOR response: status={Status}, data={Length} bytes",
                status, responseData.Length - 1);

            await userPresenceNotification.ResolveAsync(UserPresenceOutcome.Completed).ConfigureAwait(false);

            // Transfer after notification resolution; success here does not guarantee typed decoding.
            responseTransferred = true;
            return responseData[1..];
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            outcome = UserPresenceOutcome.Cancelled;
            primaryException = ex;
            throw;
        }
        catch (Exception ex)
        {
            primaryException = ex;
            throw;
        }
        finally
        {
            await userPresenceNotification.ResolveAsync(outcome, primaryException).ConfigureAwait(false);
            if (!responseTransferred && !responseData.IsEmpty)
            {
                CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(responseData).Span);
            }
        }
    }
}