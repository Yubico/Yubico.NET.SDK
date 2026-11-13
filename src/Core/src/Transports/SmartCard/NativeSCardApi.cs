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

using Yubico.YubiKit.Core.Native.Desktop.SCard;

namespace Yubico.YubiKit.Core.Transports.SmartCard;

/// <summary>Provides the shared native PC/SC discovery and listener forwarding seam.</summary>
/// <remarks>
///     Calls run synchronously on the caller's thread. Discovery and listener callers own scheduling, context
///     lifetimes and reader-state storage; this stateless seam provides no worker or admission guard.
///     Native status codes are returned unchanged, while native-loading and managed-wrapper exceptions propagate.
/// </remarks>
internal sealed class NativeSCardApi : ISCardApi
{
    /// <summary>Gets the shared stateless forwarding instance; it owns no per-caller context.</summary>
    public static NativeSCardApi Instance { get; } = new();

    /// <summary>Creates the stateless instance exposed by <see cref="Instance" /> without acquiring native resources.</summary>
    private NativeSCardApi()
    {
    }

    /// <summary>Establishes a resource-manager context for discovery or listener use.</summary>
    /// <param name="scope">The native resource-manager scope.</param>
    /// <param name="context">The returned context wrapper, whose lifetime belongs to the caller.</param>
    /// <returns>The native status code; inspect it before using the context.</returns>
    public uint SCardEstablishContext(SCARD_SCOPE scope, out SCardContext context) =>
        NativeMethods.SCardEstablishContext(scope, out context);

    /// <summary>Lists native reader names through the managed multi-string wrapper.</summary>
    /// <param name="context">The borrowed resource-manager context.</param>
    /// <param name="groups">The reader groups to query, or <see langword="null" /> for all groups.</param>
    /// <param name="readerNames">The decoded reader names; inspect the status code before consuming them.</param>
    /// <returns>The status code from the native length query or subsequent reader-list call.</returns>
    /// <exception cref="Yubico.YubiKit.Core.Native.PlatformApiException">The successful length query reports an unexpected zero length.</exception>
    public uint SCardListReaders(SCardContext context, string[]? groups, out string[] readerNames) =>
        NativeMethods.SCardListReaders(context, groups, out readerNames);

    /// <summary>Synchronously queries or waits for changes to the supplied native reader states.</summary>
    /// <remarks>
    ///     Native code updates the array entries. The caller retains the context, array and any unmanaged
    ///     reader-name storage until the call returns. The native timeout is not a managed resource-drain guarantee.
    /// </remarks>
    /// <param name="context">The borrowed context associated with the wait.</param>
    /// <param name="timeout">The native timeout in milliseconds; zero requests an immediate status query.</param>
    /// <param name="readerStates">The input states and destination for native event-state and ATR updates.</param>
    /// <param name="readerStatesCount">The number of array entries supplied to native code.</param>
    /// <returns>The native status-change result, including timeout or cancellation status.</returns>
    public uint SCardGetStatusChange(
        SCardContext context,
        int timeout,
        SCARD_READER_STATE[] readerStates,
        int readerStatesCount) =>
        NativeMethods.SCardGetStatusChange(context, timeout, readerStates, readerStatesCount);

    /// <summary>Forwards a native cancellation request for outstanding waits associated with the context.</summary>
    /// <remarks>
    ///     This request does not join a listener or prove that a native call has returned. The caller must observe
    ///     completion before releasing borrowed state; it is not the cancellation mechanism for connection-owner calls.
    /// </remarks>
    /// <param name="context">The borrowed resource-manager context whose waits are to be canceled.</param>
    /// <returns>The native cancellation-request status code.</returns>
    public uint SCardCancel(SCardContext context) => NativeMethods.SCardCancel(context);
}