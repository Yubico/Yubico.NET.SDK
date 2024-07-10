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

namespace Yubico.PlatformInterop
{
    /// <summary>
    ///     Smart card function return values
    /// </summary>
    internal class ErrorCode
    {
        /// <summary>
        ///     The client attempted a smart card operation in a remote session, such as a client
        ///     session running on a terminal server, and the operating system in use does not support
        ///     smart card redirection.
        /// </summary>
        public const uint ERROR_BROKEN_PIPE = 0x00000109;

        /// <summary>
        ///     An error occurred in setting the smart card file object pointer.
        /// </summary>
        public const uint SCARD_E_BAD_SEEK = 0x80100029;

        /// <summary>
        ///     The action was canceled by an SCardCancel request.
        /// </summary>
        public const uint SCARD_E_CANCELLED = 0x80100002;

        /// <summary>
        ///     The system could not dispose of the media in the requested manner.
        /// </summary>
        public const uint SCARD_E_CANT_DISPOSE = 0x8010000E;

        /// <summary>
        ///     The smart card does not meet minimal requirements for support.
        /// </summary>
        public const uint SCARD_E_CARD_UNSUPPORTED = 0x8010001C;

        /// <summary>
        ///     The requested certificate could not be obtained.
        /// </summary>
        public const uint SCARD_E_CERTIFICATE_UNAVAILABLE = 0x8010002D;

        /// <summary>
        ///     A communications error with the smart card has been detected.
        /// </summary>
        public const uint SCARD_E_COMM_DATA_LOST = 0x8010002F;

        /// <summary>
        ///     The specified directory does not exist in the smart card.
        /// </summary>
        public const uint SCARD_E_DIR_NOT_FOUND = 0x80100023;

        /// <summary>
        ///     The reader driver did not produce a unique reader name.
        /// </summary>
        public const uint SCARD_E_DUPLICATE_READER = 0x8010001B;

        /// <summary>
        ///     The specified file does not exist in the smart card.
        /// </summary>
        public const uint SCARD_E_FILE_NOT_FOUND = 0x80100024;

        /// <summary>
        ///     The requested order of object creation is not supported.
        /// </summary>
        public const uint SCARD_E_ICC_CREATEORDER = 0x80100021;

        /// <summary>
        ///     No primary provider can be found for the smart card.
        /// </summary>
        public const uint SCARD_E_ICC_INSTALLATION = 0x80100020;

        /// <summary>
        ///     The data buffer for returned data is too small for the returned data.
        /// </summary>
        public const uint SCARD_E_INSUFFICIENT_BUFFER = 0x80100008;

        /// <summary>
        ///     An ATR string obtained from the registry is not a valid ATR string.
        /// </summary>
        public const uint SCARD_E_INVALID_ATR = 0x80100015;

        /// <summary>
        ///     The supplied PIN is incorrect.
        /// </summary>
        public const uint SCARD_E_INVALID_CHV = 0x8010002A;

        /// <summary>
        ///     The supplied handle was not valid.
        /// </summary>
        public const uint SCARD_E_INVALID_HANDLE = 0x80100003;

        /// <summary>
        ///     One or more of the supplied parameters could not be properly interpreted.
        /// </summary>
        public const uint SCARD_E_INVALID_PARAMETER = 0x80100004;

        /// <summary>
        ///     Registry startup information is missing or not valid.
        /// </summary>
        public const uint SCARD_E_INVALID_TARGET = 0x80100005;

        /// <summary>
        ///     One or more of the supplied parameter values could not be properly interpreted.
        /// </summary>
        public const uint SCARD_E_INVALID_VALUE = 0x80100011;

        /// <summary>
        ///     Access is denied to the file.
        /// </summary>
        public const uint SCARD_E_NO_ACCESS = 0x80100027;

        /// <summary>
        ///     The supplied path does not represent a smart card directory.
        /// </summary>
        public const uint SCARD_E_NO_DIR = 0x80100025;

        /// <summary>
        ///     The supplied path does not represent a smart card file.
        /// </summary>
        public const uint SCARD_E_NO_FILE = 0x80100026;

