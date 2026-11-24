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

using System.Buffers;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

// Assemble 0x61 responses using fixed-zero CLA/P1/P2/Le and the configured remaining-data instruction.
// This receiver neither retries 0x6C corrected lengths nor imposes a total response-size/fragment budget.
// Clear consumed fragments and the current assembly buffer; the returned owner remains intact.
// Clearing the writer cannot reach backing arrays it has already replaced during growth.
internal class ChainedResponseReceiver(
    FirmwareVersion? firmwareVersion,
    IApduProcessor apduTransmitter,
    byte insSendRemaining,
    Action<Exception>? onContinuationFailure = null,
    bool reportProtectedFailures = true) : IApduProcessor, IDisposable
{
    private const byte Sw1HasMoreData = 0x61;
    private readonly ApduCommand _getMoreDataApdu = new(0, insSendRemaining, 0, 0);

    public FirmwareVersion? FirmwareVersion { get; } = firmwareVersion;

    public byte InsSendRemaining { get; } = insSendRemaining;


    public IApduFormatter Formatter => apduTransmitter.Formatter;

    public async Task<ApduResponse> TransmitAsync(
        ApduCommand command,
        bool useScp = false,
        CancellationToken cancellationToken = default)
    {
        var response = await apduTransmitter.TransmitAsync(command, useScp, cancellationToken).ConfigureAwait(false);
        if (response.SW1 != Sw1HasMoreData)
            return response;

        ArrayBufferWriter<byte>? buffer = null;
        try
        {
            buffer = new ArrayBufferWriter<byte>();
            while (response.SW1 == Sw1HasMoreData) // Partial response, more data available
            {
                buffer.Write(response.Data.Span);
                response.Clear();
                try
                {
                    response = await apduTransmitter.TransmitAsync(_getMoreDataApdu, useScp, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // A protected transmitter may already have reported this failure; plain
                    // continuations bypass it and must still notify the recovery owner.
                    if (!useScp || reportProtectedFailures)
                        onContinuationFailure?.Invoke(ex);
                    throw;
                }
            }

            buffer.Write(response.Data.Span);
            buffer.Write([response.SW1]);
            buffer.Write([response.SW2]);
            return new ApduResponse(buffer.WrittenMemory);
        }
        finally
        {
            response.Clear();
            buffer?.Clear();
        }
    }

    public void Dispose() => (apduTransmitter as IDisposable)?.Dispose();

}