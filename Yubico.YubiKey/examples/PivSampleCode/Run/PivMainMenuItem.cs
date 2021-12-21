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

        PinProtectMgmtKey = 7,
        PinDeriveMgmtKey = 8,

        GenerateKeyPair = 9,
        ImportPrivateKey = 10,
        ImportCertificate = 11,

        Sign = 12,
        Decrypt = 13,
        KeyAgree = 14,

        GetCertRequest = 15,
        BuildSelfSignedCert = 16,
        BuildCert = 17,
        RetrieveCert = 18,

        CreateAttestationStatement = 19,
        GetAttestationCertificate = 20,

        ResetPiv = 21,
        Exit = 22,
    }
}
