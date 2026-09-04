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

using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Abstractions;

public interface IApplicationSession : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     Gets the effective firmware version used to configure the application protocol and evaluate features.
    ///     This is the detected applet or device version unless session creation explicitly supplied a firmware
    ///     version override.
    /// </summary>
    FirmwareVersion FirmwareVersion { get; }

    /// <summary>Gets the type of connection used by this session.</summary>
    ConnectionType ConnectionType { get; }

    /// <summary>
    ///     Gets a value indicating whether the session has initialized its application protocol.
    ///     Returns <c>false</c> once disposal begins.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    ///     Gets a value indicating whether the session has established application-protocol authentication,
    ///     such as SCP. Applet-specific authentication state is exposed by the respective session type.
    ///     Returns <c>false</c> once disposal begins.
    /// </summary>
    bool IsAuthenticated { get; }
    bool IsSupported(Feature feature);
    void EnsureSupports(Feature feature);
}
