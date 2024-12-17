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

namespace Yubico.YubiKey.Oath
{
    /// <summary>
    /// Represents a One-Time Password (OTP) code generated on Yubikey using OATH application.
    /// </summary>
    /// <remarks>
    /// The YubiKey supports Open Authentication (OATH) standards for generating OTP codes.
    /// The OTPs need to be calculated because the YubiKey doesn't have an internal clock.
    /// The system time is used and passed to the YubiKey.
    /// </remarks>
    public class Code
    {
        /// <summary>
        /// The generated OTP code.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// The timestamp that was used to generate the OTP code.
        /// </summary>
        public DateTimeOffset? ValidFrom { get; set; }

        /// <summary>
        /// The timestamp when the OTP code becomes invalid.
        /// </summary>
        public DateTimeOffset? ValidUntil { get; set; }

        /// <summary>
        /// Constructs an instance of Code class.
        /// </summary>
        public Code()
        {

        }

        /// <summary>
        /// Constructs an instance of the <see cref="Code" /> class.
        /// </summary>
        /// <remarks>
        /// The credential period is used to set a validity for OTP code.
        /// The validity for HOTP code is set to DateTimeOffset.MaxValue because HOTP code is not time-based.
        /// The validity for TOTP code is set to DateTimeOffset.Now + credential period (15, 30, or 60 seconds).
        /// Also, it is "rounded" to the nearest 15, 30, or 60 seconds. For example, it will start at 1:14:30 and
        /// not 1:14:34 if the timestep is 30 seconds.
        /// </remarks>
        /// <param name="value">
        /// The generated OTP code.
        /// </param>
        /// <param name="period">
        /// The credential period to calculate the OTP code validity.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The provided period is invalid.
        /// </exception>
        public Code(string? value, CredentialPeriod period)
        {
            if (!Enum.IsDefined(typeof(CredentialPeriod), period))
            {
                throw new ArgumentException(ExceptionMessages.InvalidCredentialPeriod);
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                Value = value;

                var timestamp = DateTimeOffset.UtcNow;

                if (period != CredentialPeriod.Undefined)
                {
                    // The valid period can start before the calculation happens and potentially might happen even before,
                    // so that code is valid only 1 second after calculation.
                    // Taking the timestamp and rounding down to the nearest time segment given a period.
                    int secondsFromLastPeriod = (int)(timestamp.ToUnixTimeSeconds() % (int)period);
                    ValidFrom = timestamp.AddSeconds(-secondsFromLastPeriod);
                    ValidUntil = ValidFrom.Value.AddSeconds((int)period);
                }
                else
                {
                    ValidFrom = timestamp;
                    ValidUntil = DateTimeOffset.MaxValue;
                }
            }
        }

        /// <summary>
        /// Checks if the OTP code is still valid.
        /// </summary>
        public bool IsValid() => ValidUntil > DateTimeOffset.Now;
    }
}
