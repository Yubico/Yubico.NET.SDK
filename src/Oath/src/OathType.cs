// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.Oath;

/// <summary>
///     The type of OATH credential.
/// </summary>
public enum OathType : byte
{
    /// <summary>
    ///     HMAC-based One-Time Password (RFC 4226).
    /// </summary>
    Hotp = 0x10,

    /// <summary>
    ///     Time-based One-Time Password (RFC 6238).
    /// </summary>
    Totp = 0x20
}