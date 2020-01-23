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
using Xunit;

namespace Yubico.YubiKey.Oath
{
    public class CredentialTests
    {
        [Fact]
        public void CredentialName_TotpTypeAndDefaultPeriod_ReturnsCorrectlyParsedLabel()
        {
            var credential = new Credential("Microsoft", "test@outlook.com", CredentialType.Totp, CredentialPeriod.Period30);

            Assert.Equal("Microsoft:test@outlook.com", credential.Name);
        }

        [Fact]
        public void CredentialName_TotpTypeAndPeriod15_ReturnsCorrectlyParsedLabel()
        {
            var credential = new Credential("Microsoft", "test@outlook.com", CredentialType.Totp, CredentialPeriod.Period15);

            Assert.Equal("15/Microsoft:test@outlook.com", credential.Name);
        }

        [Fact]
        public void CredentialName_HotpTypeAndUndefinedPeriod_ReturnsCorrectlyParsedLabel()
        {
            var credential = new Credential("Apple", "test@icloud.com", CredentialType.Hotp, CredentialPeriod.Undefined);

            Assert.Equal("Apple:test@icloud.com", credential.Name);
        }

        [Fact]
        public void CredentialIssuerAndAccount_UriUnescape_ReturnsCorrectUnescapedStrings()
        {
            var issuer = Uri.UnescapeDataString("Microsoft%3Ademo");
            var account = Uri.UnescapeDataString("test%40outlook.com");

            Assert.Equal("Microsoft:demo", issuer);
            Assert.Equal("test@outlook.com", account);
        }

        [Fact]
        public void CredentialParseLabel_TotpTypeAndDefaultPeriod_ReturnsCorrectlyParsedLabel()
        {
            var label = "Microsoft:demo:test@outlook.com";
            var parsedLabel = Credential.ParseLabel(label, CredentialType.Totp);
            
            Assert.Equal(CredentialPeriod.Period30, parsedLabel.period);
            Assert.Equal("Microsoft:demo", parsedLabel.issuer);
            Assert.Equal("test@outlook.com", parsedLabel.account);
        }

        [Fact]
        public void CredentialParseLabel_TotpTypeAndNoDefaultPeriod_ReturnsCorrectlyParsedLabel()
        {
            var label = "60/Microsoft:demo:test@outlook.com";
            var parsedLabel = Credential.ParseLabel(label, CredentialType.Totp);

            Assert.Equal(CredentialPeriod.Period60, parsedLabel.period);
            Assert.Equal("Microsoft:demo", parsedLabel.issuer);
            Assert.Equal("test@outlook.com", parsedLabel.account);
        }

        [Fact]
        public void CredentialParseLabel_HotpType_ReturnsCorrectlyParsedLabel()
        {
            var label = "60/Microsoft:demo:test@outlook.com";
            var parsedLabel = Credential.ParseLabel(label, CredentialType.Hotp);

            Assert.Equal(CredentialPeriod.Undefined, parsedLabel.period);
            Assert.Equal("60/Microsoft:demo", parsedLabel.issuer);
            Assert.Equal("test@outlook.com", parsedLabel.account);
        }

        [Fact]
        public void CredentialParseLabel_HotpType_ReturnsCorrectlyParsedLabel_2()
        {
            var label = "Microsoft:test@outlook.com";
            var parsedLabel = Credential.ParseLabel(label, CredentialType.Hotp);

            Assert.Equal(CredentialPeriod.Undefined, parsedLabel.period);
            Assert.Equal("Microsoft", parsedLabel.issuer);
            Assert.Equal("test@outlook.com", parsedLabel.account);
        }

        [Fact]
        public void CredentialAccountNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", string.Empty, CredentialType.Totp, HashAlgorithm.Sha1, "tt", CredentialPeriod.Period30, 6, 0, false);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialNameNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", "HLKTFGJRUY6S2NWRMH7GN32IJNNJRFNQZLTIS4FB6E5COG@outlook.com", CredentialType.Totp, HashAlgorithm.Sha1, "tt", CredentialPeriod.Period30, 6, 0, false);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialTypeNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", "test@gmail.com", (CredentialType)0x03, HashAlgorithm.Sha1, "tt", CredentialPeriod.Period30, 6, 0, false);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialAlgorithmNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", "test@gmail.com", CredentialType.Totp, (HashAlgorithm)0x04, "tt", CredentialPeriod.Period30, 6, 0, false);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialSecretNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", "test@gmail.com", CredentialType.Totp, HashAlgorithm.Sha1, "8900", CredentialPeriod.Period30, 6, 0, false);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialPeriodNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", "test@gmail.com", CredentialType.Totp, HashAlgorithm.Sha1, "tt", (CredentialPeriod)32, 6, 0, false);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialDigitsNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", "test@gmail.com", CredentialType.Totp, HashAlgorithm.Sha1, "tt", CredentialPeriod.Period30, 4, 0, false);

            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialParseUri_NoOptionalParameters_ReturnsCorrectlyParsedCredentialParameters()
        {
            var uri = new Uri("otpauth://totp/Microsoft:test@outlook.com?secret=TEST&issuer=Microsoft");
            var parsedCredential= Credential.ParseUri(uri);

            Assert.Equal("Microsoft", parsedCredential.Issuer);
            Assert.Equal("test@outlook.com", parsedCredential.AccountName);
            Assert.Equal("TEST", parsedCredential.Secret);
            Assert.Equal(CredentialType.Totp, parsedCredential.Type);
            Assert.Equal(HashAlgorithm.Sha1, parsedCredential.Algorithm);
            Assert.Equal(CredentialPeriod.Period30, parsedCredential.Period);
            Assert.Equal(6, parsedCredential.Digits);
            Assert.Null(parsedCredential.Counter);
        }

        [Fact]
        public void CredentialParseUri_WithOptionalParameters_ReturnsCorrectlyParsedCredentialParameters()
        {
            var uri = new Uri("otpauth://totp/Microsoft%3Ademo:test@outlook.com?secret=TEST&issuer=Microsoft%3Ademo&algorithm=SHA256&digits=7&period=60");
            var parsedCredential = Credential.ParseUri(uri);

            Assert.Equal("Microsoft:demo", parsedCredential.Issuer);
            Assert.Equal("test@outlook.com", parsedCredential.AccountName);
            Assert.Equal("TEST", parsedCredential.Secret);
            Assert.Equal(CredentialType.Totp, parsedCredential.Type);
            Assert.Equal(HashAlgorithm.Sha256, parsedCredential.Algorithm);
            Assert.Equal(CredentialPeriod.Period60, parsedCredential.Period);
            Assert.Equal(7, parsedCredential.Digits);
            Assert.Null(parsedCredential.Counter);
        }

        [Fact]
        public void CredentialParseUri_UriNotValid_ThrowsException()
        {
            
            static void Action()
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var uri = new Uri(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                _ = Credential.ParseUri(uri);

            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialParseUri_UriNotValid_ThrowsException_2()
        {

            static void Action()
            {
                var uri = new Uri(string.Empty);
                _ = Credential.ParseUri(uri);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialParseUri_UriSchemeNotValid_ThrowsException()
        {

            static void Action()
            {
                var uri = new Uri("otp://totp/Microsoft:test@outlook.com?secret=TEST&issuer=Microsoft");
                _ = Credential.ParseUri(uri);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialParseUri_UriPathNotValid_ThrowsException()
        {

            static void Action()
            {
                var uri = new Uri("otpauth://totp/secret=TEST&issuer=Microsoft");
                _ = Credential.ParseUri(uri);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }
    }
}
