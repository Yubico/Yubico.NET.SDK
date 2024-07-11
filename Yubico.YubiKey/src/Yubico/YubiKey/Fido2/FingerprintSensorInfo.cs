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

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    ///     Contains the data returned by the YubiKey after calling the
    ///     <c>GetFingerprintSensorInfo</c> subcommand.
    /// </summary>
    public class FingerprintSensorInfo
    {
        // The default constructor explicitly defined. We don't want it to be
        // used.
        private FingerprintSensorInfo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Build a new instance of <see cref="FingerprintSensorInfo" /> with the
        ///     given values.
        /// </summary>
        /// <param name="fingerprintKind">
        ///     The technique of fingerprint capture the YubiKey uses. The standard
        ///     defines the value of <c>1</c> as touch type and <c>2</c> as swipe
        ///     type.
        /// </param>
        /// <param name="maxCaptureCount">
        ///     The number of fingerprint captures needed to enroll a fingerprint.
        /// </param>
        /// <param name="maxFrienlyNameBytes">
        ///     The maximum number of bytes available to hold a template's friendly
        ///     name.
        /// </param>
        public FingerprintSensorInfo(int fingerprintKind, int maxCaptureCount, int maxFrienlyNameBytes)
        {
            FingerprintKind = fingerprintKind;
            MaxCaptureCount = maxCaptureCount;
            MaxFriendlyNameBytes = maxFrienlyNameBytes;
        }

        /// <summary>
        ///     The kind of fingerprint reader, that is, what method the fingerprint
        ///     reader uses.
        /// </summary>
        /// <remarks>
        ///     The value returned by the YubiKey is an integer. The standard defines
        ///     what each integer means. Currently only two integers are defined:
        ///     <c>1</c> for touch type sensor and <c>2</c> for swipe type sensor.
        /// </remarks>
        public int FingerprintKind { get; private set; }

        /// <summary>
        ///     The number of "good" fingerprint captures required to enroll.
        /// </summary>
        /// <remarks>
        ///     In order to enroll a fingerprint, it is necessary to provide the
        ///     fingerprint. The reader will need several examples of the
        ///     fingerprint, generally from a number of angles. Once it has
        ///     <c>MaxCaptureCount</c> good examples, it will consider the
        ///     fingerprint enrolled. If it cannot get enough "good" examples, it
        ///     will not accept the fingerprint and will require the user to start
        ///     over.
        /// </remarks>
        public int MaxCaptureCount { get; private set; }

        /// <summary>
        ///     The maximum length, in bytes, of a template friendly name.
        /// </summary>
        /// <remarks>
        ///     A "template" is one biometric entry: all the data the YubiKey stores
        ///     for one fingerprint. The FIDO2 standard uses the word template
        ///     because there could be several types of biometric entries, such as
        ///     fingerprint, face, voice, or more. So one template is one entry. The
        ///     friendly name can be "right-index" or "left-thumb".
        ///     <para>
        ///         With the SDK, you provide a friendly name as a string. Before it is
        ///         stored, it is converted to UTF-8. The length, in bytes, of the
        ///         friendly name is therefore the number of bytes in the UTF-8 array
        ///         that is the converted string.
        ///     </para>
        /// </remarks>
        public int MaxFriendlyNameBytes { get; private set; }
    }
}
