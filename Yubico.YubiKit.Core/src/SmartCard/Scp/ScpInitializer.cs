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

    /// <summary>
    ///     Initializes an SCP session and returns an SCP-wrapped processor with data encryptor.
    /// </summary>
    /// <param name="baseProcessor">The base APDU processor (without SCP)</param>
    /// <param name="keyParams">SCP key parameters (SCP03 or SCP11)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (SCP-wrapped processor, data encryptor)</returns>
    /// <exception cref="ArgumentException">Thrown when keyParams type is unsupported</exception>
    /// <exception cref="NotSupportedException">Thrown when device doesn't support SCP</exception>
    /// <exception cref="ApduException">Thrown when SCP initialization fails</exception>
    public static async Task<(IApduProcessor scpProcessor, DataEncryptor dataEncryptor)> InitializeScpAsync(
        IApduProcessor baseProcessor,
        ScpKeyParameters keyParams,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return keyParams switch
            {
                Scp03KeyParameters scp03 => await InitScp03Async(baseProcessor, scp03, cancellationToken)
                    .ConfigureAwait(false),
                Scp11KeyParameters scp11 => await InitScp11Async(baseProcessor, scp11, cancellationToken)
                    .ConfigureAwait(false),
                _ => throw new ArgumentException($"Unsupported ScpKeyParams type: {keyParams.GetType().Name}",
                    nameof(keyParams))
            };
        }
        catch (ApduException ex) when (ex.SW == SWConstants.ClaNotSupported)
        {
            throw new NotSupportedException("SCP is not supported by this YubiKey", ex);
        }
    }

    /// <summary>
    ///     Initializes an SCP03 session.
    /// </summary>
    private static async Task<(IApduProcessor, DataEncryptor)> InitScp03Async(
        IApduProcessor baseProcessor,
        Scp03KeyParameters keyParams,
        CancellationToken cancellationToken)
    {
        // Initialize SCP03 session (sends INITIALIZE UPDATE)
        var (state, hostCryptogram) = await ScpState.Scp03InitAsync(
                baseProcessor,
                keyParams,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Create SCP processor with base processor's formatter
        var scpProcessor = new ScpProcessor(baseProcessor, baseProcessor.Formatter, state);

        // Send EXTERNAL AUTHENTICATE with host cryptogram
        // NOTE: Command must have CLA with SM bit already set - MAC is computed over this CLA
        // Matches Java: new Apdu(0x84, 0x82, 0x33, 0, pair.second)
        var authCommand = new ApduCommand(
            CLA_SECURE_MESSAGING,
            INS_EXTERNAL_AUTHENTICATE,
            SECURITY_LEVEL_CMAC_CDEC_RMAC_RENC,
            0x00,
            hostCryptogram);
        var authResponse = await scpProcessor.TransmitAsync(authCommand, true, false, cancellationToken)
            .ConfigureAwait(false);

        if (authResponse.SW != SWConstants.Success)
            throw new ApduException($"EXTERNAL AUTHENTICATE failed with SW=0x{authResponse.SW:X4}")
            {
                SW = authResponse.SW
            };

        var dataEncryptor = state.GetDataEncryptor();
        return (scpProcessor, dataEncryptor);
    }

    /// <summary>
    ///     Initializes an SCP11 session (supports variants a/b/c).
    /// </summary>
    private static async Task<(IApduProcessor, DataEncryptor)> InitScp11Async(
        IApduProcessor baseProcessor,
        Scp11KeyParameters keyParams,
        CancellationToken cancellationToken)
    {
        // Initialize SCP11 session (performs ECDH key agreement)
        var state = await ScpState.Scp11InitAsync(
                baseProcessor,
                keyParams,
                cancellationToken)
            .ConfigureAwait(false);

        // Wrap base processor with SCP
        var scpProcessor = new ScpProcessor(baseProcessor, baseProcessor.Formatter, state);

        var dataEncryptor = state.GetDataEncryptor();
        return (scpProcessor, dataEncryptor);
    }
}