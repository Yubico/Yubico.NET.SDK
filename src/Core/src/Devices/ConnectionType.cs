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

namespace Yubico.YubiKit.Core.Devices;

/// <summary>Concrete connection interfaces and group filters used by device discovery.</summary>
/// <remarks>
///     A discovery scope selects interfaces to enumerate and probe; a result filter matches any selected
///     concrete interface. Available connections contain only concrete bits, which may occur together.
/// </remarks>
[Flags]
public enum ConnectionType
{
    /// <summary>No connection bits; matches no discovered devices.</summary>
    Unknown = 0,
    /// <summary>A group filter matching FIDO HID or OTP HID; a separate bit rather than their bitwise union.</summary>
    Hid = 1,
    /// <summary>The FIDO HID report interface, shared by CTAP1/U2F and CTAP2.</summary>
    HidFido = 2,
    /// <summary>The OTP HID feature-report interface.</summary>
    HidOtp = 4,
    /// <summary>A PC/SC smart-card connection, including USB CCID and NFC readers.</summary>
    SmartCard = 8,
    /// <summary>All modeled connection filters; use a concrete type to open a connection.</summary>
    All = Hid | HidFido | HidOtp | SmartCard
}