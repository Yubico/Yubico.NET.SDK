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

namespace Yubico.YubiKit.Core.YubiKey;

public class YubiKeyManagerOptions
{
    public bool EnableAutoDiscovery { get; set; } = true;
    public Transport EnabledTransport { get; set; } = Transport.All;
    
    /// <summary>
    /// Delay after receiving device events before performing a scan.
    /// This allows multiple rapid events to be coalesced into a single scan.
    /// </summary>
    public TimeSpan EventCoalescingDelay { get; set; } = TimeSpan.FromMilliseconds(200);
}