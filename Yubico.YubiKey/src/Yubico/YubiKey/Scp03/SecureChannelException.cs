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

namespace Yubico.YubiKey.Scp03
{
    /// <summary>
    /// Represents errors that occur during encoding or decoding data for SCP03.
    /// </summary>
#pragma warning disable CA1064 // Exceptions should be public
[Obsolete("Use new ChannelEncryption instead")]
    internal class SecureChannelException : Exception
#pragma warning restore CA1064 // Exceptions should be public
    {
        public SecureChannelException()
        {

        }

        public SecureChannelException(string message) :
            base($"SCP03 CardDataException: {message}")
        {

        }

        public SecureChannelException(string message, Exception e) :
            base($"SCP03 CardDataException: {message}", e)
        {

        }
    }
}
