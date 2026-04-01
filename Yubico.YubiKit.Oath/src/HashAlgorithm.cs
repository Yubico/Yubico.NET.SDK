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
///     The hash algorithm used for OATH credential calculation.
/// </summary>
public enum OathHashAlgorithm : byte
{
    /// <summary>
    ///     SHA-1 (160-bit digest, 64-byte block size).
    /// </summary>
    Sha1 = 0x01,

    /// <summary>
    ///     SHA-256 (256-bit digest, 64-byte block size).
    /// </summary>
    Sha256 = 0x02,

    /// <summary>
    ///     SHA-512 (512-bit digest, 128-byte block size).
    /// </summary>
    Sha512 = 0x03
}