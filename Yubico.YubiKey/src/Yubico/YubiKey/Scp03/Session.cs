// Copyright 2021 Yubico AB
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

using System;
using System.Linq;
using System.Security.Cryptography;
using Yubico.Core.Cryptography;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Scp03.Commands;

namespace Yubico.YubiKey.Scp03
{
    [Obsolete("Use new SecurityDomainSesion class in Yubico.YubiKey.Scp namespace instead. This will be removed in a future release.")]
    internal class Session : IDisposable
    {
        private SessionKeys? _sessionKeys;
        private byte[]? _hostChallenge;
        private byte[]? _hostCryptogram;
        private byte[] _macChainingValue;
        private int _encryptionCounter;

        private bool _disposed;

        /// <summary>
        /// Initializes the host-side state for an SCP03 session.
        /// </summary>
        public Session()
        {
            _macChainingValue = new byte[16];
            _encryptionCounter = 1;

            _hostChallenge = null;
            _hostCryptogram = null;
            _sessionKeys = null;
            _disposed = false;
        }

        /// <summary>
        /// Builds the INITIALIZE_UPDATE APDU, using the supplied host challenge.
        /// </summary>
        /// <param name="keyVersionNumber">Which key set is to be used.</param>
        /// <param name="hostChallenge">Randomly chosen 8-byte challenge.</param>
        /// <returns>INITIALIZE_UPDATE APDU</returns>
        public InitializeUpdateCommand BuildInitializeUpdate(byte keyVersionNumber, byte[] hostChallenge)
        {
            if (hostChallenge is null)
            {
                throw new ArgumentNullException(nameof(hostChallenge));
            }

            if (hostChallenge.Length != 8)
            {
                throw new ArgumentException(ExceptionMessages.InvalidHostChallengeLength, nameof(hostChallenge));
            }

            _hostChallenge = hostChallenge;
            return new InitializeUpdateCommand(keyVersionNumber, hostChallenge);
        }

        /// <summary>
        /// Processes the card's response to the INITIALIZE_UPDATE APDU. Loads
        /// data into state. Must be called after <c>BuildInitializeUpdate</c>.
        /// </summary>
        /// <param name="initializeUpdateResponse">Response to the previous INITIALIZE_UPDATE</param>
        /// <param name="staticKeys">The secret static SCP03 keys shared by the host and card</param>
        public void LoadInitializeUpdateResponse(InitializeUpdateResponse initializeUpdateResponse, StaticKeys staticKeys)
        {
            if (_hostChallenge is null)
            {
                throw new InvalidOperationException(ExceptionMessages.LoadInitializeUpdatePriorToBuild);
            }

            if (initializeUpdateResponse is null)
            {
                throw new ArgumentNullException(nameof(initializeUpdateResponse));
            }

            if (staticKeys is null)
            {
                throw new ArgumentNullException(nameof(staticKeys));
            }

            if (initializeUpdateResponse.Status != ResponseStatus.Success)
            {
                throw new SecureChannelException(ExceptionMessages.InvalidInitializeUpdateResponse);
            }

            // parse data in response
            byte[] cardChallenge = initializeUpdateResponse.CardChallenge.ToArray();
            byte[] cardCryptogram = initializeUpdateResponse.CardCryptogram.ToArray();
            _sessionKeys = Derivation.DeriveSessionKeysFromStaticKeys(staticKeys, _hostChallenge, cardChallenge);

            // check supplied card cryptogram
            byte[] calculatedCardCryptogram = Derivation.DeriveCryptogram(
                Derivation.DDC_CARD_CRYPTOGRAM,
                _sessionKeys.GetSessionMacKey(),
                _hostChallenge,
                cardChallenge);

            if (!CryptographicOperations.FixedTimeEquals(cardCryptogram, calculatedCardCryptogram))
            {
                throw new SecureChannelException(ExceptionMessages.IncorrectCardCryptogram);
            }

            // calculate host cryptogram
            _hostCryptogram = Derivation.DeriveCryptogram(
                Derivation.DDC_HOST_CRYPTOGRAM,
                _sessionKeys.GetSessionMacKey(),
                _hostChallenge,
                cardChallenge);
        }

