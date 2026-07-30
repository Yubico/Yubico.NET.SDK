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
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.YubiHsm.Backend;

internal sealed class HsmAuthBackend(ISmartCardProtocol protocol) : IHsmAuthBackend
{
    private readonly ISmartCardProtocol _protocol =
        protocol ?? throw new ArgumentNullException(nameof(protocol));

    public async Task<FirmwareVersion> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var selectResponse = await SelectAsync(cancellationToken).ConfigureAwait(false);
        return ParseVersionFromSelectResponse(selectResponse) ?? HsmAuthSession.FeatureHsmAuth.Version;
    }

    public Task<ReadOnlyMemory<byte>> SelectAsync(CancellationToken cancellationToken = default) =>
        _protocol.SelectAsync(ApplicationIds.YubiHsmAuth, cancellationToken);

    public Task<ApduResponse> SendAsync(
        ApduCommand command,
        bool throwOnError = true,
        CancellationToken cancellationToken = default) =>
        _protocol.TransmitAndReceiveAsync(command, throwOnError, cancellationToken);

    private static FirmwareVersion? ParseVersionFromSelectResponse(ReadOnlyMemory<byte> response)
    {
        if (response.IsEmpty)
            return null;

        if (!TlvHelper.TryFindValue(HsmAuthSession.TagVersion, response.Span, out var versionData))
            return null;

        if (versionData.Length != 3)
            return null;

        var span = versionData.Span;
        return new FirmwareVersion(span[0], span[1], span[2]);
    }
}
