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
    ///     Depending on the session, the value is detected, explicitly supplied, assumed, or unknown (0.0.0).
    ///     Raw HID sessions retain 0.0.0 even after their protocol discovers a device version. Security Domain
    ///     uses the supplied override or assumes 5.3.0. The value remains readable after disposal.
    /// </summary>
    FirmwareVersion FirmwareVersion { get; }

    /// <summary>Gets the type of connection used by this session.</summary>
    ConnectionType ConnectionType { get; }

    /// <summary>
    ///     Gets whether session initialization has completed. Applet sessions complete their initializer;
    ///     raw HID sessions are attached and ready for first use, with wire initialization still deferred.
    ///     Returns <c>false</c> once disposal begins.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    ///     Gets a value indicating whether the session has established application-protocol authentication,
    ///     such as SCP. Applet-specific authentication state is exposed by the respective session type.
    ///     SCP11b authenticates the device to the host; this flag grants no applet PIN, UV, or management authority
    ///     and is not an ongoing channel-liveness check.
    ///     Returns <c>false</c> once disposal begins.
    /// </summary>
    bool IsAuthenticated { get; }
    /// <summary>Evaluates known firmware compatibility, not runtime authorization or operation success.</summary>
    /// <remarks>
    ///     This query remains available after disposal. Major-zero versions use the SDK's permissive development
    ///     policy; a positive result for an unknown raw-session version is not evidence of device support.
    /// </remarks>
    bool IsSupported(Feature feature);
}