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
    internal enum SCARD_STATUS
    {
        UNKNOWN = 0,

        /// <summary>
        /// There is no card in the reader.
        /// </summary>
        ABSENT = 1,

        /// <summary>
        /// There is a card in the reader, but it has not been moved into position for use.
        /// </summary>
        PRESENT = 2,

        /// <summary>
        /// There is a card in the reader in position for use. The card is not powered.
        /// </summary>
        SWALLOWED = 3,

        /// <summary>
        /// Power is being provided to the card, but the reader driver is unaware of the mode of
        /// the card.
        /// </summary>
        POWERED = 4,

        /// <summary>
        /// The card has been reset and is awaiting PTS negotiation.
        /// </summary>
        NEGOTIABLE = 5,

        /// <summary>
        /// The card has been reset and specific communication protocols have been established.
        /// </summary>
        SPECIFIC = 6,
    }
}
