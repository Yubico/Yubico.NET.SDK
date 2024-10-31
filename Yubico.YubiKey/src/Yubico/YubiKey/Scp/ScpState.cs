using System;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Scp
{
    internal abstract class ScpState : IDisposable
    {
        protected readonly SessionKeys SessionKeys;
        protected Memory<byte> MacChainingValue;

        private int _encryptionCounter = 1;
        private bool _disposed;

        /// <summary>
        /// Initializes the host-side state for an SCP session.
        /// </summary>
        public ScpState(SessionKeys sessionKeys, Memory<byte> macChain)
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

            var encodedCommand = new CommandApdu
            {
                Cla = (byte)(command.Cla | 0x04), //0x04 is for secure-messaging
                Ins = command.Ins,
                P1 = command.P1,
                P2 = command.P2
            };

            byte[] commandData = command.Data.ToArray();
            var encryptedData = ChannelEncryption.EncryptData(
                commandData, SessionKeys.EncKey.Span, _encryptionCounter);

            _encryptionCounter++;
            encodedCommand.Data = encryptedData;

            // Create a MAC:ed APDU
            (var macdApdu, byte[] newMacChainingValue) = MacApdu(
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

            Memory<byte> decryptedData = Array.Empty<byte>();
            if (responseData.Length > 8)
            {
                int previousEncryptionCounter = _encryptionCounter - 1;
                decryptedData = ChannelEncryption.DecryptData(
                    responseData[..^8].ToArray(),
                    SessionKeys.EncKey.ToArray(), //todo array
                    previousEncryptionCounter
                    );
            }

            byte[] fullDecryptedResponse = new byte[decryptedData.Length + 2];
            decryptedData.CopyTo(fullDecryptedResponse);
            fullDecryptedResponse[decryptedData.Length] = response.SW1;
            fullDecryptedResponse[decryptedData.Length + 1] = response.SW2;
            return new ResponseApdu(fullDecryptedResponse);
        }

        public DataEncryptor GetDataEncryptor()
        {
            if (!SessionKeys.DataEncryptionKey.HasValue)
            {
                throw new InvalidOperationException(ExceptionMessages.UnknownScpError); //todo set correct message
            }

            return plainText => AesUtilities.AesCbcEncrypt(
                SessionKeys.DataEncryptionKey.Value.ToArray(),
                new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                plainText.Span);
        }

        #pragma warning disable CA1822 // Is being used by subclasses
        protected (CommandApdu macdApdu, byte[] newMacChainingValue) MacApdu(
            #pragma warning restore CA1822
            CommandApdu commandApdu,
            ReadOnlySpan<byte> macKey,
            ReadOnlySpan<byte> macChainingValue) =>
            ChannelMac.MacApdu(commandApdu, macKey, macChainingValue);

        protected static void VerifyRmac(
            ReadOnlySpan<byte> responseData,
            ReadOnlySpan<byte> rmacKey,
            ReadOnlySpan<byte> macChainingValue) =>
            ChannelMac.VerifyRmac(responseData, rmacKey, macChainingValue);

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

    internal delegate ReadOnlyMemory<byte> DataEncryptor(ReadOnlyMemory<byte> data);
}
