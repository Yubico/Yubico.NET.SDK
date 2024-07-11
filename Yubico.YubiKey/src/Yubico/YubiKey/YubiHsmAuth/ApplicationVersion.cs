// Copyright 2022 Yubico AB
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

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    ///     The version of the YubiHSM Auth application represented as major,
    ///     minor, and patch values.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="Commands.GetApplicationVersionCommand" />
    ///     and <see cref="Commands.GetApplicationVersionResponse" /> to
    ///     retrieve the value from the YubiHSM Auth application.
    /// </remarks>
    public class ApplicationVersion : FirmwareVersion
    {
        /// <summary>
        ///     Constructs an object with default values. Use this constructor
        ///     if you prefer to use an object initializer.
        /// </summary>
        public ApplicationVersion() { }

        /// <summary>
        ///     Constructs an object with the provided values.
        /// </summary>
        /// <param name="major">
        ///     The major version of the application.
        /// </param>
        /// <param name="minor">
        ///     The minor version of the application.
        /// </param>
        /// <param name="patch">
        ///     The patch version of the application.
        /// </param>
        public ApplicationVersion(byte major, byte minor, byte patch)
            : base(major, minor, patch)
        {
        }
    }
}
