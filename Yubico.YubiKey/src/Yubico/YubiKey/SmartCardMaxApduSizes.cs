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

namespace Yubico.YubiKey;

#pragma warning disable CA1707 // Allow underscore in constant
/// <summary>
///     This contains the maximum size (in bytes) of APDU commands for the various YubiKey models.
/// </summary>
public static class SmartCardMaxApduSizes
{
    /// <summary>
    ///     The max APDU command size for the YubiKey NEO
    /// </summary>
    public const int NEO = 1390;

    /// <summary>
    ///     The max APDU command size for the YubiKey 4 and greater
    /// </summary>
    public const int YK4 = 2038;

    /// <summary>
    ///     The max APDU command size for the YubiKey 4.3 and greater
    /// </summary>
    public const int YK4_3 = 3062;
}
#pragma warning restore CA1707
