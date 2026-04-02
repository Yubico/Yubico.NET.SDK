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

namespace Yubico.YubiKit.Piv;

/// <summary>
/// Callback invoked when a YubiKey operation may require physical touch.
/// </summary>
/// <remarks>
/// <para>
/// <b>SECURITY:</b> This callback intentionally receives NO operation context (no slot, algorithm,
/// or data parameters). This design prevents information leakage about what cryptographic operation
/// is being performed, which is important for applications that handle multiple sensitive operations.
/// </para>
/// <para>
/// <b>Threading:</b> This callback may be invoked on a background thread. Implementations must
/// be thread-safe and should not block. If UI updates are required, marshal to the appropriate
/// UI thread using the platform's threading mechanism (e.g., <c>Dispatcher.Invoke</c> for WPF,
/// <c>Control.Invoke</c> for WinForms, or <c>MainThread.BeginInvokeOnMainThread</c> for MAUI).
/// </para>
/// <para>
/// <b>Reentrancy:</b> Do NOT call any PIV session methods from within this callback. Doing so
/// may cause deadlocks or undefined behavior as the YubiKey is waiting for touch during the
/// original operation.
/// </para>
/// <para>
/// <b>Touch Policy Behavior:</b>
/// <list type="bullet">
/// <item><description><see cref="PivTouchPolicy.Always"/>: Callback is invoked before every operation.</description></item>
/// <item><description><see cref="PivTouchPolicy.Cached"/>: Callback is invoked conservatively (may fire even if touch isn't required due to caching - cache expiry is unknowable).</description></item>
/// <item><description><see cref="PivTouchPolicy.Never"/>: Callback is NOT invoked.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple console notification
/// session.OnTouchRequired = () => Console.WriteLine("Touch your YubiKey now...");
/// 
/// // WPF dispatcher example
/// session.OnTouchRequired = () => 
///     Dispatcher.BeginInvoke(() => StatusText.Text = "Touch YubiKey");
/// </code>
/// </example>
public delegate void TouchNotificationCallback();
