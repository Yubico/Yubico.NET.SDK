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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Oath.Backend;

internal readonly record struct OathInitialization(
    FirmwareVersion FirmwareVersion,
    byte[] Salt,
    byte[] Challenge);

internal sealed class OathBackend(ISmartCardProtocol protocol) : IOathBackend
{
    private readonly ISmartCardProtocol _protocol =
        protocol ?? throw new ArgumentNullException(nameof(protocol));

    public async Task<OathInitialization> ReadInitialStateAsync(CancellationToken cancellationToken = default)
    {
        var selectResponse = await SelectAsync(cancellationToken).ConfigureAwait(false);

        ReadOnlyMemory<byte> version = default;
        ReadOnlyMemory<byte> salt = default;
        ReadOnlyMemory<byte> challenge = default;

        using var tlvs = TlvHelper.DecodeList(selectResponse.Span);
        foreach (var tlv in tlvs)
        {
            if (tlv.Tag == OathConstants.TagVersion)
            {
                if (tlv.Value.Length != 3)
                    throw new BadResponseException("OATH SELECT version must contain three bytes.");
                version = tlv.Value;
            }
            else if (tlv.Tag == OathConstants.TagName)
            {
                if (tlv.Value.Length == 0)
                    throw new BadResponseException("OATH SELECT requires a nonempty device ID.");
                salt = tlv.Value;
            }
            else if (tlv.Tag == OathConstants.TagChallenge)
            {
                if (tlv.Value.Length == 0)
                    throw new BadResponseException("OATH SELECT authentication challenge must not be empty.");
                challenge = tlv.Value;
            }
        }

        if (version.IsEmpty || salt.IsEmpty)
            throw new BadResponseException("OATH SELECT requires version and device ID metadata.");

        var firmwareVersion = new FirmwareVersion(version.Span[0], version.Span[1], version.Span[2]);
        return new OathInitialization(firmwareVersion, salt.ToArray(), challenge.ToArray());
    }

    public Task<ReadOnlyMemory<byte>> SelectAsync(CancellationToken cancellationToken = default) =>
        _protocol.SelectAsync(ApplicationIds.Oath, cancellationToken);

    public Task<ApduResponse> SendAsync(
        ApduCommand command,
        bool throwOnError = true,
        CancellationToken cancellationToken = default) =>
        _protocol.TransmitAndReceiveAsync(command, throwOnError, cancellationToken);
}