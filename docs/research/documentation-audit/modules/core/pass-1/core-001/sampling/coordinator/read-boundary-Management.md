PROPOSED SAMPLE — no verdict: core-001:unit:a2cf98dcc1805fbf / Management / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/examples/ManagementTool/Cli/Prompts/DeviceSelector.cs:20–23
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Formats form factor; current Core product-flag retention can produce unnamed enum values.</summary>
    public static string FormatFormFactor(FormFactor formFactor) => FormFactorFormatter.Format(formFactor);


PROPOSED SAMPLE — no verdict: core-001:unit:a815cc370e465f4c / Management / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/examples/ManagementTool/Features/DeviceConfiguration.cs:36–88
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>
    /// Sets the enabled capabilities for a transport.
    /// </summary>
    /// <param name="session">A Management session.</param>
    /// <param name="transport">The transport (USB or NFC).</param>
    /// <param name="capabilities">Complete replacement enabled set; omitted bits are disabled.
    /// Preserve a supported USB application and a recovery route; the SDK only rejects exact USB None.</param>
    /// <param name="lockCode">The current lock code if device is locked.</param>
    /// <param name="reboot">Whether to reboot the device after configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <example>
    /// <code>
    /// var result = await DeviceConfiguration.SetCapabilitiesAsync(
    ///     session,
    ///     Transport.Usb,
    ///     DeviceCapabilities.Piv | DeviceCapabilities.Fido2,
    ///     lockCode: null,
    ///     reboot: true,
    ///     ct);
    /// </code>
    /// </example>
    public static async Task<ConfigResult> SetCapabilitiesAsync(
        IManagementSession session,
        Transport transport,
        DeviceCapabilities capabilities,
        byte[]? lockCode = null,
        bool reboot = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var config = transport switch
            {
                Transport.Usb => new DeviceConfig { UsbEnabled = capabilities },
                Transport.Nfc => new DeviceConfig { NfcEnabled = capabilities },
                _ => throw new ArgumentOutOfRangeException(nameof(transport), transport, null)
            };

            await session.SetDeviceConfigAsync(
                config,
                new SetDeviceConfigOptions { Reboot = reboot, CurrentLockCode = lockCode },
                cancellationToken);
            return ConfigResult.Succeeded(reboot);
        }
        catch (Exception ex)
        {
            return ConfigResult.Failed($"Failed to set capabilities: {ex.Message}");
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6dea5c78a8314291 / Management / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/src/Backend/OtpBackend.cs:27–35
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static async ValueTask<FirmwareVersion?> ReadStatusVersionAsync(
        IOtpHidProtocol protocol,
        CancellationToken cancellationToken)
    {
        var status = await protocol.ReadStatusAsync(cancellationToken).ConfigureAwait(false);
        return protocol.FirmwareVersion ?? new FirmwareVersion(status.Span[0], status.Span[1], status.Span[2]);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:417fbf395249474a / Management / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/src/Backend/SmartCardBackend.cs:56–72
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public async ValueTask SetModeAsync(byte[] data, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);

        var apdu = new ApduCommand
        {
            Cla = 0,
            Ins = InsSetMode,
            P1 = 0,
            P2 = 0,
            Data = data
        };

        await _protocol.TransmitAndReceiveAsync(apdu, cancellationToken: cancellationToken).ConfigureAwait(false);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6cb194bebfaebec7 / Management / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/src/Yubico.YubiKit.Management.csproj:1–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- TargetFrameworks inherited from Directory.Build.props -->
    <RootNamespace>Yubico.YubiKit.Management</RootNamespace>
    <AssemblyName>Yubico.YubiKit.Management</AssemblyName>

    <!-- Package Metadata -->
    <Description>Management interface for YubiKey devices including device info queries, firmware version detection, and capability management.</Description>
    <PackageTags>yubikey;yubico;management;device-info;configuration</PackageTags>

    <!-- Opts this published library into the shared Native AOT analyzer and metadata properties. -->
    <IsYubiKitSdkLibrary>true</IsYubiKitSdkLibrary>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Core\src\Yubico.YubiKit.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Yubico.YubiKit.Management.UnitTests" />
  </ItemGroup>

</Project>



PROPOSED SAMPLE — no verdict: core-001:unit:31d79bce8daf96b9 / Management / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/examples/ManagementTool/README.md:82–111
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Configuration tasks and recovery

Before writing, confirm the selected physical key and save the current settings. USB/NFC capability
selections replace the complete enabled set for that transport; unchecked applications are disabled.
Keep at least one supported USB application and a recovery route. The SDK rejects exact USB `None`
but does not comprehensively validate supported masks. Disabling the last SmartCard-dependent application
can make this tool unable to select the key even when SDK HID configuration remains possible.

Timeout input ranges (0–3600 auto-eject seconds and 0–60 challenge-response seconds) are SDK limits, not
firmware maxima. Zero disables auto-eject or requests the challenge-response default (normally 15 seconds).
Auto-eject is a CCID inactivity timer. Touch-eject works only in CCID-only mode: OTP, U2F and FIDO2 must
be disabled; assigning a timeout does not set the flag automatically.

The configuration wrappers request reboot by default for capabilities, timeouts and flags. End old
scopes after reboot, then rediscover the intended key and open a fresh session. The result property
`RebootRequired` currently carries the requested reboot flag; it is not an observed pending-reboot state
or confirmation that re-enumeration has finished. Readback of persisted values does not prove active
interfaces have already changed.

If calling the NFC-restriction helper directly, use NFC-capable hardware with firmware 5.7 or later.
Restriction leaves the configured NDEF URL readable. Documented recovery is USB power for three seconds,
including from a charger; no physical touch is needed. A false value encodes a zero-valued request but
is not a verified recovery route. The SDK does not preflight those field-specific requirements.

The tool's reset eligibility display checks firmware only. The documented combined reset applies to
**Bio Multi-protocol Edition**, not every 5.6+ key or every Bio key. It deletes FIDO credentials,
fingerprints, the shared PIN and PIV keys/certificates/user data, except the F9 attestation slot. It
cannot be undone and is not a documented way to recover a lost Management configuration lock code.
The menu's broader erasure/default-PIN wording must not be used as a product-support check.




PROPOSED SAMPLE — no verdict: core-001:unit:555e3712647dbc07 / Management / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/examples/ManagementTool/README.md:112–126
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Security Considerations

- The sample masks lock-code input and clears returned mutable byte arrays in the callers' `finally`
  blocks. Confirmation uses a fixed-time byte comparison. The input and normalized copies are immutable
  strings and cannot be erased by clearing the arrays. If the confirmation prompt throws before returning,
  the first parsed array can also escape cleanup. Do not use this prompt as a production secret-input primitive.
- Keep borrowed lock-code buffers valid and unchanged until the awaited write completes; clear owned
  secrets even when a write fails or is cancelled. Never log codes. Authorizing one write with the
  current code does not remove the lock; removal sends the current code and sixteen zero replacement bytes.
- Configuration menus ask for confirmation; reset asks twice (yes/no and typing `RESET`). The feature
  helpers themselves do not prompt, so direct callers must authorize the operation first.
- Cancellation is not consistent end to end: menu callbacks discard the console token, and feature
  wrappers/metadata lookup can turn cancellation exceptions into ordinary failure results. This is a
  sample limitation, not the SDK session's cancellation contract. Cancellation never rolls back a sent write.




PROPOSED SAMPLE — no verdict: core-001:unit:1dd329786aeabb6c / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionCapabilityTests.cs:28–80
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>
    ///     Clears the reported USB OATH bit and checks readback, then attempts restoration and readback in finally.
    /// </summary>
    /// <remarks>
    ///     This test disables OATH on USB, reads back the config, then restores. It requires a device
    ///     that has OATH currently enabled on USB. The test does NOT reboot the device because
    ///     rebooting would disrupt the test session; instead it leaves <c>SetDeviceConfigOptions.Reboot</c> false and
    ///     validates the persisted config via a fresh <see cref="ManagementSession.GetDeviceInfoAsync" /> call.
    ///     This does not verify active OATH transport behavior after re-enumeration; restoration can fail on device errors.
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.0.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task SetDeviceConfigAsync_ToggleUsbCapability_ChangesAndRestores(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            // Save original state
            var originalUsbEnabled = deviceInfo.UsbEnabled;

            // Skip if OATH is not currently enabled (nothing to toggle)
            Skip.If((originalUsbEnabled & DeviceCapabilities.Oath) == 0,
                "OATH is not enabled on USB; cannot toggle it.");

            // Also skip if OATH is the only capability (cannot disable all USB capabilities)
            var withoutOath = originalUsbEnabled & ~DeviceCapabilities.Oath;
            Skip.If(withoutOath == DeviceCapabilities.None,
                "OATH is the only USB capability; cannot disable it.");

            try
            {
                // Disable OATH on USB
                var configWithoutOath = new DeviceConfig { UsbEnabled = withoutOath };

                await mgmt.SetDeviceConfigAsync(configWithoutOath);

                // Read back and verify OATH is now disabled
                var updatedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.Equal(DeviceCapabilities.None, updatedInfo.UsbEnabled & DeviceCapabilities.Oath);
            }
            finally
            {
                // Restore original capabilities
                var restoreConfig = new DeviceConfig { UsbEnabled = originalUsbEnabled };

                await mgmt.SetDeviceConfigAsync(restoreConfig);

                // Verify restoration
                var restoredInfo = await mgmt.GetDeviceInfoAsync();
                Assert.Equal(originalUsbEnabled, restoredInfo.UsbEnabled);
            }
        });
    }


