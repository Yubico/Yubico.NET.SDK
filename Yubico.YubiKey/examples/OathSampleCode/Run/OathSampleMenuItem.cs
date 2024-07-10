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

namespace Yubico.YubiKey.Sample.OathSampleCode
{
    public enum OathMainMenuItem
    {
        ListYubiKeys = 0,
        ChooseYubiKey = 1,

        GetOathCredentials = 2,
        CalculateOathCredentials = 3,
        CalculateSpecificOathCredential = 4,

        AddOathCredential = 5,
        RemoveOathCredential = 6,
        RenameOathCredential = 7,

        SetOathPassword = 8,
        UnsetOathPassword = 9,
        VerifyOathPassword = 10,

        ResetOath = 11,

        Exit = 12
    }
}
