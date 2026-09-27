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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Hardware checks of the protocol exchange guard on one ManagementSession over each HID transport.
/// </summary>
/// <remarks>
///     <para>
///         The guard protects each logical exchange (one GetDeviceInfo page read); it refuses, and
///         never queues, an exchange that starts while another is admitted. A public operation made of
///         several exchanges is not atomic: an overlapping call may be refused, or may run its own
///         exchanges between the first call's exchanges. Callers must sequence public operations.
///     </para>
///     <para>
///         Real hardware gives no control over when two calls overlap, so these tests assert only
///         outcomes that hold for every timing. They never assert that a refusal happened. The
///         deterministic refusal proofs are
///         ManagementSessionExchangeOverlapTests in Management.UnitTests and
///         FidoHidProtocolConcurrencyTests / OtpHidProtocolConcurrencyTests in Core.UnitTests.
///     </para>
/// </remarks>
public class ManagementHidConcurrencyTests
{
    private const int Rounds = 5;

    /// <summary>
    ///     Awaited GetDeviceInfoAsync calls reuse one HID session and return the device's data each time.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
    public async Task GetDeviceInfo_SequentialCallsOnOneHidSession_ReturnDeviceData(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, _) =>
        {
            AssertOpenedOverRequestedHidTransport(state, mgmt);

            for (var i = 0; i < Rounds; i++)
                AssertMatchesDevice(state, await mgmt.GetDeviceInfoAsync());
        },
        preferredConnection: state.ConnectionType);

    /// <summary>
    ///     Two unawaited GetDeviceInfoAsync calls on one HID session never corrupt each other. Each call
    ///     either returns the device's data or is refused by the exchange guard, at least one returns
    ///     data, and the session serves the next awaited call. A timeout, garbled response, or any other
    ///     failure fails the test.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
    public async Task GetDeviceInfo_OverlappingCallsOnOneHidSession_CompleteOrAreRefusedWithoutCorruption(
        YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, _) =>
        {
            AssertOpenedOverRequestedHidTransport(state, mgmt);

            for (var i = 0; i < Rounds; i++)
            {
                var results = await Task.WhenAll(
                    ReadOrRefusedAsync(mgmt),
                    ReadOrRefusedAsync(mgmt));

                // Only one call can lose: once it is refused, the other runs alone to completion.
                Assert.Contains(results, info => info is not null);
                foreach (var info in results)
                {
                    if (info is { } read)
                        AssertMatchesDevice(state, read);
                }

                AssertMatchesDevice(state, await mgmt.GetDeviceInfoAsync());
            }
        },
        preferredConnection: state.ConnectionType);

    /// <summary>
    ///     Returns <c>null</c> only for the exchange guard's refusal; every other failure propagates.
    /// </summary>
    private static async Task<DeviceInfo?> ReadOrRefusedAsync(ManagementSession mgmt)
    {
        try
        {
            return await mgmt.GetDeviceInfoAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("one operation at a time", StringComparison.Ordinal))
        {
            return null;
        }
    }

    // The WithYubiKey attribute is a device filter, not a transport pin: a composite key exposing SmartCard
    // satisfies a HID request. Assert the opened transport so these tests cannot silently run over SmartCard.
    private static void AssertOpenedOverRequestedHidTransport(YubiKeyTestState state, ManagementSession mgmt) =>
        Assert.Equal(state.ConnectionType, mgmt.ConnectionType);

    private static void AssertMatchesDevice(YubiKeyTestState state, DeviceInfo info)
    {
        Assert.Equal(state.SerialNumber, info.SerialNumber);
        Assert.Equal(state.FirmwareVersion, info.FirmwareVersion);
        Assert.Equal(state.FormFactor, info.FormFactor);
    }
}