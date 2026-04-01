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
///     Controls how often PIN verification is required for signature operations.
/// </summary>
public enum PinPolicy : byte
{
    /// <summary>
    ///     PIN must be verified before each signature operation.
    /// </summary>
    Always = 0x00,

    /// <summary>
    ///     PIN is verified once and remains valid until the session ends.
    /// </summary>
    Once = 0x01,
}