        /// <summary>
        ///     The requested key container does not exist on the smart card.
        /// </summary>
        public const uint SCARD_E_NO_KEY_CONTAINER = 0x80100030;

        /// <summary>
        ///     Not enough memory available to complete this command.
        /// </summary>
        public const uint SCARD_E_NO_MEMORY = 0x80100006;

        /// <summary>
        ///     The smart card PIN cannot be cached.
        /// </summary>
        public const uint SCARD_E_NO_PIN_CACHE = 0x80100033;

        /// <summary>
        ///     No smart card reader is available.
        /// </summary>
        public const uint SCARD_E_NO_READERS_AVAILABLE = 0x8010002E;

        /// <summary>
        ///     The smart card resource manager is not running.
        /// </summary>
        public const uint SCARD_E_NO_SERVICE = 0x8010001D;

        /// <summary>
        ///     The operation requires a smart card, but no smart card is currently in the device.
        /// </summary>
        public const uint SCARD_E_NO_SMARTCARD = 0x8010000C;

        /// <summary>
        ///     The requested certificate does not exist.
        /// </summary>
        public const uint SCARD_E_NO_SUCH_CERTIFICATE = 0x8010002C;

        /// <summary>
        ///     The reader or card is not ready to accept commands.
        /// </summary>
        public const uint SCARD_E_NOT_READY = 0x80100010;

        /// <summary>
        ///     An attempt was made to end a nonexistent transaction.
        /// </summary>
        public const uint SCARD_E_NOT_TRANSACTED = 0x80100016;

        /// <summary>
        ///     The PCI receive buffer was too small.
        /// </summary>
        public const uint SCARD_E_PCI_TOO_SMALL = 0x80100019;

        /// <summary>
        ///     The smart card PIN cache has expired.
        /// </summary>
        public const uint SCARD_E_PIN_CACHE_EXPIRED = 0x80100032;

        /// <summary>
        ///     The requested protocols are incompatible with the protocol currently in use with the
        ///     card.
        /// </summary>
        public const uint SCARD_E_PROTO_MISMATCH = 0x8010000F;

        /// <summary>
        ///     The smart card is read-only and cannot be written to.
        /// </summary>
        public const uint SCARD_E_READ_ONLY_CARD = 0x80100034;

        /// <summary>
        ///     The specified reader is not currently available for use.
        /// </summary>
        public const uint SCARD_E_READER_UNAVAILABLE = 0x80100017;

        /// <summary>
        ///     The reader driver does not meet minimal requirements for support.
        /// </summary>
        public const uint SCARD_E_READER_UNSUPPORTED = 0x8010001A;

        /// <summary>
        ///     The smart card resource manager is too busy to complete this operation.
        /// </summary>
        public const uint SCARD_E_SERVER_TOO_BUSY = 0x80100031;

        /// <summary>
        ///     The smart card resource manager has shut down.
        /// </summary>
        public const uint SCARD_E_SERVICE_STOPPED = 0x8010001E;

        /// <summary>
        ///     The smart card cannot be accessed because of other outstanding connections.
        /// </summary>
        public const uint SCARD_E_SHARING_VIOLATION = 0x8010000B;

        /// <summary>
        ///     The action was canceled by the system, presumably to log off or shut down.
        /// </summary>
        public const uint SCARD_E_SYSTEM_CANCELLED = 0x80100012;

        /// <summary>
        ///     The user-specified time-out value has expired.
        /// </summary>
        public const uint SCARD_E_TIMEOUT = 0x8010000A;

        /// <summary>
        ///     An unexpected card error has occurred.
        /// </summary>
        public const uint SCARD_E_UNEXPECTED = 0x8010001F;

        /// <summary>
        ///     The specified smart card name is not recognized.
        /// </summary>
        public const uint SCARD_E_UNKNOWN_CARD = 0x8010000D;

        /// <summary>
        ///     The specified reader name is not recognized.
        /// </summary>
        public const uint SCARD_E_UNKNOWN_READER = 0x80100009;

