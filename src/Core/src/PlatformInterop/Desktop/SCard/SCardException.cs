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

using System.Globalization;

namespace Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;

[Serializable]
public class SCardException : Exception
{
    public SCardException()
    {
    }

    public SCardException(string message) :
        base(message)
    {
    }

    public SCardException(string message, long errorCode) :
        base(message + " " + GetErrorString(errorCode))
    {
        HResult = (int)errorCode;
    }

    public SCardException(string message, Exception innerException) :
        base(message, innerException)
    {
    }

    internal static string GetErrorString(long errorCode) =>
        errorCode switch
        {
            ErrorCode.ERROR_BROKEN_PIPE =>
                "ERROR_BROKEN_PIPE: The client attempted a smart card operation in a remote session, such as a client " +
                "session running on a terminal server, and the operating system in use does not support smart card " +
                "redirection.",
            ErrorCode.SCARD_E_BAD_SEEK =>
                "SCARD_E_BAD_SEEK: An error occurred in setting the smart card file object pointer.",
            ErrorCode.SCARD_E_CANCELLED =>
                "SCARD_E_CANCELLED: The action was canceled by an SCardCancel request.",
            ErrorCode.SCARD_E_CANT_DISPOSE =>
                "SCARD_E_CANT_DISPOSE: The system could not dispose of the media in the requested manner.",
            ErrorCode.SCARD_E_CARD_UNSUPPORTED =>
                "SCARD_E_CARD_UNSUPPORTED: The smart card does not meet minimal requirements for support.",
            ErrorCode.SCARD_E_CERTIFICATE_UNAVAILABLE =>
                "SCARD_E_CERTIFICATE_UNAVAILABLE: The requested certificate could not be obtained.",
            ErrorCode.SCARD_E_COMM_DATA_LOST =>
                "SCARD_E_COMM_DATA_LOST: A communications error with the smart card has been detected.",
            ErrorCode.SCARD_E_DIR_NOT_FOUND =>
                "SCARD_E_DIR_NOT_FOUND: The specified directory does not exist in the smart card.",
            ErrorCode.SCARD_E_DUPLICATE_READER =>
                "SCARD_E_DUPLICATE_READER: The reader driver did not produce a unique reader name.",
            ErrorCode.SCARD_E_FILE_NOT_FOUND =>
                "SCARD_E_FILE_NOT_FOUND: The specified file does not exist in the smart card.",
            ErrorCode.SCARD_E_ICC_CREATEORDER =>
                "SCARD_E_ICC_CREATEORDER: The requested order of object creation is not supported.",
            ErrorCode.SCARD_E_ICC_INSTALLATION =>
                "SCARD_E_ICC_INSTALLATION: No primary provider can be found for the smart card.",
            ErrorCode.SCARD_E_INSUFFICIENT_BUFFER =>
                "SCARD_E_INSUFFICIENT_BUFFER: The data buffer for returned data is too small for the returned data.",
            ErrorCode.SCARD_E_INVALID_ATR =>
                "SCARD_E_INVALID_ATR: An ATR string obtained from the registry is not a valid ATR string.",
            ErrorCode.SCARD_E_INVALID_CHV =>
                "SCARD_E_INVALID_CHV: The supplied PIN is incorrect.",
            ErrorCode.SCARD_E_INVALID_HANDLE =>
                "SCARD_E_INVALID_HANDLE: The supplied handle was not valid.",
            ErrorCode.SCARD_E_INVALID_PARAMETER =>
                "SCARD_E_INVALID_PARAMETER: One or more of the supplied parameters could not be properly interpreted.",
            ErrorCode.SCARD_E_INVALID_TARGET =>
                "SCARD_E_INVALID_TARGET: Registry startup information is missing or not valid.",
            ErrorCode.SCARD_E_INVALID_VALUE =>
                "SCARD_E_INVALID_VALUE: One or more of the supplied parameter values could not be properly interpreted.",
            ErrorCode.SCARD_E_NO_ACCESS =>
                "SCARD_E_NO_ACCESS: Access is denied to the file.",
            ErrorCode.SCARD_E_NO_DIR =>
                "SCARD_E_NO_DIR: The supplied path does not represent a smart card directory.",
            ErrorCode.SCARD_E_NO_FILE =>
                "SCARD_E_NO_FILE: The supplied path does not represent a smart card file.",
            ErrorCode.SCARD_E_NO_KEY_CONTAINER =>
                "SCARD_E_NO_KEY_CONTAINER: The requested key container does not exist on the smart card.",
            ErrorCode.SCARD_E_NO_MEMORY =>
                "SCARD_E_NO_MEMORY: Not enough memory available to complete this command.",
            ErrorCode.SCARD_E_NO_PIN_CACHE =>
                "SCARD_E_NO_PIN_CACHE: The smart card PIN cannot be cached.",
            ErrorCode.SCARD_E_NO_READERS_AVAILABLE =>
                "SCARD_E_NO_READERS_AVAILABLE: No smart card reader is available.",
            ErrorCode.SCARD_E_NO_SERVICE =>
                "SCARD_E_NO_SERVICE: The smart card resource manager is not running.",
            ErrorCode.SCARD_E_NO_SMARTCARD =>
                "SCARD_E_NO_SMARTCARD: The operation requires a smart card, but no smart card is currently in the device.",
            ErrorCode.SCARD_E_NO_SUCH_CERTIFICATE =>
                "SCARD_E_NO_SUCH_CERTIFICATE: The requested certificate does not exist.",
            ErrorCode.SCARD_E_NOT_READY =>
                "SCARD_E_NOT_READY: The reader or card is not ready to accept commands.",
            ErrorCode.SCARD_E_NOT_TRANSACTED =>
                "SCARD_E_NOT_TRANSACTED: An attempt was made to end a nonexistent transaction.",
            ErrorCode.SCARD_E_PCI_TOO_SMALL =>
                "SCARD_E_PCI_TOO_SMALL: The PCI receive buffer was too small.",
            ErrorCode.SCARD_E_PIN_CACHE_EXPIRED =>
                "SCARD_E_PIN_CACHE_EXPIRED: The smart card PIN cache has expired.",
            ErrorCode.SCARD_E_PROTO_MISMATCH =>
                "SCARD_E_PROTO_MISMATCH: The requested protocols are incompatible with the protocol currently in use " +
                "with the card.",
            ErrorCode.SCARD_E_READ_ONLY_CARD =>
                "SCARD_E_READ_ONLY_CARD: The smart card is read-only and cannot be written to.",
            ErrorCode.SCARD_E_READER_UNAVAILABLE =>
                "SCARD_E_READER_UNAVAILABLE: The specified reader is not currently available for use.",
            ErrorCode.SCARD_E_READER_UNSUPPORTED =>
                "SCARD_E_READER_UNSUPPORTED: The reader driver does not meet minimal requirements for support.",
            ErrorCode.SCARD_E_SERVER_TOO_BUSY =>
                "SCARD_E_SERVER_TOO_BUSY: The smart card resource manager is too busy to complete this operation.",
            ErrorCode.SCARD_E_SERVICE_STOPPED =>
                "SCARD_E_SERVICE_STOPPED: The smart card resource manager has shut down.",
            ErrorCode.SCARD_E_SHARING_VIOLATION =>
                "SCARD_E_SHARING_VIOLATION: The smart card cannot be accessed because of other outstanding connections.",
            ErrorCode.SCARD_E_SYSTEM_CANCELLED =>
                "SCARD_E_SYSTEM_CANCELLED: The action was canceled by the system, presumably to log off or shut down.",
            ErrorCode.SCARD_E_TIMEOUT =>
                "SCARD_E_TIMEOUT: The user-specified time-out value has expired.",
            ErrorCode.SCARD_E_UNEXPECTED =>
                "SCARD_E_UNEXPECTED: An unexpected card error has occurred.",
            ErrorCode.SCARD_E_UNKNOWN_CARD =>
                "SCARD_E_UNKNOWN_CARD: The specified smart card is not recognized.",
            ErrorCode.SCARD_E_UNKNOWN_READER =>
                "SCARD_E_UNKNOWN_READER: The specified reader name is not recognized.",
            ErrorCode.SCARD_E_UNKNOWN_RES_MNG =>
                "SCARD_E_UNKNOWN_RES_MNG: An unrecognized error code was returned.",
            ErrorCode.SCARD_E_UNSUPPORTED_FEATURE =>
                "SCARD_E_UNSUPPORTED_FEATURE: The smart card does not support the requested feature.",
            ErrorCode.SCARD_E_WRITE_TOO_MANY =>
                "SCARD_E_WRITE_TOO_MANY: An attempt was made to write more data than would fit in the target object.",
            ErrorCode.SCARD_F_COMM_ERROR =>
                "SCARD_F_COMM_ERROR: An internal communications error has been detected.",
            ErrorCode.SCARD_F_INTERNAL_ERROR =>
                "SCARD_F_INTERNAL_ERROR: An internal consistency check failed.",
            ErrorCode.SCARD_F_UNKNOWN_ERROR =>
                "SCARD_F_UNKNOWN_ERROR: An internal error has been detected, but the source is unknown.",
            ErrorCode.SCARD_F_WAITED_TOO_LONG =>
                "SCARD_F_WAITED_TOO_LONG: An internal consistency timer has expired.",
            ErrorCode.SCARD_P_SHUTDOWN =>
                "SCARD_P_SHUTDOWN: The operation has been aborted to allow the server application to exit.",
            ErrorCode.SCARD_S_SUCCESS =>
                "SCARD_S_SUCCESS: No error was encountered.",
            ErrorCode.SCARD_W_CANCELLED_BY_USER =>
                "SCARD_W_CANCELLED_BY_USER: The action was canceled by the user.",
            ErrorCode.SCARD_W_CACHE_ITEM_NOT_FOUND =>
                "SCARD_W_CACHE_ITEM_NOT_FOUND: The requested item could not be found in the cache.",
            ErrorCode.SCARD_W_CACHE_ITEM_STALE =>
                "SCARD_W_CACHE_ITEM_STALE: The requested cache item is too old and was deleted from the cache.",
            ErrorCode.SCARD_W_CACHE_ITEM_TOO_BIG =>
                "SCARD_W_CACHE_ITEM_TOO_BIG: The new cache item exceeds the maximum per-item size defined for the cache.",
            ErrorCode.SCARD_W_CARD_NOT_AUTHENTICATED =>
                "SCARD_W_CARD_NOT_AUTHENTICATED: No PIN was presented to the smart card.",
            ErrorCode.SCARD_W_CHV_BLOCKED =>
                "SCARD_W_CHV_BLOCKED: The card cannot be accessed because the maximum number of PIN entry attempts has " +
                "been reached.",
            ErrorCode.SCARD_W_EOF =>
                "SCARD_W_EOF: The end of the smart card file has been reached.",
            ErrorCode.SCARD_W_REMOVED_CARD =>
                "SCARD_W_REMOVED_CARD: The smart card has been removed, so further communication is not possible.",
            ErrorCode.SCARD_W_RESET_CARD =>
                "SCARD_W_RESET_CARD: The smart card was reset.",
            ErrorCode.SCARD_W_SECURITY_VIOLATION =>
                "SCARD_W_SECURITY_VIOLATION: Access was denied because of a security violation.",
            ErrorCode.SCARD_W_UNPOWERED_CARD =>
                "SCARD_W_UNPOWERED_CARD: Power has been removed from the smart card, so that further communication is " +
                "not possible.",
            ErrorCode.SCARD_W_UNRESPONSIVE_CARD =>
                "SCARD_W_UNRESPONSIVE_CARD: The smart card is not responding to a reset.",
            ErrorCode.SCARD_W_UNSUPPORTED_CARD =>
                "SCARD_W_UNSUPPORTED_CARD: The reader cannot communicate with the card, due to ATR string configuration " +
                "conflicts.",
            ErrorCode.SCARD_W_WRONG_CHV =>
                "The card cannot be accessed because the wrong PIN was presented.",
            _ => string.Format(CultureInfo.CurrentCulture, "Encountered error {0}", errorCode)
        };
}