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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Yubico.YubiKit.OpenPgp;

public sealed partial class OpenPgpSession
{
    private static readonly byte[] InvalidPinPayload = new byte[8];

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

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

        // Confirm both commands. The vendor ATT key/certificate are outside the ordinary reset scope.
        var terminateCmd = new ApduCommand(0x00, (int)Ins.Terminate, 0x00, 0x00);
        await TransmitAsync(terminateCmd, cancellationToken).ConfigureAwait(false);

        var activateCmd = new ApduCommand(0x00, (int)Ins.Activate, 0x00, 0x00);
        await TransmitAsync(activateCmd, cancellationToken).ConfigureAwait(false);

        IsInitialized = false;
        IsAuthenticated = false;
        _backend = null;
        _updatedUifs.Clear();
        var oldProtocol = Protocol;
        Protocol = null;
        try
        {
            if (oldProtocol is IAsyncDisposable asyncProtocol)
                await asyncProtocol.DisposeAsync().ConfigureAwait(false);
            else
                oldProtocol?.Dispose();

            // After confirmation, restore the session even if the request was cancelled during ACTIVATE.
            await InitializeSessionAsync(token => InitializeAsync(_configuration, _scpKeyParameters,
                _firmwareVersionOverride, token), CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _endingAfterResetFailure = true;
            await DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException("OpenPGP reset succeeded, but session refresh failed and the session ended.", exception);
        }
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

            if (response.SW is SWConstants.AuthenticationMethodBlocked or unchecked((short)0x63C0))
                return;
            if (response.IsOK() || ((response.SW & 0xFFF0) != 0x63C0 && response.SW != SWConstants.SecurityStatusNotSatisfied))
                throw ApduException.FromResponse(response, command, "Blocking OpenPGP PIN for reset failed");
        }

        var status = await GetPinStatusAsync(cancellationToken).ConfigureAwait(false);
        if ((p2 == (byte)Pw.User ? status.AttemptsUser : status.AttemptsAdmin) != 0)
            throw new InvalidOperationException("OpenPGP PIN blocking was not confirmed; reset was not attempted.");
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
        catch when (_endingAfterResetFailure)
        {
            // The refresh exception is primary; implicit disposal must not replay a teardown failure.
        }
    }
}