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

using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Management.UnitTests.AuditV2;

/// <summary>
///     Audit 26n (YESDK-1634): a built DeviceConfig must not change when its source changes, and the
///     "at least one USB application" rule must hold for every configuration that reaches the key,
///     however it was constructed.
/// </summary>
public sealed class ManagementAuditReproTests
{
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_DerivedConfiguration_DoesNotAffectOriginal()
    {
        var original = new DeviceConfig { UsbEnabled = DeviceCapabilities.Oath };

        var derived = original with { UsbEnabled = DeviceCapabilities.Piv };

        Assert.Equal(DeviceCapabilities.Oath, original.UsbEnabled);
        Assert.Equal(DeviceCapabilities.Piv, derived.UsbEnabled);
    }

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_UsbNone_IsRejectedBeforeSerialization()
    {
        var config = new DeviceConfig { UsbEnabled = DeviceCapabilities.None };

        Assert.Throws<InvalidOperationException>(config.Validate);
        Assert.Throws<InvalidOperationException>(() => config.GetBytes());
    }

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_UsbNull_LeavesUsbUntouchedAndIsValid()
    {
        var config = new DeviceConfig { NfcEnabled = DeviceCapabilities.Oath };

        config.Validate();
        var bytes = config.GetBytes().ToArray();

        Assert.Equal(new byte[] { 0x04, 0x0E, 0x02, 0x00, 0x20 }, bytes); // len, NFC tag, 2-byte OATH (0x0020)
    }

    [Theory]
    [Trait("Audit", "YESDK-1634")]
    [InlineData(3601)]
    public void YESDK1634_AutoEjectTimeoutAboveLimit_RejectedAtAssignment(int seconds) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DeviceConfig { AutoEjectTimeout = (ushort)seconds });

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_ChallengeResponseTimeoutAboveLimit_RejectedAtAssignment() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DeviceConfig { ChallengeResponseTimeout = 61 });

    /// <summary>Wire order follows yubikit/management.py DeviceConfig.get_bytes (4ca60f7).</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_GetBytes_EmitsTlvsInYkmanOrder()
    {
        var config = new DeviceConfig
        {
            UsbEnabled = DeviceCapabilities.Otp | DeviceCapabilities.Piv,
            NfcEnabled = DeviceCapabilities.Oath,
            AutoEjectTimeout = 30,
            ChallengeResponseTimeout = 15,
            DeviceFlags = DeviceFlags.RemoteWakeup,
            NfcRestricted = true,
        };
        var lockCode = Enumerable.Repeat((byte)0xA5, 16).ToArray();

        var bytes = config.GetBytes(new SetDeviceConfigOptions { Reboot = true, CurrentLockCode = lockCode }).ToArray();

        byte[] expectedBody =
        [
            0x0C, 0x00, // reboot
            0x0B, 0x10, .. lockCode, // unlock
            0x03, 0x02, 0x00, 0x11, // usb: OTP | PIV
            0x0E, 0x02, 0x00, 0x20, // nfc: OATH
            0x06, 0x02, 0x00, 0x1E, // auto-eject 30
            0x07, 0x01, 0x0F, // challenge-response 15
            0x08, 0x01, 0x40, // flags
            0x17, 0x01, 0x01, // nfc restricted
        ];
        Assert.Equal((byte)expectedBody.Length, bytes[0]);
        Assert.Equal(expectedBody, bytes[1..]);
    }
}