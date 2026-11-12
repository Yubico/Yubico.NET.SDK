PROPOSED SAMPLE — no verdict: core-001:unit:eb4d4df0bb3c6233 / YubiOtp / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/examples/OtpTool/Commands/NdefCommand.cs:1–27
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

using Spectre.Console;
using System.Security.Cryptography;

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool.Commands;

/// <summary>
/// Configures NDEF for NFC on a slot (ykman otp ndef SLOT).
/// </summary>
/// <remarks>
/// Requires NFC-capable hardware and a suitable existing slot. URI remainder allows 53 UTF-8 bytes after
/// automatic prefix compression; English UTF-8 text allows 51. A missing prefix sends an all-zero disable
/// request, not the same request as empty text/URI. This command does not read back the on-tap record.
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:53f86b1ba80074a2 / YubiOtp / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/examples/OtpTool/DeviceHelper.cs:26–33
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Finds an eligible YubiKey, preferring SmartCard without interaction; diagnostics stay on stderr.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(CancellationToken cancellationToken = default) =>
        new DeviceSelector([ConnectionType.SmartCard, ConnectionType.HidOtp], NonInteractiveSelection.PreferFirstTransport,
            console: AnsiConsole.Profile.Capabilities.Interactive ? AnsiConsole.Console :
                AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) }))
            .SelectDeviceAsync(cancellationToken);


PROPOSED SAMPLE — no verdict: core-001:unit:dddf82740266d31e / YubiOtp / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/examples/OtpTool/OutputHelper.cs:1–23
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool;

/// <summary>
/// Provides output formatting for both Spectre.Console (human) and JSON (CI) modes.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:4a803dcc4d6bd607 / YubiOtp / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/src/KeyboardSlotConfiguration.cs:1–27
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Abstract intermediate configuration for slot types that produce keyboard output
/// (Yubico OTP, HOTP, static password, static ticket). Adds shared CR, tab, delay,
/// pacing, and numeric keypad flag setters.
/// </summary>
/// <remarks>
/// Defaults add AppendCr and FastTrigger to the base SerialApiVisible/AllowUpdate flags. Legacy
/// Manual 3.3 documents delay flags for older firmware, fast trigger only with one configuration,
/// and firmware-specific numeric-keypad support. Tagged personalization documentation notes fast
/// triggering is always effective on 3+. These setters do not validate every firmware/flag combination.
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:dccbd1f9dd9d13eb / YubiOtp / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/src/YubiOtpSlotConfiguration.cs:1–30
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Configures a slot for Yubico OTP mode.
/// </summary>
/// <remarks>
/// Wire format layout:
/// <list type="bullet">
/// <item><c>fixed[0..publicId.Length]</c> — raw binary public identity (up to 16 bytes)</item>
/// <item><c>uid</c> — 6-byte private identity</item>
/// <item><c>key</c> — 16-byte AES-128 key</item>
/// </list>
/// The key emits two modhex characters per public-ID byte; input is not pre-encoded modhex text.
/// The configuration copies inputs; dispose it and separately clear your secret source buffers.
/// This configures keyboard output, not host challenge-response or server-side OTP verification.
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:544cb91fcb796dfc / YubiOtp / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/examples/OtpTool/Commands/InfoCommand.cs:23–38
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        await using var session = await DeviceHelper.CreateSessionAsync(ct);

        var state = session.GetConfigState();

        if (options.Json)
        {
            OutputHelper.WriteJson(BuildStatusResult(state));
            return 0;
        }

        DisplayStatus(state);
        return 0;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:1d9a1e2ddc9a4a96 / YubiOtp / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/examples/OtpTool/Program.cs:1–59
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

using Yubico.YubiKit.Cli.Shared.Cli;
using Yubico.YubiKit.YubiOtp.Examples.OtpTool;
using Yubico.YubiKit.YubiOtp.Examples.OtpTool.Commands;

using var cts = CommandHelper.CreateConsoleCts();

try
{
    var options = CliOptions.Parse(args);

    int exitCode = await RunCommandAsync(options, cts.Token);

    return exitCode;
}
catch (ArgumentException ex)
{
    OutputHelper.WriteError(ex.Message);
    return 1;
}
catch (InvalidOperationException ex)
{
    OutputHelper.WriteError(ex.Message);
    return 1;
}
catch (OperationCanceledException)
{
    return 0;
}

