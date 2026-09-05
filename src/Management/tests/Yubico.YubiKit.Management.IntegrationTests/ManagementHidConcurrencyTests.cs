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
///     Hardware demonstration that concurrent operations on a single ManagementSession over HID
///     transports do not interleave packets/reports on the wire.
/// </summary>
/// <remarks>
///     <para>
///         GetDeviceInfoAsync is a multi-exchange operation (device info paging), so concurrent
///         calls exercise the protocol-level exchange gate: without serialization, CTAP packets or
///         OTP feature reports from the two operations would interleave and corrupt both exchanges
///         (typically timeouts or garbled TLVs).
///     </para>
///     <para>
///         Companion unit tests (deterministic gates):
///         FidoHidProtocolConcurrencyTests / OtpHidProtocolConcurrencyTests in Core.UnitTests.
///     </para>
/// </remarks>
public class ManagementHidConcurrencyTests
{
    /// <summary>
    ///     Concurrent GetDeviceInfoAsync calls on one session over each HID transport succeed and
    ///     return consistent data. Pre-fix, interleaved packets caused timeouts/corruption.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
    public async Task GetDeviceInfo_ConcurrentCallsOnOneHidSession_DoNotCorruptExchanges(
        YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            // The attribute above is a device FILTER, not a transport pin: a composite key exposing
            // SmartCard satisfies a HID request. Without an explicit preferredConnection this test ran
            // over SmartCard on every composite key and proved nothing about HID. Assert the transport
            // so the test cannot silently stop testing what its name claims.
            Assert.Equal(state.ConnectionType, mgmt.ConnectionType);

            const int iterations = 5;

            for (var i = 0; i < iterations; i++)
            {
                var first = mgmt.GetDeviceInfoAsync();
                var second = mgmt.GetDeviceInfoAsync();

                var results = await Task.WhenAll(first, second);

                Assert.All(results, info =>
                {
                    Assert.Equal(state.SerialNumber, info.SerialNumber);
                    Assert.Equal(state.FirmwareVersion, info.FirmwareVersion);
                    Assert.Equal(state.FormFactor, info.FormFactor);
                });
            }
        },
        preferredConnection: state.ConnectionType);
}