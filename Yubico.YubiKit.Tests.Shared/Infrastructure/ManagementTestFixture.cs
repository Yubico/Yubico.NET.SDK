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

using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Test fixture for Management application integration tests.
///     Provides automatic device acquisition, allow list verification, and state initialization.
/// </summary>
/// <remarks>
///     <para>
///     This fixture extends <see cref="YubiKeyTestBase"/> to provide Management-specific
///     test state (<see cref="ManagementTestState"/>). Tests that inherit from this fixture
///     automatically have access to:
///     - A verified YubiKey device (checked against allow list)
///     - Device information (firmware version, capabilities, form factor)
///     - Management session helper methods
///     </para>
///     <para>
///     Usage Example:
///     <code>
///     public class ManagementIntegrationTests : ManagementTestFixture
///     {
///         [SkippableFact]
///         public async Task TestGetDeviceInfo()
///         {
///             RequireFirmware(4, 1, 0); // Management requires 4.1.0+
///
///             await State.WithManagementAsync(async (mgmt, state) =>
///             {
///                 var deviceInfo = await mgmt.GetDeviceInfoAsync();
///                 Assert.Equal(state.DeviceInfo.SerialNumber, deviceInfo.SerialNumber);
///             });
///         }
///     }
///     </code>
///     </para>
/// </remarks>
public abstract class ManagementTestFixture : YubiKeyTestBase
{
    /// <summary>
    ///     Gets the Management test state, providing access to device information
    ///     and management session helpers.
    /// </summary>
    protected ManagementTestState State { get; private set; } = null!;

    /// <summary>
    ///     Gets or sets the SCP key parameters for establishing secure channels.
    ///     Override this property in derived classes to enable SCP for all tests.
    /// </summary>
    /// <example>
    ///     <code>
    ///     public class ManagementScp11Tests : ManagementTestFixture
    ///     {
    ///         protected override ScpKeyParams? ScpKeyParams => GetScp11bKeyParams();
    ///     }
    ///     </code>
    /// </example>
    protected virtual ScpKeyParams? ScpKeyParams => null;

    /// <summary>
    ///     Initializes the test fixture by acquiring a device and creating Management test state.
    /// </summary>
    /// <remarks>
    ///     This method:
    ///     1. Calls base.InitializeAsync() to acquire and verify device
    ///     2. Creates ManagementTestState with the verified device
    ///     3. Makes state available via the State property
    /// </remarks>
    public override async Task InitializeAsync()
    {
        // Base class acquires device and verifies allow list
        await base.InitializeAsync().ConfigureAwait(false);

        // Create Management-specific state
        State = await ManagementTestState.CreateAsync(
            Device,
            ScpKeyParams,
            reconnectCallback: AcquireDeviceAsync).ConfigureAwait(false);
    }
}
