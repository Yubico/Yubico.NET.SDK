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
///     Checks configured serial numbers for integration-test fixture authorization.
/// </summary>
/// <remarks>
///     <para>
///         Construction exits with Environment.Exit(-1) only when the configured serial list is empty
///         and AllowUnknownSerials is false. IsDeviceAllowed returns a Boolean; it does not exit for
///         a missing or disallowed serial.
///     </para>
///     <para>
///         The fixture harness filters rejected devices and failed identification reads. An empty
///         fixture list causes SkipException at lazy row binding. Discovery and live identification
///         reads can occur before per-device authorization, so connect only dedicated test hardware.
///     </para>
/// </remarks>
public sealed class AllowList
{
    private readonly HashSet<int> _allowedSerials;

    /// <summary>
    ///     Initializes a new instance of <see cref="AllowList" /> with the specified provider.
    /// </summary>
    /// <param name="provider">The provider that supplies allowed serial numbers.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when provider is null.</exception>
    /// <remarks>
    ///     Exits with Environment.Exit(-1) when the configured serial list is empty and
    ///     <see cref="AllowUnknownSerials"/> is false. Individual serial checks do not exit the process.
    /// </remarks>
    public AllowList(IAllowListProvider provider, ILogger<AllowList>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var serialList = provider.GetList();
        _allowedSerials = [.. serialList];
        AllowUnknownSerials = provider.AllowUnknownSerials;

        if (_allowedSerials.Count == 0 && !AllowUnknownSerials)
        {
            var errorMessage = provider.OnInvalidInputErrorMessage();
            logger?.LogCritical("{ErrorMessage}", errorMessage);
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(-1); // Hard fail - cannot continue without allow list
        }

        logger?.LogInformation("AllowList initialized with {Count} allowed serial numbers", _allowedSerials.Count);
        if (AllowUnknownSerials)
            logger?.LogWarning("AllowUnknownSerials is enabled — devices without serial numbers will be authorized");
    }

    /// <summary>
    ///     Gets whether devices without serial numbers are allowed for testing.
    /// </summary>
    /// <remarks>
    ///     This should only be <see langword="true"/> when testing YubiKey Security Key series
    ///     devices (FIDO-only editions that report no serial number). It authorizes ANY device
    ///     with an unreadable serial, which risks running destructive tests against a production
    ///     key. See <see cref="IAllowListProvider.AllowUnknownSerials"/>.
    /// </remarks>
    public bool AllowUnknownSerials { get; }

    /// <summary>
    ///     Determines whether the specified serial number is in the allow list.
    /// </summary>
    /// <param name="serialNumber">The serial number to check.</param>
    /// <returns>True if the serial number is allowed; otherwise, false.</returns>
    public bool IsDeviceAllowed(int? serialNumber) =>
        serialNumber is not null && _allowedSerials.Contains(serialNumber.Value);
}