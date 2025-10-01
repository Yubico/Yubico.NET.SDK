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

namespace Yubico.YubiKit.Core.Core.Devices.SmartCard;

/// <summary>
///     Represents the means in which the smart card is connected to the system or reader.
/// </summary>
public enum PscsConnectionKind
{
    /// <summary>
    ///     Match any type of smart card during a query.
    /// </summary>
    Any = 0,

    /// <summary>
    ///     The kind of connection used for this smart card could not be determined.
    /// </summary>
    Unknown = 1,

    /// <summary>
    ///     The smart card is connected through a USB smart card reader.
    /// </summary>
    Usb = 2,

    /// <summary>
    ///     The smart card is connected through a Near-Field Communication (NFC) reader.
    /// </summary>
    Nfc = 3
}