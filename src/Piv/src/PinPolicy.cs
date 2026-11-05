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

namespace Yubico.YubiKit.Piv;

/// <summary>
/// PIN policy for PIV private key usage.
/// </summary>
public enum PinPolicy : byte
{
    /// <summary>Use the slot's device default: normally Always for Signature, Never for Card Authentication and Once otherwise.</summary>
    Default = 0x00,

    /// <summary>PIN verification is never required.</summary>
    Never = 0x01,

    /// <summary>The card reuses successful PIN verification for subsequent private-key operations while its verification state remains valid.</summary>
    Once = 0x02,

    /// <summary>PIN verification required for every operation.</summary>
    Always = 0x03,

    /// <summary>The card reuses successful fingerprint verification while its verification state remains valid; requires a capable, configured Bio device.</summary>
    MatchOnce = 0x04,

    /// <summary>Fingerprint verification is required for every private-key operation; requires a capable, configured Bio device.</summary>
    MatchAlways = 0x05
}