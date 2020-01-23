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
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Complete the process to authenticate the PIV management key.
    /// </summary>
    /// <remarks>
    /// In the PIV standard, there is a command called GENERAL AUTHENTICATE.
    /// Although it is one command, it can do four things: authenticate a
    /// management key (challenge-response), sign arbitrary data, RSA decryption,
    /// and EC Diffie-Hellman. The SDK breaks these four operations into separate
    /// classes. This class is how you complete the process of performing
    /// "GENERAL AUTHENTICATE: management key".
    /// <para>
    /// The partner Response class is <see cref="CompleteAuthenticateManagementKeyResponse"/>.
    /// </para>
    /// <para>
    /// See the comments for the class
    /// <see cref="InitializeAuthenticateManagementKeyCommand"/>, there is a
    /// lengthy discussion of the process of authenticating the management key,
    /// including descriptions of the challenges and responses.
    /// </para>
    /// </remarks>
    public sealed class CompleteAuthenticateManagementKeyCommand
        : IYubiKeyCommand<CompleteAuthenticateManagementKeyResponse>
    {
        private const byte AuthMgmtKeyInstruction = 0x87;
        private const byte AuthMgmtKeyParameter1 = 0x03;
        private const byte AuthMgmtKeyParameter2 = 0x9B;

        private const int ManagementKeyLength = 24;
        private const int ChallengeLength = 8;
        private const int Step2SingleLength = 12;
        private const int Step2MutualLength = 24;
        private const int ResponseOffset = 4;
        private const int ChallengeOffset = 14;
        private const int L0Index = 1;
        private const byte L0Single = 10;
        private const int T1Index = 2;
        private const byte T1Single = 0x82;

        private readonly byte[] _data = new byte[Step2MutualLength] {
            0x7C, 0x16, 0x80, 0x08, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x81, 0x08, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x82, 0x00
        };
        private readonly int _dataLength;
        private readonly byte[] _yubiKeyAuthenticationExpectedResponse;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        // Note that there is no object-initializer constructor. All the
        // constructor inputs have no default or are secret byte arrays.
        private CompleteAuthenticateManagementKeyCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of the CompleteAuthenticateManagementKeyCommand class.
        /// </summary>
        /// <remarks>
        /// The input Response Object is the successful Response from step 1. The
        /// response has information on whether the process was initiated for
        /// single or mutual authentication. The object created using this
        /// constructor will therefore be able to perform the appropriate
        /// operations and build the appropriate APDU based on how the process
        /// was initiated.
        /// <para>
        /// This class will use the random number generator and Triple-DES
        /// classes from <see cref="CryptographyProviders"/>. If you want this
        /// class to use classes other than the defaults, change them. See also
        /// the user's manual entry on
        /// <xref href="UsersManualAlternateCrypto"> alternate crypto </xref> for
        /// information on how to do so.
        /// </para>
        /// </remarks>
        /// <param name="initializeAuthenticationResponse">
        /// The Response Object from Step 1.
        /// </param>
        /// <param name="managementKey">
        /// The bytes of the management key.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>initializeAuthenticationResponse</c> argument is null
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <c>initializeAuthenticationResponse</c> argument does not
        /// represent a complete response.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <c>managementKey</c> argument is not a valid Triple-DES key.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The Triple-DES operation failed.
        /// </exception>
        public CompleteAuthenticateManagementKeyCommand(
            InitializeAuthenticateManagementKeyResponse initializeAuthenticationResponse,
            ReadOnlySpan<byte> managementKey)
        {
            if (initializeAuthenticationResponse is null)
            {
                throw new ArgumentNullException(nameof(initializeAuthenticationResponse));
            }
            if (initializeAuthenticationResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidApduResponseData));
            }
            if (managementKey.Length != ManagementKeyLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectTripleDesKeyLength));
            }

            _yubiKeyAuthenticationExpectedResponse = Array.Empty<byte>();

            (bool isMutual, ReadOnlyMemory<byte> ClientAuthenticationChallenge) = initializeAuthenticationResponse.GetData();

            int bytesWritten = 0;
            int expectedWritten = ChallengeLength;

            // With single auth, encrypt the challenge. Mutual decrypts.
            // Note that the constructor for the TDES object takes in an arg
            // "isEncrypting". If true, encrypt. We want to decrypt for mutual
            // auth, so when isMutual is true, we want to pass false to the TDES
            // constructor. And vice versa.

            // JUSTIFICATION: We are using the TripleDesForManagementKey class in the way it was intended.
#pragma warning disable 618
            using var tripleDes = new TripleDesForManagementKey(managementKey, !isMutual);
#pragma warning restore 618

            if (isMutual == true)
            {
                // For mutual auth, we will decrypt the witness
                using RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();
                _yubiKeyAuthenticationExpectedResponse = new byte[ChallengeLength];
                randomObject.GetBytes(_yubiKeyAuthenticationExpectedResponse, 0, ChallengeLength);

                // The app will send the YubiKey a challenge in the clear. The
                // YubiKey will encrypt it. So we want to verify that what the
                // YubiKey returns is the encrypted challenge.
                // Instead of creating a new encryption object, just use decrypt.
                // Generate random data and call it the encrypted challenge, it is
                // the expected response. We know that encrypting the challenge
                // will produce the response, so decrypting the response will
                // produce the challenge.
                // Decrypt the YubiKey Authentication Expected Response
                // to get YubiKey Authentication Challenge.
                // The (Triple-)DES API needs the key data in a byte array.

                bytesWritten += tripleDes.TransformBlock(
                    _yubiKeyAuthenticationExpectedResponse,
                    0,
                    ChallengeLength,
                    _data,
                    ChallengeOffset);
                expectedWritten += ChallengeLength;
                _dataLength = Step2MutualLength;
            }
            else
            {
                _data[L0Index] = L0Single;
                _data[T1Index] = T1Single;
                _dataLength = Step2SingleLength;
            }

            // (Mutual auth) Decrypt Client Authentication Challenge to generate
            // Client Authentication Response.
            // - or -
            // (Single auth) Encrypt Client Authentication Witness to generate
            // Client Authentication Response.
            bytesWritten += tripleDes.TransformBlock(
                ClientAuthenticationChallenge.ToArray(),
                0,
                ChallengeLength,
                _data,
                ResponseOffset);

            if (bytesWritten != expectedWritten)
            {
                throw new CryptographicException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.TripleDesFailed));
            }
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Ins = AuthMgmtKeyInstruction,
            P1 = AuthMgmtKeyParameter1,
            P2 = AuthMgmtKeyParameter2,
            Data = _data.Take(_dataLength).ToArray(),
        };

        /// <inheritdoc />
        public CompleteAuthenticateManagementKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new CompleteAuthenticateManagementKeyResponse(responseApdu, _yubiKeyAuthenticationExpectedResponse);
    }
}
