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

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    /// The three YubiKey OTP flags collected in one class.
    /// </summary>
    public class YubiKeyFlags
    {
        /// <summary>
        /// A <see langword="byte"/> containing the flags from <see cref="ExtendedFlags"/>l
        /// </summary>
        public byte Extended { get; set; }
        /// <summary>
        /// A <see langword="byte"/> containing the flags from <see cref="TicketFlags"/>l
        /// </summary>
        public byte Ticket { get; set; }
        /// <summary>
        /// A <see langword="byte"/> containing the flags from <see cref="ConfigurationFlags"/>l
        /// </summary>
        public byte Configuration { get; set; }

#pragma warning disable CA2225, CA1065 // Justification: Not necessary to have the expected named alternative method
        /// <summary>
        /// Implicitly extract <see cref="ExtendedFlags"/> from a <see cref="YubiKeyFlags"/> object.
        /// </summary>
        /// <param name="flags"><see cref="YubiKeyFlags"/> object containing flags.</param>
        public static implicit operator ExtendedFlags(YubiKeyFlags flags)
        {
            if (flags is null)
            {
                throw new ArgumentNullException(nameof(flags));
            }

            return flags.Extended;
        }

        /// <summary>
        /// Implicitly extract <see cref="TicketFlags"/> from a <see cref="YubiKeyFlags"/> object.
        /// </summary>
        /// <param name="flags"><see cref="YubiKeyFlags"/> object containing flags.</param>
        public static implicit operator TicketFlags(YubiKeyFlags flags)
        {
            if (flags is null)
            {
                throw new ArgumentNullException(nameof(flags));
            }

            return flags.Ticket;
        }

        /// <summary>
        /// Implicitly extract <see cref="ConfigurationFlags"/> from a <see cref="YubiKeyFlags"/> object.
        /// </summary>
        /// <param name="flags"><see cref="YubiKeyFlags"/> object containing flags.</param>
        public static implicit operator ConfigurationFlags(YubiKeyFlags flags)
        {
            if (flags is null)
            {
                throw new ArgumentNullException(nameof(flags));
            }

            return flags.Configuration;
        }
#pragma warning restore CA2225, CA1065
    }
}
