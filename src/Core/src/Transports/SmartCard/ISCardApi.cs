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

internal interface ISCardApi
{
    uint SCardEstablishContext(SCARD_SCOPE scope, out SCardContext context);

    uint SCardListReaders(SCardContext context, string[]? groups, out string[] readerNames);

    uint SCardGetStatusChange(
        SCardContext context,
        int timeout,
        SCARD_READER_STATE[] readerStates,
        int readerStatesCount);

    uint SCardCancel(SCardContext context);
}

internal sealed class NativeSCardApi : ISCardApi
{
    public static NativeSCardApi Instance { get; } = new();

    private NativeSCardApi()
    {
    }

    public uint SCardEstablishContext(SCARD_SCOPE scope, out SCardContext context) =>
        NativeMethods.SCardEstablishContext(scope, out context);

    public uint SCardListReaders(SCardContext context, string[]? groups, out string[] readerNames) =>
        NativeMethods.SCardListReaders(context, groups, out readerNames);

    public uint SCardGetStatusChange(
        SCardContext context,
        int timeout,
        SCARD_READER_STATE[] readerStates,
        int readerStatesCount) =>
        NativeMethods.SCardGetStatusChange(context, timeout, readerStates, readerStatesCount);

    public uint SCardCancel(SCardContext context) => NativeMethods.SCardCancel(context);
}