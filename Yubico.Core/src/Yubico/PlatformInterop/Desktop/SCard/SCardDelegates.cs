// Copyright 2021 Yubico AB
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

using System;
using System.Runtime.InteropServices;

namespace Yubico.PlatformInterop
{
    internal class SCardDelegates
    {
        public delegate uint BeginTransation(SCardCardHandle cardHandle);

        public delegate uint Cancel(SCardContext context);

        public delegate uint Connect(
            SCardContext context,
            byte[] readerName,
            SCARD_SHARE shareMode,
            SCARD_PROTOCOL preferredProtocols,
            out IntPtr cardHandle,
            out SCARD_PROTOCOL activeProtocol
            );

        public delegate uint Disconnect(IntPtr cardHandle, SCARD_DISPOSITION disposition);

        public delegate uint EndTransaction(SCardCardHandle cardHandle, SCARD_DISPOSITION disposition);

        public delegate uint EstablishContext(
            SCARD_SCOPE scope,
            IntPtr mustBeZero1,
            IntPtr mustBeZero2,
            out IntPtr context);

        public delegate uint GetStatusChange(
            SCardContext context,
            int timeout,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1)]
            byte[] readerStates,
            int readerStatesCount);

        public delegate uint ListReaders(
            SCardContext context,
            byte[]? groups,
            byte[]? readerNames,
            ref int readerNamesLength);

        public delegate uint Reconnect(
            SCardCardHandle cardHandle,
            SCARD_SHARE shareMode,
            SCARD_PROTOCOL preferredProtocols,
            SCARD_DISPOSITION initialization,
            out SCARD_PROTOCOL activeProtocol);

        public delegate uint ReleaseContext(IntPtr context);

        public delegate uint Status(
            SCardCardHandle cardHandle,
            byte[]? readerNames,
            ref int readerNamesLength,
            out SCARD_STATUS status,
            out SCARD_PROTOCOL protocol,
            byte[]? atr,
            ref int atrLength);

        public delegate uint Transmit(
            SCardCardHandle cardHandle,
            ref SCARD_IO_REQUEST ioSendPci,
            IntPtr sendBuffer,
            int sendLength,
            IntPtr ioRecvPci,
            IntPtr recvBuffer,
            ref int recvLength);
    }
}
