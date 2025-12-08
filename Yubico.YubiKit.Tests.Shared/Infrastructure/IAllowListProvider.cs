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

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Interface for providers that supply allowed serial numbers for integration testing.
///     This is a critical safety mechanism to prevent tests from running on production YubiKeys.
/// </summary>
public interface IAllowListProvider
{
    /// <summary>
    ///     Gets the list of allowed YubiKey serial numbers for integration testing.
    /// </summary>
    /// <returns>List of allowed serial numbers. Returns empty list if not configured.</returns>
    IReadOnlyList<int> GetList();

    /// <summary>
    ///     Gets the error message to display when the allow list is not configured or is empty.
    /// </summary>
    /// <returns>Error message with instructions for configuring the allow list.</returns>
    string OnInvalidInputErrorMessage();

    /// <summary>
    ///     Gets the error message to display when a device's serial number is not in the allow list.
    /// </summary>
    /// <param name="serialNumber">The serial number of the device that was rejected.</param>
    /// <returns>Error message explaining why the device was rejected.</returns>
    string OnNotAllowedErrorMessage(int serialNumber);
}