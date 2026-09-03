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

namespace Yubico.YubiKit.Core.Abstractions;

/// <summary>
///     The tri-state answer to "do these two <see cref="IYubiKey" /> references describe the same
///     physical key?" as returned by <see cref="IYubiKey.SameDeviceAs" />.
/// </summary>
/// <remarks>
///     The answer is honest rather than convenient: physical identity can only be proven by the
///     hardware serial number, and whole device classes (for example the Security Key series) never
///     report one. When either side's serial is unknown, the answer is <see cref="Unknown" /> —
///     never a guess based on transport paths, product IDs, or timing.
/// </remarks>
public enum DeviceCorrelation
{
    /// <summary>
    ///     At least one side's serial number is unknown, so physical identity cannot be determined.
    ///     Treat this as "cannot correlate", not as "different".
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     The references describe the same physical key: they are the same object, or both serials
    ///     are known and equal.
    /// </summary>
    Same,

    /// <summary>Both serials are known and unequal: provably different physical keys.</summary>
    Different
}