// Copyright 2023 Yubico AB
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

namespace Yubico.YubiKey
{
    public enum BioEnrollSampleStatus
    {
        // CTAP2_ENROLL_FEEDBACK_FP_GOOD
        FpGood = 0x00,

        // CTAP2_ENROLL_FEEDBACK_FP_TOO_HIGH
        FpTooHigh = 0x01,

        // CTAP2_ENROLL_FEEDBACK_FP_TOO_LOW
        FpTooLow = 0x02,

        // CTAP2_ENROLL_FEEDBACK_FP_TOO_LEFT
        FpTooLeft = 0x03,

        // CTAP2_ENROLL_FEEDBACK_FP_TOO_RIGHT
        FpTooRight = 0x04,

        // CTAP2_ENROLL_FEEDBACK_FP_TOO_FAST
        FpTooFast = 0x05,

        // CTAP2_ENROLL_FEEDBACK_FP_TOO_SLOW
        FpTooSlow = 0x06,

        // CTAP2_ENROLL_FEEDBACK_FP_POOR_QUALITY
        FpPoorQuality = 0x07,

        // CTAP2_ENROLL_FEEDBACK_FP_TOO_SKEWED
        FpTooSkewed = 0x08,

        // CTAP2_ENROLL_FEEDBACK_FP_TOO_SHORT
        FpTooShort = 0x09,

        // CTAP2_ENROLL_FEEDBACK_FP_MERGE_FAILURE
        FpMergeFailure = 0x0A,

        // CTAP2_ENROLL_FEEDBACK_FP_EXISTS
        FpExists = 0x0B,

        // CTAP2_ENROLL_FEEDBACK_NO_USER_ACTIVITY
        NoUserActivity = 0x0D,

        // CTAP2_ENROLL_FEEDBACK_NO_USER_PRESENCE_TRANSITION
        NoUserPresenceTransition = 0x0E,

        Unknown = -1,
    }
}
