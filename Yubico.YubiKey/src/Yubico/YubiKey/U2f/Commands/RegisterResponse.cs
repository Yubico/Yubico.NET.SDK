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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// The response to the U2F Register command.
    /// </summary>
    /// <remarks>
    /// This is the partner response class to <see cref="RegisterCommand"/>.
    /// <p>
    /// Registration on most devices will first fail with <see cref="ResponseStatus.ConditionsNotSatisfied"/>
    /// and then the device will begin waiting for a touch to verify user presence.
    /// See <see cref="RegisterCommand"/> for more details.
    /// </p>
    /// </remarks>
    public class RegisterResponse : U2fResponse, IYubiKeyResponseWithData<RegistrationData>
    {
        private const byte ReservedResponseValue = 0x05;
        private const int KeyHandleOffset = 67;
        private const int MinDataLength = KeyHandleOffset;
        private const int EcPublicKeyLength = 65;
        private const int EcPublicKeyTag = 0x04;
        private const int EcCoordinateLength = 32;

        /// <summary>
        /// Constructs a RegisterResponse from the given ResponseApdu.
        /// </summary>
        /// <param name="responseApdu">The response to a <see cref="RegisterCommand"/>.</param>
        public RegisterResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {

        }

        /// <summary>
        /// Gets the registration data from the response.
        /// </summary>
        /// <remarks>
        /// If the status of the response is not 'Success', this method will fail. If the
        /// status of the response is <see cref="ResponseStatus.ConditionsNotSatisfied"/> then
        /// clients should retry the command until it succeeds (when user presence is confirmed,
        /// generally through touch).
        /// <p>
        /// Throws a <see cref="MalformedYubiKeyResponseException"/> in the event of an error
        /// parsing the device response.
        /// </p>
        /// </remarks>
        /// <returns>
        /// The data in the response APDU, presented as a <see cref="RegistrationData"/> object.
        /// </returns>
        public RegistrationData GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(ExceptionMessages.NoResponseDataApduFailed);
            }

            if (ResponseApdu.Data.Length < MinDataLength || ResponseApdu.Data.Span[0] != ReservedResponseValue)
            {
                ThrowMalformedResponse();
            }

            ReadOnlySpan<byte> data = ResponseApdu.Data.Span;
            ReadOnlySpan<byte> userPublicKeyBytes = data.Slice(1, EcPublicKeyLength);

            if (userPublicKeyBytes[0] != EcPublicKeyTag)
            {
                ThrowMalformedResponse();
            }

            var userPublicKey = new ECPoint
            {
                X = userPublicKeyBytes.Slice(1, EcCoordinateLength).ToArray(),
                Y = userPublicKeyBytes.Slice(1 + EcCoordinateLength, EcCoordinateLength).ToArray()
            };

            byte keyHandleLength = data[66];

            if (keyHandleLength == 0 || data.Length < KeyHandleOffset + keyHandleLength)
            {
                ThrowMalformedResponse();
            }

            ReadOnlySpan<byte> keyHandle = data.Slice(KeyHandleOffset, keyHandleLength);

            int certificateOffset = KeyHandleOffset + keyHandleLength;
            ReadOnlySpan<byte> certificateAndSignatureBytes = data.Slice(certificateOffset);

            X509Certificate2 attestationCertificate;

            try
            {
                attestationCertificate = new X509Certificate2(certificateAndSignatureBytes.ToArray());
            }
            catch (CryptographicException cryptoException)
            {
                throw new MalformedYubiKeyResponseException(ExceptionMessages.FailedParsingCertificate, cryptoException);
            }

            ReadOnlySpan<byte> signature = certificateAndSignatureBytes.Slice(attestationCertificate.RawData.Length);

            // TODO: Span -> Memory here to avoid the .ToArray calls
            return new RegistrationData(userPublicKey, keyHandle.ToArray(), attestationCertificate, signature.ToArray());
        }

        private static void ThrowMalformedResponse() =>
            throw new MalformedYubiKeyResponseException()
            {
                ResponseClass = nameof(RegisterResponse)
            };
    }
}