        /// <summary>
        ///     An unrecognized error code was returned.
        /// </summary>
        public const uint SCARD_E_UNKNOWN_RES_MNG = 0x8010002B;

        /// <summary>
        ///     The smart card does not support the requested feature.
        /// </summary>
        public const uint SCARD_E_UNSUPPORTED_FEATURE = 0x80100022;

        /// <summary>
        ///     An attempt was made to write more data than would fit in the target object.
        /// </summary>
        public const uint SCARD_E_WRITE_TOO_MANY = 0x80100028;

        /// <summary>
        ///     An internal communications error has been detected.
        /// </summary>
        public const uint SCARD_F_COMM_ERROR = 0x80100013;

        /// <summary>
        ///     An internal consistency check failed.
        /// </summary>
        public const uint SCARD_F_INTERNAL_ERROR = 0x80100001;

        /// <summary>
        ///     An internal error has been detected, but the source is unknown.
        /// </summary>
        public const uint SCARD_F_UNKNOWN_ERROR = 0x80100014;

        /// <summary>
        ///     An internal consistency timer has expired.
        /// </summary>
        public const uint SCARD_F_WAITED_TOO_LONG = 0x80100007;

        /// <summary>
        ///     The operation has been aborted to allow the server application to exit.
        /// </summary>
        public const uint SCARD_P_SHUTDOWN = 0x80100018;

        /// <summary>
        ///     No error was encountered.
        /// </summary>
        public const uint SCARD_S_SUCCESS = 0;

        /// <summary>
        ///     The action was canceled by the user.
        /// </summary>
        public const uint SCARD_W_CANCELLED_BY_USER = 0x8010006E;

        /// <summary>
        ///     The requested item could not be found in the cache.
        /// </summary>
        public const uint SCARD_W_CACHE_ITEM_NOT_FOUND = 0x80100070;

        /// <summary>
        ///     The requested cache item is too old and was deleted from the cache.
        /// </summary>
        public const uint SCARD_W_CACHE_ITEM_STALE = 0x80100071;

        /// <summary>
        ///     The new cache item exceeds the maximum per-item size defined for the cache.
        /// </summary>
        public const uint SCARD_W_CACHE_ITEM_TOO_BIG = 0x80100072;

        /// <summary>
        ///     No PIN was presented to the smart card.
        /// </summary>
        public const uint SCARD_W_CARD_NOT_AUTHENTICATED = 0x8010006F;

        /// <summary>
        ///     The card cannot be accessed because the maximum number of PIN entry attempts has been
        ///     reached.
        /// </summary>
        public const uint SCARD_W_CHV_BLOCKED = 0x8010006C;

        /// <summary>
        ///     The end of the smart card file has been reached.
        /// </summary>
        public const uint SCARD_W_EOF = 0x8010006D;

        /// <summary>
        ///     The smart card has been removed, so further communication is not possible.
        /// </summary>
        public const uint SCARD_W_REMOVED_CARD = 0x80100069;

        /// <summary>
        ///     The smart card was reset.
        /// </summary>
        public const uint SCARD_W_RESET_CARD = 0x80100068;

        /// <summary>
        ///     Access was denied because of a security violation.
        /// </summary>
        public const uint SCARD_W_SECURITY_VIOLATION = 0x8010006A;

        /// <summary>
        ///     Power has been removed from the smart card, so that further communication is not
        ///     possible.
        /// </summary>
        public const uint SCARD_W_UNPOWERED_CARD = 0x80100067;

        /// <summary>
        ///     The smart card is not responding to a reset.
        /// </summary>
        public const uint SCARD_W_UNRESPONSIVE_CARD = 0x80100066;

        /// <summary>
        ///     The reader cannot communicate with the card, due to ATR string configuration conflicts.
        /// </summary>
        public const uint SCARD_W_UNSUPPORTED_CARD = 0x80100065;

        /// <summary>
        ///     The card cannot be accessed because the wrong PIN was presented.
        /// </summary>
        public const uint SCARD_W_WRONG_CHV = 0x8010006B;
    }
}
