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

using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Yubico.YubiKit.Oath;

/// <summary>
///     Identifies why an <see cref="OathException" /> was thrown.
/// </summary>
public enum OathFailureReason
{
    /// <summary>
    ///     The OATH applet requires authentication before the attempted operation can succeed.
    /// </summary>
    Locked,

    /// <summary>
    ///     Password verification failed because the supplied password (or derived key) was incorrect.
    /// </summary>
    WrongPassword,
}

/// <summary>
///     Exception thrown when an OATH operation fails because the applet is locked or because
///     password verification failed.
/// </summary>
public class OathException : Exception
{
    // Existing alpha constructors provide default, custom-message, and inner-exception forms.
#pragma warning disable RS0026
    /// <summary>
    ///     Gets the reason this exception was thrown.
    /// </summary>
    public OathFailureReason Reason { get; }

    /// <summary>
    ///     Gets the ISO 7816 status word returned by the device, when the protocol supplied one.
    /// </summary>
    public short? StatusWord { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OathException" /> class.
    /// </summary>
    /// <param name="reason">The reason the operation failed.</param>
    /// <param name="statusWord">The device status word associated with the failure, if any.</param>
    public OathException(OathFailureReason reason, short? statusWord = null)
        : base(GetMessage(reason, statusWord))
    {
        Reason = reason;
        StatusWord = statusWord;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OathException" /> class with a custom message.
    /// </summary>
    /// <param name="reason">The reason the operation failed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="statusWord">The device status word associated with the failure, if any.</param>
    public OathException(OathFailureReason reason, string message, short? statusWord = null)
        : base(message)
    {
        Reason = reason;
        StatusWord = statusWord;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OathException" /> class, wrapping the
    ///     lower-level protocol exception that caused it.
    /// </summary>
    /// <param name="reason">The reason the operation failed.</param>
    /// <param name="statusWord">The device status word associated with the failure, if any.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public OathException(OathFailureReason reason, short? statusWord, Exception innerException)
        : base(GetMessage(reason, statusWord), innerException)
    {
        Reason = reason;
        StatusWord = statusWord;
    }
#pragma warning restore RS0026

    private static string GetMessage(OathFailureReason reason, short? statusWord)
    {
        string baseMessage = reason switch
        {
            OathFailureReason.Locked =>
                "The OATH application is locked. Authenticate with ValidateAsync (or AuthenticateAndRetryAsync) before retrying this operation.",
            OathFailureReason.WrongPassword =>
                "OATH password verification failed. The supplied password is incorrect.",
            _ => "OATH operation failed.",
        };

        return statusWord is { } sw
            ? $"{baseMessage} {SWConstants.GetStatusMessage(sw)} (SW=0x{sw:X4})"
            : baseMessage;
    }
}