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
using Yubico.Core;
using Yubico.Core.Buffers;

namespace Yubico.PlatformInterop
{
    internal class SCard : ISCard, IDisposable
    {
        private static readonly Lazy<SCard> _instance = new Lazy<SCard>(() => new SCard());
        readonly UnmanagedDynamicLibrary _scardLib;

        public static SCard Instance => _instance.Value;

        private SCard()
        {
            _scardLib = LoadSCardLibrary();

            _scardLib.GetFunction("SCardBeginTransaction", out _beginTransaction);
            _scardLib.GetFunction("SCardCancel", out _cancel);
            _scardLib.GetFunction("SCardConnect", out _connect);
            _scardLib.GetFunction("SCardDisconnect", out _disconnect);
            _scardLib.GetFunction("SCardEndTransaction", out _endTransaction);
            _scardLib.GetFunction("SCardEstablishContext", out _establishContext);
            _scardLib.GetFunction("SCardGetStatusChange", out _getStatusChange);
            _scardLib.GetFunction("SCardListReaders", out _listReaders);
            _scardLib.GetFunction("SCardReconnect", out _reconnect);
            _scardLib.GetFunction("SCardReleaseContext", out _releaseContext);
            _scardLib.GetFunction("SCardStatus", out _status);
            _scardLib.GetFunction("SCardTransmit", out _transmit);
        }

        private static UnmanagedDynamicLibrary LoadSCardLibrary()
        {
            string libraryName = SdkPlatformInfo.OperatingSystem switch
            {
                SdkPlatform.Windows => "WinSCard.dll",
                SdkPlatform.MacOS => "PCSC.framework/PCSC",
                SdkPlatform.Linux => "libpcsclite.so",
                _ => throw new PlatformNotSupportedException()
            };

            return UnmanagedDynamicLibrary.Open(libraryName);
        }

        #region Wrappers

        public uint Connect(
            SCardContext context,
            string readerName,
            SCARD_SHARE shareMode,
            SCARD_PROTOCOL preferredProtocols,
            out SCardCardHandle cardHandle,
            out SCARD_PROTOCOL activeProtocol
            )
        {
            uint result = Connect(
                context,
                readerName,
                shareMode,
                preferredProtocols,
                out IntPtr hCard,
                out activeProtocol);
            cardHandle = new SCardCardHandle(hCard);
            return result;
        }

        public uint EstablishContext(
            SCARD_SCOPE scope,
            out SCardContext context
            )
        {
            uint result = EstablishContext(scope, out IntPtr hContext);
            context = new SCardContext(hContext);
            return result;
        }

        public uint ListReaders(
            SCardContext context,
            string[]? groups,
            out string[] readerNames
            )
        {
            readerNames = Array.Empty<string>();

            byte[]? rawGroups = null;

            if (!(groups is null))
            {
                rawGroups = MultiString.GetBytes(groups, SdkPlatformInfo.Encoding);
            }

            int readerNamesLength = 0;

            uint result = ListReaders(
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

                byte[] rawReaderNames = new byte[readerNamesLength * SdkPlatformInfo.CharSize];

                result = ListReaders(
                    context,
                    rawGroups,
                    rawReaderNames,
                    ref readerNamesLength);

                readerNames = MultiString.GetStrings(rawReaderNames, SdkPlatformInfo.Encoding);
            }

            return result;
        }

        public uint Status(
            SCardCardHandle card,
            out string[] readerNames,
            out SCARD_STATUS status,
            out SCARD_PROTOCOL protocol,
            out byte[]? atr
            )
        {
            int readerNameLength = 0;
            int atrLength = 0;

            // Get the lengths for the reader names and ATR first
            uint result = Status(
                card,
                null,
                ref readerNameLength,
                out _,
                out _,
                null,
                ref atrLength);

            if (result == ErrorCode.SCARD_S_SUCCESS)
            {
                byte[] rawReaderNames = new byte[readerNameLength];
                byte[] atrBuffer = new byte[atrLength];

                // Now we get the actual values
                result = Status(
                    card,
                    rawReaderNames,
                    ref readerNameLength,
                    out status,
                    out protocol,
                    atrBuffer,
                    ref atrLength);

                if (result == ErrorCode.SCARD_S_SUCCESS)
                {
                    readerNames = MultiString.GetStrings(rawReaderNames, SdkPlatformInfo.Encoding);

                    atr = atrBuffer;
                    return result;
                }
            }

            readerNames = Array.Empty<string>();
            status = SCARD_STATUS.UNKNOWN;
            protocol = SCARD_PROTOCOL.Undefined;
            atr = null;
            return result;
        }