PROPOSED SAMPLE — no verdict: core-001:unit:0ef7b0ff9c5e908c / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionSimpleTests.cs:117–131
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Reads a key exposing HID OTP using the unpinned one-shot extension's default transport selection.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
    public async Task GetDeviceInfo_WithHidOtpDevice_ReturnsValidInfo(YubiKeyTestState state)
    {
        Assert.Equal(ConnectionType.HidOtp, state.ConnectionType);

        var deviceInfo = await state.Device.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
        Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:94cf1d1225852eb6 / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionSimpleTests.cs:242–265
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:




    /// <summary>
    ///     Demonstrates using AuthorizedDevices directly without [WithYubiKey] attribute.
    ///     This pattern is useful for tests that need custom device selection logic.
    /// </summary>
    [SkippableFact]
    public async Task CreateManagementSession_UsingAuthorizedDevicesDirectly_WorksWithoutAttributes()
    {
        // Get the first authorized SmartCard device directly (no attribute needed)
        var device = AuthorizedDevices
            .GetByConnectionType(ConnectionType.SmartCard)
            .FirstOrDefault();

        Skip.If(device is null, "No SmartCard device available");

        await using var mgmtSession = await device.Device.CreateManagementSessionAsync();
        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();

        Assert.NotEqual(0, deviceInfo.SerialNumber);
        Assert.True(AuthorizedDevices.IsAllowed(deviceInfo.SerialNumber));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:02ae797f211c1fb1 / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionSimpleTests.cs:265–287
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Demonstrates filtering with FilterCriteria directly.
    /// </summary>
    [SkippableFact]
    public async Task GetDeviceInfo_UsingFilterCriteria_AppliesFiltersCorrectly()
    {
        // Build custom filter criteria
        var criteria = new FilterCriteria
        {
            MinFirmware = "5.0.0",
            ConnectionType = ConnectionType.SmartCard
        };

        // Get first device matching criteria or skip
        var device = AuthorizedDevices.GetFirstOrSkip(criteria);

        var deviceInfo = await device.Device.GetDeviceInfoAsync();

        Assert.NotEqual(0, deviceInfo.SerialNumber);
        Assert.True(deviceInfo.FirmwareVersion.IsAtLeast(5, 0, 0));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c2126db982bbfcb5 / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionTests.cs:39–57
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>
    ///     Verify we can read device information.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey]
    public async Task GetDeviceInfo_ReturnsValidInformation(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            // Get device info via Management session
            var info = await mgmt.GetDeviceInfoAsync();

            // Verify we got valid device information
            Assert.True(info.SerialNumber > 0);

            // Device info should match what we cached
            Assert.Equal(deviceInfo.SerialNumber, info.SerialNumber);
            Assert.Equal(deviceInfo.FirmwareVersion, info.FirmwareVersion);
        });


PROPOSED SAMPLE — no verdict: core-001:unit:c5c41a16f0ac5681 / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionTests.cs:73–102
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Verify form factor matches expected type.
    /// </summary>
    /// <remarks>
    ///     Selection and assertions use decoded metadata. They do not validate Core's handling of
    ///     product flags in the wire form-factor byte; exact filters can miss flagged variants.
    /// </remarks>
    [WithYubiKey(FormFactor = FormFactor.UsbAKeychain)]
    [WithYubiKey(FormFactor = FormFactor.UsbCKeychain)]
    [WithYubiKey(FormFactor = FormFactor.UsbABiometricKeychain)]
    [WithYubiKey(FormFactor = FormFactor.UsbCBiometricKeychain)]
    [WithYubiKey(FormFactor = FormFactor.UsbANano)]
    [WithYubiKey(FormFactor = FormFactor.UsbCNano)]
    [WithYubiKey(FormFactor = FormFactor.UsbCLightning)]
    [SkippableTheory]
    public async Task FormFactor_MatchesExpectedType(YubiKeyTestState state) =>
        await state.WithManagementAsync((mgmt, deviceInfo) =>
        {
            // Form factor should be one of the known types
            Assert.True(
                deviceInfo.FormFactor is FormFactor.UsbAKeychain
                    or FormFactor.UsbCKeychain
                    or FormFactor.UsbABiometricKeychain
                    or FormFactor.UsbCBiometricKeychain
                    or FormFactor.UsbANano
                    or FormFactor.UsbCNano
                    or FormFactor.UsbCLightning);
        });


PROPOSED SAMPLE — no verdict: core-001:unit:c486d9f36101dbbf / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/OwnedMonitorReadOnlyTests.cs:170–181
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private async Task<IReadOnlyList<IYubiKey>> ReferenceFixturesAsync(CancellationToken token)
    {
        var reference = await _scoped.ReferenceScanAsync(token);
        var fixtures = ScopedDiscoveryReadOnlyTests.ConnectionsBySerial(reference);
        Assert.True(fixtures.Count >= 2, "This suite requires two attached authorized serial-bearing USB keys; fewer usable fixtures is an unmet hardware prerequisite.");
        var index = 0;
        foreach (var connections in fixtures.Values)
            output.WriteLine($"Authorized fixture {++index}: supported observed interfaces {connections}.");
        return reference;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:61eb51979d55e830 / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ScopedDiscoveryReadinessTests.cs:64–85
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(null)]
    [InlineData(AuthorizedSerial)]
    [InlineData(NonallowlistedSerial)]
    public async Task ReferenceScanAsync_ViewWithoutMetadata_ExhaustsReadinessRetries(int? incompleteSerial)
    {
        var fixture = new ScanView("authorized", AuthorizedSerial, Metadata(AuthorizedSerial));
        var incomplete = new ScanView("incomplete", incompleteSerial, null);
        var suite = new ScopedDiscoveryReadOnlyTests(output, new AllowList(new FixtureAllowListProvider()));
        var attempts = 0;

        var failure = await Assert.ThrowsAsync<FailException>(() => suite.ReferenceScanAsync(CancellationToken.None, () =>
        {
            attempts++;
            return Task.FromResult<IReadOnlyList<IYubiKey>>([fixture, incomplete]);
        }));

        Assert.Equal(3, attempts);
        Assert.Contains("missing expected interfaces after three scans", failure.Message);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:920aed6dbc2c5cd8 / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.UnitTests/FirmwareVersionTests.cs:128–142
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void AlphaOrBeta_OperatorComparisons()
    {
        var alphaKey = new FirmwareVersion(0, 0, 0);
        var v5 = new FirmwareVersion(5, 0, 0);

        Assert.True(alphaKey > v5);
        Assert.True(alphaKey >= v5);
        Assert.False(alphaKey < v5);
        Assert.False(alphaKey <= v5);
        Assert.False(alphaKey == v5);
        Assert.True(alphaKey != v5);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:409d46be3df37198 / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.UnitTests/FirmwareVersionTests.cs:259–268
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void Equality_BothNull_ReturnsTrue()
    {
        FirmwareVersion? left = null;
        FirmwareVersion? right = null;

        Assert.True(left == right);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:a469b3cef18fecb5 / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.UnitTests/FirmwareVersionTests.cs:373–382
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        var version = new FirmwareVersion(5, 0, 0);

        Assert.False(version.Equals("5.0.0"));
        Assert.False(version.Equals(500));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:30cfbefbd21e34c1 / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.UnitTests/FirmwareVersionTests.cs:505–516
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void Constructor_Parameterless()
    {
        var version = new FirmwareVersion();

        Assert.Equal(0, version.Major);
        Assert.Equal(0, version.Minor);
        Assert.Equal(0, version.Patch);
        Assert.True(version.IsAlphaOrBeta);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ab1ac215210a05f3 / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.UnitTests/FirmwareVersionTests.cs:563–598
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:




    [Fact]
    public void Operators_AreConsistentWithCompareTo()
    {
        var v1 = new FirmwareVersion(5, 7, 0);
        var v2 = new FirmwareVersion(5, 7, 2);
        var v3 = new FirmwareVersion(5, 7, 2);

        // v1 < v2
        Assert.True(v1.CompareTo(v2) < 0);
        Assert.True(v1 < v2);
        Assert.True(v1 <= v2);
        Assert.False(v1 > v2);
        Assert.False(v1 >= v2);
        Assert.False(v1 == v2);
        Assert.True(v1 != v2);

        // v2 == v3
        Assert.Equal(0, v2.CompareTo(v3));
        Assert.False(v2 < v3);
        Assert.True(v2 <= v3);
        Assert.False(v2 > v3);
        Assert.True(v2 >= v3);
        Assert.True(v2 == v3);
        Assert.False(v2 != v3);

        // v2 > v1
        Assert.True(v2.CompareTo(v1) > 0);
        Assert.False(v2 < v1);
        Assert.False(v2 <= v1);
        Assert.True(v2 > v1);
        Assert.True(v2 >= v1);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:0a4bf24a663dd6e8 / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.UnitTests/IYubiKeyExtensionsTransportTests.cs:208–209
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class SelectionProbeYubiKey(ConnectionType available) : IYubiKey
    {


PROPOSED SAMPLE — no verdict: core-001:unit:20ce16d2fb7eff11 / Management / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/tests/Yubico.YubiKit.Management.UnitTests/ManagementBackendLifecycleTests.cs:82–84
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null) { }


PROPOSED SAMPLE — no verdict: core-001:unit:1adf219a85f82c90 / Management / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Management/src/Backend/OtpBackend.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed class OtpBackend(IOtpHidProtocol otpProtocol) : IManagementBackend
{
