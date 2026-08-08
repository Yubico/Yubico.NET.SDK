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

namespace Yubico.YubiKit.YubiHsm;

/// <summary>
///     Callback invoked when a YubiHSM Auth operation may require physical touch.
/// </summary>
/// <remarks>
///     <para>
///         <b>SECURITY:</b> This callback intentionally receives NO operation context (no
///         credential label or algorithm). This design prevents information leakage about which
///         credential is being used, which is important for applications that manage multiple
///         YubiHSM Auth credentials.
///     </para>
///     <para>
///         <b>Threading:</b> This callback is invoked on the calling async flow before the
///         blocking CALCULATE exchange with the YubiKey. Implementations must be thread-safe and
///         should not block. If UI updates are required, marshal to the appropriate UI thread
///         using the platform's threading mechanism (e.g., <c>Dispatcher.Invoke</c> for WPF,
///         <c>Control.Invoke</c> for WinForms, or <c>MainThread.BeginInvokeOnMainThread</c> for
///         MAUI).
///     </para>
///     <para>
///         <b>Reentrancy:</b> Do NOT call any <see cref="HsmAuthSession" /> methods from within
///         this callback. Doing so may cause deadlocks or undefined behavior as the YubiKey is
///         waiting for touch during the original operation.
///     </para>
///     <para>
///         <b>Firing conditions:</b> The callback fires before
///         <see cref="IHsmAuthSession.CalculateSessionKeysSymmetricAsync" /> or
///         <see cref="IHsmAuthSession.CalculateSessionKeysAsymmetricAsync" /> sends its CALCULATE
///         command, when the target credential's touch requirement is known to be set or cannot
///         be determined. It does not fire when the credential is known not to require touch, or
///         when no callback is registered.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Simple console notification
/// session.OnTouchRequired = () => Console.WriteLine("Touch your YubiKey now...");
/// </code>
/// </example>
public delegate void TouchNotificationCallback();