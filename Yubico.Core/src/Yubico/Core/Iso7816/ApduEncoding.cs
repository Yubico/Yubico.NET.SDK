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

namespace Yubico.Core.Iso7816
{
    /// <summary>
    ///     Represents encoding options for an APDU's length fields.
    /// </summary>
    public enum ApduEncoding
    {
        /// <summary>
        ///     Automatically determine the encoding length.
        /// </summary>
        Automatic = 0,

        /// <summary>
        ///     Use short encoding.
        /// </summary>
        ShortLength = 1,

        /// <summary>
        ///     Use extended encoding.
        /// </summary>
        ExtendedLength = 2
    }
}
