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

using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Tests.Shared;

/// <summary>
///     Example custom filter that only matches YubiKeys with firmware version 5.0 or higher.
///     This demonstrates the IYubiKeyFilter interface for custom filtering logic.
/// </summary>
public class ModernFirmwareFilter : IYubiKeyFilter
{
    #region IYubiKeyFilter Members

    public bool Matches(YubiKeyTestState device) =>
        device.FirmwareVersion >= new FirmwareVersion(5);

    public string GetDescription() => "Firmware >= 5.0.0";

    #endregion
}