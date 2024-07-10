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
using System.Linq;
using System.Text;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class YubiKeyFeaturePlugin : PluginBase
    {
        public YubiKeyFeaturePlugin(IOutput output) : base(output) { }
        public override string Name => "Feature query";

        public override string Description => "This plugin displays different features of YubiKeys.";

        public override bool Execute()
        {
            char inputChar;
            do
            {
                Output.WriteLine("YubiKey Feature Options");
                Output.WriteLine("1. General");
                Output.WriteLine("2. OTP Application");
                Output.WriteLine("3. PIV Application");
                Output.WriteLine("4. OATH Application");
                Output.WriteLine();
                Output.Write("Select an option, or any other key to exit: ");

                inputChar = Console.ReadKey().KeyChar;
                Output.WriteLine();

                var feature = inputChar switch
                {
                    '1' => GetGeneralFeature(),
                    '2' => GetOtpFeature(),
                    '3' => GetPivFeature(),
                    '4' => GetOathFeature(),
                    _ => throw new NotImplementedException()
                };

                Output.WriteLine(Eol + "");
                OutputResult(feature);
            } while (inputChar >= '1' && inputChar <= '4');

            return true;
        }

        private void OutputResult(YubiKeyFeature feature)
        {
            var yubiKey = YubiKeyDevice.FindAll().First();
            var result = yubiKey.HasFeature(feature);

            if (result)
            {
                Output.WriteLine($"The feature [{feature}] is available on this YubiKey");
                Output.WriteLine();
            }
            else
            {
                Output.WriteLine($"The feature [{feature}] is not available on this YubiKey");
                Output.WriteLine();
            }
        }

        private YubiKeyFeature GetGeneralFeature()
        {
            YubiKeyFeature feature;

            Output.WriteLine("YubiKey General Features");
            Output.WriteLine("1. OTP application");
            Output.WriteLine("2. PIV application");
            Output.WriteLine("3. OATH application");
            Output.WriteLine("4. Management application");
            Output.WriteLine("5. Serial Number visibility controls");
            Output.WriteLine();
            Output.Write("Select an option, or any other key to exit: ");

            var inputChar = Console.ReadKey().KeyChar;
            Output.WriteLine();

            feature = inputChar switch
            {
                '1' => YubiKeyFeature.OtpApplication,
                '2' => YubiKeyFeature.PivApplication,
                '3' => YubiKeyFeature.OathApplication,
                '4' => YubiKeyFeature.ManagementApplication,
                '5' => YubiKeyFeature.SerialNumberVisibilityControls,
                _ => throw new NotImplementedException()
            };

            return feature;
        }

        private YubiKeyFeature GetPivFeature()
        {
            YubiKeyFeature feature;

            Output.WriteLine("YubiKey PIV Features");
            Output.WriteLine("1. Attestation");
            Output.WriteLine("2. Metadata");
            Output.WriteLine("3. RSA-1024 algorithm");
            Output.WriteLine("4. RSA-2048 algorithm");
            Output.WriteLine("5. ECC-P256 algorithm");
            Output.WriteLine("6. ECC-P384 algorithm");
            Output.WriteLine("7. Touch Policy for management key");
            Output.WriteLine("8. Touch Policy - Cached");
            Output.WriteLine("9. Touch Policy - Cached, for private key");
            Output.WriteLine();
            Output.Write("Select an option, or any other key to exit: ");

            var inputChar = Console.ReadKey().KeyChar;
            Output.WriteLine();

            feature = inputChar switch
            {
                '1' => YubiKeyFeature.PivAttestation,
                '2' => YubiKeyFeature.PivMetadata,
                '3' => YubiKeyFeature.PivRsa1024,
                '4' => YubiKeyFeature.PivRsa2048,
                '5' => YubiKeyFeature.PivEccP256,
                '6' => YubiKeyFeature.PivEccP384,
                '7' => YubiKeyFeature.PivManagementKeyTouchPolicy,
                '8' => YubiKeyFeature.PivTouchPolicyCached,
                '9' => YubiKeyFeature.PivPrivateKeyTouchPolicyCached,
                _ => throw new NotImplementedException()
            };

            return feature;
        }

        private YubiKeyFeature GetOtpFeature()
        {
            YubiKeyFeature feature;

            Output.WriteLine("YubiKey OTP Features");
            Output.WriteLine("1. OATH HOTP mode");
            Output.WriteLine("2. Protected long-press slot");
            Output.WriteLine("3. Numeric keypad");
            Output.WriteLine("4. Fast trigger");
            Output.WriteLine("5. Updatable slots");
            Output.WriteLine("6. Dormant slots");
            Output.WriteLine("7. Invert LED");
            Output.WriteLine("8. Short tickets");
            Output.WriteLine("9. Static password mode");
            Output.WriteLine("10. Variable size HMAC");
            Output.WriteLine("11. Button trigger");
            Output.WriteLine("12. Mixed case passwords");
            Output.WriteLine("13. Fixed modhex");
            Output.WriteLine("14. Challenge-Response mode");
            Output.WriteLine("15. Alpha numeric passwords");
            Output.WriteLine("16. Password manual updates");
            Output.WriteLine();
            Output.Write("Select an option and press ENTER. Press any other key to exit: ");

            var inputKey = Console.ReadKey(intercept: true);
            var inputStr = new StringBuilder(inputKey.KeyChar.ToString());
            Console.Write(inputStr);

            while (inputKey.Key != ConsoleKey.Enter)
            {
                inputKey = Console.ReadKey(intercept: true);
                if (inputKey.Key != ConsoleKey.Enter)
                {
                    _ = inputStr.Append(inputKey.KeyChar);
                    Console.Write(inputKey.KeyChar.ToString());
                }
            }

            feature = inputStr.ToString() switch
            {
                "1" => YubiKeyFeature.OtpOathHotpMode,
                "2" => YubiKeyFeature.OtpProtectedLongPressSlot,
                "3" => YubiKeyFeature.OtpNumericKeypad,
                "4" => YubiKeyFeature.OtpFastTrigger,
                "5" => YubiKeyFeature.OtpUpdatableSlots,
                "6" => YubiKeyFeature.OtpDormantSlots,
                "7" => YubiKeyFeature.OtpInvertLed,
                "8" => YubiKeyFeature.OtpShortTickets,
                "9" => YubiKeyFeature.OtpStaticPasswordMode,
                "10" => YubiKeyFeature.OtpVariableSizeHmac,
                "11" => YubiKeyFeature.OtpButtonTrigger,
                "12" => YubiKeyFeature.OtpMixedCasePasswords,
                "13" => YubiKeyFeature.OtpFixedModhex,
                "14" => YubiKeyFeature.OtpChallengeResponseMode,
                "15" => YubiKeyFeature.OtpAlphaNumericPasswords,
                "16" => YubiKeyFeature.OtpPasswordManualUpdates,
                _ => throw new NotImplementedException()
            };

            return feature;
        }

        private YubiKeyFeature GetOathFeature()
        {
            YubiKeyFeature feature;

            Output.WriteLine("YubiKey OATH Features");
            Output.WriteLine("1. Rename credential");
            Output.WriteLine("2. Touch credential");
            Output.WriteLine("3. SHA-512 algorithm");
            Output.WriteLine();
            Output.Write("Select an option, or any other key to exit: ");

            var inputChar = Console.ReadKey().KeyChar;
            Output.WriteLine();

            feature = inputChar switch
            {
                '1' => YubiKeyFeature.OathRenameCredential,
                '2' => YubiKeyFeature.OathTouchCredential,
                '3' => YubiKeyFeature.OathSha512,
                _ => throw new NotImplementedException()
            };

            return feature;
        }
    }
}
