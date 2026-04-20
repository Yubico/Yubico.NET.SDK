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

using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Cli.Shared.Device;

/// <summary>
/// Formats YubiKey connection types for display in CLI tools.
/// </summary>
public static class ConnectionTypeFormatter
{
    /// <summary>
    /// Formats a connection type as a human-readable string.
    /// </summary>
    public static string Format(ConnectionType connectionType) =>
        connectionType switch
        {
            ConnectionType.SmartCard => "SmartCard",
            ConnectionType.HidFido => "FIDO HID",
            ConnectionType.HidOtp => "OTP HID",
            _ => "Unknown"
        };
}