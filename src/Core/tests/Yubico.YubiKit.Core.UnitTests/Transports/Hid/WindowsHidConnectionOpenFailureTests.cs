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

using Yubico.YubiKit.Core.Native.Windows.HidD;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

/// <summary>
///     A constructor that acquires a native handle and then throws leaves nothing for the caller to dispose:
///     the object never finishes construction, so no <c>using</c>, <c>finally</c>, or factory <c>catch</c> can
///     reach it. Both Windows HID connections open the device first and then open the report path, so a failing
///     report open must dispose the device itself.
/// </summary>
/// <remarks>
///     Found by cross-vendor review of commit <c>6289c774</c>, which changed the feature-report open path.
///     The device is faked, so these run on every platform and need no Windows hardware.
/// </remarks>
public class WindowsHidConnectionOpenFailureTests
{
    [Fact]
    public void FeatureReportConnection_WhenFeatureOpenThrows_DisposesTheDevice()
    {
        var device = new ThrowingHidDDevice(throwOnFeatureOpen: true);

        Assert.Throws<InvalidOperationException>(
            () => new WindowsHidFeatureReportConnection(device));

        Assert.True(device.WasDisposed, "the device handle leaked: the failing constructor never disposed it");
    }

    [Fact]
    public void IOReportConnection_WhenIOOpenThrows_DisposesTheDevice()
    {
        var device = new ThrowingHidDDevice(throwOnIOOpen: true);

        Assert.Throws<InvalidOperationException>(
            () => new WindowsHidIOReportConnection(device));

        Assert.True(device.WasDisposed, "the device handle leaked: the failing constructor never disposed it");
    }

    /// <summary>
    ///     The success path must NOT dispose the device: the connection goes on to use it, and disposing here
    ///     would turn a leak fix into a use-after-dispose.
    /// </summary>
    [Fact]
    public void FeatureReportConnection_WhenOpenSucceeds_DoesNotDisposeTheDevice()
    {
        var device = new ThrowingHidDDevice();

        var connection = new WindowsHidFeatureReportConnection(device);

        Assert.False(device.WasDisposed);
        // HidD lengths include the report ID byte; the connection exposes payload-only sizes.
        Assert.Equal(device.FeatureReportByteLength - 1, connection.InputReportSize);
    }

    private sealed class ThrowingHidDDevice(bool throwOnFeatureOpen = false, bool throwOnIOOpen = false)
        : IHidDDevice
    {
        public bool WasDisposed { get; private set; }

        public string DevicePath => @"\\?\fake-hid-path";
        public short Usage => 0x06;
        public short UsagePage => 0x01;
        public short InputReportByteLength => 65;
        public short OutputReportByteLength => 65;
        public short FeatureReportByteLength => 9;

        public void OpenIOConnection()
        {
            if (throwOnIOOpen)
                throw new InvalidOperationException("simulated IO report open failure");
        }

        public void OpenFeatureConnection()
        {
            if (throwOnFeatureOpen)
                throw new InvalidOperationException("simulated feature report open failure");
        }

        public byte[] GetFeatureReport() => new byte[FeatureReportByteLength];
        public void SetFeatureReport(byte[] buffer) { }
        public byte[] GetInputReport() => new byte[InputReportByteLength];
        public void SetOutputReport(byte[] buffer) { }

        public void Dispose() => WasDisposed = true;
    }
}