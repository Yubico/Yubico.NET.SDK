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

using Yubico.YubiKit.Core.Native.Desktop.SCard;

namespace Yubico.YubiKit.Core.Transports.SmartCard;

/// <summary>Forwards connection operations to the native PC/SC shim.</summary>
/// <remarks>
///     Calls run synchronously on the caller's thread and return native status codes without translating failures.
///     Construction acquires no native resources. This seam supplies no worker, admission guard, cancellation
///     or resource-lifetime authority.
///     <see cref="PcscConnectionNativeOwner" /> schedules calls and <see cref="PcscNativeResources" /> owns the
///     returned handles and checks release results. Borrowed handles and buffers must remain valid throughout each call.
/// </remarks>
internal sealed class NativeSCardConnectionApi : ISCardConnectionApi
{
    /// <summary>Establishes a PC/SC resource-manager context for the requested scope.</summary>
    /// <param name="scope">The native resource-manager scope.</param>
    /// <param name="context">The returned context wrapper, whose lifetime belongs to the caller.</param>
    /// <returns>The native status code; inspect it before using the context.</returns>
    public uint EstablishContext(SCARD_SCOPE scope, out SCardContext context) =>
        NativeMethods.SCardEstablishContext(scope, out context);

    /// <summary>Opens the named reader using the supplied sharing and protocol preferences.</summary>
    /// <param name="context">The borrowed resource-manager context.</param>
    /// <param name="readerName">The native reader name.</param>
    /// <param name="shareMode">The requested native sharing mode; this method performs no fallback.</param>
    /// <param name="preferredProtocols">The protocols allowed during negotiation.</param>
    /// <param name="cardHandle">The returned card wrapper, whose lifetime belongs to the caller.</param>
    /// <param name="activeProtocol">The protocol negotiated on success.</param>
    /// <returns>The native connection status code.</returns>
    public uint Connect(
        SCardContext context,
        string readerName,
        SCARD_SHARE shareMode,
        SCARD_PROTOCOL preferredProtocols,
        out SCardCardHandle cardHandle,
        out SCARD_PROTOCOL activeProtocol) =>
        NativeMethods.SCardConnect(
            context,
            readerName,
            shareMode,
            preferredProtocols,
            out cardHandle,
            out activeProtocol);

    /// <summary>Synchronously requests a native transaction on the borrowed card handle.</summary>
    /// <remarks>Native transaction acquisition may block; this seam provides no cancellation token or wait bound.</remarks>
    /// <returns>The native transaction-acquisition status code.</returns>
    public uint BeginTransaction(SCardCardHandle cardHandle) =>
        NativeMethods.SCardBeginTransaction(cardHandle);

    /// <summary>Synchronously ends a native transaction with the requested card disposition.</summary>
    /// <param name="cardHandle">The borrowed card handle.</param>
    /// <param name="disposition">The native action to apply to the card when ending the transaction.</param>
    /// <returns>The native transaction-end status code.</returns>
    public uint EndTransaction(SCardCardHandle cardHandle, SCARD_DISPOSITION disposition) =>
        NativeMethods.SCardEndTransaction(cardHandle, disposition);

    /// <summary>Synchronously transmits borrowed command bytes and writes the native response into borrowed storage.</summary>
    /// <remarks>
    ///     The native wrapper pins both spans for the call and requests no receive protocol-control information.
    ///     Neither buffer is copied or cleared here; their storage must remain valid until this method returns.
    /// </remarks>
    /// <param name="cardHandle">The borrowed connected card handle.</param>
    /// <param name="sendPci">The protocol-control information for the outgoing command.</param>
    /// <param name="sendBuffer">The caller-owned command bytes.</param>
    /// <param name="receiveBuffer">The caller-owned destination and its maximum receive capacity.</param>
    /// <param name="bytesReceived">The receive length reported by native code; interpret it with the status code.</param>
    /// <returns>The native transmission status code.</returns>
    public uint Transmit(
        SCardCardHandle cardHandle,
        SCARD_IO_REQUEST sendPci,
        ReadOnlySpan<byte> sendBuffer,
        Span<byte> receiveBuffer,
        out int bytesReceived) =>
        NativeMethods.SCardTransmit(
            cardHandle,
            sendPci,
            sendBuffer,
            nint.Zero,
            receiveBuffer,
            out bytesReceived);

    /// <summary>Attempts native disconnection using the wrapper's raw card handle.</summary>
    /// <remarks>
    ///     The caller must exclude concurrent use and keep the wrapper alive. This method does not invalidate or
    ///     dispose it; the resource owner must check the result before marking the handle released.
    /// </remarks>
    /// <param name="cardHandle">The card wrapper whose native handle is to be disconnected.</param>
    /// <param name="disposition">The requested card disposition on disconnection.</param>
    /// <returns>The native disconnection status code, not unconditional release proof.</returns>
    public uint Disconnect(SCardCardHandle cardHandle, SCARD_DISPOSITION disposition) =>
        NativeMethods.SCardDisconnect(cardHandle.DangerousGetHandle(), disposition);

    /// <summary>Attempts native release using the wrapper's raw resource-manager context.</summary>
    /// <remarks>
    ///     The caller must exclude concurrent use and keep the wrapper alive. This method does not invalidate or
    ///     dispose it; the resource owner must check the result before marking the context released.
    /// </remarks>
    /// <returns>The native context-release status code, not unconditional release proof.</returns>
    public uint ReleaseContext(SCardContext context) =>
        NativeMethods.SCardReleaseContext(context.DangerousGetHandle());
}