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
///     OpenPGP instruction bytes for APDU commands.
/// </summary>
internal enum Ins : byte
{
    Verify = 0x20,
    ChangePin = 0x24,
    ResetRetryCounter = 0x2C,
    Pso = 0x2A,
    Activate = 0x44,
    GenerateAsym = 0x47,
    GetChallenge = 0x84,
    InternalAuthenticate = 0x88,
    SelectData = 0xA5,
    GetData = 0xCA,
    PutData = 0xDA,
    PutDataOdd = 0xDB,
    Terminate = 0xE6,
    GetVersion = 0xF1,
    SetPinRetries = 0xF2,
    GetAttestation = 0xFB,
}
