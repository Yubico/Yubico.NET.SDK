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
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2.Backend;

/// <summary>
/// FIDO backend that communicates over SmartCard (CCID) transport using ISO 7816-4 APDUs.
/// </summary>
internal sealed class SmartCardBackend : IFidoBackend
{
    private const byte CtapHidCbor = 0x10;  // Used as APDU INS

    private readonly ISmartCardProtocol _protocol;
    private readonly ILogger _logger;

    public SmartCardBackend(ISmartCardProtocol protocol)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        _protocol = protocol;
        _logger = YubiKitLogging.LoggerFactory.CreateLogger(nameof(SmartCardBackend));
    }

    /// <summary>
    /// Initializes the backend by selecting the FIDO2 application.
    /// </summary>
    public async Task SelectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Selecting FIDO2 application via SmartCard");
        await _protocol.SelectAsync(ApplicationIds.Fido2, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReadOnlyMemory<byte>> SendCborAsync(
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken = default)
    {
        if (request.Length < 1)
        {
            throw new ArgumentException("Request must contain at least the command byte.", nameof(request));
        }

        _logger.LogDebug(
            "Sending CTAP CBOR via SmartCard: command=0x{Command:X2}, payload={Length} bytes",
            request.Span[0], request.Length - 1);

        // For SmartCard transport, the CTAP request is wrapped in an APDU:
        // CLA=0x80, INS=0x10, P1=0x00, P2=0x00, Data=CTAP request
        var apdu = new ApduCommand
        {
            Cla = 0x80,
            Ins = CtapHidCbor,
            P1 = 0x00,
            P2 = 0x00,
            Data = request,
            Le = 0  // Maximum response length
        };

        var apduResponse = await _protocol.TransmitAndReceiveAsync(apdu, cancellationToken: cancellationToken).ConfigureAwait(false);
        var responseData = apduResponse.Data;

        // First byte of response data is the CTAP status
        if (responseData.Length < 1)
        {
            throw new Ctap.CtapException(CtapStatus.Other, "Empty response from authenticator");
        }

        var status = (CtapStatus)responseData.Span[0];
        Ctap.CtapException.ThrowIfError(status);

        _logger.LogDebug("CTAP CBOR response: status={Status}, data={Length} bytes",
            status, responseData.Length - 1);

        // Return the response data (without status byte)
        return responseData[1..];
    }
}