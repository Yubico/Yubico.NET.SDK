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

namespace Yubico.YubiKey
{
    /// <summary>
    ///     This defines the minimal set of information returned by a YubiKey in response to a command.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Types that implement this interface are used by <see cref="IYubiKeyCommand{TResponse}" />
    ///         to capture the command's success or failure state as reported by the YubiKey.
    ///     </para>
    ///     <para>
    ///         Implementations of IYubiKeyResponse which also need to return data should
    ///         implement <see cref="IYubiKeyResponseWithData{TData}" />.
    ///     </para>
    /// </remarks>
    public interface IYubiKeyResponse
    {
        /// <summary>
        ///     An application independent status.
        /// </summary>
        /// <remarks>
        ///     The Status property communicates many common error conditions. For example
        ///     there is no data to return, or the command required a touch interaction.
        ///     These errors are best checked and handled before calling methods
        ///     that use the data returned by the YubiKey such as
        ///     <see cref="IYubiKeyResponseWithData{TData}.GetData" />.
        ///     (this is known as the Tester-Doer pattern).
        /// </remarks>
        /// <value>
        ///     ResponseStatus.Success, ResponseStatus.Failed, etc.
        /// </value>
        ResponseStatus Status { get; }

        /// <summary>
        ///     The application specific status word.
        /// </summary>
        /// <remarks>
        ///     This is the two-byte response code of the response APDU. It is also
        ///     known as the "Status Word", made up of SW1 and SW2. For example, the
        ///     response code for Success is <c>9000</c>, which is <c>SW1=90</c> and
        ///     <c>SW2=00</c>.
        /// </remarks>
        /// <value>
        ///     0x9000, 0x6A82, etc.
        /// </value>
        short StatusWord { get; }

        /// <summary>
        ///     A short textual description of the status.
        /// </summary>
        /// <remarks>
        ///     This intended to displayed to the end-user, when an unhandled error occurs.
        ///     Programs should use <see cref="YubiKeyResponse.Status" /> for normal flow control.
        /// </remarks>
        string StatusMessage { get; }
    }
}
