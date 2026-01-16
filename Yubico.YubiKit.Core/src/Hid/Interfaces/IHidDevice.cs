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

using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.Hid.Interfaces;

/// <summary>
/// Represents a HID device.
/// </summary>
public interface IHidDevice : IDevice
{
    /// <summary>
    /// Raw HID descriptor information as reported by the operating system.
    /// </summary>
    HidDescriptorInfo DescriptorInfo { get; }

    /// <summary>
    /// The classified YubiKey HID interface type.
    /// </summary>
    HidInterfaceType InterfaceType { get; }

    /// <summary>
    /// Establishes an active connection to the HID device for the transmittal of data through feature reports.
    /// </summary>
    IHidConnection ConnectToFeatureReports();

    /// <summary>
    /// Establishes an active connection to the HID device for the transmittal of data through I/O reports.
    /// </summary>
    IHidConnection ConnectToIOReports();
}