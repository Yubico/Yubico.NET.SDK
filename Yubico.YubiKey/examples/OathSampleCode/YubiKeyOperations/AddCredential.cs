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
using System.Globalization;
using System.Text;
using Yubico.YubiKey.Oath;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.OathSampleCode
{
    public static class AddCredential
    {
        // Add a non-default TOTP credential.
        // If menuObject is null it means the user picked to add example credential,
        // otherwise we prompt to enter a custom credential.
        public static bool RunAddTotpCredential(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            SampleMenu menuObject)
        {
            using var oathSession = new OathSession(yubiKey);
            {
                oathSession.KeyCollector = KeyCollectorDelegate;

                if (menuObject is null)
                {
                    var credential = new Credential
                    {
                        Issuer = "Yubico",
                        AccountName = "testTotp@example.com",
                        Type = CredentialType.Totp,
                        Period = CredentialPeriod.Period60,
                        Algorithm = HashAlgorithm.Sha1,
                        Secret = "test2345",
                        Digits = 6
                    };

                    oathSession.AddCredential(credential);
                    ReportResult(credential);
                }
                else
                {
                    Credential credential = CollectTotpCredential(menuObject);
                    oathSession.AddCredential(credential);
                    ReportResult(credential);
                }
            }

            return true;
        }

        // Add non-default HOTP credential.
        // If menuObject is null it means the user picked to add example credential,
        // otherwise we prompt to enter a custom credential.
        public static bool RunAddHotpCredential(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            SampleMenu menuObject)
        {
            using var oathSession = new OathSession(yubiKey);
            {
                oathSession.KeyCollector = KeyCollectorDelegate;

                if (menuObject is null)
                {
                    var credential = new Credential
                    {
                        Issuer = "Yubico",
                        AccountName = "testHotp@example.com",
                        Type = CredentialType.Hotp,
                        Period = CredentialPeriod.Undefined,
                        Algorithm = HashAlgorithm.Sha1,
                        Secret = "test2345",
                        Digits = 8,
                        Counter = 100,
                        RequiresTouch = true
                    };

                    oathSession.AddCredential(credential);
                    ReportResult(credential);
                }
                else
                {
                    Credential credential = CollectHotpCredential(menuObject);
                    oathSession.AddCredential(credential);
                    ReportResult(credential);
                }
            }

            return true;
        }

        // Add a default TOTP credential.
        // If menuObject is null it means the user picked to add example credential,
        // otherwise we prompt to enter a custom credential.
        public static bool RunAddDefaultTotpCredential(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            SampleMenu menuObject)
        {
            using var oathSession = new OathSession(yubiKey);
            {
                oathSession.KeyCollector = KeyCollectorDelegate;

                if (menuObject is null)
                {
                    Credential credential = oathSession.AddCredential("Yubico", "testDefaultTotp@example.com");
                    ReportResult(credential);
                }
                else
                {
                    Credential credential = CollectDefaultTotpCredential();
                    oathSession.AddCredential(credential);
                    ReportResult(credential);
                }
            }

            return true;
        }

        // Add a default HOTP credential.
        // If menuObject is null it means the user picked to add example credential,
        // otherwise we prompt to enter a custom credential.
        public static bool RunAddDefaultHotpCredential(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            SampleMenu menuObject)
        {
            using var oathSession = new OathSession(yubiKey);
            {
                oathSession.KeyCollector = KeyCollectorDelegate;

                if (menuObject is null)
                {
                    Credential credential = oathSession.AddCredential(
                        "Yubico",
                        "testDefaultHotp@example.com",
                        CredentialType.Hotp,
                        CredentialPeriod.Undefined);
                    ReportResult(credential);
                }
                else
                {
                    Credential credential = CollectDefaultHotpCredential();
                    oathSession.AddCredential(credential);
                    ReportResult(credential);
                }
            }

            return true;
        }

        // Add a credential from URI.
        // If menuObject is null it means the user picked to add example credential,
        // otherwise we prompt to enter a custom credential.
        public static bool RunAddCredentialFromQR(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            SampleMenu menuObject)
        {
            using var oathSession = new OathSession(yubiKey);
            {
                oathSession.KeyCollector = KeyCollectorDelegate;

                if (menuObject is null)
                {
                    Credential credential = oathSession.AddCredential(
                        "otpauth://totp/Yubico:testUri@example.com?secret=YY4KVNOUQ5IIUBAOGIDRYZ7FGY54VW54&issuer=Yubico&algorithm=SHA1&digits=6&period=30");
                    ReportResult(credential);
                }
                else
                {
                    string stringFromUri = CollectStringFromUri();
                    Credential credential = oathSession.AddCredential(stringFromUri);
                    ReportResult(credential);
                }
            }

            return true;
        }

        private static void ReportResult(Credential credential)
        {
            var outputList = new StringBuilder("Added credential:");
            _ = outputList.AppendLine();
            _ = outputList.AppendLine($"Issuer    : {credential.Issuer}");
            _ = outputList.AppendLine($"Account   : {credential.AccountName}");
            _ = outputList.AppendLine($"Type      : {credential.Type}");
            _ = outputList.AppendLine($"Period    : {(int?)credential.Period}sec");
            _ = outputList.AppendLine($"Digits    : {credential.Digits}");
            _ = outputList.AppendLine($"Algorithm : {credential.Algorithm}");
            _ = outputList.AppendLine($"Secret    : {credential.Secret}");
            _ = outputList.AppendLine($"Counter   : {credential.Counter}");
            _ = outputList.AppendLine($"Touch     : {credential.RequiresTouch}");

            SampleMenu.WriteMessage(MessageType.Special, 0, outputList.ToString());
        }

        // Collect a default TOTP credential.
        private static Credential CollectDefaultTotpCredential()
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter issuer");
            _ = SampleMenu.ReadResponse(out string issuer);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter account name");
            _ = SampleMenu.ReadResponse(out string account);

            var credential = new Credential(issuer, account, CredentialType.Totp, CredentialPeriod.Period30);

            return credential;
        }

        // Collect a default HOTP credential.
        private static Credential CollectDefaultHotpCredential()
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter issuer");
            _ = SampleMenu.ReadResponse(out string issuer);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter account name");
            _ = SampleMenu.ReadResponse(out string account);

            var credential = new Credential(issuer, account, CredentialType.Hotp, CredentialPeriod.Undefined);

            return credential;
        }

        // Collect a string from URI.
        private static string CollectStringFromUri()
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter string from URI");
            _ = SampleMenu.ReadResponse(out string stringFromUri);

            return stringFromUri;
        }

        // Collect a TOTP credential.
        private static Credential CollectTotpCredential(SampleMenu menuObject)
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter issuer");
            _ = SampleMenu.ReadResponse(out string issuer);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter account name");
            _ = SampleMenu.ReadResponse(out string account);

            _ = ChooseCredentialProperties.RunChoosePeriodOption(menuObject, out CredentialPeriod? period);

            _ = ChooseCredentialProperties.RunChooseAlgorithmOption(menuObject, out HashAlgorithm? algorithm);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter secret");
            _ = SampleMenu.ReadResponse(out string secret);

            _ = ChooseCredentialProperties.RunChooseDigitsOption(menuObject, out int? digits);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Set require touch property? Answer Yes or No.");
            _ = SampleMenu.ReadResponse(out string touch);

            var credential = new Credential
            {
                Issuer = issuer,
                AccountName = account,
                Type = CredentialType.Totp,
                Period = period,
                Algorithm = algorithm,
                Secret = secret,
                Digits = digits,
                RequiresTouch = touch.Equals("Yes", StringComparison.OrdinalIgnoreCase)
            };

            return credential;
        }

        // Collect a HOTP credential.
        private static Credential CollectHotpCredential(SampleMenu menuObject)
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter issuer");
            _ = SampleMenu.ReadResponse(out string issuer);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter account name");
            _ = SampleMenu.ReadResponse(out string account);

            _ = ChooseCredentialProperties.RunChooseAlgorithmOption(menuObject, out HashAlgorithm? algorithm);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter secret");
            _ = SampleMenu.ReadResponse(out string secret);

            _ = ChooseCredentialProperties.RunChooseDigitsOption(menuObject, out int? digits);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter counter.");
            _ = SampleMenu.ReadResponse(out string counter);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Set require touch property? Answer Yes or No.");
            _ = SampleMenu.ReadResponse(out string touch);

            var credential = new Credential
            {
                Issuer = issuer,
                AccountName = account,
                Type = CredentialType.Hotp,
                Period = CredentialPeriod.Undefined,
                Algorithm = algorithm,
                Secret = secret,
                Digits = digits,
                Counter = int.Parse(counter, CultureInfo.InvariantCulture),
                RequiresTouch = touch.Equals("Yes", StringComparison.OrdinalIgnoreCase)
            };

            return credential;
        }
    }
}
