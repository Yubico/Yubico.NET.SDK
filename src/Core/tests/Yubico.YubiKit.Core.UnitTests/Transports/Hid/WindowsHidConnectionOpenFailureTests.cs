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

using Yubico.YubiKit.Core.Transports.Hid.Windows;
using Yubico.YubiKit.Core.UnitTests.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

/// <summary>
///     A constructor that acquires a native handle and then throws leaves nothing for the caller to dispose:
///     the object never finishes construction, so no <c>using</c>, <c>finally</c>, or factory <c>catch</c> can
///     reach it. Both Windows HID facades delegate acquisition to their native owner, so a failing
///     constructor must await that owner's checked cleanup before propagating the open failure.
/// </summary>
/// <remarks>
///     Found by cross-vendor review of commit <c>6289c774</c>, which changed the feature-report open path.
///     Only the native API is faked, so these run on every platform and need no Windows hardware.
/// </remarks>
public class WindowsHidConnectionOpenFailureTests
{
    [Fact]
    public void FeatureReportConnection_WhenFeatureOpenThrows_DisposesTheDevice()
    {
        var device = new WindowsRoutingApi { OpenError = new InvalidOperationException("feature open"), Length = 9 };

        Assert.Throws<InvalidOperationException>(
            () => new WindowsHidFeatureReportConnection("feature", device));

        Assert.Equal(1, device.Closes);
    }

    [Fact]
    public void IOReportConnection_WhenIOOpenThrows_DisposesTheDevice()
    {
        var device = new WindowsRoutingApi { OpenError = new InvalidOperationException("IO open") };

        Assert.Throws<InvalidOperationException>(
            () => new WindowsHidIOReportConnection("io", device));

        Assert.Equal(1, device.Closes);
    }

    /// <summary>
    ///     The success path must NOT dispose the device: the connection goes on to use it, and disposing here
    ///     would turn a leak fix into a use-after-dispose.
    /// </summary>
    [Fact]
    public void FeatureReportConnection_WhenOpenSucceeds_DoesNotDisposeTheDevice()
    {
        var device = new WindowsRoutingApi { Length = 9 };

        using var connection = new WindowsHidFeatureReportConnection("feature", device);

        Assert.Equal(0, device.Closes);
        // HidD lengths include the report ID byte; the connection exposes payload-only sizes.
        Assert.Equal(device.Length - 1, connection.InputReportSize);
    }
}
