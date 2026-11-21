// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
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

namespace Yubico.YubiKit.OpenPgp.Backend;



internal sealed class OpenPgpBackend(ISmartCardProtocol protocol) : IOpenPgpBackend
{
    private static readonly ILogger Logger =
        YubiKitLogging.LoggerFactory.CreateLogger<OpenPgpBackend>();

    private readonly ISmartCardProtocol _protocol =
        protocol ?? throw new ArgumentNullException(nameof(protocol));

    public async Task SelectForInitializationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SelectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ApduException ex) when (ex.SW is SWConstants.FileTerminated or SWConstants.ConditionsNotSatisfied)
        {
            Logger.LogDebug("OpenPGP applet in terminated state, sending ACTIVATE");
            await SendAsync(
                    new ApduCommand(0x00, (int)Ins.Activate, 0x00, 0x00),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await SelectAsync(cancellationToken).ConfigureAwait(false);
        }

    }

    public Task<ReadOnlyMemory<byte>> SelectAsync(CancellationToken cancellationToken = default) =>
        _protocol.SelectAsync(ApplicationIds.OpenPgp, cancellationToken);

    public Task<ApduResponse> SendAsync(
        ApduCommand command,
        bool throwOnError = true,
        CancellationToken cancellationToken = default) =>
        _protocol.TransmitAndReceiveAsync(
            command,
            throwOnError: throwOnError,
            cancellationToken: cancellationToken);

    public async Task<FirmwareVersion?> ReadVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await SendAsync(
                    new ApduCommand(0x00, (int)Ins.GetVersion, 0x00, 0x00),
                    throwOnError: false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (response.IsOK() && response.Data.Length >= 3)
            {
                var data = response.Data.Span;
                foreach (byte value in data[..3])
                    if ((value >> 4) > 9 || (value & 0x0F) > 9)
                        return null;
                return new FirmwareVersion(
                    (byte)BcdHelper.DecodeByte(data[0]),
                    (byte)BcdHelper.DecodeByte(data[1]),
                    (byte)BcdHelper.DecodeByte(data[2]));
            }
        }
        catch (ApduException)
        {
            // Version is advisory. Required selection, configuration and app data failures remain visible.
        }

        return null;
    }
}