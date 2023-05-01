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

using System;

namespace Yubico.YubiKey
{
    /// <summary>
    /// Contains the information about a sample provided for Bio enrollment.
    /// </summary>
    /// <remarks>
    /// When enrolling a fingerprint, it generally requires multiple readings of
    /// the fingerprint before it is enrolled. This class is used to report the
    /// result of one reading.
    /// <para>
    /// Generally, when a user wants to enroll a fingerprint, the app will
    /// initialize the process, and call on the YubiKey to take a reading. The
    /// app notifies the user that it is time to press their finger to the reader
    /// and when they do so, the YubiKey will take a reading.
    /// </para>
    /// <para>
    /// This class contains the information the YubiKey returns to describe the
    /// results of that reading.
    /// </para>
    /// </remarks>
    public class BioEnrollSampleResult
    {
        /// <summary>
        /// The template ID of the fingerprint being enrolled.
        /// </summary>
        public ReadOnlyMemory<byte> TemplateId { get; private set; }

        /// <summary>
        /// The result of the most recent attempt to provide a fingerprint
        /// sample.
        /// </summary>
        public BioEnrollSampleStatus LastEnrollSampleStatus { get; private set; }

        /// <summary>
        /// The number of successful fingerprint samples required to complete an
        /// enrollment.
        /// </summary>
        public int RemainingSampleCount { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private BioEnrollSampleResult()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of <see cref="BioEnrollSampleResult"/> with the
        /// given values.
        /// </summary>
        /// <param name="templateId">
        /// The ID of the fingerprint being enrolled.
        /// </param>
        /// <param name="lastEnrollSampleStatus">
        /// The status code of the most recent sample attempt.
        /// </param>
        /// <param name="remainingSampleCount">
        /// The number of successful readings necessary to enroll the fingerprint.
        /// </param>
        public BioEnrollSampleResult(
            ReadOnlyMemory<byte> templateId,
            int lastEnrollSampleStatus,
            int remainingSampleCount)
        {
            TemplateId = templateId;
            LastEnrollSampleStatus = Enum.IsDefined(typeof(BioEnrollSampleStatus), lastEnrollSampleStatus) ?
                (BioEnrollSampleStatus)lastEnrollSampleStatus
                : BioEnrollSampleStatus.Unknown;
            RemainingSampleCount = remainingSampleCount;
        }
    }
}
