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

using System.Globalization;
using Yubico.YubiKit.Core.Native.Desktop.SCard;

namespace Yubico.YubiKit.Core.Transports.SmartCard;

/// <summary>Owns one connection's synchronous PC/SC resources and native calls.</summary>
/// <remarks>
///     This type provides no scheduling or synchronization. <see cref="PcscConnectionNativeState" /> confines every
///     call to its owned worker and does not publish completion until the native borrow has ended.
/// </remarks>
internal sealed class PcscNativeResources
{
    private readonly string _readerName;
    private readonly ISCardConnectionApi _api;
    private SCardContext? _context;
    private SCardCardHandle? _cardHandle;
    private SCARD_PROTOCOL? _protocol;

    private SCARD_PROTOCOL Protocol => _protocol
        ?? throw new InvalidOperationException("The smart-card connection is not initialized.");

    private SCardCardHandle CardHandle => _cardHandle
        ?? throw new InvalidOperationException("The smart-card connection is not initialized.");

    public PcscNativeResources(string readerName, ISCardConnectionApi api)
    {
        _readerName = readerName;
        _api = api;
    }

    public void Open()
    {
        var result = _api.EstablishContext(SCARD_SCOPE.USER, out var context);
        _context = context;
        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new SCardException("ExceptionMessages.SCardCantEstablish", result);

        var shareMode = SCARD_SHARE.SHARED;
        if (AppContext.TryGetSwitch(CoreCompatSwitches.OpenSmartCardHandlesExclusively, out var enabled) && enabled)
            shareMode = SCARD_SHARE.EXCLUSIVE;

        result = _api.Connect(
            context,
            _readerName,
            shareMode,
            SCARD_PROTOCOL.Tx,
            out var cardHandle,
            out var activeProtocol);
        _cardHandle = cardHandle;
        if (result != ErrorCode.SCARD_S_SUCCESS)
        {
            throw new SCardException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "ExceptionMessages.SCardCardCantConnect {0}",
                    _readerName),
                result);
        }

        _protocol = activeProtocol;
    }

    public int Transmit(ReadOnlySpan<byte> command, Span<byte> response)
    {
        var result = _api.Transmit(
            CardHandle,
            new SCARD_IO_REQUEST(Protocol),
            command,
            response,
            out var bytesReceived);
        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new SCardException("ExceptionMessages.SCardTransmitFailure", result);

        return bytesReceived;
    }

    public uint BeginTransaction() => _api.BeginTransaction(CardHandle);

    public uint EndTransaction(SCARD_DISPOSITION disposition) =>
        _cardHandle is null || _cardHandle.IsInvalid
            ? ErrorCode.SCARD_S_SUCCESS
            : _api.EndTransaction(_cardHandle, disposition);

    public bool TryRelease(out Exception? cleanupError)
    {
        cleanupError = null;
        if (_cardHandle is { IsInvalid: false } cardHandle)
        {
            uint result;
            try
            {
                result = _api.Disconnect(cardHandle, SCARD_DISPOSITION.LEAVE_CARD);
            }
            catch (Exception ex)
            {
                cleanupError = ex;
                return false;
            }

            if (result != ErrorCode.SCARD_S_SUCCESS)
            {
                cleanupError = new SCardException("ExceptionMessages.SCardDisconnectFailure", result);
                return false;
            }

            cardHandle.SetHandleAsInvalid();
            cardHandle.Dispose();
        }

        if (_context is { IsInvalid: false } context)
        {
            uint result;
            try
            {
                result = _api.ReleaseContext(context);
            }
            catch (Exception ex)
            {
                cleanupError = ex;
                return false;
            }

            if (result != ErrorCode.SCARD_S_SUCCESS)
            {
                cleanupError = new SCardException("ExceptionMessages.SCardReleaseContextFailure", result);
                return false;
            }

            context.SetHandleAsInvalid();
            context.Dispose();
        }

        _protocol = null;
        return true;
    }
}