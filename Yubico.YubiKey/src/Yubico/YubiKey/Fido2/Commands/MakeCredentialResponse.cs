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
using System.Formats.Cbor;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Represents a response to the MakeCredential FIDO2 command. Contains a <see cref="IMakeCredentialOutput"/> as its data.
    /// </summary>
    internal class MakeCredentialResponse : Fido2Response, IYubiKeyResponseWithData<IMakeCredentialOutput>
    {
        public MakeCredentialResponse(ResponseApdu responseApdu) : base(responseApdu)
        {

        }

        public IMakeCredentialOutput GetData()
        {
            ThrowIfFailed();

            byte[] cborData = ResponseApdu.Data.Slice(1).ToArray();
            string? attestationFormatIdentifier = TryReadingAttestationFormatIdentifier(cborData);
            var reader = new CborReader(cborData, CborConformanceMode.Ctap2Canonical);

            // Attempt to deserialize to the various attestation formats
            IMakeCredentialOutput makeCredentialOutput = attestationFormatIdentifier switch
            {
                "packed" => Ctap2CborSerializer.Deserialize<MakeCredentialOutput<PackedAttestation>>(reader),
                "none" => Ctap2CborSerializer.Deserialize<MakeCredentialOutput<NoneAttesation>>(reader),
                _ => throw new MalformedYubiKeyResponseException(ExceptionMessages.Ctap2UnknownAttestationFormat)
            };

            return makeCredentialOutput;
        }

        private static string? TryReadingAttestationFormatIdentifier(byte[] cborData)
        {
            var reader = new CborReader(cborData, CborConformanceMode.Ctap2Canonical);

            try
            {
                _ = reader.ReadStartMap();

                if (reader.ReadUInt32() != 1) // the 'AttestationFormatIdentifier' label per the CTAP2 spec
                {
                    return null;
                }

                return reader.ReadTextString();
            }
            catch (InvalidOperationException e)
            {
                throw new CborContentException(ExceptionMessages.Ctap2CborDeserializationError, e);
            }
        }
    }
}