        public unsafe uint Transmit(
            SCardCardHandle card,
            SCARD_IO_REQUEST ioSendPci,
            ReadOnlySpan<byte> sendBuffer,
            IntPtr ioRecvPci,
            Span<byte> recvBuffer,
            out int bytesReceived
            )
        {
            fixed (byte* sendBufferPtr = sendBuffer)
            fixed (byte* recvBufferPtr = recvBuffer)
            {
                int recvBufferSize = recvBuffer.Length;

                uint result = Transmit(
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

        #endregion

        #region Delegate binding

        private readonly SCardDelegates.BeginTransation _beginTransaction;
        private readonly SCardDelegates.Cancel _cancel;
        private readonly SCardDelegates.Connect _connect;
        private readonly SCardDelegates.Disconnect _disconnect;
        private readonly SCardDelegates.EndTransaction _endTransaction;
        private readonly SCardDelegates.EstablishContext _establishContext;
        private readonly SCardDelegates.GetStatusChange _getStatusChange;
        private readonly SCardDelegates.ListReaders _listReaders;
        private readonly SCardDelegates.Reconnect _reconnect;
        private readonly SCardDelegates.ReleaseContext _releaseContext;
        private readonly SCardDelegates.Status _status;
        private readonly SCardDelegates.Transmit _transmit;
        private bool disposedValue;

        public uint BeginTransaction(SCardCardHandle cardHandle) =>
            _beginTransaction(cardHandle);

        public uint Cancel(SCardContext context) =>
            _cancel(context);

        public uint Connect(
            SCardContext context,
            string readerName,
            SCARD_SHARE shareMode,
            SCARD_PROTOCOL preferredProtocols,
            out IntPtr cardHandle,
            out SCARD_PROTOCOL activeProtocol) =>
            _connect(
                context,
                SdkPlatformInfo.Encoding.GetBytes(readerName),
                shareMode,
                preferredProtocols,
                out cardHandle,
                out activeProtocol);

        public uint Disconnect(IntPtr cardHandle, SCARD_DISPOSITION disposition) =>
            _disconnect(cardHandle, disposition);

        public uint EndTransaction(SCardCardHandle cardHandle, SCARD_DISPOSITION disposition) =>
            _endTransaction(cardHandle, disposition);

        public uint EstablishContext(
            SCARD_SCOPE scope,
            out IntPtr context) =>
            _establishContext(scope, IntPtr.Zero, IntPtr.Zero, out context);

        public uint GetStatusChange(
            SCardContext context,
            int timeout,
            SCardReaderStates readerStates) =>
            _getStatusChange(context, timeout, readerStates.Buffer, readerStates.Count);

        public uint ListReaders(
            SCardContext context,
            byte[]? groups,
            byte[]? readerNames,
            ref int readerNamesLength) =>
            _listReaders(context, groups, readerNames, ref readerNamesLength);

        public uint Reconnect(
            SCardCardHandle cardHandle,
            SCARD_SHARE shareMode,
            SCARD_PROTOCOL preferredProtocols,
            SCARD_DISPOSITION initialization,
            out SCARD_PROTOCOL activeProtocol) =>
            _reconnect(cardHandle, shareMode, preferredProtocols, initialization, out activeProtocol);

        public uint ReleaseContext(
            IntPtr context) =>
            _releaseContext(context);

        public uint Status(
            SCardCardHandle card,
            byte[]? readerNames,
            ref int readerNamesLength,
            out SCARD_STATUS status,
            out SCARD_PROTOCOL protocol,
            byte[]? atr,
            ref int atrLength) =>
            _status(
                card,
                readerNames,
                ref readerNamesLength,
                out status,
                out protocol,
                atr,
                ref atrLength);

        public uint Transmit(
            SCardCardHandle card,
            ref SCARD_IO_REQUEST ioSendPci,
            IntPtr sendBuffer,
            int sendLength,
            IntPtr ioRecvPci,
            IntPtr recvBuffer,
            ref int recvLength) =>
            _transmit(
                card,
                ref ioSendPci,
                sendBuffer,
                sendLength,
                ioRecvPci,
                recvBuffer,
                ref recvLength);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _scardLib.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
