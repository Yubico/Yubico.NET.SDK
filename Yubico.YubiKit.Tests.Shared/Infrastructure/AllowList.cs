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

using Microsoft.Extensions.Logging;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Implements allow list verification to prevent integration tests from running on production YubiKeys.
///     This is a CRITICAL safety mechanism that validates device serial numbers before any test operations.
/// </summary>
/// <remarks>
///     <para>
///         The AllowList performs a hard fail (Environment.Exit(-1)) if:
///         - No allowed serial numbers are configured
///         - A device's serial number is not in the allow list
///     </para>
///     <para>
///         This aggressive approach prevents accidental test execution on production keys, which could
///         lead to data loss or device misconfiguration.
///     </para>
/// </remarks>
public class AllowList
{
    private readonly IReadOnlyList<int> _allowedSerials;

    /// <summary>
    ///     Initializes a new instance of <see cref="AllowList" /> with the specified provider.
    /// </summary>
    /// <param name="provider">The provider that supplies allowed serial numbers.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when provider is null.</exception>
    /// <remarks>
    ///     Constructor performs hard fail (Environment.Exit(-1)) if allow list is empty.
    /// </remarks>
    public AllowList(IAllowListProvider provider, ILogger<AllowList>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _allowedSerials = provider.GetList();

        if (_allowedSerials.Count == 0)
        {
            var errorMessage = provider.OnInvalidInputErrorMessage();
            logger?.LogCritical("{ErrorMessage}", errorMessage);
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(-1); // Hard fail - cannot continue without allow list
        }

        logger?.LogInformation("AllowList initialized with {Count} allowed serial numbers", _allowedSerials.Count);
    }

    /// <summary>
    ///     Determines whether the specified serial number is in the allow list.
    /// </summary>
    /// <param name="serialNumber">The serial number to check.</param>
    /// <returns>True if the serial number is allowed; otherwise, false.</returns>
    public bool IsDeviceAllowed(int serialNumber) => _allowedSerials.Contains(serialNumber);
}