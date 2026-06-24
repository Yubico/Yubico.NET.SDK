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

using Yubico.YubiKit.Core.Transports.Hid;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public class HidInterfaceClassifierTests
{
    private static HidDescriptorInfo MakeDescriptor(ushort usagePage, ushort usage) =>
        new()
        {
            UsagePage = usagePage,
            Usage = usage,
            DevicePath = "test",
            VendorId = HidConstants.YubicoVendorId,
            ProductId = 0x0407
        };

    [Fact]
    public void Classify_FidoDescriptor_ReturnsFido() =>
        Assert.Equal(
            HidInterfaceType.Fido,
            HidInterfaceClassifier.Classify(MakeDescriptor(0xF1D0, 0x0001)));

    [Fact]
    public void Classify_OtpDescriptor_ReturnsOtp() =>
        Assert.Equal(
            HidInterfaceType.Otp,
            HidInterfaceClassifier.Classify(MakeDescriptor(0x0001, 0x0006)));

    [Fact]
    public void Classify_UnknownDescriptor_ReturnsUnknown() =>
        Assert.Equal(
            HidInterfaceType.Unknown,
            HidInterfaceClassifier.Classify(MakeDescriptor(0x0001, 0x0001)));

    [Fact]
    public void IsSupported_FidoDescriptor_ReturnsTrue() =>
        Assert.True(HidInterfaceClassifier.IsSupported(MakeDescriptor(0xF1D0, 0x0001)));

    [Fact]
    public void IsSupported_OtpDescriptor_ReturnsTrue() =>
        Assert.True(HidInterfaceClassifier.IsSupported(MakeDescriptor(0x0001, 0x0006)));

    [Fact]
    public void IsSupported_UnknownDescriptor_ReturnsFalse() =>
        Assert.False(HidInterfaceClassifier.IsSupported(MakeDescriptor(0x0001, 0x0001)));

    [Fact]
    public void GetReportType_Fido_ReturnsInputOutput() =>
        Assert.Equal(HidReportType.InputOutput, HidInterfaceClassifier.GetReportType(HidInterfaceType.Fido));

    [Fact]
    public void GetReportType_Otp_ReturnsFeature() =>
        Assert.Equal(HidReportType.Feature, HidInterfaceClassifier.GetReportType(HidInterfaceType.Otp));

    [Fact]
    public void GetReportType_Unknown_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => HidInterfaceClassifier.GetReportType(HidInterfaceType.Unknown));
}
