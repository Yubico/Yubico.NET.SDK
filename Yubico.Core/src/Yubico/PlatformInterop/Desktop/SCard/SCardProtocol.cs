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
using System.Diagnostics.CodeAnalysis;

namespace Yubico.PlatformInterop
{
    [Flags]
    [SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "Keeping interop as close to original C headers as possible")]
    internal enum SCARD_PROTOCOL
    {
        Undefined = 0x00000000,

        Optimal = 0x00000000,

        /// <summary>
        /// Use the T=0 protocol - an asynchronous, character-oriented half-duplex transmission
        /// protocol.
        /// </summary>
        T0 = 0x00000001,

        /// <summary>
        /// Use the T=1 protocol - an asynchronous, block-oriented half-duplex transmission
        /// protocol.
        /// </summary>
        T1 = 0x00000002,

        Raw = 0x00010000,

        /// <summary>
        /// Both the T=0 and T=1 protocol may be used.
        /// </summary>
        Tx = T0 | T1,

        Default = unchecked((int)0x80000000),
    }
}
