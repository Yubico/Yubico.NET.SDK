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

namespace Yubico.YubiKey.Fido2.Commands
{
    internal enum CtapHidCommand
    {
        /// <summary>
        /// Sends a CTAP1/U2F message to the device.
        /// </summary>
        Ctap1Message = 0x03,

        /// <summary>
        /// Sends a CTAP2 CBOR-encoded message to the device; also used for NFC messages.
        /// </summary>
        Cbor = 0x10,

        /// <summary>
        /// Allocates or resets a CTAPHID channel.
        /// </summary>
        InitializeChannel = 0x06,

        /// <summary>
        /// Sends a transaction to the device, which immediately echoes the same data back.
        /// </summary>
        Ping = 0x01,

        /// <summary>
        /// Cancel any outstanding requests on a channel.
        /// </summary>
        CancelChannel = 0x11,

        /// <summary>
        /// Used only in response messages to indicate an error.
        /// </summary>
        Error = 0x3f,

        /// <summary>
        /// Received every 100ms to indicate an ongoing <see cref="Ctap1Message" /> command.
        /// </summary>
        KeepAlive = 0x3b,

        /// <summary>
        /// Optional command to blink the LED on the authenticator.
        /// </summary>
        Wink = 0x08,

        /// <summary>
        /// Optional command to place an exclusive lock on a channel.
        /// </summary>
        LockChannel = 0x04,
    }
}
