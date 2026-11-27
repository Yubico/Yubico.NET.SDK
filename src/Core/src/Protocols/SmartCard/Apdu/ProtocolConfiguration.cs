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

/// <summary>Supplies APDU formatting and response-follow-up settings before SCP establishment.</summary>
public readonly record struct ProtocolConfiguration
{
    /// <summary>Gets whether configured production firmware should use short APDU command chaining.</summary>
    /// <remarks>
    ///     Applied for production firmware 4.0 and later. Earlier firmware retains the existing format;
    ///     major-zero development firmware follows a separate permissive configuration path.
    /// </remarks>
    public bool ForceShortApdus { get; init; }

    /// <summary>Gets the instruction for 0x61 response follow-ups; null leaves the current instruction unchanged.</summary>
    /// <value>The override instruction, or null. A default protocol starts with 0xC0.</value>
    /// <remarks>Follow-ups use CLA, P1, P2, and API Le zero rather than copying the original header or SW2.</remarks>
    public byte? InsSendRemaining { get; init; }
}