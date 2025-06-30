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

using System;
using System.Globalization;
using System.Linq;
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    /// Container for a YubiKey OTP Slot access code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Access codes must be exactly six bytes when used in the YubiKey
    /// protocol. This helper-class assures that the access code does not
    /// exceed the maximum length, and pads the code with zeros (0x00) if less than six bytes
    /// are specified.
    /// </para>
    /// <para>
    /// This class has an explicit operator that allows you to cast a byte
    /// array as a <see cref="SlotAccessCode"/> if that is more convenient
    /// than constructing one.
    /// </para>
    /// </remarks>
    public class SlotAccessCode
    {
        /// <summary>
        /// Constructs a <see cref="SlotAccessCode"/> instance with an
        /// empty (all zeros) code.
        /// </summary>
        public SlotAccessCode()
        {
            AccessCodeBytes = new byte[SlotConfigureBase.AccessCodeLength];
        }

        /// <summary>
        /// Constructs a <see cref="SlotAccessCode"/> instance using
        /// the bytes in the collection.
        /// </summary>
        /// <param name="bytes">The bytes to use to construct the <see cref="SlotAccessCode"/>.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the length exceeds <see cref="MaxAccessCodeLength"/>
        /// </exception>
        public SlotAccessCode(ReadOnlyMemory<byte> bytes)
        {
            AccessCodeBytes = GetAccessCode(bytes.ToArray());
        }

        /// <summary>
        /// Explicit operator to allow a byte array to be used as a parameter
        /// for methods expecting a <see cref="SlotAccessCode"/> instance.
        /// </summary>
        /// <param name="bytes">The bytes to use to construct the <see cref="SlotAccessCode"/>.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the length exceeds <see cref="MaxAccessCodeLength"/>
        /// </exception>
#pragma warning disable CA2225 // Justification: Not necessary to have the expected named alternative method
        public static explicit operator SlotAccessCode(byte[] bytes) =>
            new SlotAccessCode(bytes);
#pragma warning restore CA2225
        /// <summary>
        /// Constant giving a reference to the maximum length of an access code.
        /// </summary>
        public const int MaxAccessCodeLength = SlotConfigureBase.AccessCodeLength;

        internal byte[] AccessCodeBytes { get; }

        // The access code has to be exactly six bytes. This method takes a byte array and makes sure
        // it's the correct length. If it's too short, it extends it with zeros (0x00). If it's too long, it throws an
        // exception.

        private static byte[] GetAccessCode(byte[] code) =>
            // Is the code too short?
            code.Length < SlotConfigureBase.AccessCodeLength
            // If yes, add bytes to the end.
            ? code.Concat(new byte[SlotConfigureBase.AccessCodeLength - code.Length]).ToArray()
            // If not, is it too long?
            : code.Length > SlotConfigureBase.AccessCodeLength
                // If yes, throw an exception.
                ? throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.AccessCodeTooLong,
                        SlotConfigureBase.AccessCodeLength))
                // Otherwise, we're all set.
                : code;
    }
}
