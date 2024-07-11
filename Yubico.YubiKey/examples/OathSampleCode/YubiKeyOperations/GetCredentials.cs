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
using System.Text;
using Yubico.YubiKey.Oath;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.OathSampleCode
{
    public static class GetCredentials
    {
        // Get all configured credentials on the YbiKey.
        public static bool RunGetCredentials(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using var oathSession = new OathSession(yubiKey);
            {
                oathSession.KeyCollector = KeyCollectorDelegate;
                IList<Credential> result = oathSession.GetCredentials();
                ReportResult(result);
            }

            return true;
        }

        private static void ReportResult(IList<Credential> credentials)
        {
            var outputList = new StringBuilder("");
            if (credentials.Count > 0)
            {
                _ = outputList.AppendLine($"Number of credentials: {credentials.Count}");
                _ = outputList.AppendLine();
                foreach (Credential currentCredential in credentials)
                {
                    _ = outputList.AppendLine($"Issuer    : {currentCredential.Issuer}");
                    _ = outputList.AppendLine($"Account   : {currentCredential.AccountName}");
                    _ = outputList.AppendLine($"Type      : {currentCredential.Type}");
                    _ = outputList.AppendLine($"Period    : {(int?)currentCredential.Period}sec");
                    _ = outputList.AppendLine($"Algorithm : {currentCredential.Algorithm}");
                    _ = outputList.AppendLine($"Name      : {currentCredential.Name}");
                    _ = outputList.AppendLine();
                }
            }
            else
            {
                _ = outputList.AppendLine("No credentials on this YubiKey");
            }

            SampleMenu.WriteMessage(MessageType.Special, 0, outputList.ToString());
        }
    }
}
