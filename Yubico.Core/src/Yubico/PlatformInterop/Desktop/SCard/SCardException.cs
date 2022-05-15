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
using System.Runtime.Serialization;

namespace Yubico.PlatformInterop
{
    /// <inheritdoc />
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

        [CLSCompliant(false)]
        public SCardException(string message, uint errorCode) :
            base(message + $" Error code: 0x{errorCode.ToString("x8", CultureInfo.InvariantCulture)}.")
        {
            HResult = (int)errorCode;
        }

        public SCardException(string message, Exception innerException) :
            base(message, innerException)
        {

        }

        protected SCardException(SerializationInfo serializationInfo, StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {

        }
    }
}
