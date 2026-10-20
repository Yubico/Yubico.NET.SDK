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

namespace Yubico.YubiKit.Core;

/// <summary>
///     Compatibility switch names that can be used with `AppContext.SetSwitch` to control breaking behavioral changes
///     within the `Yubico.Core` layer.
/// </summary>
public static class CoreCompatSwitches
{
    /// <summary>
    ///     If set to true, smart card handles are opened exclusively with no shared fallback
    ///     (<see cref="Transports.SmartCard.SmartCardSharing.Exclusive"/>). The default is exclusive with a shared
    ///     fallback, as ykman does. Ignored when a <see cref="Transports.SmartCard.SmartCardConnectionFactory"/>
    ///     is constructed with an explicit sharing mode.
    /// </summary>
    public const string OpenSmartCardHandlesExclusively = nameof(OpenSmartCardHandlesExclusively);

    /// <summary>
    ///     If set to true, smart card handles are opened shared and the exclusive attempt is skipped
    ///     (<see cref="Transports.SmartCard.SmartCardSharing.Shared"/>): the equivalent of ykman's
    ///     <c>YKMAN_NO_EXLUSIVE</c>, for applications that must coexist with other smart-card users and accept
    ///     that the card-wide PIN state is reachable from other handles. Ignored when a factory is constructed
    ///     with an explicit sharing mode.
    /// </summary>
    public const string OpenSmartCardHandlesShared = nameof(OpenSmartCardHandlesShared);
}