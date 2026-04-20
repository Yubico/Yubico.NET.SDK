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

namespace Yubico.YubiKit.Core.PlatformInterop.MacOS.IOKitFramework;

internal static class IOKitHidConstants
{
    public const string DevicePropertyVendorId = "VendorID";
    public const string DevicePropertyProductId = "ProductID";
    public const string DevicePropertyLocationId = "LocationID";
    public const string DevicePropertyPrimaryUsage = "PrimaryUsage";
    public const string DevicePropertyPrimaryUsagePage = "PrimaryUsagePage";
    public const string MaxInputReportSize = "MaxInputReportSize";
    public const string MaxOutputReportSize = "MaxOutputReportSize";

    public const int kIOHidReportTypeInput = 0;
    public const int kIOHidReportTypeOutput = 1;
    public const int kIOHidReportTypeFeature = 2;
}