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

using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Base class for YubiKey integration tests that provides:
///     - Automatic device acquisition and release
///     - Allow list verification (prevents tests on production keys)
///     - Device requirements checking (firmware, form factor, transport, capabilities)
///     - Access to device information
/// </summary>
/// <remarks>
///     <para>
///         This class implements <see cref="IAsyncLifetime" /> to ensure proper setup and teardown:
///         - <see cref="InitializeAsync" />: Acquires device, verifies allow list, gets device info
///         - <see cref="DisposeAsync" />: Releases device resources
///     </para>
///     <para>
///         CRITICAL SAFETY: All tests MUST pass allow list verification before running.
///         If device is not in allow list, the test infrastructure will hard fail (Environment.Exit(-1)).
///     </para>
/// </remarks>
public abstract class YubiKeyTestBase : IAsyncLifetime
{
    private static readonly AllowList s_allowList = new(
        new AppSettingsAllowListProvider(),
        NullLogger<AllowList>.Instance);

    /// <summary>
    ///     Gets the YubiKey device under test.
    ///     Available after <see cref="InitializeAsync" /> completes.
    /// </summary>
    protected IYubiKey Device { get; private set; } = null!;

    /// <summary>
    ///     Gets device information (firmware version, form factor, capabilities, etc.).
    ///     Available after <see cref="InitializeAsync" /> completes.
    /// </summary>
    protected DeviceInfo DeviceInfo { get; private set; }

    /// <summary>
    ///     Gets the firmware version of the device under test.
    ///     Convenience property for <c>DeviceInfo.FirmwareVersion</c>.
    /// </summary>
    protected FirmwareVersion FirmwareVersion => DeviceInfo.FirmwareVersion;

    /// <summary>
    ///     Gets the form factor of the device under test.
    ///     Convenience property for <c>DeviceInfo.FormFactor</c>.
    /// </summary>
    protected FormFactor FormFactor => DeviceInfo.FormFactor;

    #region Helper Methods

    /// <summary>
    ///     Gets a human-readable string of supported transports.
    /// </summary>
    private string GetSupportedTransports()
    {
        var transports = new List<string>();

        if (DeviceInfo.UsbSupported != DeviceCapabilities.None)
            transports.Add("USB");

        if (DeviceInfo.NfcSupported != DeviceCapabilities.None)
            transports.Add("NFC");

        return transports.Count > 0 ? string.Join(", ", transports) : "None";
    }

    #endregion

    #region IAsyncLifetime Implementation

    /// <summary>
    ///     Initializes the test fixture by acquiring a YubiKey device and verifying it against the allow list.
    /// </summary>
    /// <remarks>
    ///     This method:
    ///     1. Finds an available YubiKey device
    ///     2. Verifies device serial number against allow list (HARD FAIL if not allowed)
    ///     3. Retrieves device information (firmware, form factor, capabilities)
    /// </remarks>
    public virtual async Task InitializeAsync()
    {
        // Acquire device
        Device = await AcquireDeviceAsync().ConfigureAwait(false);

        // CRITICAL: Verify device is in allow list (hard fail if not)
        await s_allowList.VerifyAsync(Device).ConfigureAwait(false);

        // Get device information for requirements checking
        DeviceInfo = await GetDeviceInfoAsync(Device).ConfigureAwait(false);
    }

    /// <summary>
    ///     Disposes test resources and releases the YubiKey device.
    /// </summary>
    public virtual Task DisposeAsync() =>
        // Future: Release device connection if we're pooling devices
        Task.CompletedTask;

    #endregion

    #region Device Acquisition

    /// <summary>
    ///     Acquires a YubiKey device for testing.
    /// </summary>
    /// <returns>The first available YubiKey device.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no YubiKey is found.</exception>
    protected virtual async Task<IYubiKey> AcquireDeviceAsync()
    {
        var devices = await YubiKey.FindAllAsync().ConfigureAwait(false);

        if (devices.Count == 0)
            throw new InvalidOperationException(
                "No YubiKey devices found. Please connect a test YubiKey to run integration tests.");

        // Return first device (in future, could support device selection)
        return devices[0];
    }

    /// <summary>
    ///     Gets device information from a YubiKey.
    /// </summary>
    protected virtual async Task<DeviceInfo> GetDeviceInfoAsync(IYubiKey device)
    {
        using var connection = await device.ConnectAsync<ISmartCardConnection>().ConfigureAwait(false);
        using var mgmt = await ManagementSession<ISmartCardConnection>.CreateAsync(connection).ConfigureAwait(false);
        return await mgmt.GetDeviceInfoAsync().ConfigureAwait(false);
    }

    #endregion

    #region Test Requirements (Phase 1.4)

    /// <summary>
    ///     Requires the device to have a minimum firmware version.
    ///     Skips the test if the requirement is not met.
    /// </summary>
    /// <param name="major">Major version number.</param>
    /// <param name="minor">Minor version number.</param>
    /// <param name="patch">Patch version number.</param>
    /// <example>
    ///     <code>
    ///     [Fact]
    ///     public void TestScp11()
    ///     {
    ///         RequireFirmware(5, 7, 2); // SCP11 requires 5.7.2+
    ///         // Test code...
    ///     }
    ///     </code>
    /// </example>
    protected void RequireFirmware(int major, int minor, int patch) =>
        Skip.If(
            !FirmwareVersion.IsAtLeast(major, minor, patch),
            $"Test requires firmware {major}.{minor}.{patch} or newer. Device has {FirmwareVersion}");

