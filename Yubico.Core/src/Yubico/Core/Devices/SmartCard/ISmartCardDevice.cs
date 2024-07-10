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

using Yubico.Core.Iso7816;

namespace Yubico.Core.Devices.SmartCard
{
    /// <summary>
    ///     Represents an ISO 7816 compliant smart card, visible either through CCID or NFC.
    /// </summary>
    public interface ISmartCardDevice : IDevice
    {
        /// <summary>
        ///     The "answer to reset" (ATR) for the smart card.
        /// </summary>
        /// <value>
        ///     The ATR.
        /// </value>
        /// <remarks>
        ///     The ATR for a smart card can act as an identifier for the type of card that is inserted.
        /// </remarks>
        AnswerToReset? Atr { get; }

        /// <summary>
        ///     Gets the smart card's connection type.
        /// </summary>
        SmartCardConnectionKind Kind { get; }

        /// <summary>
        ///     Establishes an active connection to the smart card for the transmittal of data.
        /// </summary>
        ISmartCardConnection Connect();
    }
}
