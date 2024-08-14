// Copyright 2024 Yubico AB
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
using Yubico.Core.Tlv;
using Yubico.YubiKey.Piv.Commands;

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// This class parses the response data from the PIV Get Bio Metadata command. It
    /// holds data about Bio multi-protocol key.
    /// </summary>
    /// <remarks>
    /// The response to the
    /// <see cref="Commands.GetBioMetadataCommand"/> is
    /// <see cref="Commands.GetBioMetadataResponse"/>.
    /// Call the <c>GetData</c> method in the response object to get the
    /// metadata. An instance of this class will be returned.
    /// </remarks>
    public class PivBioMetadata
    {
        private const int AttemptsRemainingTag = 0x06;

        private const int BioConfiguredTag = 0x07;

        private const int TemporaryPinTag = 0x08;

        /// <summary>
        /// The constructor that takes in the bio metadata encoding returned by the
        /// YubiKey in response to the <see cref="GetBioMetadataCommand"/>.
        /// </summary>
        /// <param name="responseData">
        /// The data portion of the response APDU, this is the encoded bio metadata.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The data supplied is not valid PIV bio metadata.
        /// </exception>
        public PivBioMetadata(ReadOnlyMemory<byte> responseData)
        {

            var tlvReader = new TlvReader(responseData);

            bool? isConfigured = null;
            int? attemptsRemaining = null;
            bool? hasTemporaryPin = null;


            while (tlvReader.HasData)
            {
                int tag = tlvReader.PeekTag();
                var value = tlvReader.ReadValue(tag);

                switch (tag)
                {
                    case BioConfiguredTag:
                        isConfigured = value.Span[0] == 1;
                        break;

                    case AttemptsRemainingTag:
                        attemptsRemaining = value.Span[0];
                        break;

                    case TemporaryPinTag:
                        hasTemporaryPin = value.Span[0] == 1;
                        break;

                    default:
                        throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.InvalidApduResponseData));
                }
            }


            if (isConfigured == null || attemptsRemaining == null || hasTemporaryPin == null)
            {
                throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidApduResponseData));
            }

            IsConfigured = isConfigured.Value;
            AttemptsRemaining = attemptsRemaining.Value;
            HasTemporaryPin = hasTemporaryPin.Value;
        }


        /// <summary>
        /// Indicates whether biometrics are configured or not (fingerprints enrolled or not).
        /// A false return value indicates a YubiKey Bio without biometrics configured and hence the
        /// client should fallback to a PIN based authentication.
        /// </summary>
        /// <returns>true if biometrics are configured or not.</returns>
        public bool IsConfigured { get; private set; }

        /// <summary>
        /// Returns value of biometric match retry counter which states how many biometric match retries
        /// are left until a YubiKey Bio is blocked.
        /// If this method returns 0 and {@link #isConfigured()} returns true, the device is blocked for
        /// biometric match and the client should invoke PIN based authentication to reset the biometric
        /// match retry counter.
        /// </summary>
        public int AttemptsRemaining { get; private set; }

        /// <summary>
        /// Indicates whether a temporary PIN has been generated in the YubiKey in relation to a 
        /// successful biometric match.
        /// </summary>
        /// <returns>true if a temporary PIN has been generated.</returns>
        public bool HasTemporaryPin { get; private set; }
    }
}
