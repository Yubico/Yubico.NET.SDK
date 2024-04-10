// Copyright 2022 Yubico AB
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

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    /// This class is used to perform integration testing on the YubiHSM
    /// Auth session methods. It returns values stored in
    /// <see cref="YhaTestUtilities"/>.
    /// </summary>
    public class SimpleKeyCollector
    {
        /// <summary>
        /// Used by the flip flop delegate to determine whether to return
        /// "default" or "alternate" values.
        /// </summary>
        public bool UseDefaultValue = true;

        /// <summary>
        /// Alternates between returning values from the "default" and
        /// "alternate" set.
        /// </summary>
        public bool FlipFlopCollectorDelegate(KeyEntryData keyEntryData)
        {
            bool returnValue =
                UseDefaultValue
                ? DefaultValueCollectorDelegate(keyEntryData)
                : AlternateValueCollectorDelegate(keyEntryData);

            UseDefaultValue = !UseDefaultValue;
            return returnValue;
        }

        /// <summary>
        /// Returns values from the "default" set.
        /// </summary>
        public static bool DefaultValueCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            switch (keyEntryData.Request)
            {
                case KeyEntryRequest.AuthenticateYubiHsmAuthManagementKey:
                    keyEntryData.SubmitValue(YhaTestUtilities.DefaultMgmtKey);
                    return true;

                case KeyEntryRequest.ChangeYubiHsmAuthManagementKey:
                    keyEntryData.SubmitValues(YhaTestUtilities.DefaultMgmtKey, YhaTestUtilities.AlternateMgmtKey);
                    return true;

                case KeyEntryRequest.AuthenticateYubiHsmAuthCredentialPassword:
                    keyEntryData.SubmitValue(YhaTestUtilities.DefaultCredPassword);
                    return true;

                case KeyEntryRequest.TouchRequest:
                    // "default" cred does not require touch, so we should
                    // never get this request.
                    throw new InvalidOperationException("Unexpected key entry request: TouchRequest");

                case KeyEntryRequest.Release:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns values from the "alternate" set.
        /// </summary>
        public static bool AlternateValueCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            switch (keyEntryData.Request)
            {
                case KeyEntryRequest.AuthenticateYubiHsmAuthManagementKey:
                    keyEntryData.SubmitValue(YhaTestUtilities.AlternateMgmtKey);
                    return true;

                case KeyEntryRequest.ChangeYubiHsmAuthManagementKey:
                    keyEntryData.SubmitValues(YhaTestUtilities.AlternateMgmtKey, YhaTestUtilities.DefaultMgmtKey);
                    return true;

                case KeyEntryRequest.AuthenticateYubiHsmAuthCredentialPassword:
                    keyEntryData.SubmitValue(YhaTestUtilities.AlternateCredPassword);
                    return true;

                case KeyEntryRequest.TouchRequest:
                    // For integration tests that require touch, run the test
                    // in "debug" and add a breakpoint here so you know when
                    // to provide touch
                    return true;

                case KeyEntryRequest.Release:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Always returns false.
        /// </summary>
        public static bool ReturnsFalseCollectorDelegate(KeyEntryData _) => false;
    }
}
