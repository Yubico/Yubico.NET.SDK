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

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Exception that represents invalid data passed to a CTAP2 command.
    /// </summary>
#pragma warning disable CA1064 // Exceptions should be public
    internal class Ctap2Exception : Exception
#pragma warning restore CA1064 // Exceptions should be public
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ctap2Exception"/> class.
        /// </summary>
        public Ctap2Exception()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ctap2Exception"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public Ctap2Exception(string message) : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ctap2Exception"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public Ctap2Exception(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}

