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

    public async Task<OathInitialization> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var selectResponse = await SelectAsync(cancellationToken).ConfigureAwait(false);

        var firmwareVersion = new FirmwareVersion();
        byte[] salt = [];
        byte[] challenge = [];

        using var tlvs = TlvHelper.DecodeList(selectResponse.Span);
        foreach (var tlv in tlvs)
        {
            switch (tlv.Tag)
            {
                case OathConstants.TagVersion:
                    var versionBytes = tlv.Value.Span;
                    firmwareVersion = new FirmwareVersion(versionBytes[0], versionBytes[1], versionBytes[2]);
                    break;

                case OathConstants.TagName:
                    salt = tlv.Value.ToArray();
                    break;

                case OathConstants.TagChallenge:
                    challenge = tlv.Value.ToArray();
                    break;
            }
        }

        return new OathInitialization(firmwareVersion, salt, challenge);
    }

    public Task<ReadOnlyMemory<byte>> SelectAsync(CancellationToken cancellationToken = default) =>
        _protocol.SelectAsync(ApplicationIds.Oath, cancellationToken);

    public Task<ApduResponse> SendAsync(
        ApduCommand command,
        bool throwOnError = true,
        CancellationToken cancellationToken = default) =>
        _protocol.TransmitAndReceiveAsync(command, throwOnError, cancellationToken);
}
