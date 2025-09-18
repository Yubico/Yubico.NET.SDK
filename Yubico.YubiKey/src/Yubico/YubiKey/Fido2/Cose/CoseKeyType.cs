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

namespace Yubico.YubiKey.Fido2.Cose;

/// <summary>
///     An enumeration of the key families supported by COSE.
///     <remarks>
///         This enumeration is based on the IANA COSE Key Common Parameters registry.
///         <para>
///             https://www.iana.org/assignments/cose/cose.xhtml#key-type
///         </para>
///     </remarks>
/// </summary>
public enum CoseKeyType
{
    /// <summary>
    ///     The type could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     Octet Key Pair
    /// </summary>
    Okp = 1,

    /// <summary>
    ///     Elliptic Curve keys with x- and y-coordinates
    /// </summary>
    Ec2 = 2,

    /// <summary>
    ///     Symmetric keys
    /// </summary>
    Symmetric = 4
}
