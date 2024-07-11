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

using System.Runtime.InteropServices;

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// The SCARD_IO_REQUEST
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SCARD_IO_REQUEST
    {
        /// <summary>
        /// Protocol in use.
        /// </summary>
        public readonly SCARD_PROTOCOL Protocol;

        /// <summary>
        /// Length, in bytes, of the SCARD_IO_REQUEST structure plus any following PCI-specific
        /// information.
        /// </summary>
        public readonly int PciLength;

        /// <summary>
        /// Generates an IO Request structure based on the desired protocol.
        /// </summary>
        /// <param name="protocol">
        /// The protocol to use.
        /// </param>
        public SCARD_IO_REQUEST(SCARD_PROTOCOL protocol)
        {
            Protocol = protocol;
            PciLength = Marshal.SizeOf(typeof(SCARD_IO_REQUEST));
        }
    }
}
