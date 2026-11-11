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
///     Flags indicating the extended capabilities of the OpenPGP applet.
/// </summary>
[Flags]
public enum ExtendedCapabilityFlags : byte
{
    None = 0,
    Kdf = 1 << 0,
    PsoDecEncAes = 1 << 1,
    AlgorithmAttributesChangeable = 1 << 2,
    PrivateUse = 1 << 3,
    PwStatusChangeable = 1 << 4,
    KeyImport = 1 << 5,
    GetChallenge = 1 << 6,
    SecureMessaging = 1 << 7,
}