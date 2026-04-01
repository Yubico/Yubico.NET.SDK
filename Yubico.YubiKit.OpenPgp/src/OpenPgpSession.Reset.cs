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
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.OpenPgp;

public sealed partial class OpenPgpSession
{
    private static readonly byte[] InvalidPinPayload = new byte[8];

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureReset);

        _logger.LogInformation("Starting OpenPGP factory reset");

        // Step 1: Get current PIN attempts
        var status = await GetPinStatusAsync(cancellationToken).ConfigureAwait(false);

        // Step 2: Block User PIN (P2=0x81)
        await BlockPinAsync((byte)Pw.User, status.AttemptsUser, cancellationToken)
            .ConfigureAwait(false);

        // Step 3: Block Admin PIN (P2=0x83)
        await BlockPinAsync((byte)Pw.Admin, status.AttemptsAdmin, cancellationToken)
            .ConfigureAwait(false);

        // Step 4: TERMINATE
        _logger.LogDebug("Sending TERMINATE");
        var terminateCmd = new ApduCommand(0x00, (int)Ins.Terminate, 0x00, 0x00);
        await TransmitNoThrowAsync(terminateCmd, cancellationToken).ConfigureAwait(false);

        // Step 5: ACTIVATE
        _logger.LogDebug("Sending ACTIVATE");
        var activateCmd = new ApduCommand(0x00, (int)Ins.Activate, 0x00, 0x00);
        await TransmitNoThrowAsync(activateCmd, cancellationToken).ConfigureAwait(false);

        // Step 6: Re-initialize — re-SELECT and refresh cached state
        _logger.LogDebug("Re-initializing session after reset");

        await _protocol!.SelectAsync(ApplicationIds.OpenPgp, cancellationToken)
            .ConfigureAwait(false);

        _appData = await GetApplicationRelatedDataCoreAsync(cancellationToken)
            .ConfigureAwait(false);
        _kdf = null;

        _logger.LogInformation("OpenPGP factory reset complete");
    }

    private async Task BlockPinAsync(
        byte p2,
        int remainingAttempts,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Blocking PIN P2=0x{P2:X2} ({Remaining} attempts remaining)", p2, remainingAttempts);

        for (var i = 0; i < remainingAttempts; i++)
        {
            var command = new ApduCommand(0x00, (int)Ins.Verify, 0x00, p2, InvalidPinPayload);
            var response = await TransmitNoThrowAsync(command, cancellationToken)
                .ConfigureAwait(false);

            // 0x6983 = authentication blocked, we're done
            if (response.SW == SWConstants.AuthenticationMethodBlocked)
            {
                _logger.LogDebug("PIN P2=0x{P2:X2} blocked after {Count} attempts", p2, i + 1);
                return;
            }
        }

        _logger.LogDebug("PIN P2=0x{P2:X2} exhausted {Count} attempts", p2, remainingAttempts);
    }
}
