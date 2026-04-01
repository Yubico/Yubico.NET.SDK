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
///     Identifies the different PIN types used by the OpenPGP application.
/// </summary>
public enum Pw : byte
{
    /// <summary>
    ///     The User PIN (PW1), used for signing and general operations.
    /// </summary>
    User = 0x81,

    /// <summary>
    ///     The Reset Code (PW1 with extended verification), used to reset the User PIN.
    /// </summary>
    Reset = 0x82,

    /// <summary>
    ///     The Admin PIN (PW3), used for administrative operations.
    /// </summary>
    Admin = 0x83,
}