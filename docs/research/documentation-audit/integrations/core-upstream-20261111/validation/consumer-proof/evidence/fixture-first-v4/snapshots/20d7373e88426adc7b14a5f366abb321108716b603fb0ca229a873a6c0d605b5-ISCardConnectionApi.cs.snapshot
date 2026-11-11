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

internal interface ISCardConnectionApi
{
    uint EstablishContext(SCARD_SCOPE scope, out SCardContext context);

    uint Connect(
        SCardContext context,
        string readerName,
        SCARD_SHARE shareMode,
        SCARD_PROTOCOL preferredProtocols,
        out SCardCardHandle cardHandle,
        out SCARD_PROTOCOL activeProtocol);

    uint BeginTransaction(SCardCardHandle cardHandle);

    uint EndTransaction(SCardCardHandle cardHandle, SCARD_DISPOSITION disposition);

    uint Transmit(
        SCardCardHandle cardHandle,
        SCARD_IO_REQUEST sendPci,
        ReadOnlySpan<byte> sendBuffer,
        Span<byte> receiveBuffer,
        out int bytesReceived);

    uint Disconnect(SCardCardHandle cardHandle, SCARD_DISPOSITION disposition);

    uint ReleaseContext(SCardContext context);
}

internal sealed class NativeSCardConnectionApi : ISCardConnectionApi
{
    public uint EstablishContext(SCARD_SCOPE scope, out SCardContext context) =>
        NativeMethods.SCardEstablishContext(scope, out context);

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

    public uint BeginTransaction(SCardCardHandle cardHandle) =>
        NativeMethods.SCardBeginTransaction(cardHandle);

    public uint EndTransaction(SCardCardHandle cardHandle, SCARD_DISPOSITION disposition) =>
        NativeMethods.SCardEndTransaction(cardHandle, disposition);

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

    public uint Disconnect(SCardCardHandle cardHandle, SCARD_DISPOSITION disposition) =>
        NativeMethods.SCardDisconnect(cardHandle.DangerousGetHandle(), disposition);

    public uint ReleaseContext(SCardContext context) =>
        NativeMethods.SCardReleaseContext(context.DangerousGetHandle());
}