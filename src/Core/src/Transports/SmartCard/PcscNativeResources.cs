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

    private readonly SmartCardSharing _sharing;

    public PcscNativeResources(string readerName, ISCardConnectionApi api, SmartCardSharing sharing = SmartCardSharing.ExclusiveWithSharedFallback)
    {
        _readerName = readerName;
        _api = api;
        _sharing = sharing;
    }

    /// <summary>True when the handle was opened with SCARD_SHARE_EXCLUSIVE.</summary>
    public bool IsExclusive { get; private set; }

    public void Open()
    {
        var result = _api.EstablishContext(SCARD_SCOPE.USER, out var context);
        _context = context;
        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new SCardException("ExceptionMessages.SCardCantEstablish", result);

        // ykman (pcsc/__init__.py L133-144): exclusive first, shared when another client holds the card.
        // Exclusive access keeps card-wide security state (the PIV PIN, SP 800-73-5 §2.4.2) out of other
        // local processes' reach while this connection is open.
        SCardCardHandle cardHandle;
        SCARD_PROTOCOL activeProtocol;
        if (_sharing is SmartCardSharing.ExclusiveWithSharedFallback or SmartCardSharing.Exclusive)
        {
            result = _api.Connect(context, _readerName, SCARD_SHARE.EXCLUSIVE, SCARD_PROTOCOL.Tx, out cardHandle, out activeProtocol);
            if (result == ErrorCode.SCARD_S_SUCCESS)
            {
                IsExclusive = true;
            }
            else if (result == ErrorCode.SCARD_E_SHARING_VIOLATION && _sharing == SmartCardSharing.Exclusive)
            {
                throw new Devices.ConnectionInUseException(
                    $"Another smart card client holds reader '{_readerName}' and SmartCardSharing.Exclusive does not fall back to shared access.",
                    new SCardException("SCARD_E_SHARING_VIOLATION", result));
            }
            else if (result == ErrorCode.SCARD_E_SHARING_VIOLATION)
            {
                result = _api.Connect(context, _readerName, SCARD_SHARE.SHARED, SCARD_PROTOCOL.Tx, out cardHandle, out activeProtocol);
            }
        }
        else
        {
            result = _api.Connect(context, _readerName, SCARD_SHARE.SHARED, SCARD_PROTOCOL.Tx, out cardHandle, out activeProtocol);
        }

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
                // pyscard's default disposition (PCSCCardConnection.py L154-155): power the card off so card-wide
                // security state (a verified PIV PIN, SP 800-73-5 §2.4.2) cannot outlive this connection.
                result = _api.Disconnect(cardHandle, SCARD_DISPOSITION.UNPOWER_CARD);
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