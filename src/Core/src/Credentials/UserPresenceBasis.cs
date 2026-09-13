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

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>Identifies why the SDK issued a user-presence notification.</summary>
public enum UserPresenceBasis
{
    /// <summary>
    ///     The device or protocol has reported that it is currently waiting for physical user presence.
    /// </summary>
    /// <remarks>
    ///     This value represents an observed in-flight wait, not a prediction based only on configured policy.
    /// </remarks>
    DeviceWaiting = 0,

    /// <summary>
    ///     The effective operation or credential policy requires user presence, but the device has not reported
    ///     that it is currently waiting.
    /// </summary>
    PolicyRequires = 1,

    /// <summary>
    ///     The SDK cannot determine in advance whether the effective operation or credential policy will require
    ///     user presence.
    /// </summary>
    /// <remarks>
    ///     This value is advisory: it means neither that presence is definitely required nor that the device is
    ///     currently waiting.
    /// </remarks>
    PolicyMayRequire = 2
}