static Task<int> RunCommandAsync(CliOptions options, CancellationToken ct) =>
    options.Command switch
    {
        "info" => InfoCommand.RunAsync(options, ct),
        "swap" => SwapCommand.RunAsync(options, ct),
        "delete" => DeleteCommand.RunAsync(options, ct),
        "chalresp" => ChalRespCommand.RunAsync(options, ct),
        "hotp" => HotpCommand.RunAsync(options, ct),
        "static" => StaticCommand.RunAsync(options, ct),
        "yubiotp" => YubiOtpCommand.RunAsync(options, ct),
        "calculate" => CalculateCommand.RunAsync(options, ct),
        "ndef" => NdefCommand.RunAsync(options, ct),
        "settings" => SettingsCommand.RunAsync(options, ct),
        _ => throw new ArgumentException(
            $"Unknown command: {options.Command}. Run 'OtpTool --help' for usage.")
    };


PROPOSED SAMPLE — no verdict: core-001:unit:a97df01d328e9157 / YubiOtp / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/src/Backend/HidBackend.cs:32–34
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly ILogger Logger =
        YubiKitLogging.LoggerFactory.CreateLogger<HidBackend>();


PROPOSED SAMPLE — no verdict: core-001:unit:5fa8e291ce0e50fa / YubiOtp / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/src/YubiOtpSession.cs:41–42
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly Feature FeatureNdef = new("NDEF Configuration", 3, 0, 0);


PROPOSED SAMPLE — no verdict: core-001:unit:f02eac5f2126a9c4 / YubiOtp / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/CLAUDE.md:204–223
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Challenge-response

Composable fragment within an already open `YubiOtpSession session` for a correctly provisioned slot.
`byte[] challenge` is owned by this task. Import System, System.Runtime.InteropServices and
System.Security.Cryptography plus the module namespace. AsMemory below is only for the caller-owned SDK output.

```csharp
ReadOnlyMemory<byte> response = default;
try
{
    response = await session.CalculateHmacSha1Async(Slot.Two, challenge);
    // Consume with the application's verifier.
}
finally
{
    CryptographicOperations.ZeroMemory(challenge);
    CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response).Span);
}
```




PROPOSED SAMPLE — no verdict: core-001:unit:6a99ceaa75ce028b / YubiOtp / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/README.md:206–240
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Calculate a Yubico OTP AES ticket

This alternative fragment replaces slot 2 and calculates a ticket using OTP HID. Provision the same AES key
securely to your verifier before clearing it; a random key known only to this process is not a deployed verifier.

```csharp
byte[] aesKey = [];
byte[] challenge = [];
ReadOnlyMemory<byte> ticket = default;
try
{
    aesKey = RandomNumberGenerator.GetBytes(16);
    challenge = RandomNumberGenerator.GetBytes(6);
    // Securely provision aesKey to the verifier here as part of your application's enrollment.
    await using var session = await device.CreateYubiOtpSessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
    using var config = new YubicoOtpChallengeResponseSlotConfiguration(aesKey);
    await session.PutConfigurationAsync(Slot.Two, config);
    ticket = await session.CalculateYubicoOtpAsync(Slot.Two, challenge);
    // Send the ticket to your verifier through your application's secure channel.
}
finally
{
    CryptographicOperations.ZeroMemory(aesKey);
    CryptographicOperations.ZeroMemory(challenge);
    CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(ticket).Span);
}
```

The device encrypts a Yubico ticket with changing counter/random fields, not just the six challenge bytes.
Repeated challenges can therefore return different tickets. In the legacy protocol the private ID is XORed
with the challenge; this configuration leaves the private ID zero, so a verifier expects the first six
decrypted bytes to equal the challenge. The verifier must also validate the ticket CRC and its freshness/
replay policy. The SDK returns bytes; it does not decrypt, verify, format an OTP, or enforce replay protection.




