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

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// The scope or domain in which database operations are to be performed.
    /// </summary>
    internal enum SCARD_SCOPE
    {
        /// <summary>
        /// Database operations are performed within the domain of the user.
        /// </summary>
        USER,

        TERMINAL,

        /// <summary>
        /// Database operations are performed within the domain of the system. The calling
        /// application must have appropriate access permissions for any database actions.
        /// </summary>
        SYSTEM,
    }
}
