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

using System;
using System.Globalization;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Exception that represents when the authenticator presents an unsuccessful FIDO2 status.
    /// </summary>
    public class Fido2Exception : Exception
    {
        /// <summary>
        /// The FIDO2 status returned by the authenticator.
        /// </summary>
        public Fido2Status? Status { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Fido2Exception"/> class.
        /// </summary>
        public Fido2Exception()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Fido2Exception"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public Fido2Exception(string message) : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Fido2Exception"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public Fido2Exception(string message, Exception innerException) : base(message, innerException)
        {

        }

        /// <summary>
        /// Intializes a new instance of the <see cref="Fido2Exception"/> class, and sets an appropriate message.
        /// </summary>
        /// <param name="status">The error status returned by the authenticator.</param>
        public Fido2Exception(byte status)
            : base(ConstructMessage(status))
        {
            Status = Enum.IsDefined(typeof(Fido2Status), (int)status)
                ? (Fido2Status?)status
                : null;
        }

        private static string ConstructMessage(byte status)
        {
            if (Enum.IsDefined(typeof(Fido2Status), (int)status))
            {
                string name = Enum.GetName(typeof(Fido2Status), (int)status);
                return string.Format(CultureInfo.InvariantCulture, ExceptionMessages.BadFido2Status, status, name);
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, ExceptionMessages.UnknownFido2Status);
            }
        }
    }
}
