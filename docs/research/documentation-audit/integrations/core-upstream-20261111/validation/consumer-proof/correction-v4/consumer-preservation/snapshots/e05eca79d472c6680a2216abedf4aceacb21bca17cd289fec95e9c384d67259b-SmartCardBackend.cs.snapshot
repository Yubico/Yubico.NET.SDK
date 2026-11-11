// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Piv.Backends;


internal readonly record struct Initialization(FirmwareVersion FirmwareVersion);

internal sealed class SmartCardBackend(ISmartCardProtocol protocol) : IBackend
{
    private const byte Cla = 0x00;
    private const byte InsGetVersion = 0xFD;

    private readonly ISmartCardProtocol _protocol =
        protocol ?? throw new ArgumentNullException(nameof(protocol));

    public async Task<Initialization> ReadInitialStateAsync(CancellationToken cancellationToken = default)
    {
        await SelectAsync(cancellationToken).ConfigureAwait(false);

        var versionCommand = new ApduCommand(Cla, InsGetVersion, 0, 0);
        var versionResponse = await SendAsync(versionCommand, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new Initialization(ParseVersionResponse(versionResponse.Data.Span));
    }

    public Task<ReadOnlyMemory<byte>> SelectAsync(CancellationToken cancellationToken = default) =>
        _protocol.SelectAsync(ApplicationIds.Piv, cancellationToken);

    public Task<ApduResponse> SendAsync(
        ApduCommand command,
        bool throwOnError = true,
        CancellationToken cancellationToken = default) =>
        _protocol.TransmitAndReceiveAsync(command, throwOnError, cancellationToken);

    private static FirmwareVersion ParseVersionResponse(ReadOnlySpan<byte> response)
    {
        if (response.Length < 3)
        {
            throw new InvalidOperationException(
                $"Invalid version response: expected at least 3 bytes, got {response.Length}");
        }

        return new FirmwareVersion(response[0], response[1], response[2]);
    }
}