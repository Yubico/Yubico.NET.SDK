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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
///     Initializes SCP (Secure Channel Protocol) sessions for SmartCard connections.
///     Supports SCP03 and SCP11 (variants a/b/c).
/// </summary>
internal static class ScpInitializer
{
    // SCP03 Constants
    private const byte CLA_SECURE_MESSAGING = 0x84; // CLA with SM bit set
    private const byte INS_EXTERNAL_AUTHENTICATE = 0x82; // EXTERNAL AUTHENTICATE instruction

    private const byte
        SECURITY_LEVEL_CMAC_CDEC_RMAC_RENC = 0x33; // Security level: C-MAC + C-DECRYPTION + R-MAC + R-ENCRYPTION

    private static readonly Feature FeatureScp03 = new("SCP03", 5, 3, 0);
    private static readonly Feature FeatureScp11 = new("SCP11", 5, 7, 2);

    /// <summary>
    ///     Initializes an SCP session and returns an SCP-wrapped processor with data encryptor.
    /// </summary>
    /// <param name="initializationProcessor">The APDU processor used during SCP setup before secure messaging is established.</param>
    /// <param name="commandProcessor">The raw APDU command processor wrapped by SCP after setup completes.</param>
    /// <param name="insSendRemaining">The instruction byte for response-chaining follow-up commands.</param>
    /// <param name="keyParams">SCP key parameters (SCP03 or SCP11)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (SCP-wrapped processor, data encryptor)</returns>
    /// <exception cref="ArgumentException">Thrown when keyParams type is unsupported</exception>
    /// <exception cref="NotSupportedException">Thrown when device doesn't support SCP</exception>
    /// <exception cref="ApduException">Thrown when SCP initialization fails</exception>
    public static async Task<(IApduProcessor scpProcessor, DataEncryptor dataEncryptor)> InitializeScpAsync(
        IApduProcessor initializationProcessor,
        IApduProcessor commandProcessor,
        byte insSendRemaining,
        ScpKeyParameters keyParams,
        CancellationToken cancellationToken = default)
    {
        if (initializationProcessor is not ChainedResponseReceiver { FirmwareVersion: not null } mainProcessor)
            throw new ArgumentException("Initialization processor must be a ChainedResponseReceiver",
                nameof(initializationProcessor)); // TODO. Could also put FirmwareVersion on the IApduProcessor interface

        try
        {
            return keyParams switch
            {
                Scp03KeyParameters scp03Parameters => Supports(mainProcessor.FirmwareVersion, FeatureScp03)
                    ? await InitScp03Async(
                            mainProcessor,
                            commandProcessor,
                            insSendRemaining,
                            scp03Parameters,
                            cancellationToken)
                        .ConfigureAwait(false)
                    : throw new NotSupportedException("SCP03 only supported on YubiKey 5.3.0 and later"),
                Scp11KeyParameters scp11Parameters => Supports(mainProcessor.FirmwareVersion, FeatureScp11)
                    ? await InitScp11Async(
                            mainProcessor,
                            commandProcessor,
                            insSendRemaining,
                            scp11Parameters,
                            cancellationToken)
                        .ConfigureAwait(false)
                    : throw new NotSupportedException("SCP11 only supported on YubiKey 5.7.2 and later"),
                _ => throw new ArgumentException("Unsupported SCP key parameters type")
            };
        }
        catch (ApduException ex) when (ex.SW == SWConstants.ClaNotSupported)
        {
            throw new NotSupportedException("SCP is not supported by this YubiKey", ex);
        }
    }

    private static bool Supports(FirmwareVersion firmwareVersion, Feature feature) =>
        firmwareVersion.Major == 0 || firmwareVersion >= feature.Version;

    internal static IApduProcessor CreateSecureProcessor(
        IApduProcessor commandProcessor,
        ScpState state,
        FirmwareVersion? firmwareVersion,
        byte insSendRemaining)
    {
        var scpProcessor = new ScpProcessor(commandProcessor, state);
        return new ChainedResponseReceiver(firmwareVersion, scpProcessor, insSendRemaining);
    }

    /// <summary>
    ///     Initializes an SCP03 session.
    /// </summary>
    private static async Task<(IApduProcessor, DataEncryptor)> InitScp03Async(
        ChainedResponseReceiver initializationProcessor,
        IApduProcessor commandProcessor,
        byte insSendRemaining,
        Scp03KeyParameters keyParams,
        CancellationToken cancellationToken)
    {
        // Initialize SCP03 session (sends INITIALIZE UPDATE)
        var (state, hostCryptogram) = await ScpState.Scp03InitAsync(
                initializationProcessor,
                keyParams,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var scpProcessor = new ScpProcessor(commandProcessor, state);

            // Send EXTERNAL AUTHENTICATE with host cryptogram.
            // Dispose scpProcessor (and its ScpState session keys) on any failure —
            // otherwise session keys leak to the GC finalizer (T10).
            try
            {
                var authCommand = new ApduCommand(
                    CLA_SECURE_MESSAGING,
                    INS_EXTERNAL_AUTHENTICATE,
                    SECURITY_LEVEL_CMAC_CDEC_RMAC_RENC,
                    0x00,
                    hostCryptogram);

                var authResponse = await scpProcessor.TransmitAsync(authCommand, true, false, cancellationToken)
                    .ConfigureAwait(false);
                if (authResponse.SW != SWConstants.Success)
                    throw ApduException.FromResponse(authResponse, authCommand, "SCP03 EXTERNAL AUTHENTICATE failed");
            }
            catch
            {
                scpProcessor.Dispose();
                throw;
            }

            var dataEncryptor = state.GetDataEncryptor();
            return (
                new ChainedResponseReceiver(initializationProcessor.FirmwareVersion, scpProcessor, insSendRemaining),
                dataEncryptor);
        }
        finally
        {
            // Zero hostCryptogram here — ApduCommand stores a reference (no clone), so zeroing
            // the source buffer is the caller's responsibility under the passthrough ownership model.
            CryptographicOperations.ZeroMemory(hostCryptogram);
        }
    }

    /// <summary>
    ///     Initializes an SCP11 session (supports variants a/b/c).
    /// </summary>
    private static async Task<(IApduProcessor, DataEncryptor)> InitScp11Async(
        ChainedResponseReceiver initializationProcessor,
        IApduProcessor commandProcessor,
        byte insSendRemaining,
        Scp11KeyParameters keyParams,
        CancellationToken cancellationToken)
    {
        // Initialize SCP11 session (performs ECDH key agreement)
        var state = await ScpState.Scp11InitAsync(
                initializationProcessor,
                keyParams,
                cancellationToken)
            .ConfigureAwait(false);

        var scpProcessor = CreateSecureProcessor(
            commandProcessor,
            state,
            initializationProcessor.FirmwareVersion,
            insSendRemaining);

        var dataEncryptor = state.GetDataEncryptor();
        return (scpProcessor, dataEncryptor);
    }
}