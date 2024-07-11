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
using System.Text;
using Xunit;

namespace Yubico.YubiKey.Oath
{
    public class CredentialTests
    {
        private readonly string DefaultTestIssuer = "Microsoft";
        private readonly string DefaultTestAccount = "test@outlook.com";

        #region Issuer

        [Fact]
        public void Issuer_GetDefaultValue_ReturnsNull()
        {
            Credential cred = new Credential();

            Assert.Null(cred.Issuer);
        }

        [Fact]
        public void Issuer_SetToTestString_ReturnsTestString()
        {
            Credential cred = new Credential
            {
                Issuer = DefaultTestIssuer
            };
            string? actualIssuer = cred.Issuer;

            Assert.Equal(DefaultTestIssuer, actualIssuer);
        }

        [Fact]
        public void Issuer_SetToTestStringWithLeadingTrailingWhiteSpace_ReturnsTestString()
        {
            Credential cred = new Credential();

            string? expectedIssuer = "  " + DefaultTestIssuer + " \t ";
            cred.Issuer = expectedIssuer;
            string? actualIssuer = cred.Issuer;

            Assert.Equal(expectedIssuer, actualIssuer);
        }

        [Fact]
        public void Issuer_SetToNull_ReturnsNull()
        {
            Credential cred = new Credential
            {
                Issuer = null
            };
            string? actualIssuer = cred.Issuer;

            Assert.Null(actualIssuer);
        }

        [Fact]
        public void Issuer_SetToEmptyString_ReturnsNull()
        {
            Credential cred = new Credential
            {
                Issuer = string.Empty
            };
            string? actualIssuer = cred.Issuer;

            Assert.Null(actualIssuer);
        }

        [Theory]
        [InlineData("      ")]
        [InlineData("   \t   ")]
        [InlineData("\u2000\u2000\u2000")]
        public void Issuer_SetToWhiteSpace_ReturnsNull(string? issuerValue)
        {
            Credential cred = new Credential
            {
                Issuer = issuerValue
            };
            string? actualIssuer = cred.Issuer;

            Assert.Null(actualIssuer);
        }

        #endregion Issuer

        #region Name: valid credentials

        // "15/Microsoft:test@outlook.com" TOTP @ 15s, Issuer = Microsoft, Account = "test@outlook.com"
        [Fact]
        public void Name_Totp15sIssuerAccount_ReturnsCorrectName()
        {
            Credential cred = new Credential
            {
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period15,
                Issuer = DefaultTestIssuer,
                AccountName = DefaultTestAccount
            };

            string expectedName = $"15/{DefaultTestIssuer}:{DefaultTestAccount}";

            Assert.Equal(expectedName, cred.Name);
        }

        // "Microsoft:test@outlook.com" [TOTP @ 30s/HOTP], Issuer = Microsoft, Account = "test@outlook.com"
        [Fact]
        public void Name_Totp30sIssuerAccount_ReturnsCorrectName()
        {
            Credential cred = new Credential
            {
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period30,
                Issuer = DefaultTestIssuer,
                AccountName = DefaultTestAccount
            };

            string expectedName = $"{DefaultTestIssuer}:{DefaultTestAccount}";

            Assert.Equal(expectedName, cred.Name);
        }

        // "Microsoft:test@outlook.com" [TOTP @ 30s/HOTP], Issuer = Microsoft, Account = "test@outlook.com"
        [Fact]
        public void Name_HotpIssuerAccount_ReturnsCorrectName()
        {
            Credential cred = new Credential
            {
                Type = CredentialType.Hotp,
                Issuer = DefaultTestIssuer,
                AccountName = DefaultTestAccount
            };

            string expectedName = $"{DefaultTestIssuer}:{DefaultTestAccount}";

            Assert.Equal(expectedName, cred.Name);
        }

        // "15/test@outlook.com" TOTP @ 15s, Account = "test@outlook.com"
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("      ")]
        public void Name_Totp15sAccount_ReturnsCorrectName(string? issuerValue)
        {
            Credential cred = new Credential
            {
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period15,
                Issuer = issuerValue,
                AccountName = DefaultTestAccount
            };

            string expectedName = $"15/{DefaultTestAccount}";

            Assert.Equal(expectedName, cred.Name);
        }

        // "15/test@outlook.com" TOTP @ 15s, Account = "test@outlook.com"
        [Fact]
        public void Name_Totp15sAccountDefaultIssuer_ReturnsCorrectName()
        {
            Credential cred = new Credential
            {
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period15,
                AccountName = DefaultTestAccount
            };

            string expectedName = $"15/{DefaultTestAccount}";

            Assert.Equal(expectedName, cred.Name);
        }

