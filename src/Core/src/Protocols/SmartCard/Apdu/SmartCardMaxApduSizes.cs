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

namespace Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

public static class SmartCardMaxApduSizes
{
    /// <summary>
    ///     The SDK's NEO complete-command ceiling in bytes, including framing rather than just data.
    /// </summary>
    public const int Neo = 1390;

    /// <summary>
    ///     The SDK's YubiKey 4 complete-command ceiling in bytes, including framing.
    /// </summary>
    public const int Yk4 = 2038;

    /// <summary>
    ///     The SDK's YubiKey 4.3-and-later complete-command ceiling in bytes, including framing.
    /// </summary>
    public const int Yk43 = 3062;

    /// <summary>Maximum command-data bytes per short APDU fragment, distinct from a complete-command ceiling.</summary>
    public const int ShortApduMaxChunkSize = 255;
}