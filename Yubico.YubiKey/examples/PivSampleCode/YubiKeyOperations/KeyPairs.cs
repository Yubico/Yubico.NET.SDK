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
using Yubico.YubiKey.Piv;
using System.Security.Cryptography.X509Certificates;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    public static class KeyPairs
    {
        public static bool RunGenerateKeyPair(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            byte slotNumber,
            PivAlgorithm algorithm,
            PivPinPolicy pinPolicy,
            PivTouchPolicy touchPolicy,
            out SamplePivSlotContents slotContents)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;

                // OPTIONAL
                // This operation requires mgmt key authentication.
                // If you want to always check to see if the mgmt key is
                // PIN-Protected or PIN-Derived, and if so, set the session, call
                // this method. It will require you to enter the PIN, even if the
                // PIN has already been verified, even if the YubiKey operation
                // does not require the PIN, and even if the YubiKey is not
                // PIN-Protected or PIN-Derived.
                // You don't have to make this check, in which case, the SDK will
                // behave normally. Don't make the check if you know your
                // application will never use a PIN-based mgmt key.
                // However, if the mgmt key is PIN-based, it might not be
                // possible for your KeyCollector to obtain the mgmt key (the
                // user won't know what it is) in which case this call will be
                // useful.
                SpecialMgmtKey.SetSessionIfMgmtKeyPinBased(pivSession);

                PivPublicKey pivPublicKey = pivSession.GenerateKeyPair(slotNumber, algorithm, pinPolicy, touchPolicy);

                // At this point you will likely want to save the public key and
                // other information. For this sample, we're simply going to
                // build a SlotContents object.
                slotContents = new SamplePivSlotContents()
                {
                    SlotNumber = slotNumber,
                        Algorithm = algorithm,
                        PinPolicy = pinPolicy,
                        TouchPolicy = touchPolicy,
                        PublicKey = pivPublicKey,
                };
            }

            return true;
        }

        public static bool RunImportPrivateKey(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            PivPrivateKey privateKey,
            PivPublicKey publicKey,
            byte slotNumber,
            PivPinPolicy pinPolicy,
            PivTouchPolicy touchPolicy,
            out SamplePivSlotContents slotContents)
        {
            if (privateKey is null)
            {
                throw new ArgumentNullException(nameof(privateKey));
            }
            if (publicKey is null)
            {
                throw new ArgumentNullException(nameof(publicKey));
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;

                // OPTIONAL
                // This operation requires mgmt key authentication.
                // If you want to always check to see if the mgmt key is
                // PIN-Protected or PIN-Derived, and if so, set the session, call
                // this method. It will require you to enter the PIN, even if the
                // PIN has already been verified, even if the YubiKey operation
                // does not require the PIN, and even if the YubiKey is not
                // PIN-Protected or PIN-Derived.
                // You don't have to make this check, in which case, the SDK will
                // behave normally. Don't make the check if you know your
                // application will never use a PIN-based mgmt key.
                // However, if the mgmt key is PIN-based, it might not be
                // possible for your KeyCollector to obtain the mgmt key (the
                // user won't know what it is) in which case this call will be
                // useful.
                SpecialMgmtKey.SetSessionIfMgmtKeyPinBased(pivSession);

                pivSession.ImportPrivateKey(slotNumber, privateKey, pinPolicy, touchPolicy);

                // At this point you will likely want to save the public key and
                // other information. For this sample, we're simply going to
                // build a SlotContents object.
                // The Import method does not need the public key, so we're
                // building it with no public key. If you want, you can add the
                // public key.
                slotContents = new SamplePivSlotContents()
                {
                    SlotNumber = slotNumber,
                    Algorithm = privateKey.Algorithm,
                    PinPolicy = pinPolicy,
                    TouchPolicy = touchPolicy,
                    PublicKey = PivPublicKey.Create(publicKey.YubiKeyEncodedPublicKey),
                };
            }

            return true;
        }

        public static void RunRetrieveCert(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            byte slotNumber,
            out X509Certificate2 certificate)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;
                certificate = pivSession.GetCertificate(slotNumber);
            }
        }

        public static void RunCreateAttestationStatement(
            IYubiKeyDevice yubiKey,
            byte slotNumber,
            out X509Certificate2 certificate)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                certificate = pivSession.CreateAttestationStatement(slotNumber);
            }
        }

        public static void RunGetAttestationCert(
            IYubiKeyDevice yubiKey,
            out X509Certificate2 certificate)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                certificate = pivSession.GetAttestationCertificate();
            }
        }
    }
}
