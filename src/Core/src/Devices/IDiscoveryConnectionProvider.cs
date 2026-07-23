// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Internal connection path used only while the caller holds the interface's discovery lease. Unlike a
///     normal session connect, this path does not attempt to acquire shared session ownership itself.
/// </summary>
internal interface IDiscoveryConnectionProvider
{
    Task<IConnection> ConnectForDiscoveryAsync(
        ConnectionType connection,
        CancellationToken cancellationToken = default);
}