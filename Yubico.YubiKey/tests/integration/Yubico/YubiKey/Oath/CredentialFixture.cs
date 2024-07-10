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

namespace Yubico.YubiKey.Oath
{
    public class CredentialFixture
    {
        public CredentialFixture()
        {
            TotpCredential = new Credential
            {
                Issuer = "Microsoft",
                AccountName = "test@outlook.com",
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period15,
                Algorithm = HashAlgorithm.Sha1
            };

            HotpCredential = new Credential
            {
                Issuer = "Apple",
                AccountName = "test@icloud.com",
                Type = CredentialType.Hotp,
                Period = CredentialPeriod.Undefined,
                Algorithm = HashAlgorithm.Sha1
            };

            TotpWithTouchCredential = new Credential
            {
                Issuer = "Yubico",
                AccountName = "test@yubico.com",
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period30,
                Algorithm = HashAlgorithm.Sha1,
                RequiresTouch = true
            };

            TotpWithSha512Credential = new Credential
            {
                Issuer = "Facebook",
                AccountName = "test@yubico.com",
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period30,
                Algorithm = HashAlgorithm.Sha512
            };

            TotpCredentialWithDefaultPeriod = new Credential
            {
                Issuer = "Amazon",
                AccountName = "test@gmail.com",
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period30,
                Algorithm = HashAlgorithm.Sha1
            };

            CredentialToDelete = new Credential
            {
                Issuer = "Twitter",
                AccountName = "test@gmail.com",
                Type = CredentialType.Hotp,
                Period = CredentialPeriod.Undefined,
                Algorithm = HashAlgorithm.Sha1
            };
        }

        public Credential TotpCredential { get; private set; }

        public Credential HotpCredential { get; private set; }

        public Credential TotpWithTouchCredential { get; private set; }

        public Credential TotpWithSha512Credential { get; private set; }

        public Credential TotpCredentialWithDefaultPeriod { get; private set; }

        public Credential CredentialToDelete { get; private set; }
    }
}
