using System;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Scp.Helpers;

namespace Yubico.YubiKey.Scp
{
    internal abstract class ScpState : IDisposable
    {
        /// <summary>
        /// The session keys for secure channel communication.
        /// </summary>
        protected readonly SessionKeys SessionKeys;

        /// <summary>
        /// The chaining value for the MAC.
        /// </summary>
        protected ReadOnlyMemory<byte> MacChainingValue;

        private int _encryptionCounter = 1;
        private bool _disposed;

        /// <summary>
        /// Initializes the host-side state for an SCP session.
        /// </summary>
        protected ScpState(SessionKeys sessionKeys, ReadOnlyMemory<byte> macChain)
        {
            MacChainingValue = macChain;
            SessionKeys = sessionKeys;
        }

        /// <summary>
        /// Encodes (encrypt then MAC) a command using SCP03. Modifies state,
        /// and must be sent in-order. Must be called after LoadInitializeUpdate.
        /// </summary>
        /// <returns></returns>
        public CommandApdu EncodeCommand(CommandApdu command)
        {
            if (SessionKeys == null)
            {
                throw new InvalidOperationException(ExceptionMessages.UnknownScpError);
            }

            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            // Create an encrypted APDU
            var encodedCommand = new CommandApdu
            {
                Cla = (byte)(command.Cla | 0x04), //0x04 is for secure-messaging
                Ins = command.Ins,
                P1 = command.P1,
                P2 = command.P2
            };

            // Encrypt the data
            var encryptedData = ChannelEncryption.EncryptData(
                command.Data.Span, SessionKeys.EncKey.Span, _encryptionCounter);

            // Update the encryption counter
            _encryptionCounter++;
            encodedCommand.Data = encryptedData;

            // Create a MAC:ed APDU
            var (macdApdu, newMacChainingValue) = MacApdu(
                encodedCommand,
                SessionKeys.MacKey.Span,
                MacChainingValue.Span);

            // Update the sessions MacChainingValue
            MacChainingValue = newMacChainingValue;

            return macdApdu;
        }

        /// <summary>
        /// Decodes (verify RMAC then decrypt) a raw response from the device.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public ResponseApdu DecodeResponse(ResponseApdu response)
        {
            if (SessionKeys is null)
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
            var responseData = response.Data;
            VerifyRmac(responseData.Span, SessionKeys.RmacKey.Span, MacChainingValue.Span);

            // Initialize decryptedData as an empty array
            ReadOnlyMemory<byte> decryptedData = Array.Empty<byte>();
            if (responseData.Length > 8)
            {
                // Decrypt the response data if it's longer than 8 bytes
                int previousEncryptionCounter = _encryptionCounter - 1;
                decryptedData = ChannelEncryption.DecryptData(
                    responseData[..^8].Span,
                    SessionKeys.EncKey.Span,
                    previousEncryptionCounter
                    );
            }

            // Create a new array to hold the decrypted data and status words
            byte[] fullDecryptedResponse = new byte[decryptedData.Length + 2];
            decryptedData.CopyTo(fullDecryptedResponse);
            
            // Append the status words to the decrypted data
            fullDecryptedResponse[decryptedData.Length] = response.SW1;
            fullDecryptedResponse[decryptedData.Length + 1] = response.SW2;
            
            // Return a new ResponseApdu with the decrypted data and status words
            return new ResponseApdu(fullDecryptedResponse);
        }

        /// <summary>
        /// Get the encryptor to encrypt any data for a SCP command.
        /// </summary>
        /// <returns>
        /// An encryptor function that takes the plaintext as a parameter and
        /// returns the encrypted data.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// If the data encryption key has not been set on the session keys.
        /// </exception>
        public EncryptDataFunc GetDataEncryptor()
        {
            if (!SessionKeys.DataEncryptionKey.HasValue)
            {
                throw new InvalidOperationException(ExceptionMessages.UnknownScpError);
            }

            return plainText => AesUtilities.AesCbcEncrypt(
                SessionKeys.DataEncryptionKey.Value.Span,
                new byte[16],
                plainText.Span);
        }

        /// <summary>
        /// Performs the MAC on the APDU
        /// </summary>
        /// <param name="commandApdu"></param>
        /// <param name="macKey"></param>
        /// <param name="macChainingValue"></param>
        /// <returns></returns>
        protected static (CommandApdu macdApdu, ReadOnlyMemory<byte> newMacChainingValue) MacApdu(
            CommandApdu commandApdu,
            ReadOnlySpan<byte> macKey,
            ReadOnlySpan<byte> macChainingValue) 
            => ChannelMac.MacApdu(commandApdu, macKey, macChainingValue);

        private static void VerifyRmac(
            ReadOnlySpan<byte> responseData,
            ReadOnlySpan<byte> rmacKey,
            ReadOnlySpan<byte> macChainingValue) 
            => ChannelMac.VerifyRmac(responseData, rmacKey, macChainingValue);

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
                    SessionKeys?.Dispose();

                    _disposed = true;
                }
            }
        }
    }

    /// <summary>
    /// This delegate is used to encrypt data with the session keys
    /// <seealso cref="ScpState.GetDataEncryptor"/>
    /// </summary>
    internal delegate ReadOnlyMemory<byte> EncryptDataFunc(ReadOnlyMemory<byte> data);
}
