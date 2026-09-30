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

using System.Buffers;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Piv.Backend;
using Yubico.YubiKit.Piv.Metadata;

namespace Yubico.YubiKit.Piv.Authentication;

/// <summary>Handles PIV-specific credential requests and retry decisions within an admitted session operation.</summary>
internal sealed class PivPromptedAuthentication(
    PivSession session,
    IPivBackend backend,
    ILogger logger,
    ICredentialPrompt prompt,
    int maxAttempts)
{
    internal async Task VerifyPinForSigningAsync(PivSlot slot, PivPinPolicy? pinPolicy, CancellationToken cancellationToken)
    {
        if (pinPolicy is PivPinPolicy.Never or PivPinPolicy.MatchOnce or PivPinPolicy.MatchAlways)
            return;

        // Empty VERIFY may say 9000 because a preceding Once verification persists, but that
        // cannot authorize Always. PIN metadata supplies its first retry count instead.
        (bool Verified, int? RetriesRemaining) state = pinPolicy is PivPinPolicy.Always
            ? (false, (await PivMetadataProtocol.GetPinMetadataSnapshotAsync(backend, logger, cancellationToken)
                .ConfigureAwait(false)).RetriesRemaining)
            : await PivAuthenticationProtocol.GetPinVerificationStateAsync(backend, cancellationToken).ConfigureAwait(false);
        if (state.Verified)
            return;
        if (state.RetriesRemaining == 0)
            throw new InvalidPinException(0, "PIN is blocked.");

        Exception? lastRejection = null;
        int? retries = state.RetriesRemaining;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var context = new CredentialPromptContext
            {
                Kind = CredentialKind.Pin,
                Application = "PIV",
                Scope = slot.ToString(),
                RetriesRemaining = retries,
                IsRetry = attempt > 0,
                MinLengthBytes = 6,
                MaxLengthBytes = 8
            };
            IMemoryOwner<byte> owner = await PivCredentialAcquisition.AcquireAsync(
                prompt, context, cancellationToken, session.MarkCredentialCallback).ConfigureAwait(false);
            try
            {
                session.CheckPromptActive(cancellationToken);
                if (owner.Memory.Length is < 6 or > 8)
                {
                    lastRejection = new ArgumentException("PIN must be 6-8 bytes");
                    continue;
                }
                try
                {
                    await PivAuthenticationProtocol.VerifyPinAsync(backend, logger, owner.Memory, cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }
                catch (InvalidPinException ex) when (ex.RetriesRemaining > 0)
                {
                    retries = ex.RetriesRemaining;
                    lastRejection = ex;
                }
            }
            finally
            {
                PivCredentialAcquisition.Release(owner);
            }
        }
        throw lastRejection ?? new InvalidOperationException("No PIN was submitted.");
    }

    internal async Task AuthenticateManagementKeyAsync(PivManagementKeyType keyType, CancellationToken cancellationToken)
    {
        int keyLength = keyType.KeyLength();
        Exception? lastRejection = null;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var context = new CredentialPromptContext
            {
                Kind = CredentialKind.ManagementKey,
                Application = "PIV",
                Scope = "Card management",
                IsRetry = attempt > 0,
                MinLengthBytes = keyLength,
                MaxLengthBytes = keyLength
            };
            IMemoryOwner<byte> owner = await PivCredentialAcquisition.AcquireAsync(
                prompt, context, cancellationToken, session.MarkCredentialCallback).ConfigureAwait(false);
            try
            {
                session.CheckPromptActive(cancellationToken);
                if (owner.Memory.Length != keyLength)
                {
                    lastRejection = new ArgumentException($"Management key must be {keyLength} bytes");
                    continue;
                }
                try
                {
                    await PivAuthenticationProtocol.AuthenticateAsync(backend, logger, keyType, owner.Memory, cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }
                catch (PivAuthenticationProtocol.ManagementKeyRejectedException ex)
                {
                    lastRejection = ex;
                }
            }
            finally
            {
                PivCredentialAcquisition.Release(owner);
            }
        }
        throw lastRejection ?? new InvalidOperationException("No management key was submitted.");
    }
}
