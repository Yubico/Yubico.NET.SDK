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
using System.Runtime.Serialization;

namespace Yubico.YubiKey
{
    /// <summary>
    /// The exception thrown when the data received from the YubiKey does not match the expectations
    /// of the response class's parser.
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class MalformedYubiKeyResponseException : Exception
    {
        /// <summary>
        /// Gets or sets the expected length of the data for this response.
        /// </summary>
        /// <value>
        /// The expected length of the data.
        /// </value>
        public int? ExpectedDataLength { get; set; }

        /// <summary>
        /// Gets or sets the actual length of the data received from the YubiKey.
        /// </summary>
        /// <value>
        /// The actual length of the data.
        /// </value>
        public int? ActualDataLength { get; set; }

        /// <summary>
        /// Gets or sets the index into the data field where the parsing error occurred, if known.
        /// </summary>
        /// <value>
        /// The index of the data error.
        /// </value>
        public int? DataErrorIndex { get; set; }

        /// <summary>
        /// Gets or sets the name of the IYubiKeyResponse implementation class.
        /// </summary>
        /// <value>
        /// The name of the class throwing the error.
        /// </value>
        public string? ResponseClass { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MalformedYubiKeyResponseException"/> class.
        /// </summary>
        public MalformedYubiKeyResponseException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MalformedYubiKeyResponseException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MalformedYubiKeyResponseException(string message) :
            base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MalformedYubiKeyResponseException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
        public MalformedYubiKeyResponseException(string message, Exception innerException) :
            base(message, innerException)
        {
        }

        protected MalformedYubiKeyResponseException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
