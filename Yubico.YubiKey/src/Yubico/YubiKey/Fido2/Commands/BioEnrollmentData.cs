// Copyright 2023 Yubico AB
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
using System.Collections.Generic;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Contains the data returned by the YubiKey after calling one of the
    /// <c>authenticatorBioEnrollment</c> subcommands.
    /// </summary>
    /// <remarks>
    /// When a BioEnrollment subcommand is sent to the YubiKey, it returns data
    /// encoded following the definition of the <c>authenticatorBioEnrollment</c>
    /// response. The FIDO2 standard defines this encoded response as a map of a
    /// set of elements. The standard also specifies which subset of the total
    /// data is returned by each subcommand.
    /// <para>
    /// After calling one of the subcommands, get the data out of the response.
    /// It will be an instance of this class. Only those elements the particular
    /// subcommand returns will be represented in the object, the rest will be
    /// null.
    /// </para>
    /// <para>
    /// For example, if you call the get modality subcommand, the YubiKey will
    /// return the modality (an integer). Hence, the only property with a value
    /// will be <c>Modality</c>. All other properties will be null.
    /// </para>
    /// </remarks>
    public class BioEnrollmentData
    {
        private const int KeyModality = 1;
        private const int KeyFingerprintKind = 2;
        private const int KeyMaxCaptureCount = 3;
        private const int KeyTemplateId = 4;
        private const int KeyLastEnrollStatus = 5;
        private const int KeyRemainingSampleCount = 6;
        private const int KeyTemplateInfos = 7;
        private const int KeyMaxFriendlyNameBytes = 8;

        private const int KeyTemplateInfoId = 1;
        private const int KeyFriendlyName = 2;

        /// <summary>
        /// The modality of the YubiKey Bio component. The modality is the
        /// technique used to obtain Bio authentication. This is an optional
        /// element and can be null.
        /// </summary>
        /// <remarks>
        /// The value returned by the YubiKey is an integer. The standard defines
        /// what each integer means. Currently, the standard defines only one
        /// modality, fingerprint, and that is the integer <c>1</c>.
        /// </remarks>
        public int? Modality { get; private set; }

        /// <summary>
        /// The kind of fingerprint reader, that is, what method the fingerprint
        /// reader uses. This is an optional element and can be null.
        /// </summary>
        /// <remarks>
        /// The value returned by the YubiKey is an integer. The standard defines
        /// what each integer means. Currently only two integers are defined:
        /// <c>1</c> for touch type sensor and <c>2</c> for swipe type sensor.
        /// </remarks>
        public int? FingerprintKind { get; private set; }

        /// <summary>
        /// The number of "good" fingerprint captures required to enroll. This is
        /// an optional element and can be null.
        /// </summary>
        /// <remarks>
        /// In order to enroll a fingerprint, it is necessary to provide the
        /// fingerprint. The reader will need several examples of the
        /// fingerprint, generally from a number of angles. Once it has
        /// <c>MaxCaptureCount</c> good examples, it will consider the
        /// fingerprint enrolled. If it cannot get enough "good" examples, it
        /// will not accept the fingerprint and will require the user to start
        /// over.
        /// </remarks>
        public int? MaxCaptureCount { get; private set; }

        /// <summary>
        /// The maximum length, in bytes, of a template friendly name. This is an
        /// optional element and can be null.
        /// </summary>
        /// <remarks>
        /// A "template" is one biometric entry: all the data the YubiKey stores
        /// for one fingerprint. The FIDO2 standard uses the word template
        /// because there could be several types of biometric entries, such as
        /// fingerprint, face, voice, or more. So one template is one entry. The
        /// friendly name can be "right-index" or "left-thumb".
        /// <para>
        /// With the SDK, you provide a friendly name as a string. Before it is
        /// stored, it is converted to UTF-8. The length, in bytes, of the
        /// friendly name is therefore the number of bytes in the UTF-8 array
        /// that is the converted string.
        /// </para>
        /// </remarks>
        public int? MaxFriendlyNameBytes { get; private set; }

        /// <summary>
        /// The template ID of the fingerprint being enrolled. This is an
        /// optional element and can be null.
        /// </summary>
        public ReadOnlyMemory<byte>? TemplateId { get; private set; }

        /// <summary>
        /// The result of the most recent attempt to provide a fingerprint
        /// sample. This is an optional element and can be null.
        /// </summary>
        public int? LastEnrollSampleStatus { get; private set; }

        /// <summary>
        /// The number of successful fingerprint samples required to complete an
        /// enrollment. This is an optional element and can be null.
        /// </summary>
        public int? RemainingSampleCount { get; private set; }

        /// <summary>
        /// The enumeration of enrolled fingerprints. This is an optional element
        /// and can be null.
        /// </summary>
        /// <remarks>
        /// This is a list of templateId/friendlyName pairs.
        /// </remarks>
        public IReadOnlyList<TemplateInfo>? TemplateInfos { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private BioEnrollmentData()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of <see cref="BioEnrollmentData"/> based on the
        /// given CBOR encoding.
        /// </summary>
        /// <param name="cborEncoding">
        /// The BioEnrollment data, encoded following the CTAP 2.1 and CBOR (RFC
        /// 8949) standards.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The <c>cborEncoding</c> is not a valid CBOR encoding, or it is not a
        /// correct encoding for FIDO2 BioEnrollment data.
        /// </exception>
        public BioEnrollmentData(ReadOnlyMemory<byte> cborEncoding)
        {
            var cborMap = new CborMap<int>(cborEncoding);

            Modality = (int?)cborMap.ReadOptional<int>(KeyModality);
            FingerprintKind = (int?)cborMap.ReadOptional<int>(KeyFingerprintKind);
            MaxCaptureCount = (int?)cborMap.ReadOptional<int>(KeyMaxCaptureCount);
            MaxFriendlyNameBytes = (int?)cborMap.ReadOptional<int>(KeyMaxFriendlyNameBytes);
            byte[]? templateId = (byte[]?)cborMap.ReadOptional<byte[]>(KeyTemplateId);
            if (!(templateId is null))
            {
                TemplateId = new ReadOnlyMemory<byte>(templateId);
            }

            LastEnrollSampleStatus = (int?)cborMap.ReadOptional<int>(KeyLastEnrollStatus);
            RemainingSampleCount = (int?)cborMap.ReadOptional<int>(KeyRemainingSampleCount);

            if (cborMap.Contains(KeyTemplateInfos))
            {
                IReadOnlyList<CborMap<int>> templateList = cborMap.ReadArray<CborMap<int>>(KeyTemplateInfos);
                var templateInfos = new List<TemplateInfo>(templateList.Count);
                foreach (CborMap<int> currentMap in templateList)
                {
                    byte[] currentId = currentMap.ReadByteString(KeyTemplateInfoId).ToArray();
                    string friendlyName = currentMap.Contains(KeyFriendlyName)
                        ? currentMap.ReadTextString(KeyFriendlyName)
                        : "";

                    templateInfos.Add(new TemplateInfo(currentId, friendlyName));
                }

                TemplateInfos = templateInfos;
            }
        }
    }
}