        /// <summary>
        /// Builds the EXTERNAL_AUTHENTICATE APDU. Must be called after
        /// <c>LoadInitializeUpdateResponse</c>.
        /// </summary>
        /// <returns>EXTERNAL_AUTHENTICATE APDU</returns>
        public ExternalAuthenticateCommand BuildExternalAuthenticate()
        {
            if (_sessionKeys == null)
            {
                throw new InvalidOperationException(ExceptionMessages.BuildExternalAuthenticatePriorToLoadInitializeUpdateResponse);
            }

            if (_hostCryptogram is null)
            {
                throw new InvalidOperationException(ExceptionMessages.BuildExternalAuthenticatePriorToLoadInitializeUpdateResponse);
            }

            var eaCommandInitial = new ExternalAuthenticateCommand(_hostCryptogram);
            CommandApdu macdApdu;
            (macdApdu, _macChainingValue) = ChannelMac.MacApdu(eaCommandInitial.CreateCommandApdu(), _sessionKeys.GetSessionMacKey(), _macChainingValue);
            var eaCommand = new ExternalAuthenticateCommand(macdApdu.Data.ToArray());
            return eaCommand;
        }

        /// <summary>
        /// Verifies that the EXTERNAL_AUTHENTICATE command was successful.
        /// </summary>
        /// <param name="externalAuthenticateResponse">Response to the previous EXTERNAL_AUTHENTICATE</param>
        public void LoadExternalAuthenticateResponse(ExternalAuthenticateResponse externalAuthenticateResponse)
        {
            if (_sessionKeys == null)
            {
                throw new InvalidOperationException(ExceptionMessages.LoadExternalAuthenticateResponsePriorToLoadInitializUpdateResponse);
            }

            if (externalAuthenticateResponse is null)
            {
                throw new ArgumentNullException(nameof(externalAuthenticateResponse));
            }

            externalAuthenticateResponse.ThrowIfFailed();
        }

        /// <summary>
        /// Encodes (encrypt then MAC) a command using SCP03. Modifies state,
        /// and must be sent in-order. Must be called after LoadInitializeUpdate.
        /// </summary>
        /// <returns></returns>
        public CommandApdu EncodeCommand(CommandApdu command)
        {
            if (_sessionKeys == null)
            {
                throw new InvalidOperationException(ExceptionMessages.UnknownScpError);
            }

            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var encodedCommand = new CommandApdu()
            {
                Cla = (byte)(command.Cla | 0x84),
                Ins = command.Ins,
                P1 = command.P1,
                P2 = command.P2
            };

            byte[] commandData = command.Data.ToArray();
            byte[] encryptedData = ChannelEncryption.EncryptData(commandData, _sessionKeys.GetSessionEncKey(), _encryptionCounter);
            _encryptionCounter += 1;
            encodedCommand.Data = encryptedData;

            CommandApdu encodedApdu;
            (encodedApdu, _macChainingValue) = ChannelMac.MacApdu(encodedCommand, _sessionKeys.GetSessionMacKey(), _macChainingValue);
            return encodedApdu;
        }

        /// <summary>
        /// Decodes (verify RMAC then decrypt) a raw response from the device.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public ResponseApdu DecodeResponse(ResponseApdu response)
        {
            if (_sessionKeys is null)
            {
                throw new InvalidOperationException(ExceptionMessages.UnknownScpError);
            }

            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            // If the response is not Success, just return the response. The
            // standard says, "No R-MAC shall be generated and no protection
            // shall be applied to a response that includes an error status word:
            // in this case only the status word shall be returned in the
            // response."
            if (response.SW != SWConstants.Success)
            {
                return response;
            }

            // ALWAYS check RMAC before decryption
            byte[] responseData = response.Data.ToArray();
            ChannelMac.VerifyRmac(responseData, _sessionKeys.GetSessionRmacKey(), _macChainingValue);

            byte[] decryptedData = Array.Empty<byte>();
            if (responseData.Length > 8)
            {
                decryptedData = ChannelEncryption.DecryptData(
                    responseData.Take(responseData.Length - 8).ToArray(),
                    _sessionKeys.GetSessionEncKey(),
                    _encryptionCounter - 1
                );
            }

            byte[] fullDecryptedResponse = new byte[decryptedData.Length + 2];
            decryptedData.CopyTo(fullDecryptedResponse, 0);
            fullDecryptedResponse[decryptedData.Length] = response.SW1;
            fullDecryptedResponse[decryptedData.Length + 1] = response.SW2;
            return new ResponseApdu(fullDecryptedResponse);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _sessionKeys?.Dispose();

                    _disposed = true;
                }
            }
        }
    }
}
