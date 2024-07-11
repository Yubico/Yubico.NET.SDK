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

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    public enum PivMainMenuItem
    {
        ListYubiKeys = 0,
        ChooseYubiKey = 1,
        ChangePivPinAndPukRetryCount = 2,
        ChangePivPin = 3,
        ChangePivPuk = 4,
        ChangePivManagementKey = 5,
        ResetPivPinWithPuk = 6,

        GetPinOnlyMode = 7,
        SetPinOnlyMode = 8,
        SetPinOnlyNoKeyCollector = 9,
        RecoverPinOnlyMode = 10,

        GenerateKeyPair = 11,
        ImportPrivateKey = 12,
        ImportCertificate = 13,

        Sign = 14,
        Decrypt = 15,
        KeyAgree = 16,

        GetCertRequest = 17,
        BuildSelfSignedCert = 18,
        BuildCert = 19,
        RetrieveCert = 20,

        CreateAttestationStatement = 21,
        GetAttestationCertificate = 22,

        ResetPiv = 23,
        Exit = 24,
    }
}
