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
using Yubico.Core.Iso7816;
// Feature hold-back
#if false
namespace Yubico.PlatformInterop
{
    /// <summary>
    /// Event arguments for smart card events.
    /// </summary>
    public class SCardEventArgs : EventArgs
    {
        /// <summary>
        /// The name of the smart card reader given by the operating system's smart card subsystem.
        /// </summary>
        /// <remarks>
        /// The reader name acts as the unique identifier for a smart card device.
        /// </remarks>
        public string ReaderName { get; private set; }

        /// <summary>
        /// The "answer to reset" (ATR) for the smart card.
        /// </summary>
        /// <remarks>
        /// The ATR for a smart card can act as an identifier for the type of card that is inserted.
        /// </remarks>
        public AnswerToReset Atr { get; private set; }

        /// <summary>
        /// Constructs an instance of the <see cref="SCardEventArgs"/> class. 
        /// </summary>
        /// <param name="readerName">The ID for smart card reader.</param>
        /// <param name="atr">The "answer to reset" (ATR) for the smart card.</param>
        public SCardEventArgs(string readerName, AnswerToReset atr)
        {
            ReaderName = readerName;
            Atr = atr;
        }
    }
}
#endif
