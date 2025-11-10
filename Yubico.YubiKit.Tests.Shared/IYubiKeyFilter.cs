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

namespace Yubico.YubiKit.Tests.Shared;

/// <summary>
///     Interface for custom YubiKey device filtering logic.
///     Implement this interface to create custom filters for YubiKeyTheoryAttribute.
/// </summary>
/// <remarks>
///     <para>
///         Custom filters allow test authors to specify complex filtering logic
///         that cannot be expressed through the built-in attribute properties.
///     </para>
///     <para>
///         Example usage:
///         <code>
///     public class OnlyProductionKeysFilter : IYubiKeyFilter
///     {
///         public bool Matches(YubiKeyTestState device)
///         {
///             // Only match devices with serial numbers above 10000000
///             return device.SerialNumber > 10_000_000;
///         }
///
///         public string GetDescription()
///         {
///             return "Production keys only (SN > 10000000)";
///         }
///     }
///
///     [YubiKeyTheory(CustomFilter = typeof(OnlyProductionKeysFilter))]
///     public async Task TestOnProductionKeys(YubiKeyTestDevice device)
///     {
///         // Test runs only on production keys
///     }
///         </code>
///     </para>
/// </remarks>
public interface IYubiKeyFilter
{
    /// <summary>
    ///     Determines whether the specified device matches the filter criteria.
    /// </summary>
    /// <param name="device">The device to test against the filter.</param>
    /// <returns>true if the device matches the filter; otherwise, false.</returns>
    bool Matches(YubiKeyTestState device);

    /// <summary>
    ///     Gets a human-readable description of the filter for diagnostic output.
    /// </summary>
    /// <returns>A description of the filter criteria.</returns>
    string GetDescription();
}