        // "test@outlook.com" [TOTP @ 30s/HOTP], Account = "test@outlook.com"
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("      ")]
        public void Name_Totp30sAccount_ReturnsCorrectName(string? issuerValue)
        {
            Credential cred = new Credential
            {
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period30,
                Issuer = issuerValue,
                AccountName = DefaultTestAccount
            };

            string expectedName = $"{DefaultTestAccount}";

            Assert.Equal(expectedName, cred.Name);
        }

        // "test@outlook.com" [TOTP @ 30s/HOTP], Account = "test@outlook.com"
        [Fact]
        public void Name_Totp30sAccountDefaultIssuer_ReturnsCorrectName()
        {
            Credential cred = new Credential
            {
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period30,
                AccountName = DefaultTestAccount
            };

            string expectedName = $"{DefaultTestAccount}";

            Assert.Equal(expectedName, cred.Name);
        }

        // "test@outlook.com" [TOTP @ 30s/HOTP], Account = "test@outlook.com"
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("      ")]
        public void Name_HotpAccount_ReturnsCorrectName(string? issuerValue)
        {
            Credential cred = new Credential
            {
                Type = CredentialType.Hotp,
                Issuer = issuerValue,
                AccountName = DefaultTestAccount
            };

            string expectedName = $"{DefaultTestAccount}";

            Assert.Equal(expectedName, cred.Name);
        }

        // "test@outlook.com" [TOTP @ 30s/HOTP], Account = "test@outlook.com"
        [Fact]
        public void Name_HotpAccountDefaultIssuer_ReturnsCorrectName()
        {
            Credential cred = new Credential
            {
                Type = CredentialType.Hotp,
                AccountName = DefaultTestAccount
            };

            string expectedName = $"{DefaultTestAccount}";

            Assert.Equal(expectedName, cred.Name);
        }

        [Theory]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period15, "123456",
            "123456789012345678901234567890123456789012345678901234")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period30, "123456789",
            "123456789012345678901234567890123456789012345678901234")]
        [InlineData(CredentialType.Hotp, CredentialPeriod.Undefined, "123456789",
            "123456789012345678901234567890123456789012345678901234")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period15, null,
            "1234567890123456789012345678901234567890123456789012345678901")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period15, "",
            "1234567890123456789012345678901234567890123456789012345678901")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period15, "     ",
            "1234567890123456789012345678901234567890123456789012345678901")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period30, null,
            "1234567890123456789012345678901234567890123456789012345678901234")]
        [InlineData(CredentialType.Hotp, CredentialPeriod.Undefined, null,
            "1234567890123456789012345678901234567890123456789012345678901234")]
        public void Name_64ByteNameLength_ReturnsCorrectName(
            CredentialType credType, CredentialPeriod credPeriod, string? issuer, string account)
        {
            Credential cred = new Credential
            {
                Type = credType,
                Period = credPeriod,
                Issuer = issuer,
                AccountName = account
            };

            string actualCredName = cred.Name;

            Assert.Equal(64, Encoding.UTF8.GetByteCount(actualCredName));
        }

        #endregion

        #region Name: invalid credentials

        [Theory]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period15, "123456",
            "1234567890123456789012345678901234567890123456789012345")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period30, "123456789",
            "1234567890123456789012345678901234567890123456789012345")]
        [InlineData(CredentialType.Hotp, CredentialPeriod.Undefined, "123456789",
            "1234567890123456789012345678901234567890123456789012345")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period15, null,
            "12345678901234567890123456789012345678901234567890123456789012")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period15, "",
            "12345678901234567890123456789012345678901234567890123456789012")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period15, "     ",
            "12345678901234567890123456789012345678901234567890123456789012")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period30, null,
            "12345678901234567890123456789012345678901234567890123456789012345")]
        [InlineData(CredentialType.Hotp, CredentialPeriod.Undefined, null,
            "12345678901234567890123456789012345678901234567890123456789012345")]
        public void Name_65ByteNameLength_ThrowsInvalidOperationException(
            CredentialType credType, CredentialPeriod credPeriod, string? issuer, string account)
        {
            Credential cred = new Credential
            {
                Type = credType,
                Period = credPeriod,
                Issuer = issuer,
                AccountName = account
            };

            _ = Assert.Throws<InvalidOperationException>(() => cred.Name);
        }

        [Fact]
        public void Name_CredTypeDefault_ThrowsInvalidOperationException()
        {
            Credential cred = new Credential
            {
                Issuer = DefaultTestIssuer,
                AccountName = DefaultTestAccount
            };

            _ = Assert.Throws<InvalidOperationException>(() => cred.Name);
        }

        [Fact]
        public void Name_CredTypeNone_ThrowsInvalidOperationException()
        {
            Credential cred = new Credential
            {
                Type = CredentialType.None,
                Issuer = DefaultTestIssuer,
                AccountName = DefaultTestAccount
            };

            _ = Assert.Throws<InvalidOperationException>(() => cred.Name);
        }

        [Fact]
        public void Name_TotpCredPeriodDefault_ThrowsInvalidOperationException()
        {
            Credential cred = new Credential
            {
                Type = CredentialType.Totp,
                Issuer = DefaultTestIssuer,
                AccountName = DefaultTestAccount
            };

            _ = Assert.Throws<InvalidOperationException>(() => cred.Name);
        }

        [Fact]
        public void Name_TotpCredPeriodUndefined_ThrowsInvalidOperationException()
        {
            Credential cred = new Credential
            {
                Period = CredentialPeriod.Undefined,
                Type = CredentialType.Totp,
                Issuer = DefaultTestIssuer,
                AccountName = DefaultTestAccount
            };

            _ = Assert.Throws<InvalidOperationException>(() => cred.Name);
        }

        [Fact]
        public void Name_Totp30sAccountDefault_ThrowsInvalidOperationException()
        {
            Credential cred = new Credential
            {
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period30,
                Issuer = DefaultTestIssuer
            };

            _ = Assert.Throws<InvalidOperationException>(() => cred.Name);
        }

        [Fact]
        public void Name_HotpAccountDefault_ThrowsInvalidOperationException()
        {
            Credential cred = new Credential
            {
                Type = CredentialType.Hotp,
                Issuer = DefaultTestIssuer
            };

            _ = Assert.Throws<InvalidOperationException>(() => cred.Name);
        }

        #endregion

        #region parsing

        [Fact]
        public void CredentialIssuerAndAccount_UriUnescape_ReturnsCorrectUnescapedStrings()
        {
            string? issuer = Uri.UnescapeDataString("Microsoft%3Ademo");
            string? account = Uri.UnescapeDataString("test%40outlook.com");

            Assert.Equal("Microsoft:demo", issuer);
            Assert.Equal("test@outlook.com", account);
        }

        [Fact]
        public void CredentialParseLabel_TotpTypeAndDefaultPeriod_ReturnsCorrectlyParsedLabel()
        {
            string? label = "Microsoft:demo:test@outlook.com";
            (CredentialPeriod period, string? issuer, string account) =
                Credential.ParseLabel(label, CredentialType.Totp);

            Assert.Equal(CredentialPeriod.Period30, period);
            Assert.Equal("Microsoft:demo", issuer);
            Assert.Equal("test@outlook.com", account);
        }

        [Fact]
        public void CredentialParseLabel_TotpTypeAndNoDefaultPeriod_ReturnsCorrectlyParsedLabel()
        {
            string? label = "60/Microsoft:demo:test@outlook.com";
            (CredentialPeriod period, string? issuer, string account) =
                Credential.ParseLabel(label, CredentialType.Totp);

            Assert.Equal(CredentialPeriod.Period60, period);
            Assert.Equal("Microsoft:demo", issuer);
            Assert.Equal("test@outlook.com", account);
        }

        [Fact]
        public void CredentialParseLabel_HotpType_ReturnsCorrectlyParsedLabel()
        {
            string? label = "60/Microsoft:demo:test@outlook.com";
            (CredentialPeriod period, string? issuer, string account) =
                Credential.ParseLabel(label, CredentialType.Hotp);

            Assert.Equal(CredentialPeriod.Undefined, period);
            Assert.Equal("60/Microsoft:demo", issuer);
            Assert.Equal("test@outlook.com", account);
        }

        [Fact]
        public void CredentialParseLabel_HotpType_ReturnsCorrectlyParsedLabel_2()
        {
            string? label = "Microsoft:test@outlook.com";
            (CredentialPeriod period, string? issuer, string account) =
                Credential.ParseLabel(label, CredentialType.Hotp);

            Assert.Equal(CredentialPeriod.Undefined, period);
            Assert.Equal("Microsoft", issuer);
            Assert.Equal("test@outlook.com", account);
        }

        [Fact]
        public void CredentialParseLabel_Totp15sAccount_ReturnsCorrectlyParsedLabel()
        {
            string? label = "15/test@outlook.com";
            (CredentialPeriod period, string? issuer, string account) =
                Credential.ParseLabel(label, CredentialType.Totp);

            Assert.Equal(CredentialPeriod.Period15, period);
            Assert.Null(issuer);
            Assert.Equal("test@outlook.com", account);
        }

        [Fact]
        public void CredentialParseLabel_Totp30sAccount_ReturnsCorrectlyParsedLabel()
        {
            string? label = "test@outlook.com";
            (CredentialPeriod period, string? issuer, string account) =
                Credential.ParseLabel(label, CredentialType.Totp);

            Assert.Equal(CredentialPeriod.Period30, period);
            Assert.Null(issuer);
            Assert.Equal("test@outlook.com", account);
        }

        [Fact]
        public void CredentialParseLabel_HotpAccount_ReturnsCorrectlyParsedLabel()
        {
            string? label = "test@outlook.com";
            (CredentialPeriod period, string? issuer, string account) =
                Credential.ParseLabel(label, CredentialType.Hotp);

            Assert.Equal(CredentialPeriod.Undefined, period);
            Assert.Null(issuer);
            Assert.Equal("test@outlook.com", account);
        }

        [Theory]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period15, "123456",
            "123456789012345678901234567890123456789012345678901234",
            "15/123456:123456789012345678901234567890123456789012345678901234")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period30, "123456789",
            "123456789012345678901234567890123456789012345678901234",
            "123456789:123456789012345678901234567890123456789012345678901234")]
        [InlineData(CredentialType.Hotp, CredentialPeriod.Undefined, "123456789",
            "123456789012345678901234567890123456789012345678901234",
            "123456789:123456789012345678901234567890123456789012345678901234")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period15, null,
            "1234567890123456789012345678901234567890123456789012345678901",
            "15/1234567890123456789012345678901234567890123456789012345678901")]
        [InlineData(CredentialType.Totp, CredentialPeriod.Period30, null,
            "1234567890123456789012345678901234567890123456789012345678901234",
            "1234567890123456789012345678901234567890123456789012345678901234")]
        [InlineData(CredentialType.Hotp, CredentialPeriod.Undefined, null,
            "1234567890123456789012345678901234567890123456789012345678901234",
            "1234567890123456789012345678901234567890123456789012345678901234")]
        public void CredentialParseLabel_TotalLength64_ReturnsCorrectlyParsedLabel(
            CredentialType type, CredentialPeriod period, string? issuer, string account, string label)
        {
            (CredentialPeriod period, string? issuer, string account) parsedLabel = Credential.ParseLabel(label, type);

            Assert.Equal(period, parsedLabel.period);
            Assert.Equal(issuer, parsedLabel.issuer);
            Assert.Equal(account, parsedLabel.account);
        }

        [Fact]
        public void CredentialParseUri_NoOptionalParameters_ReturnsCorrectlyParsedCredentialParameters()
        {
            var uri = new Uri("otpauth://totp/Microsoft:test@outlook.com?secret=TEST&issuer=Microsoft");
            var parsedCredential = Credential.ParseUri(uri);

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
            var uri = new Uri(
                "otpauth://totp/Microsoft%3Ademo:test@outlook.com?secret=TEST&issuer=Microsoft%3Ademo&algorithm=SHA256&digits=7&period=60");
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
        public void CredentialParseUri_NoIssuer_ReturnsCorrectlyParsedCredentialParameters()
        {
            var uri = new Uri("otpauth://totp/test@outlook.com?secret=TEST");
            var parsedCredential = Credential.ParseUri(uri);

            Assert.Null(parsedCredential.Issuer);
            Assert.Equal("test@outlook.com", parsedCredential.AccountName);
            Assert.Equal("TEST", parsedCredential.Secret);
            Assert.Equal(CredentialType.Totp, parsedCredential.Type);
            Assert.Equal(HashAlgorithm.Sha1, parsedCredential.Algorithm);
            Assert.Equal(CredentialPeriod.Period30, parsedCredential.Period);
            Assert.Equal(6, parsedCredential.Digits);
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

        #endregion

        #region non-default constructor

        [Fact]
        public void CredentialAccountNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", string.Empty, CredentialType.Totp, HashAlgorithm.Sha1, "tt",
                    CredentialPeriod.Period30, 6, 0, false);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialTypeNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", "test@gmail.com", (CredentialType)0x03, HashAlgorithm.Sha1, "tt",
                    CredentialPeriod.Period30, 6, 0, false);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialAlgorithmNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", "test@gmail.com", CredentialType.Totp, (HashAlgorithm)0x04, "tt",
                    CredentialPeriod.Period30, 6, 0, false);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialSecretNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", "test@gmail.com", CredentialType.Totp, HashAlgorithm.Sha1, "8900",
                    CredentialPeriod.Period30, 6, 0, false);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialPeriodNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", "test@gmail.com", CredentialType.Totp, HashAlgorithm.Sha1, "tt",
                    (CredentialPeriod)32, 6, 0, false);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CredentialDigitsNotValid_ThrowsException()
        {
            static void Action()
            {
                _ = new Credential("Microsoft", "test@gmail.com", CredentialType.Totp, HashAlgorithm.Sha1, "tt",
                    CredentialPeriod.Period30, 4, 0, false);
            }

            Exception? ex = Record.Exception(Action);
            Assert.NotNull(ex);
        }

        #endregion
    }
}
