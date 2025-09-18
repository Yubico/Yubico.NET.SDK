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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey;

/// <summary>
///     An interface for representing a command that can be run on a YubiKey.
/// </summary>
/// <remarks>
///     <para>
///         Classes that implement this interface represent a low level command that can be
///         Sent to the YubiKey.
///     </para>
///     <para>
///         An implementation of this interface can be thought of as a factory class for creating
///         the necessary CommandApdu to send to the YubiKey (but does not actually send it itself).
///         In addition, the implementation serves as a factory for creating the necessary IYubiKeyResponse
///         based on the ResponseApdu.
///     </para>
///     <para>
///         Derived classes should expose strongly typed properties and methods to take in the
///         parameters and data that the YubiKey command requires.
///     </para>
/// </remarks>
/// <typeparam name="TResponse">The concrete type of the response to this command.</typeparam>
public interface IYubiKeyCommand<out TResponse> where TResponse : IYubiKeyResponse
{
    /// <summary>
    ///     Gets the <see cref="YubiKeyApplication" /> (e.g. PIV, OATH, etc.) to
    ///     which this command applies.
    /// </summary>
    /// <value>
    ///     YubiKeyApplication.Otp, YubiKeyApplication.Piv, etc.
    /// </value>
    YubiKeyApplication Application { get; }

    /// <summary>
    ///     Creates a well-formed CommandApdu to send to the YubiKey.
    /// </summary>
    /// <remarks>
    ///     This method will first perform validation on all of the parameters and data provided
    ///     to it. The CommandAPDU it creates should contain all of the data payload for the
    ///     command, even if it exceeds 65,535 bytes as specified by the ISO 7816-4 specification.
    ///     The APDU will be properly chained by the device connection prior to being sent to the
    ///     YubiKey, and the responses will collapsed into a single result.
    /// </remarks>
    /// <returns>
    ///     A valid CommandApdu that is ready to be sent to the YubiKey, or passed along
    ///     to additional encoders for further processing.
    /// </returns>
    CommandApdu CreateCommandApdu();

    /// <summary>
    ///     Creates the corresponding IYubiKeyResponse implementation for the
    ///     current command.
    /// </summary>
    /// <param name="responseApdu">
    ///     The ResponseApdu returned by the YubiKey.
    /// </param>
    /// <returns>
    ///     The implementation of <see cref="IYubiKeyResponse" /> that parses and
    ///     presents ths response APDU.
    /// </returns>
    TResponse CreateResponseForApdu(ResponseApdu responseApdu);
}
