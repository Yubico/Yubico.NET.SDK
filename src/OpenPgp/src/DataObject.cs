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

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     OpenPGP Data Object tags used in GET DATA and PUT DATA commands.
/// </summary>
public enum DataObject
{
    PrivateUse1 = 0x0101,
    PrivateUse2 = 0x0102,
    PrivateUse3 = 0x0103,
    PrivateUse4 = 0x0104,
    Aid = 0x4F,
    Name = 0x5B,
    LoginData = 0x5E,
    CardholderRelatedData = 0x65,
    ApplicationRelatedData = 0x6E,
    SecuritySupportTemplate = 0x7A,
    CardholderCertificate = 0x7F21,
    ExtendedLengthInfo = 0x7F66,
    GeneralFeatureManagement = 0x7F74,
    AlgorithmAttributesSig = 0xC1,
    AlgorithmAttributesDec = 0xC2,
    AlgorithmAttributesAut = 0xC3,
    PwStatusBytes = 0xC4,
    FingerprintSig = 0xC7,
    FingerprintDec = 0xC8,
    FingerprintAut = 0xC9,
    CaFingerprint1 = 0xCA,
    CaFingerprint2 = 0xCB,
    CaFingerprint3 = 0xCC,
    GenerationTimeSig = 0xCE,
    GenerationTimeDec = 0xCF,
    GenerationTimeAut = 0xD0,
    ResettingCode = 0xD3,
    UifSig = 0xD6,
    UifDec = 0xD7,
    UifAut = 0xD8,
    UifAtt = 0xD9,
    AlgorithmAttributesAtt = 0xDA,
    FingerprintAtt = 0xDB,
    CaFingerprint4 = 0xDC,
    GenerationTimeAtt = 0xDD,
    Kdf = 0xF9,
    AlgorithmInformation = 0xFA,
    AttCertificate = 0xFC,

    // Composite tags used within DiscretionaryDataObjects (not directly addressable via GET DATA)
    DiscretionaryDataObjects = 0x73,
    ExtendedCapabilitiesTag = 0xC0,
    Fingerprints = 0xC5,
    CaFingerprints = 0xC6,
    GenerationTimes = 0xCD,
    KeyInformation = 0xDE,
    SignatureCounter = 0x93,

    /// <summary>
    ///     Language preference, encoded as two bytes within the cardholder data.
    /// </summary>
    Language = 0x5F2D,

    /// <summary>
    ///     Sex of cardholder per ISO 5218.
    /// </summary>
    Sex = 0x5F35,

    /// <summary>
    ///     URL for public key retrieval.
    /// </summary>
    Url = 0x5F50,

    /// <summary>
    ///     Historical bytes from ATR.
    /// </summary>
    HistoricalBytes = 0x5F52,
}