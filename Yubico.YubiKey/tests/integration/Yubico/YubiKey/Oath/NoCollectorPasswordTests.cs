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
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Yubico.YubiKey.Oath.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Oath
{
    public sealed class NoCollectorPasswordTests
    {
        [Fact]
        public void SetPassword_Succeeds()
        {
            bool isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKeyDevice);
            Assert.True(isValid);

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                oathSession.ResetApplication();

                Assert.False(oathSession.IsPasswordProtected);

                var newCred = new Credential(
                    "issuer", "account@yubico.com", CredentialPeriod.Undefined, CredentialType.Hotp, HashAlgorithm.Sha256);
                oathSession.AddCredential(newCred);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                Assert.False(oathSession.IsPasswordProtected);

                IList<Credential> credentialList = oathSession.GetCredentials();
                _ = Assert.Single(credentialList);

                Credential cred = credentialList[0];
                _ = Assert.NotNull(cred.Algorithm);
                if (!(cred.Algorithm is null))
                {
                    Assert.Equal(HashAlgorithm.Sha256, cred.Algorithm);
                }
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                var password = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x62, 0x63, 0x64 });

                Assert.False(oathSession.IsPasswordProtected);
                bool isSet = oathSession.TrySetPassword(ReadOnlyMemory<byte>.Empty, password);
                Assert.True(isSet);
                Assert.True(oathSession.IsPasswordProtected);

                IList<Credential> credentialList = oathSession.GetCredentials();
                _ = Assert.Single(credentialList);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                Assert.True(oathSession.IsPasswordProtected);
                _ = Assert.Throws<InvalidOperationException>(() => oathSession.GetCredentials());
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                Assert.True(oathSession.IsPasswordProtected);

                var password = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x62, 0x63, 0x64 });

                bool isVerified = oathSession.TryVerifyPassword(password);
                Assert.True(isVerified);

                IList<Credential> credentialList = oathSession.GetCredentials();
                _ = Assert.Single(credentialList);

                Credential cred = credentialList[0];
                _ = Assert.NotNull(cred.Algorithm);
                if (!(cred.Algorithm is null))
                {
                    Assert.Equal(HashAlgorithm.Sha256, cred.Algorithm);
                }
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                Assert.True(oathSession.IsPasswordProtected);

                var wrongPassword = new ReadOnlyMemory<byte>(new byte[] { 0x71, 0x62, 0x63, 0x64 });
                var newPassword = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x72, 0x63, 0x64 });

                bool isSet = oathSession.TrySetPassword(wrongPassword, newPassword);
                Assert.False(isSet);
                _ = Assert.Throws<InvalidOperationException>(() => oathSession.GetCredentials());
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                Assert.True(oathSession.IsPasswordProtected);

                var password = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x62, 0x63, 0x64 });
                var wrongPassword = new ReadOnlyMemory<byte>(new byte[] { 0x71, 0x62, 0x63, 0x64 });
                var newPassword = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x72, 0x63, 0x64 });

                bool isVerified = oathSession.TryVerifyPassword(password);
                Assert.True(isVerified);

                bool isSet = oathSession.TrySetPassword(wrongPassword, newPassword);
                Assert.False(isSet);

                IList<Credential> credentialList = oathSession.GetCredentials();
                _ = Assert.Single(credentialList);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                Assert.True(oathSession.IsPasswordProtected);

                var password = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x62, 0x63, 0x64 });
                var newPassword = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x72, 0x63, 0x64 });

                bool isVerified = oathSession.TryVerifyPassword(password);
                Assert.True(isVerified);

                bool isSet = oathSession.TrySetPassword(ReadOnlyMemory<byte>.Empty, newPassword);
                Assert.False(isSet);

                IList<Credential> credentialList = oathSession.GetCredentials();
                _ = Assert.Single(credentialList);

                oathSession.ResetApplication();
            }
        }

        [Fact]
        public void UnsetPassword_Succeeds()
        {
            bool isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKeyDevice);
            Assert.True(isValid);

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                oathSession.ResetApplication();

                Assert.False(oathSession.IsPasswordProtected);

                var newCred = new Credential(
                    "issuer", "account@yubico.com", CredentialPeriod.Undefined, CredentialType.Hotp, HashAlgorithm.Sha256);
                oathSession.AddCredential(newCred);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                var password = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x62, 0x63, 0x64 });

                Assert.False(oathSession.IsPasswordProtected);
                bool isSet = oathSession.TrySetPassword(ReadOnlyMemory<byte>.Empty, password);
                Assert.True(isSet);
                Assert.True(oathSession.IsPasswordProtected);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                Assert.True(oathSession.IsPasswordProtected);

                _ = Assert.Throws<InvalidOperationException>(() => oathSession.GetCredentials());

                var password = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x62, 0x63, 0x64 });

                bool isUnset = oathSession.TryUnsetPassword(password);
                Assert.True(isUnset);
                Assert.False(oathSession.IsPasswordProtected);

                IList<Credential> credentialList = oathSession.GetCredentials();
                _ = Assert.Single(credentialList);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                IList<Credential> credentialList = oathSession.GetCredentials();
                _ = Assert.Single(credentialList);

                Credential cred = credentialList[0];
                _ = Assert.NotNull(cred.Algorithm);
                if (!(cred.Algorithm is null))
                {
                    Assert.Equal(HashAlgorithm.Sha256, cred.Algorithm);
                }

                oathSession.ResetApplication();
            }
        }

        [Fact]
        public void UnsetPassword_WrongPassword_ReturnsFalse()
        {
            bool isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKeyDevice);
            Assert.True(isValid);

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                oathSession.ResetApplication();

                Assert.False(oathSession.IsPasswordProtected);

                var newCred = new Credential(
                    "issuer", "account@yubico.com", CredentialPeriod.Undefined, CredentialType.Hotp, HashAlgorithm.Sha256);
                oathSession.AddCredential(newCred);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                var password = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x62, 0x63, 0x64 });

                Assert.False(oathSession.IsPasswordProtected);
                bool isSet = oathSession.TrySetPassword(ReadOnlyMemory<byte>.Empty, password);
                Assert.True(isSet);
                Assert.True(oathSession.IsPasswordProtected);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                Assert.True(oathSession.IsPasswordProtected);

                _ = Assert.Throws<InvalidOperationException>(() => oathSession.GetCredentials());

                var password = new ReadOnlyMemory<byte>(new byte[] { 0x71, 0x62, 0x63, 0x64 });

                bool isUnset = oathSession.TryUnsetPassword(password);
                Assert.False(isUnset);
                Assert.True(oathSession.IsPasswordProtected);

                _ = Assert.Throws<InvalidOperationException>(() => oathSession.GetCredentials());

                oathSession.ResetApplication();
            }
        }

        [Fact]
        public void VerifyPassword_UnsetNoCurrent_Succeeds()
        {
            bool isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKeyDevice);
            Assert.True(isValid);

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                oathSession.ResetApplication();

                Assert.False(oathSession.IsPasswordProtected);

                var newCred = new Credential(
                    "issuer", "account@yubico.com", CredentialPeriod.Undefined, CredentialType.Hotp, HashAlgorithm.Sha256);
                oathSession.AddCredential(newCred);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                var password = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x62, 0x63, 0x64 });

                Assert.False(oathSession.IsPasswordProtected);
                bool isSet = oathSession.TrySetPassword(ReadOnlyMemory<byte>.Empty, password);
                Assert.True(isSet);
                Assert.True(oathSession.IsPasswordProtected);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                Assert.True(oathSession.IsPasswordProtected);

                _ = Assert.Throws<InvalidOperationException>(() => oathSession.GetCredentials());

                var password = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x62, 0x63, 0x64 });

                bool isVerified = oathSession.TryVerifyPassword(password);
                Assert.True(isVerified);

                bool isUnset = oathSession.TryUnsetPassword(ReadOnlyMemory<byte>.Empty);
                Assert.False(isUnset);
                Assert.True(oathSession.IsPasswordProtected);

                isUnset = oathSession.TryUnsetPassword(password);
                Assert.True(isUnset);
                Assert.False(oathSession.IsPasswordProtected);

                IList<Credential> credentialList = oathSession.GetCredentials();
                _ = Assert.Single(credentialList);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                IList<Credential> credentialList = oathSession.GetCredentials();
                _ = Assert.Single(credentialList);

                Credential cred = credentialList[0];
                _ = Assert.NotNull(cred.Algorithm);
                if (!(cred.Algorithm is null))
                {
                    Assert.Equal(HashAlgorithm.Sha256, cred.Algorithm);
                }

                oathSession.ResetApplication();
            }
        }

        [Fact]
        public void PasswordNotSet_Verify_ReturnsFalse()
        {
            bool isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKeyDevice);
            Assert.True(isValid);

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                oathSession.ResetApplication();
                Assert.False(oathSession.IsPasswordProtected);

                var password = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x62, 0x63, 0x64 });

                bool isVerified = oathSession.TryVerifyPassword(password);
                Assert.False(isVerified);

                var newCred = new Credential(
                    "issuer", "account@yubico.com", CredentialPeriod.Undefined, CredentialType.Hotp, HashAlgorithm.Sha256);
                oathSession.AddCredential(newCred);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                Assert.False(oathSession.IsPasswordProtected);

                IList<Credential> credentialList = oathSession.GetCredentials();
                _ = Assert.Single(credentialList);

                oathSession.ResetApplication();
            }
        }

        [Fact]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            bool isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKeyDevice);
            Assert.True(isValid);

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                oathSession.ResetApplication();
                Assert.False(oathSession.IsPasswordProtected);

                var password = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x62, 0x63, 0x64 });

                bool isSet = oathSession.TrySetPassword(ReadOnlyMemory<byte>.Empty, password);
                Assert.True(isSet);
                Assert.True(oathSession.IsPasswordProtected);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                Assert.True(oathSession.IsPasswordProtected);

                var password = new ReadOnlyMemory<byte>(new byte[] { 0x62, 0x62, 0x62, 0x62 });

                bool isVerified = oathSession.TryVerifyPassword(password);
                Assert.False(isVerified);

                oathSession.ResetApplication();
            }
        }

        [Fact]
        public void KeyCollector_WrongPassword_ReturnsFalse()
        {
            bool isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKeyDevice);
            Assert.True(isValid);

            var simpleCollector = new SimpleOathKeyCollector();

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                oathSession.ResetApplication();
                Assert.False(oathSession.IsPasswordProtected);

                var password = new ReadOnlyMemory<byte>(new byte[] { 0x61, 0x62, 0x63, 0x64 });

                bool isSet = oathSession.TrySetPassword(ReadOnlyMemory<byte>.Empty, password);
                Assert.True(isSet);
                Assert.True(oathSession.IsPasswordProtected);
            }

            using (var oathSession = new OathSession(yubiKeyDevice))
            {
                oathSession.KeyCollector = simpleCollector.SimpleKeyCollectorDelegate;

                Assert.True(oathSession.IsPasswordProtected);
                bool isVerified = oathSession.TryVerifyPassword();
                Assert.False(isVerified);

                oathSession.ResetApplication();
            }
        }
    }
}
