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
using System.Collections.Generic;
using System.Linq;
using Yubico.YubiKey.Oath;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.OathSampleCode
{
    public static class ChooseCredentialProperties
    {
        // Choose credential type.
        public static bool RunChooseTypeOption(SampleMenu menuObject, out CredentialType? type)
        {
            if (menuObject is null)
            {
                throw new ArgumentNullException(nameof(menuObject));
            }

            type = null;
            // Write out a menu requesting the caller choose one.
            string[] choices = _types.Keys.ToArray();

            int indexChosen = menuObject.RunMenu("Choose credential type", choices);

            if (indexChosen >= 0 && indexChosen < choices.Length)
            {
                type = _types[choices[indexChosen]];
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Special, 0, "Invalid response");
            return false;
        }

        // Choose credential period.
        public static bool RunChoosePeriodOption(SampleMenu menuObject, out CredentialPeriod? period)
        {
            if (menuObject is null)
            {
                throw new ArgumentNullException(nameof(menuObject));
            }

            period = null;
            // Write out a menu requesting the caller choose one.
            string[] choices = _periods.Keys.ToArray();

            int indexChosen = menuObject.RunMenu("Choose credential period", choices);

            if (indexChosen >= 0 && indexChosen < choices.Length)
            {
                period = _periods[choices[indexChosen]];
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Special, 0, "Invalid response");
            return false;
        }

        // Choose credential algorithm.
        public static bool RunChooseAlgorithmOption(SampleMenu menuObject, out HashAlgorithm? algorithm)
        {
            if (menuObject is null)
            {
                throw new ArgumentNullException(nameof(menuObject));
            }

            algorithm = null;

            // Write out a menu requesting the caller choose one.
            string[] choices = _hashAlgorithms.Keys.ToArray();

            int indexChosen = menuObject.RunMenu("Choose credential algorithm", choices);

            if (indexChosen >= 0 && indexChosen < choices.Length)
            {
                algorithm = _hashAlgorithms[choices[indexChosen]];
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Special, 0, "Invalid response");
            return false;
        }

        // Choose credential digits.
        public static bool RunChooseDigitsOption(SampleMenu menuObject, out int? digits)
        {
            if (menuObject is null)
            {
                throw new ArgumentNullException(nameof(menuObject));
            }

            digits = null;
            // Write out a menu requesting the caller choose one.
            string[] choices = _digits.Keys.ToArray();

            int indexChosen = menuObject.RunMenu("Choose the number of digits in OTP code", choices);

            if (indexChosen >= 0 && indexChosen < choices.Length)
            {
                digits = _digits[choices[indexChosen]];
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Special, 0, "Invalid response");
            return false;
        }

        private static readonly Dictionary<string, CredentialType> _types =
            new Dictionary<string, CredentialType>
            {
                ["HOTP"] = CredentialType.Hotp,
                ["TOTP"] = CredentialType.Totp
            };

        private static readonly Dictionary<string, CredentialPeriod> _periods =
            new Dictionary<string, CredentialPeriod>
            {
                ["15sec"] = CredentialPeriod.Period15,
                ["30sec"] = CredentialPeriod.Period30,
                ["60sec"] = CredentialPeriod.Period60
            };

        private static readonly Dictionary<string, HashAlgorithm> _hashAlgorithms =
            new Dictionary<string, HashAlgorithm>
            {
                ["SHA-1"] = HashAlgorithm.Sha1,
                ["SHA-256"] = HashAlgorithm.Sha256,
                ["SHA-512"] = HashAlgorithm.Sha512
            };

        private static readonly Dictionary<string, int> _digits =
            new Dictionary<string, int>
            {
                ["6 digits"] = 6,
                ["7 digits"] = 7,
                ["8 digits"] = 8
            };
    }
}
