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

namespace Yubico.YubiKit.Core.Core.PlatformInterop.Desktop.SCard;

/// <summary>
///     A value that indicates whether other applications may form connections to the card once
///     a connection has been established.
/// </summary>
internal enum SCARD_SHARE
{
    /// <summary>
    ///     This application is not willing to share the card with other applications.
    /// </summary>
    EXCLUSIVE = 1,

    /// <summary>
    ///     This application is willing to share the card with other applications.
    /// </summary>
    SHARED = 2,

    /// <summary>
    ///     This application is allocating the reader for its private use, and will be controlling
    ///     it directly. No other applications are allowed access to it.
    /// </summary>
    DIRECT = 3
}