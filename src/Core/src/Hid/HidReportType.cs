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

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Type of HID report used for communication.
/// </summary>
public enum HidReportType
{
    /// <summary>
    /// Input/Output reports (used for FIDO).
    /// Typically 64-byte packets for CTAPHID protocol.
    /// </summary>
    InputOutput,
    
    /// <summary>
    /// Feature reports (used for OTP).
    /// Typically 8-byte packets for YubiOTP protocol.
    /// </summary>
    Feature
}
