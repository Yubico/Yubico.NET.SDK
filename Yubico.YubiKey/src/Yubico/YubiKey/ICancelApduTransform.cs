// Copyright 2023 Yubico AB
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
    ///     IApduTransform classes that implement this interface can cancel
    ///     operations "mid-command".
    /// </summary>
    /// <remarks>
    ///     See the documentation for the <see cref="ICancelConnection" /> interface.
    ///     That contains a discussion of how the cancellation works. It describes how
    ///     a caller can set a Connection with a <see cref="QueryCancel" /> to be used
    ///     to determine if an operation should be canceled or not. In practice, the
    ///     Connection object won't be the one that handles cancellation. Rather, the
    ///     Connection will contain an IApduTransform object that will. Hence, the
    ///     caller will call a Connection.LoadQueryCancel method, which will in turn
    ///     set the IApduTransform object, if it also implements this interface.
    /// </remarks>
    internal interface ICancelApduTransform
    {
        /// <summary>
        ///     If not null, this is the delegate this object will call to determine
        ///     if the operation has been canceled. If null, there's nothing to
        ///     check, so finish the operation.
        /// </summary>
        /// <remarks>
        ///     See the declaration of <see cref="Yubico.YubiKey.QueryCancel" />.
        ///     <para>
        ///         This property is set by the object that determines cancellation. That
        ///         is, there is an object, such as <see cref="TouchFingerprintTask" />,
        ///         that will be able to accept the user's cancellation request. Call
        ///         this the "Cancel Recipient Object". The Cancel Recipient Object will
        ///         load a delegate onto the <c>ICancelApduTransform</c> object as the
        ///         <c>QueryCancel</c>. This ApduTransform object will then have a way to
        ///         call the Cancel Recipient Object to determine if the user has
        ///         canceled or not.
        ///     </para>
        ///     <para>
        ///         While the ApduTransform object is performing an operation, it will
        ///         periodically call the <c>QueryCancel</c> to find out if the user has
        ///         canceled. If the query returns true
        ///     </para>
        ///     <para>
        ///         If no <c>QueryCancel</c> is loaded, there's nothing to check, so the
        ///         operation will simply execute until complete.
        ///     </para>
        /// </remarks>
        public QueryCancel? QueryCancel { get; set; }
    }
}
