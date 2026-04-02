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
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2.Backend;

/// <summary>
/// FIDO backend that communicates over FIDO HID (CTAPHID_CBOR command).
/// </summary>
internal sealed class FidoHidBackend : IFidoBackend
{
    private const byte CtapHidCbor = 0x10;  // CTAPHID_CBOR command
    
    private readonly IFidoHidProtocol _protocol;
    private readonly ILogger _logger;
    private bool _disposed;
    
    public FidoHidBackend(IFidoHidProtocol protocol)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        _protocol = protocol;
        _logger = YubiKitLogging.LoggerFactory.CreateLogger(nameof(FidoHidBackend));
    }
    
    public async Task<ReadOnlyMemory<byte>> SendCborAsync(
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (request.Length < 1)
        {
            throw new ArgumentException("Request must contain at least the command byte.", nameof(request));
        }
        
        _logger.LogDebug(
            "Sending CTAP CBOR via HID: command=0x{Command:X2}, payload={Length} bytes",
            request.Span[0], request.Length - 1);
        
        // Send the full CTAP request (command byte + CBOR payload) via CTAPHID_CBOR
        var response = await _protocol.SendVendorCommandAsync(CtapHidCbor, request, cancellationToken)
            .ConfigureAwait(false);
        
        // First byte is the CTAP status
        if (response.Length < 1)
        {
            throw new Ctap.CtapException(CtapStatus.Other, "Empty response from authenticator");
        }
        
        var status = (CtapStatus)response.Span[0];
        Ctap.CtapException.ThrowIfError(status);
        
        _logger.LogDebug("CTAP CBOR response: status={Status}, data={Length} bytes", 
            status, response.Length - 1);
        
        // Return the response data (without status byte)
        return response[1..];
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _protocol.Dispose();
        _disposed = true;
    }
}
