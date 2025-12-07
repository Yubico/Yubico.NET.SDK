// Copyright (C) 2024 Yubico.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
///     Constants for SCP (Secure Channel Protocol) Key Identifier values.
/// </summary>
public static class ScpKid
{
    /// <summary>
    ///     Key ID for SCP03 protocol.
    /// </summary>
    public const byte SCP03 = 0x1;

    /// <summary>
    ///     Key ID for SCP11a protocol (mutual authentication with certificate chain).
    /// </summary>
    public const byte SCP11a = 0x11;

    /// <summary>
    ///     Key ID for SCP11b protocol (authentication without off-card entity verification).
    /// </summary>
    public const byte SCP11b = 0x13;

    /// <summary>
    ///     Key ID for SCP11c protocol (mutual authentication with certificate chain, variant C).
    /// </summary>
    public const byte SCP11c = 0x15;
}