PROPOSED SAMPLE — no verdict: core-001:unit:bd29ead0f592421f / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/UserPresenceProbeTests.cs:45–146
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(MinFirmware = "3.0.0", ConnectionType = ConnectionType.HidOtp)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task CalculateHmacSha1Async_TouchRequired_CompletesOnceAndReturnsExpectedResponse(
        YubiKeyTestState state)
    {
        var prompt = new RecordingUserPresencePrompt(output);
        await using var session = await state.Device.CreateYubiOtpSessionAsync(
            new SessionCreationOptions
            {
                PreferredConnectionType = ConnectionType.HidOtp,
                UserPresencePrompt = prompt
            });

        ConfigState initialState = session.GetConfigState();
        output.WriteLine(
            $"Prerequisites: Firmware={initialState.FirmwareVersion}, " +
            $"Slot1Configured={initialState.IsConfigured(Slot.One)}, " +
            $"Slot2Configured={initialState.IsConfigured(Slot.Two)}, " +
            $"Slot2TouchTriggered={initialState.IsTouchTriggered(Slot.Two)}");
        Skip.If(
            initialState.IsConfigured(Slot.Two),
            "YubiOTP slot 2 is configured; refusing to overwrite it. Clear slot 2 deliberately before running this probe.");

        byte[] key =
        [
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
            0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13
        ];
        byte[] challenge = "YubiOTP touch probe"u8.ToArray();
        byte[]? expectedResponse = null;
        ReadOnlyMemory<byte> response = default;
        var slotCreated = false;

        try
        {
            using var configuration = new HmacSha1SlotConfiguration(key).RequireTouch();
            try
            {
                await session.PutConfigurationAsync(Slot.Two, configuration);
                slotCreated = true;
            }
            catch
            {
                output.WriteLine(
                    "The slot 2 write did not complete unambiguously. This test will not delete slot 2 because it " +
                    "cannot prove ownership; inspect slot state before retrying.");
                throw;
            }

            ConfigState configuredState = session.GetConfigState();
            Assert.True(configuredState.IsConfigured(Slot.Two));
            output.WriteLine(
                $"Post-configuration status: Slot2IsTouchTriggered={configuredState.IsTouchTriggered(Slot.Two)} " +
                "(reported for inspection; HID wait behavior is authoritative for this probe).");

            Assert.Empty(prompt.Requested);
            Assert.Empty(prompt.Resolved);
            expectedResponse = HMACSHA1.HashData(key, challenge);

            using var operationCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            response = await session.CalculateHmacSha1Async(Slot.Two, challenge, operationCts.Token);

            Assert.True(CryptographicOperations.FixedTimeEquals(expectedResponse, response.Span));

            UserPresenceContext requested = Assert.Single(prompt.Requested);
            Assert.Equal(UserPresenceBasis.DeviceWaiting, requested.Basis);
            Assert.Equal("YubiOTP", requested.Application);
            Assert.Equal(Slot.Two.ToString(), requested.Scope);

            (UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken) resolved =
                Assert.Single(prompt.Resolved);
            Assert.Same(requested, resolved.Context);
            Assert.Equal(UserPresenceOutcome.Completed, resolved.Outcome);
            Assert.Equal(CancellationToken.None, resolved.CancellationToken);
        }
        finally
        {
            if (!response.IsEmpty)
            {
                CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response).Span);
            }

            if (expectedResponse is not null)
            {
                CryptographicOperations.ZeroMemory(expectedResponse);
            }

            CryptographicOperations.ZeroMemory(challenge);
            CryptographicOperations.ZeroMemory(key);

            if (slotCreated)
            {
                await session.DeleteSlotAsync(Slot.Two);
                Assert.False(session.GetConfigState().IsConfigured(Slot.Two));
                output.WriteLine("Cleanup: deleted the test-created configuration from YubiOTP slot 2.");
            }
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:864bf94e1240f012 / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/YubiOtpSessionIntegrationTests.cs:70–102
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.HidOtp)]
    public async Task CalculateHmacSha1_WithKnownKey_ReturnsExpectedResponse(YubiKeyTestState state)
    {
        // [WithYubiKey] filters the device; it does not select the session transport. Pin HidOtp so
        // this test cannot silently run over the default SmartCard path while claiming HID coverage.
        await using var session = await state.Device.CreateYubiOtpSessionAsync(
            new SessionCreationOptions { PreferredConnectionType = state.ConnectionType });

        // Program slot 2 with a known HMAC-SHA1 key
        byte[] key = new byte[20];
        Array.Fill(key, (byte)0x31); // "1111..." as key

        using var config = new HmacSha1SlotConfiguration(key);
        await session.PutConfigurationAsync(Slot.Two, config);

        try
        {
            // Send a challenge
            byte[] challenge = [0x01, 0x02, 0x03, 0x04];
            var response = await session.CalculateHmacSha1Async(Slot.Two, challenge);

            // Length-only assertion: this does not compare the returned HMAC with a known-answer value.
            Assert.Equal(20, response.Length);
        }
        finally
        {
            // Clean up: delete slot 2
            await session.DeleteSlotAsync(Slot.Two);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:45ba1288acf4038a / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/YubiOtpSlotConfigTests.cs:27–64
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>
    ///     Programs slot 2 with a static password configuration using keyboard scan codes,
    ///     verifies the slot is configured, then deletes it and verifies cleanup.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.HidOtp)]
    public async Task PutConfiguration_StaticPassword_ConfiguresAndDeletesSlot(YubiKeyTestState state)
    {
        // The test creates the connection, so the test disposes it: YubiOtpSession.CreateAsync borrows a
        // caller-created connection and never closes it. Leaking it would hold the physical-device lease
        // for the process lifetime and fail later opens through any known interface of this key.
        await using var connection = await state.Device.ConnectAsync<IOtpHidConnection>();
        await using var session = await YubiOtpSession.CreateAsync(connection);

        // Simple scan codes representing a static password (US keyboard layout)
        // These are USB HID scan codes, not ASCII characters
        byte[] scanCodes = [0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B]; // "abcdefgh"

        using var config = new StaticPasswordSlotConfiguration(scanCodes);

        await session.PutConfigurationAsync(Slot.Two, config);

        try
        {
            // Verify slot 2 is now configured
            var configState = session.GetConfigState();
            Assert.True(configState.IsConfigured(Slot.Two));
        }
        finally
        {
            // Clean up: delete slot 2
            await session.DeleteSlotAsync(Slot.Two);

            var stateAfterDelete = session.GetConfigState();
            Assert.False(stateAfterDelete.IsConfigured(Slot.Two));
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c3dfa3ac3526a31a / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/HidBackendTests.cs:74–103
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task SendAndReceiveAsync_ValidCrc_ReturnsDataWithoutCrc()
    {
        byte[] hmacResult = [0xAA, 0xBB, 0xCC, 0xDD, 0x11, 0x22, 0x33, 0x44,
                             0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xAA, 0xBB,
                             0xCC, 0xDD, 0xEE, 0xFF];
        var responseWithCrc = MakeResponseWithCrc(hmacResult);

        _protocol.SendAndReceiveAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<UserPresenceNotification>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(responseWithCrc));

        var backend = CreateBackend();
        var result = await backend.SendAndReceiveAsync(
            ConfigSlot.ChalHmac1,
            new byte[64],
            20,
            UserPresenceNotification.None,
            cancellationToken: CancellationToken.None);

        Assert.Equal(20, result.Length);
        Assert.Equal(0xAA, result.Span[0]);
        Assert.Equal(0xFF, result.Span[19]);
        Assert.Equal(responseWithCrc, result.ToArray().Concat(responseWithCrc[^2..]).ToArray());
    }


PROPOSED SAMPLE — no verdict: core-001:unit:805d70e10d397c4c / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/HidBackendTests.cs:440–448
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static byte[] DataReport(byte sequence, ReadOnlySpan<byte> data)
    {
        var report = new byte[OtpConstants.FeatureReportSize];
        data.CopyTo(report);
        report[OtpConstants.FeatureReportDataSize] = (byte)(OtpConstants.ResponsePendingFlag | sequence);
        return report;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:d64fbe6746b8ec24 / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/HidBackendTests.cs:505–508
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class ThrowingPrompt(
        Exception? requestException = null,
        Exception? resolutionException = null) : IUserPresencePrompt
    {


PROPOSED SAMPLE — no verdict: core-001:unit:a323bdc19346c4b1 / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/IYubiKeyExtensionsTransportTests.cs:175–176
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public ConnectionType AvailableConnections { get; } = available;


PROPOSED SAMPLE — no verdict: core-001:unit:e79d924e62408464 / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/SlotConfigurationTests.cs:633–644
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void StaticPassword_FromString_UppercaseCharacters_SetShiftBit()
    {
        using var config = new StaticPasswordSlotConfiguration("Ab1", KeyboardLayout.en_US);
        var result = config.GetConfig();

        // 'A' = 0x04 with the shift bit (0x80) set, 'b' = 0x05, '1' = 0x1e
        byte[] expectedScanCodes = [0x04 | 0x80, 0x05, 0x1e];
        Assert.Equal(expectedScanCodes, result[FixedOffset..(FixedOffset + 3)]);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:1bebb9d5f7f0877b / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/SlotConfigurationTests.cs:690–704
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void StaticPassword_FromString_WhenInitializationFails_ZeroesOwnedScanCodes()
    {
        byte[] ownedScanCodes = Enumerable.Repeat((byte)0x04, 39).ToArray();

        Assert.Throws<ArgumentException>(() =>
            new StaticPasswordSlotConfiguration(
                new string('a', 39),
                KeyboardLayout.en_US,
                (_, _) => ownedScanCodes));

        Assert.All(ownedScanCodes, scanCode => Assert.Equal(0, scanCode));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c34204e2d27aaf15 / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/SmartCardBackendTests.cs:31–38
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static ApduResponse MakeStatusResponse(byte progSeq, byte touchLo = 0, byte touchHi = 0)
    {
        // Response data: [fw_major, fw_minor, fw_patch, prog_seq, touch_lo, touch_hi] + SW 9000
        byte[] raw = [5, 7, 0, progSeq, touchLo, touchHi, 0x90, 0x00];
        return new ApduResponse(raw);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:3843b52721516f81 / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/SmartCardBackendTests.cs:390–393
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class ThrowingPrompt(
        Exception? requestException = null,
        Exception? resolutionException = null) : IUserPresencePrompt
    {


PROPOSED SAMPLE — no verdict: core-001:unit:5c54ff22c326b0b3 / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/YubiOtpSessionLinuxCrossLayerTests.cs:167–169
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken token)
        { Requested.Add(context); cancellation?.Cancel(); return ValueTask.CompletedTask; }


PROPOSED SAMPLE — no verdict: core-001:unit:b429d29004cc107f / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/YubiOtpSessionLinuxCrossLayerTests.cs:196–198
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags)
        { var handle = new LinuxFileSafeHandle(_ => new(0, 0)); handle.Initialize(42); return handle; }


PROPOSED SAMPLE — no verdict: core-001:unit:84ede3c0f0fbbe78 / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/YubiOtpSessionTests.cs:108–133
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task CalculateHmacSha1Async_TouchConfiguredSmartCard_NotifiesPolicyRequires()
    {
        var prompt = new RecordingUserPresencePrompt();
        var (connection, session) = await CreateFakeSessionAsync(touchLow: 0x05, prompt);
        using var sessionLease = session;
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((ReadOnlyMemory<byte>)CreateApduResponseData(20)));

        _ = await session.CalculateHmacSha1Async(
            Slot.One,
            new byte[] { 0x01 },
            TestContext.Current.CancellationToken);

        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceBasis.PolicyRequires, requested.Basis);
        Assert.Equal("YubiOTP", requested.Application);
        Assert.Equal(UserPresenceOperations.YubiOtp.CalculateHmacSha1, requested.Operation);
        Assert.Equal(Slot.One.ToString(), requested.Scope);
        var resolved = Assert.Single(prompt.Resolved);
        Assert.Same(requested, resolved.Context);
        Assert.Equal(UserPresenceOutcome.Completed, resolved.Outcome);
        Assert.Equal(CancellationToken.None, resolved.CancellationToken);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:a8517209eac277b9 / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/YubiOtpSessionTests.cs:187–204
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task CalculateHmacSha1Async_SmartCardTransportTimeout_ResolvesFailed()
    {
        var prompt = new RecordingUserPresencePrompt();
        var (connection, session) = await CreateFakeSessionAsync(touchLow: 0x05, prompt);
        using var _ = session;
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReadOnlyMemory<byte>>(new TimeoutException("Touch timed out.")));

        await Assert.ThrowsAsync<TimeoutException>(() => session.CalculateHmacSha1Async(
            Slot.One,
            new byte[] { 0x01 },
            TestContext.Current.CancellationToken));

        Assert.Equal(UserPresenceOutcome.Failed, Assert.Single(prompt.Resolved).Outcome);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:0356e60bb431d88f / YubiOtp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/YubiOtpSessionTests.cs:669–669
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class ProbeSession(IConnection connection) : ApplicationSession(connection);


PROPOSED SAMPLE — no verdict: core-001:unit:159b455a45df887f / YubiOtp / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiOtp/src/Backend/IYubiOtpBackend.cs:20–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal readonly record struct YubiOtpInitialization(
    FirmwareVersion FirmwareVersion,
    ReadOnlyMemory<byte> Status);