    /// <summary>
    ///     Requires the device to have a specific form factor.
    ///     Skips the test if the requirement is not met.
    /// </summary>
    /// <param name="formFactor">Required form factor (e.g., Bio, UsbAKeychain, UsbCKeychain).</param>
    /// <example>
    ///     <code>
    ///     [Fact]
    ///     public void TestBiometricEnrollment()
    ///     {
    ///         RequireFormFactor(FormFactor.UsbABio); // Requires Bio key
    ///         // Test biometric features...
    ///     }
    ///     </code>
    /// </example>
    protected void RequireFormFactor(FormFactor formFactor) =>
        Skip.If(
            FormFactor != formFactor,
            $"Test requires {formFactor} form factor. Device is {FormFactor}");

    /// <summary>
    ///     Requires the device to support a specific transport.
    ///     Skips the test if the requirement is not met.
    /// </summary>
    /// <param name="transport">Required transport (Usb, Nfc).</param>
    /// <example>
    ///     <code>
    ///     [Fact]
    ///     public void TestNfcTransaction()
    ///     {
    ///         RequireTransport(Transport.Nfc);
    ///         // Test NFC functionality...
    ///     }
    ///     </code>
    /// </example>
    protected void RequireTransport(Transport transport)
    {
        var hasTransport = transport switch
        {
            Transport.Usb => DeviceInfo.UsbSupported != DeviceCapabilities.None,
            Transport.Nfc => DeviceInfo.NfcSupported != DeviceCapabilities.None,
            _ => false
        };

        Skip.If(
            !hasTransport,
            $"Test requires {transport} transport. Device supports: " +
            $"{GetSupportedTransports()}");
    }

    /// <summary>
    ///     Requires the device to have a specific capability enabled.
    ///     Skips the test if the requirement is not met.
    /// </summary>
    /// <param name="capability">Required capability (e.g., Piv, Oath, Fido2).</param>
    /// <example>
    ///     <code>
    ///     [Fact]
    ///     public void TestPivOperations()
    ///     {
    ///         RequireCapability(DeviceCapabilities.Piv);
    ///         // Test PIV functionality...
    ///     }
    ///     </code>
    /// </example>
    protected void RequireCapability(DeviceCapabilities capability)
    {
        var hasCapability = (DeviceInfo.UsbEnabled & capability) != 0 ||
                            (DeviceInfo.NfcEnabled & capability) != 0;

        Skip.If(
            !hasCapability,
            $"Test requires {capability} capability to be enabled. " +
            $"Device has USB: {DeviceInfo.UsbEnabled}, NFC: {DeviceInfo.NfcEnabled}");
    }

    /// <summary>
    ///     Requires the device to be FIPS capable for a specific capability.
    ///     Skips the test if the requirement is not met.
    /// </summary>
    /// <param name="capability">The capability that must be FIPS capable.</param>
    /// <example>
    ///     <code>
    ///     [Fact]
    ///     public void TestFipsPiv()
    ///     {
    ///         RequireFipsCapable(DeviceCapabilities.Piv);
    ///         // Test FIPS PIV features...
    ///     }
    ///     </code>
    /// </example>
    protected void RequireFipsCapable(DeviceCapabilities capability)
    {
        var isFipsCapable = (DeviceInfo.FipsCapabilities & capability) != 0;

        Skip.If(
            !isFipsCapable,
            $"Test requires FIPS-capable {capability}. " +
            $"Device FIPS capabilities: {DeviceInfo.FipsCapabilities}");
    }

    /// <summary>
    ///     Requires the device to be in FIPS approved mode for a specific capability.
    ///     Skips the test if the requirement is not met.
    /// </summary>
    /// <param name="capability">The capability that must be FIPS approved.</param>
    /// <example>
    ///     <code>
    ///     [Fact]
    ///     public void TestFipsApprovedPiv()
    ///     {
    ///         RequireFipsApproved(DeviceCapabilities.Piv);
    ///         // Test FIPS-approved PIV operations...
    ///     }
    ///     </code>
    /// </example>
    protected void RequireFipsApproved(DeviceCapabilities capability)
    {
        var isFipsApproved = (DeviceInfo.FipsApproved & capability) != 0;

        Skip.If(
            !isFipsApproved,
            $"Test requires {capability} to be in FIPS approved mode. " +
            $"Device FIPS approved: {DeviceInfo.FipsApproved}");
    }

    /// <summary>
    ///     Combination helper for requiring multiple device attributes at once.
    /// </summary>
    /// <param name="minFirmware">Minimum firmware version (optional).</param>
    /// <param name="formFactor">Required form factor (optional).</param>
    /// <param name="transport">Required transport (optional).</param>
    /// <param name="capability">Required capability (optional).</param>
    /// <example>
    ///     <code>
    ///     [Fact]
    ///     public void TestScp11Bio()
    ///     {
    ///         RequireDevice(
    ///             minFirmware: new FirmwareVersion(5, 7, 2),
    ///             formFactor: FormFactor.UsbABio,
    ///             transport: Transport.Usb);
    ///         // Test code...
    ///     }
    ///     </code>
    /// </example>
    protected void RequireDevice(
        FirmwareVersion? minFirmware = null,
        FormFactor? formFactor = null,
        Transport? transport = null,
        DeviceCapabilities? capability = null)
    {
        if (minFirmware is not null)
            RequireFirmware(minFirmware.Major, minFirmware.Minor, minFirmware.Patch);

        if (formFactor is not null)
            RequireFormFactor(formFactor.Value);

        if (transport is not null)
            RequireTransport(transport.Value);

        if (capability is not null)
            RequireCapability(capability.Value);
    }

    #endregion
}