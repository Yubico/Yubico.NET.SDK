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
using Yubico.YubiKey.U2f;


namespace Yubico.YubiKey.Sample.U2fSampleCode
{
    public static class U2fProtocol
    {
        public static bool Register(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            ReadOnlyMemory<byte> applicationId,
            ReadOnlyMemory<byte> clientDataHash,
            out RegistrationData registrationData)
        {
            using (var u2fSession = new U2fSession(yubiKey))
            {
                u2fSession.KeyCollector = KeyCollectorDelegate;
                if (u2fSession.TryRegister(applicationId, clientDataHash, TimeSpan.FromSeconds(10), out registrationData))
                {
                    return registrationData.VerifySignature(applicationId, clientDataHash);
                }

                return false;
            }
        }

        // This will determine if the keyHandle matches the applicationId (origin
        // data).
        // If the keyHandle matches, set isVerified to true. Otherwise set
        // isVerified to false.
        // The return value indicates whether the method could complete its task
        // or not. If it can make the determination, it returns true. If it can't
        // (we simply don't know if the keyHanlde matches or not), return false.
        // It is possible to return true and set isVerified to false. This
        // happens when the method was able to complete its task, it simply
        // determined that the keyHandle does not verify. That is, it is not an
        // error if the keyHandle is not verified, because the method was able to
        // complete the task it was asked to do.
        public static bool VerifyKeyHandle(
            IYubiKeyDevice yubiKey,
            ReadOnlyMemory<byte> applicationId,
            ReadOnlyMemory<byte> clientDataHash,
            ReadOnlyMemory<byte> keyHandle,
            out bool isVerified
            )
        {
            using (var u2fSession = new U2fSession(yubiKey))
            {
                isVerified = u2fSession.VerifyKeyHandle(applicationId, clientDataHash, keyHandle);
            }

            return true;
        }

        public static bool Authenticate(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            ReadOnlyMemory<byte> applicationId,
            ReadOnlyMemory<byte> clientDataHash,
            ReadOnlyMemory<byte> keyHandle,
            out AuthenticationData authenticationData)
        {
            using (var u2fSession = new U2fSession(yubiKey))
            {
                u2fSession.KeyCollector = KeyCollectorDelegate;

                return u2fSession.TryAuthenticate(
                    applicationId, clientDataHash, keyHandle,
                    TimeSpan.FromSeconds(10), out authenticationData, true);
            }
        }
    }
}
