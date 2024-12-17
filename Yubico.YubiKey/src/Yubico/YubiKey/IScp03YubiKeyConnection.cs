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

using System;

namespace Yubico.YubiKey
{
    /// <summary>
    /// The connection class that can perform SCP03 operations will implement not
    /// only <see cref="IYubiKeyConnection"/>, but this interface as well.
    /// </summary>
    [Obsolete("Use IScpYubiKeyConnection")]
    public interface IScp03YubiKeyConnection : IYubiKeyConnection
    {
        /// <summary>
        /// Return a reference to the SCP03 key set used to make the connection.
        /// </summary>
        public Yubico.YubiKey.Scp03.StaticKeys GetScp03Keys();
    }
}
