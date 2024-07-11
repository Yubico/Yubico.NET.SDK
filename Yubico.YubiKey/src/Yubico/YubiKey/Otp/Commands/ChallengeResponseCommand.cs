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

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    /// Issues a challenge to the YubiKey. The slot must be configured in challenge-response mode for
    /// this command's response to be valid.
    /// </summary>
    public class ChallengeResponseCommand : IYubiKeyCommand<ChallengeResponseResponse>
    {
        private const byte ShortPressSlot = 0x20;
        private const byte LongPressSlot = 0x28;
        private const byte UseHmacSha1 = 0x10;

        private Slot _otpSlot = Slot.ShortPress;
        private ChallengeResponseAlgorithm _algorithm;
        private readonly ReadOnlyMemory<byte> _challenge;

        /// <summary>
        /// The OTP slot to issue the challenge.
        /// </summary>
        public Slot OtpSlot
        {
            get => _otpSlot;
            private set
            {
                if (value != Slot.ShortPress && value != Slot.LongPress)
                {
                    throw new ArgumentException(ExceptionMessages.InvalidOtpSlot, nameof(value));
                }

                _otpSlot = value;
            }
        }

        /// <summary>
        /// The algorithm the YubiKey should use when generating the response.
        /// </summary>
        public ChallengeResponseAlgorithm Algorithm
        {
            get => _algorithm;
            private set
            {
                if (value != ChallengeResponseAlgorithm.HmacSha1 && value != ChallengeResponseAlgorithm.YubicoOtp)
                {
                    throw new ArgumentException(ExceptionMessages.InvalidOtpChallengeResponseAlgorithm, nameof(value));
                }

                _algorithm = value;
            }
        }

        public YubiKeyApplication Application => YubiKeyApplication.Otp;

        // We explicitly do not want a default constructor for this command.
        private ChallengeResponseCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="ChallengeResponseCommand"/> class with the
        /// challenge and slot information supplied.
        /// </summary>
        /// <param name="slot">The OTP slot to issue the challenge.</param>
        /// <param name="algorithm">The algorithm the YubiKey should use when generating the response.</param>
        /// <param name="challenge">
        /// The challenge to send to the YubiKey. For the YubicoOtp algorithm, the challenge must be
        /// exactly 6 bytes. If the algorithm is HmacSha1, the challenge must be 64 bytes, or 0-63 bytes
        /// if the <see cref="ConfigurationFlags.HmacLessThan64Bytes"/> is set on this slot's configuration.
        /// </param>
        public ChallengeResponseCommand(
            Slot slot,
            ChallengeResponseAlgorithm algorithm,
            ReadOnlyMemory<byte> challenge)
        {
            if (algorithm == ChallengeResponseAlgorithm.YubicoOtp && challenge.Length != 6)
            {
                throw new ArgumentException(ExceptionMessages.YubicoOtpChallengeLengthInvalid, nameof(challenge));
            }

            if (algorithm == ChallengeResponseAlgorithm.HmacSha1 && challenge.Length > 64)
            {
                throw new ArgumentException(ExceptionMessages.HmacChallengeTooLong, nameof(challenge));
            }

            _challenge = challenge;
            OtpSlot = slot;
            Algorithm = algorithm;
        }

        /// <inheritdoc/>
        public CommandApdu CreateCommandApdu()
        {
            byte otpSlot = OtpSlot == Slot.ShortPress
                ? ShortPressSlot
                : LongPressSlot;

            if (Algorithm == ChallengeResponseAlgorithm.HmacSha1)
            {
                otpSlot |= UseHmacSha1;
            }

            return new CommandApdu()
            {
                Ins = OtpConstants.RequestSlotInstruction,
                P1 = otpSlot,
                Data = _challenge
            };
        }

        /// <inheritdoc/>
        public ChallengeResponseResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ChallengeResponseResponse(responseApdu, Algorithm);
    }
}
