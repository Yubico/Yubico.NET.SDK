// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKey.Scp
{
    /// <summary>
    /// The connection class that can perform SCP03 operations will implement not
    /// only <see cref="IYubiKeyConnection"/>, but this interface as well.
    /// </summary>
    public interface IScpYubiKeyConnection : IYubiKeyConnection
    {
        /// <summary>
        /// Return a reference to the SCP key set used to make the connection.
        /// </summary>
        public ScpKeyParameters KeyParameters { get; }

        /// <summary>
        /// Get the encryptor function to encrypt any data for a SCP command using the current session keys.
        /// </summary>
        internal EncryptDataFunc EncryptDataFunc
        {
            get;
        }
    }
}
