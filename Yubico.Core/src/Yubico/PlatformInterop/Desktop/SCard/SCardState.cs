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

namespace Yubico.PlatformInterop
{
    /// <summary>
    ///     Flags to represent the current (expected) state of a smart card reader, as well as reflect
    ///     what is known to be true by the system.
    /// </summary>
    [Flags]
    internal enum SCARD_STATE
    {
        /// <summary>
        ///     <para>
        ///         CurrentState: The application is unaware of the current state, and would like to know.
        ///         The use of this value results in an immediate return from the state transition
        ///         monitoring services. This is represented by all bits set to zero.
        ///     </para>
        ///     <para>
        ///         EventState: This flag will not be set.
        ///     </para>
        /// </summary>
        UNAWARE = 0x0000,

        /// <summary>
        ///     <para>
        ///         CurrentState: The application is not interested in this reader, and it should not be
        ///         considered during monitoring operations. If this bit value is set, all other bits
        ///         are ignored.
        ///     </para>
        ///     <para>
        ///         EventState: This reader should be ignored.
        ///     </para>
        /// </summary>
        IGNORE = 0x0001,

        /// <summary>
        ///     <para>
        ///         CurrentState: This flag has no meaning and should not be used.
        ///     </para>
        ///     <para>
        ///         EventState: There is a difference between the state believed by the application, and
        ///         the state known by the resource manager. When this bit is set, the application may
        ///         assume a significant state change has occurred on this reader.
        ///     </para>
        /// </summary>
        CHANGED = 0x0002,

        /// <summary>
        ///     <para>
        ///         CurrentState: This flag has no meaning and should not be used.
        ///     </para>
        ///     <para>
        ///         EventState: The given reader name is not recognized by the resource manager. If this
        ///         bit is set, then CHANGED and IGNORE will also be set.
        ///     </para>
        /// </summary>
        UNKNOWN = 0x0004,

        /// <summary>
        ///     <para>
        ///         CurrentState: The application expects that this reader is not available for use. If
        ///         this bit is set, all the following bits are ignored.
        ///     </para>
        ///     <para>
        ///         EventState: The actual state of this reader is not available. If this bit is set,
        ///         then all the following bits are clear.
        ///     </para>
        /// </summary>
        UNAVAILABLE = 0x0008,

        /// <summary>
        ///     <para>
        ///         CurrentState: The application expects that there is no card in the reader. If this
        ///         bit is set, all the following bits are ignored.
        ///     </para>
        ///     <para>
        ///         EventState: There is no card in the reader. If this bit is set, all the following
        ///         bits with be clear.
        ///     </para>
        /// </summary>
        EMPTY = 0x0010,

        /// <summary>
        ///     <para>
        ///         CurrentState: The application expects that there is a card in the reader.
        ///     </para>
        ///     <para>
        ///         EventState: There is a card in the reader.
        ///     </para>
        /// </summary>
        PRESENT = 0x0020,

        /// <summary>
        ///     <para>
        ///         CurrentState: The application expects that there is a card in the reader with an ATR
        ///         that matches one of the target cards. If this bit is set, PRESENT is assumed. This
        ///         bit has no meaning to SCardGetStatusChange beyond PRESENT.
        ///     </para>
        ///     <para>
        ///         EventState: There is a card in the reader with an ATR matching one of the target cards.
        ///         If this bit is set, PRESENT will also be set. This bit is only returned on the
        ///         SCardLocateCards function.
        ///     </para>
        /// </summary>
        ATRMATCH = 0x0040,

        /// <summary>
        ///     <para>
        ///         CurrentState: The application expects that the card in the reader is allocated for
        ///         exclusive use by another application. If this bit is set, PRESENT is assumed.
        ///     </para>
        ///     <para>
        ///         EventState: The card in the reader is allocated for exclusive use by another application.
        ///         If this bit is set, PRESENT will also be set.
        ///     </para>
        /// </summary>
        EXCLUSIVE = 0x0080,

        /// <summary>
        ///     <para>
        ///         CurrentState: The application expects that the card in the reader is in use by one
        ///         or more other applications, but may be connected to in shared mode. If this bit is
        ///         set, PRESENT is assumed.
        ///     </para>
        ///     <para>
        ///         EventState: The card in the reader is in use by one or more other applications, but
        ///         may be connected to in shared mode. If this bit is set, PRESENT will also be set.
        ///     </para>
        /// </summary>
        INUSE = 0x0100,

        /// <summary>
        ///     <para>
        ///         CurrentState: The application expects that there is an unresponsive card in the reader.
        ///     </para>
        ///     <para>
        ///         EventState: There is an unresponsive card in the reader.
        ///     </para>
        /// </summary>
        MUTE = 0x0200,

        /// <summary>
        ///     <para>
        ///         CurrentState: This flag has no meaning and should not be used.
        ///     </para>
        ///     <para>
        ///         EventState: There is a card in the reader, but it is unpowered.
        ///     </para>
        /// </summary>
        UNPOWERED = 0x0400
    }
}
