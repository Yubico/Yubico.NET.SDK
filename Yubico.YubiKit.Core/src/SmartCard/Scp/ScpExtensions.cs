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
///     Extension methods for enabling SCP (Secure Channel Protocol) on SmartCard protocols.
/// </summary>
public static class ScpExtensions
{
    #region Nested type: <extension>

    extension(ISmartCardProtocol protocol)
    {
        /// <summary>
        ///     Initializes SCP on this protocol and returns an SCP-wrapped protocol instance.
        ///     This is a convenience method that combines SCP initialization and protocol wrapping.
        /// </summary>
        /// <param name="keyParams">SCP key parameters (SCP03 or SCP11)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        ///     An SCP-enabled protocol that encrypts and MACs all commands.
        ///     All subsequent operations through this protocol will use SCP.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when protocol is not PcscProtocol</exception>
        /// <exception cref="NotSupportedException">Thrown when device doesn't support SCP</exception>
        /// <exception cref="ApduException">Thrown when SCP initialization fails</exception>
        /// <example>
        ///     <code>
        /// // Enable SCP03 on a protocol
        /// ISmartCardProtocol protocol = new PcscProtocol(logger, connection);
        /// protocol = await protocol.WithScpAsync(scp03KeyParams);
        /// 
        /// // Now all commands go through SCP
        /// var response = await protocol.TransmitAndReceiveAsync(command);
        /// </code>
        /// </example>
        public async Task<ISmartCardProtocol> WithScpAsync(
            ScpKeyParams keyParams,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(protocol);
            ArgumentNullException.ThrowIfNull(keyParams);

            if (protocol is not PcscProtocol pcscProtocol)
                throw new ArgumentException(
                    "SCP is only supported on PcscProtocol. " +
                    "Ensure the protocol was created via PcscProtocolFactory.",
                    nameof(protocol));

            // Validate firmware version if available
            if (pcscProtocol.FirmwareVersion is not null)
                switch (keyParams)
                {
                    case Scp03KeyParams when !pcscProtocol.FirmwareVersion.IsAtLeast(5, 3, 0):
                        throw new NotSupportedException("SCP03 requires YubiKey firmware 5.3.0 or newer");
                    case Scp11KeyParams when !pcscProtocol.FirmwareVersion.IsAtLeast(5, 7, 2):
                        throw new NotSupportedException("SCP11 requires YubiKey firmware 5.7.2 or newer");
                }

            var (scpProcessor, encryptor) = await ScpInitializer.InitializeScpAsync(
                    pcscProtocol.GetBaseProcessor(),
                    keyParams,
                    cancellationToken)
                .ConfigureAwait(false);

            return new ScpProtocolAdapter(protocol, scpProcessor, encryptor);
        }
    }

    #endregion
}