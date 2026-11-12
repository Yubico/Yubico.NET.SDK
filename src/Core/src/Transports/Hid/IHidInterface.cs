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

using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Transports.Hid;

/// <summary>
/// Represents an operating-system-exposed HID interface, not a physical YubiKey.
/// </summary>
public interface IHidInterface : IDevice
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
    /// Establishes an active connection to the HID interface for the transmittal of data through feature reports.
    /// </summary>
    /// <remarks>
    ///     Opens synchronously. The caller owns the returned connection and must dispose it after all report
    ///     calls finish. This direct open acquires no grouped-key registry claim; coordinate other opens and
    ///     discovery for the entire lifetime. Prefer typed IYubiKey connections for SDK-managed admission.
    /// </remarks>
    IHidConnection ConnectToFeatureReports();

    /// <summary>
    /// Establishes an active connection to the HID interface for the transmittal of data through I/O reports.
    /// </summary>
    /// <remarks>
    ///     Opens synchronously and transfers the connection to the caller. Dispose it after report calls finish.
    ///     This direct open acquires no grouped-key registry claim. Blocking, report normalization, cancellation,
    ///     and native release behavior depend on the platform; descriptor sizes alone do not define every result.
    /// </remarks>
    IHidConnection ConnectToIOReports();
}