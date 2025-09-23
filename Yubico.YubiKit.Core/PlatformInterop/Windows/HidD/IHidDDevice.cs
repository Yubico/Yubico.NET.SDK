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

namespace Yubico.YubiKit.Core.PlatformInterop.Windows.HidD;

interface IHidDDevice : IDisposable
{
    string DevicePath { get; }
    short Usage { get; }
    short UsagePage { get; }
    short InputReportByteLength { get; }
    short OutputReportByteLength { get; }
    short FeatureReportByteLength { get; }

    public void OpenIOConnection();
    public void OpenFeatureConnection();
    public byte[] GetFeatureReport();
    public void SetFeatureReport(byte[] buffer);
    public byte[] GetInputReport();
    public void SetOutputReport(byte[] buffer);
}