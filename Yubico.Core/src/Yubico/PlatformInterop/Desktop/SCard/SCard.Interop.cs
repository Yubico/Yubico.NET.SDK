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
using Yubico.Core;
using Yubico.Core.Buffers;

namespace Yubico.PlatformInterop
{
    internal static partial class NativeMethods
    {
        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_SCardBeginTransaction", ExactSpelling = true,
            CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern uint SCardBeginTransaction(SCardCardHandle cardHandle);

        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_SCardCancel", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern uint SCardCancel(SCardContext context);

        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_SCardConnect", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern uint SCardConnect(
            SCardContext context,
            string readerName,
            SCARD_SHARE shareMode,
            SCARD_PROTOCOL preferredProtocols,
            out SCardCardHandle cardHandle,
            out SCARD_PROTOCOL activeProtocol);

        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_SCardDisconnect", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern uint SCardDisconnect(
            IntPtr cardHandle,
            SCARD_DISPOSITION disposition);

        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_SCardEndTransaction", ExactSpelling = true,
            CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern uint SCardEndTransaction(
            SCardCardHandle cardHandle,
            SCARD_DISPOSITION disposition);

        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_SCardEstablishContext", ExactSpelling = true,
            CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern uint SCardEstablishContext(
            SCARD_SCOPE scope,
            out SCardContext context);

        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_SCardGetStatusChange", ExactSpelling = true,
            CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern uint SCardGetStatusChange(
            SCardContext context,
            int timeout,
            [In, Out] SCARD_READER_STATE[] readerStates,
            int readerStatesCount);

        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_SCardListReaders", ExactSpelling = true,
            CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern uint SCardListReaders(
            SCardContext context,
            byte[]? groups,
            byte[]? readerNames,
            ref int readerNamesLength);

        public static uint SCardListReaders(
            SCardContext context,
            string[]? groups,
            out string[] readerNames)
        {
            readerNames = Array.Empty<string>();

            byte[]? rawGroups = null;

            if (!(groups is null))
            {
                rawGroups = MultiString.GetBytes(groups, System.Text.Encoding.ASCII);
            }

            int readerNamesLength = 0;

            uint result = SCardListReaders(
                context,
                rawGroups,
                null,
                ref readerNamesLength);

            if (result == ErrorCode.SCARD_S_SUCCESS)
            {
                if (readerNamesLength == 0)
                {
                    throw new PlatformApiException(ExceptionMessages.SCardListReadersUnexpectedLength);
                }

                byte[] rawReaderNames = new byte[readerNamesLength];

                result = SCardListReaders(
                    context,
                    rawGroups,
                    rawReaderNames,
                    ref readerNamesLength);

                readerNames = MultiString.GetStrings(rawReaderNames, System.Text.Encoding.ASCII);
            }

            return result;
        }

        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_SCardReconnect", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern uint SCardReconnect(
            SCardCardHandle cardHandle,
            SCARD_SHARE shareMode,
            SCARD_PROTOCOL preferredProtocols,
            SCARD_DISPOSITION initialization,
            out SCARD_PROTOCOL activeProtocol);

        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_SCardReleaseContext", ExactSpelling = true,
            CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern uint SCardReleaseContext(IntPtr context);

        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_SCardTransmit", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern uint SCardTransmit(
            SCardCardHandle cardHandle,
            ref SCARD_IO_REQUEST ioSendPci,
            IntPtr sendBuffer,
            int sendLength,
            IntPtr ioRecvPci,
            IntPtr recvBuffer,
            ref int recvLength);

        public static unsafe uint SCardTransmit(
            SCardCardHandle card,
            SCARD_IO_REQUEST ioSendPci,
            ReadOnlySpan<byte> sendBuffer,
            IntPtr ioRecvPci,
            Span<byte> recvBuffer,
            out int bytesReceived)
        {
            fixed (byte* sendBufferPtr = sendBuffer)
            fixed (byte* recvBufferPtr = recvBuffer)
            {
                int recvBufferSize = recvBuffer.Length;

                uint result = SCardTransmit(
                    card,
                    ref ioSendPci,
                    (IntPtr)sendBufferPtr,
                    sendBuffer.Length,
                    ioRecvPci,
                    (IntPtr)recvBufferPtr,
                    ref recvBufferSize);

                bytesReceived = recvBufferSize;
                return result;
            }
        }
    }
}
