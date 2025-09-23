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

using System.Runtime.InteropServices;
using System.Text;
using Yubico.YubiKit.Core.Core.Buffers;

namespace Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;

internal static partial class NativeMethods
{
    [LibraryImport(Libraries.NativeShims, EntryPoint = "Native_SCardBeginTransaction")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial uint SCardBeginTransaction(SCardCardHandle cardHandle);

    [LibraryImport(Libraries.NativeShims, EntryPoint = "Native_SCardCancel")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial uint SCardCancel(SCardContext context);

    [LibraryImport(Libraries.NativeShims, EntryPoint = "Native_SCardConnect",
        StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial uint SCardConnect(
        SCardContext context,
        string readerName,
        SCARD_SHARE shareMode,
        SCARD_PROTOCOL preferredProtocols,
        out SCardCardHandle cardHandle,
        out SCARD_PROTOCOL activeProtocol
    );

    [LibraryImport(Libraries.NativeShims, EntryPoint = "Native_SCardDisconnect")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial uint SCardDisconnect(
        IntPtr cardHandle,
        SCARD_DISPOSITION disposition
    );

    [LibraryImport(Libraries.NativeShims, EntryPoint = "Native_SCardEndTransaction")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial uint SCardEndTransaction(
        SCardCardHandle cardHandle,
        SCARD_DISPOSITION disposition
    );

    [LibraryImport(Libraries.NativeShims, EntryPoint = "Native_SCardEstablishContext")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial uint SCardEstablishContext(
        SCARD_SCOPE scope,
        out SCardContext context
    );

    // https://claude.ai/chat/a74ac3c8-f0fd-4482-ab6f-fe194b4831c2
    // Make into blittable, with LibraryImport
    [LibraryImport(Libraries.NativeShims, EntryPoint = "Native_SCardGetStatusChange")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial uint SCardGetStatusChange(
        SCardContext context,
        int timeout,
        [In] [Out] SCARD_READER_STATE[] readerStates,
        int readerStatesCount
    );

    [LibraryImport(Libraries.NativeShims, EntryPoint = "Native_SCardListReaders")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial uint SCardListReaders(
        SCardContext context,
        byte[]? groups,
        byte[]? readerNames,
        ref int readerNamesLength
    );

    public static uint SCardListReaders(
        SCardContext context,
        string[]? groups,
        out string[] readerNames)
    {
        readerNames = Array.Empty<string>();
        byte[]? rawGroups = null;

        if (groups is not null) rawGroups = MultiString.GetBytes(groups, Encoding.ASCII);

        int readerNamesLength = 0;
        uint result = SCardListReaders(
            context,
            rawGroups,
            null,
            ref readerNamesLength);

        if (result != ErrorCode.SCARD_S_SUCCESS) return result;

        if (readerNamesLength == 0)
            throw new PlatformApiException("ExceptionMessages.SCardListReadersUnexpectedLength");

        byte[] rawReaderNames = new byte[readerNamesLength];
        result = SCardListReaders(
            context,
            rawGroups,
            rawReaderNames,
            ref readerNamesLength);

        readerNames = MultiString.GetStrings(rawReaderNames, Encoding.ASCII);

        return result;
    }

    [DllImport(Libraries.NativeShims, EntryPoint = "Native_SCardReconnect", ExactSpelling = true,
        CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static extern uint SCardReconnect(
        SCardCardHandle cardHandle,
        SCARD_SHARE shareMode,
        SCARD_PROTOCOL preferredProtocols,
        SCARD_DISPOSITION initialization,
        out SCARD_PROTOCOL activeProtocol
    );

    [DllImport(Libraries.NativeShims, EntryPoint = "Native_SCardReleaseContext", ExactSpelling = true,
        CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static extern uint SCardReleaseContext(IntPtr context);

    [DllImport(Libraries.NativeShims, EntryPoint = "Native_SCardTransmit", ExactSpelling = true,
        CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern uint SCardTransmit(
        SCardCardHandle cardHandle,
        ref SCARD_IO_REQUEST ioSendPci,
        IntPtr sendBuffer,
        int sendLength,
        IntPtr ioRecvPci,
        IntPtr recvBuffer,
        ref int recvLength
    );
}