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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This file contains the methods to run each of the main menu items.
    // The main menu is displayed, the user selects an option, and the code that
    // receives the choice will call the appropriate method in this file to
    // make the appropriate calls to perform the operation selected.
    public partial class PivSampleRun
    {
        public bool RunMenuItem(PivMainMenuItem menuItem) => menuItem switch
        {
            PivMainMenuItem.Exit => false,
            // Find all currently connected YubiKeys that can communicate
            // over the SmartCard (CCID) protocol. This is the protocol
            // used to communicate with the PIV application.
            // Using Transport.SmartCard finds all YubiKeys connected via USB and
            // NFC.
            // To get only YubiKeys connected via USB, call
            //   YubiKey.FindByTransport(Transport.UsbSmartCard);
            // To get only YubiKeys connected via NFC, call
            //   YubiKey.FindByTransport(Transport.NfcSmartCard);
            PivMainMenuItem.ListYubiKeys => ListYubiKeys.RunListYubiKeys(Transport.SmartCard),
            PivMainMenuItem.ChooseYubiKey => RunChooseYubiKey(),
            PivMainMenuItem.ChangePivPinAndPukRetryCount => RunChangeRetryCount(),
            PivMainMenuItem.ChangePivPin =>
                ChangeSecret.RunChangePivPin(_yubiKeyChosen, _keyCollector.SampleKeyCollectorDelegate),
            PivMainMenuItem.ChangePivPuk =>
                ChangeSecret.RunChangePivPuk(_yubiKeyChosen, _keyCollector.SampleKeyCollectorDelegate),
            PivMainMenuItem.ResetPivPinWithPuk =>
                ChangeSecret.RunResetPivPinWithPuk(_yubiKeyChosen, _keyCollector.SampleKeyCollectorDelegate),
            PivMainMenuItem.ChangePivManagementKey =>
                ChangeSecret.RunChangePivManagementKey(_yubiKeyChosen, _keyCollector.SampleKeyCollectorDelegate),
            PivMainMenuItem.GetPinOnlyMode => RunGetPinOnlyMode(),
            PivMainMenuItem.SetPinOnlyMode => RunSetPinOnlyMode(),
            PivMainMenuItem.SetPinOnlyNoKeyCollector => RunSetPinOnlyNoKeyCollector(),
            PivMainMenuItem.RecoverPinOnlyMode => RunRecoverPinOnlyMode(),
            PivMainMenuItem.GenerateKeyPair => RunGenerateKeyPair(),
            PivMainMenuItem.ImportPrivateKey => RunImportPrivateKey(),
            PivMainMenuItem.ImportCertificate => WriteImportCertMessage(),
            PivMainMenuItem.Sign => RunSignData(),
            PivMainMenuItem.Decrypt => RunDecryptData(),
            PivMainMenuItem.KeyAgree => RunKeyAgree(),
            PivMainMenuItem.GetCertRequest => RunGetCertRequest(),
            PivMainMenuItem.BuildSelfSignedCert => RunBuildSelfSignedCert(),
            PivMainMenuItem.BuildCert => RunBuildCert(),
            PivMainMenuItem.RetrieveCert => RunRetrieveCert(),
            PivMainMenuItem.CreateAttestationStatement => RunCreateAttestationStatement(),
            PivMainMenuItem.GetAttestationCertificate => RunGetAttestationCert(),
            PivMainMenuItem.ResetPiv =>
                ChangeSecret.RunResetPiv(_yubiKeyChosen, _keyCollector.SampleKeyCollectorDelegate),
            _ => RunUnimplementedOperation(),
        };

        public static bool RunInvalidEntry()
        {
            SampleMenu.WriteMessage(MessageType.Special, 0, "Invalid entry");
            return true;
        }

        public static bool RunUnimplementedOperation()
        {
            SampleMenu.WriteMessage(MessageType.Special, 0, "Unimplemented operation");
            return true;
        }

        public bool RunChangeRetryCount()
        {
            if (!GetNewRetryCounts(out byte newRetryCountPin, out byte newRetryCountPuk))
            {
                return RunInvalidEntry();
            }

            if (ChangeSecret.RunChangeRetryCount(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                newRetryCountPin,
                newRetryCountPuk))
            {
                return true;
            }

            return false;
        }

        public bool RunGetPinOnlyMode()
        {
            if (PinOnlyMode.RunGetPivPinOnlyMode(_yubiKeyChosen, out var mode))
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "PIN-only mode: " + mode.ToString() + "\n");
                return true;
            }

            return false;
        }

        public bool RunSetPinOnlyMode()
        {
            if (!GetRequestedPinOnlyMode(out var mode))
            {
                return RunInvalidEntry();
            }

            return PinOnlyMode.RunSetPivPinOnlyMode(_yubiKeyChosen, _keyCollector.SampleKeyCollectorDelegate, mode);
        }

        public bool RunSetPinOnlyNoKeyCollector()
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "It is possible to set a YubiKey to PIN-only mode without a");
            SampleMenu.WriteMessage(MessageType.Title, 0, "KeyCollector under two conditions:");
            SampleMenu.WriteMessage(MessageType.Title, 0, "1. The mgmt key is currently set to the default, and");
            SampleMenu.WriteMessage(MessageType.Title, 0, "2. The mode to set is PinProtected.");
            SampleMenu.WriteMessage(MessageType.Title, 0, "You must verify the PIN in the session first, how you obtain");
            SampleMenu.WriteMessage(MessageType.Title, 0, "the PIN is up to you.\n");

            int response;

            do
            {
                if (DoSetPinOnlyNoKeyCollector(_yubiKeyChosen, out int? retriesRemaining))
                {
                    return true;
                }

                string retryString = retriesRemaining is null ? "(unknown)" : retriesRemaining.ToString();
                SampleMenu.WriteMessage(MessageType.Title, 0, "\nWrong PIN, retries remaining: " + retryString);
                string[] menuItems = new string[] {
                    "yes",
                    "no",
                };

                response = _menuObject.RunMenu("Try again?", menuItems);
            } while (response == 0);

            return false;
        }

        private static bool DoSetPinOnlyNoKeyCollector(
            IYubiKeyDevice yubiKey,
            out int? retriesRemaining)
        {
            byte[] pinData = Array.Empty<byte>();
            try
            {
                pinData = SampleKeyCollector.CollectValue("123456", "PIN");
                var pin = new ReadOnlyMemory<byte>(pinData);
                return PinOnlyMode.RunSetPinOnlyNoKeyCollector(yubiKey, pin, out retriesRemaining);
            }
            finally
            {
                Array.Fill<byte>(pinData, 0);
            }
        }

        public bool RunRecoverPinOnlyMode()
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Recover PIN-only mode will try to restore the PIN-only mode");
            SampleMenu.WriteMessage(MessageType.Title, 0, "if the ADMIN DATA and/or PRINTED storage areas were improperly");
            SampleMenu.WriteMessage(MessageType.Title, 0, "overwritten. The result is the PivPinOnly mode of the YubiKey");
            SampleMenu.WriteMessage(MessageType.Title, 0, "after recovery.\n");
            if (PinOnlyMode.RunRecoverPivPinOnlyMode(
                _yubiKeyChosen, _keyCollector.SampleKeyCollectorDelegate, out var mode))
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "PIN-only mode: " + mode.ToString() + "\n");
                return true;
            }

            return false;
        }

        public bool RunGenerateKeyPair()
        {
            if (!GetAsymmetricSlotNumber(out byte slotNumber))
            {
                return RunInvalidEntry();
            }

            if (!GetAsymmetricAlgorithm(out var algorithm))
            {
                return RunInvalidEntry();
            }

            if (!GetPinPolicy(out var pinPolicy))
            {
                return RunInvalidEntry();
            }

            if (!GetTouchPolicy(out var touchPolicy))
            {
                return RunInvalidEntry();
            }

            if (KeyPairs.RunGenerateKeyPair(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                slotNumber,
                algorithm,
                pinPolicy,
                touchPolicy,
                out var newSlotContents))
            {
                newSlotContents.PrintPublicKeyPem();
                AddSlotContents(newSlotContents);
                return true;
            }

            return false;
        }

        public bool RunImportPrivateKey()
        {
            if (!GetAsymmetricSlotNumber(out byte slotNumber))
            {
                return RunInvalidEntry();
            }

            if (!GetAsymmetricAlgorithm(out var algorithm))
            {
                return RunInvalidEntry();
            }

            if (!GetPinPolicy(out var pinPolicy))
            {
                return RunInvalidEntry();
            }

            if (!GetTouchPolicy(out var touchPolicy))
            {
                return RunInvalidEntry();
            }

            if (!GetPemPrivateKey(algorithm, out string pemPrivateKey))
            {
                return false;
            }
            
            if (!GetPemPublicKey(algorithm, out string pemPublicKey))
            {
                return false;
            }

            // var pivPrivateKey = KeyConverter.GetPivPrivateKeyFromPem(pemKey.ToCharArray());
            // var pivPublicKey = KeyConverter.GetPivPublicKeyFromPem(pemKey.ToCharArray());

            var base64PrivateKey = GetBytesFromPem(pemPrivateKey);
            var privateKeyParameters = Curve25519PrivateKeyParameters.CreateFromPkcs8(base64PrivateKey);
            
            var base64PublicKey = GetBytesFromPem(pemPublicKey);
            var publicKeyParameters = Curve25519PublicKeyParameters.CreateFromPkcs8(base64PublicKey);

            if (KeyPairs.RunImportPrivateKey(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                privateKeyParameters,
                publicKeyParameters,
                // pivPrivateKey,
                // pivPublicKey,
                slotNumber,
                pinPolicy,
                touchPolicy,
                out var newSlotContents))
            {
                newSlotContents.PrintPublicKeyPem();
                AddSlotContents(newSlotContents);
                return true;
            }

            return false;
        }
        
        public static bool WriteImportCertMessage()
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "See the items/code for BuildSelfSignedCert and BuildCert");
            SampleMenu.WriteMessage(MessageType.Title, 0, "for examples on importing a certificate. The code is in");
            SampleMenu.WriteMessage(MessageType.Title, 0, "CertificateOperations/SampleCertificateOperations.cs.\n");
            return true;
        }

        public bool RunSignData()
        {
            if (!GetAsymmetricSlotNumber(out byte slotNumber))
            {
                return RunInvalidEntry();
            }

            // This sample will choose the digest algorithm based on the
            // algorithm of the key in the slot specified. In addition,
            // the code needs to know if the key's algorithm is RSA. If
            // so, it will need to pad the digest.
            // Hence, get the algorithm.
            var signSlotContents = _slotContentsList.Find(x => x.SlotNumber == slotNumber);
            if (signSlotContents is null)
            {
                return RunInvalidEntry();
            }

            // This sample code will use SHA-384 for EccP384, and SHA-256
            // for all other algorithms.
            var hashAlgorithm = HashAlgorithmName.SHA384;
            
            if (signSlotContents.Algorithm == PivAlgorithm.EccEd25519)
            {
                hashAlgorithm = HashAlgorithmName.SHA512;
            }
            else
            {
                if (signSlotContents.Algorithm != PivAlgorithm.EccP384)
                {
                    hashAlgorithm = HashAlgorithmName.SHA256;
                }
            }


            byte[] dataToSign = GetArbitraryDataToSign();

            // This sample code will always use PSS for the RSA padding
            // scheme.
            var signPaddingScheme = RSASignaturePadding.Pss;

            if (!PrivateKeyOperations.RunSignData(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                slotNumber,
                dataToSign,
                hashAlgorithm,
                signPaddingScheme,
                signSlotContents.Algorithm,
                out byte[] signature))
            {
                return false;
            }

            // Demonstrate that the signature verifies.
            if (!PublicKeyOperations.SampleVerifySignature(
                signSlotContents.PublicKey,
                dataToSign,
                hashAlgorithm,
                signPaddingScheme,
                signature,
                out bool isVerified))
            {
                return false;
            }

            if (isVerified)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "Signature Verifies");
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "Signature Did Not Verify");
            }

            return true;
        }

        public bool RunDecryptData()
        {
            if (!GetAsymmetricSlotNumber(out byte slotNumber))
            {
                return RunInvalidEntry();
            }

            // It is possible to decrypt only if the key is RSA.
            var decryptSlotContents = _slotContentsList.Find(x => x.SlotNumber == slotNumber);
            if (decryptSlotContents is null)
            {
                return RunInvalidEntry();
            }
            if (!decryptSlotContents.Algorithm.IsRsa())
            {
                return RunInvalidEntry();
            }

            // This sample will encrypt arbitrary data. In a real-world
            // application, RSA encryption is used almost exclusively in
            // digital envelopes, so this sample will encrypt a 16-byte
            // symmetric key.
            byte[] dataToEncrypt = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18
            };

            // This sample uses OAEP with SHA-256 as the padding scheme
            // for all RSA key sizes.
            var encryptPaddingScheme = RSAEncryptionPadding.OaepSHA256;

            if (!PublicKeyOperations.SampleEncryptRsa(
                decryptSlotContents.PublicKey,
                dataToEncrypt,
                encryptPaddingScheme,
                out byte[] encryptedData))
            {
                return false;
            }

            if (!PrivateKeyOperations.RunDecryptData(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                slotNumber,
                encryptedData,
                encryptPaddingScheme,
                out byte[] decryptedData))
            {
                return false;
            }

            // Verify that the decrypted data is the same as the
            // encrypted data.
            if (decryptedData.SequenceEqual(dataToEncrypt))
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "Data decrypted correctly");
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "Data did not decrypt correctly");
            }

            return true;
        }

        public bool RunKeyAgree()
        {
            if (!GetAsymmetricSlotNumber(out byte slotNumber))
            {
                return RunInvalidEntry();
            }

            // It is possible to perform Key Agreement only if the key is ECC.
            var keyAgreeSlotContents = _slotContentsList.Find(x => x.SlotNumber == slotNumber);
            if (keyAgreeSlotContents is null)
            {
                return RunInvalidEntry();
            }
            if (!keyAgreeSlotContents.Algorithm.IsEcc())
            {
                return RunInvalidEntry();
            }

            // A correspondent performs ECC Key Agree phase 1, then
            // performs phase 2 using our public key. They send us their
            // public value (as a public key) and we must complete the
            // process using our private key in combination with their
            // public value.
            // The .NET Key Agree API will return the shared secret as
            // the digest of the computed value, so we need to specify
            // the algorithm. This sample always uses SHA-256 when the
            // key is ECC-P256, and SHA-384 when the key is ECC-P384.
            // Of course, in the real world, the correspondent would not
            // send the shared secret, but for this sample, we're
            // returning it as well so that we can compare the two
            // results to make sure they match.
            var hashAlgorithm = HashAlgorithmName.SHA256;
            if (keyAgreeSlotContents.Algorithm == PivAlgorithm.EccP384)
            {
                hashAlgorithm = HashAlgorithmName.SHA384;
            }

            if (!PublicKeyOperations.SampleKeyAgreeEcc(
                keyAgreeSlotContents.PublicKey,
                hashAlgorithm,
                out char[] correspondentPublicKey,
                out byte[] correspondentSharedSecret))
            {
                return false;
            }

            var correspondentKey = KeyConverter.GetPivPublicKeyFromPem(correspondentPublicKey);

            if (!PrivateKeyOperations.RunKeyAgree(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                slotNumber,
                correspondentKey,
                out byte[] computedSecret))
            {
                return false;
            }

            // The YubiKey generated the secret value, but the .NET
            // implementation digests that result.
            byte[] sharedSecret = MessageDigestOperations.ComputeMessageDigest(computedSecret, hashAlgorithm);

            // Verify that the decrypted data is the same as the
            // encrypted data.
            if (sharedSecret.SequenceEqual(correspondentSharedSecret))
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "The shared secrets match");
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "The shared secrets do not match");
            }

            return true;
        }

        public bool RunGetCertRequest()
        {
            if (!GetAsymmetricSlotNumber(out byte slotNumber))
            {
                return RunInvalidEntry();
            }

            var requestSlotContents = _slotContentsList.Find(x => x.SlotNumber == slotNumber);
            if (requestSlotContents is null)
            {
                return RunInvalidEntry();
            }

            var nameBuilder = new X500NameBuilder();
            nameBuilder.AddNameElement(X500NameElement.Country, "US");
            nameBuilder.AddNameElement(X500NameElement.State, "CA");
            nameBuilder.AddNameElement(X500NameElement.Locality, "Palo Alto");
            nameBuilder.AddNameElement(X500NameElement.Organization, "Fake");
            nameBuilder.AddNameElement(X500NameElement.CommonName, "Fake Cert");
            var sampleCertName = nameBuilder.GetDistinguishedName();
            SampleCertificateOperations.GetCertRequest(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                sampleCertName,
                requestSlotContents);

            requestSlotContents.PrintCertRequestPem();

            char[] request = requestSlotContents.GetCertRequestPem();
            return SampleCertificateOperations.IsValidCertRequestSignature(request);
        }

        public bool RunBuildSelfSignedCert()
        {
            if (!GetAsymmetricSlotNumber(out byte slotNumber))
            {
                return RunInvalidEntry();
            }

            var selfSignedSlotContents = _slotContentsList.Find(x => x.SlotNumber == slotNumber);
            if (selfSignedSlotContents is null)
            {
                return RunInvalidEntry();
            }

            SampleCertificateOperations.GetSelfSignedCert(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                selfSignedSlotContents);

            return SampleCertificateOperations.IsValidCertificateSignature(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                slotNumber,
                slotNumber);
        }

        public bool RunBuildCert()
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "For the requestor...");
            if (!GetAsymmetricSlotNumber(out byte requestorSlotNumber))
            {
                return RunInvalidEntry();
            }

            var requestorSlotContents = _slotContentsList.Find(x => x.SlotNumber == requestorSlotNumber);
            if (requestorSlotContents is null)
            {
                return RunInvalidEntry();
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "For the signing cert...");
            if (!GetAsymmetricSlotNumber(out byte signerSlotNumber))
            {
                return RunInvalidEntry();
            }

            var signerSlotContents = _slotContentsList.Find(x => x.SlotNumber == signerSlotNumber);
            if (signerSlotContents is null)
            {
                return RunInvalidEntry();
            }

            if (!SampleCertificateOperations.GetSignedCert(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                requestorSlotContents,
                signerSlotContents))
            {
                return false;
            }

            return SampleCertificateOperations.IsValidCertificateSignature(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                requestorSlotNumber,
                signerSlotNumber);
        }

        public bool RunRetrieveCert()
        {
            if (!GetAsymmetricSlotNumber(out byte slotNumber))
            {
                return RunInvalidEntry();
            }

            KeyPairs.RunRetrieveCert(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                slotNumber,
                out var certificate);

            byte[] certDer = certificate.Export(X509ContentType.Cert);
            char[] certPem = PemOperations.BuildPem("CERTIFICATE", certDer);
            SampleMenu.WriteMessage(MessageType.Title, 0, "\n" + new string(certPem) + "\n");
            return true;
        }

        public bool RunCreateAttestationStatement()
        {
            if (!GetAsymmetricSlotNumber(out byte slotNumber))
            {
                return RunInvalidEntry();
            }

            KeyPairs.RunCreateAttestationStatement(
                _yubiKeyChosen,
                slotNumber,
                out var certificate);

            byte[] certDer = certificate.Export(X509ContentType.Cert);
            char[] certPem = PemOperations.BuildPem("CERTIFICATE", certDer);
            SampleMenu.WriteMessage(MessageType.Title, 0, "\n" + new string(certPem) + "\n");
            return true;
        }

        public bool RunGetAttestationCert()
        {
            KeyPairs.RunGetAttestationCert(_yubiKeyChosen, out var certificate);

            byte[] certDer = certificate.Export(X509ContentType.Cert);
            char[] certPem = PemOperations.BuildPem("CERTIFICATE", certDer);
            SampleMenu.WriteMessage(MessageType.Title, 0, "\n" + new string(certPem) + "\n");
            return true;
        }

        private void AddSlotContents(SamplePivSlotContents newSlotContents)
        {
            if (!(newSlotContents is null))
            {
                // If we have slot contents already for this slot, replace them.
                _ = _slotContentsList.RemoveAll(x => x.SlotNumber == newSlotContents.SlotNumber);
                _slotContentsList.Add(newSlotContents);
            }
        }

        // Ask the user to specify a slot number. Offer and accept only numbers
        // for asymmetric key slots.
        private static bool GetAsymmetricSlotNumber(out byte slotNumber)
        {
            slotNumber = 0;

            SampleMenu.WriteMessage(MessageType.Title, 0, "Which Slot? (9A, 9C, 9D, 9E, or 82 - 95)");
            char[] valueChars = SampleMenu.ReadResponse(out int _);

            if (valueChars.Length != 2)
            {
                return false;
            }
            if (valueChars[0] != '8' && valueChars[0] != '9')
            {
                return false;
            }

            byte subVal = 0x30;
            byte hiVal = (byte)valueChars[0];
            byte loVal = (byte)valueChars[1];

            hiVal = (byte)((hiVal - subVal) << 4);

            if (valueChars[1] < '0' || valueChars[1] > '9')
            {
                subVal = 0x37;
                if (valueChars[1] < 'A' || valueChars[1] > 'F')
                {
                    subVal = 0x57;
                    if (valueChars[1] < 'a' || valueChars[1] > 'f')
                    {
                        return false;
                    }
                }
            }

            loVal -= subVal;

            slotNumber = (byte)(hiVal + loVal);
            if (slotNumber < 0x82 || slotNumber > 0x9E)
            {
                return false;
            }
            if (slotNumber > 0x95 && slotNumber < 0x9A)
            {
                return false;
            }
            if (slotNumber == 0x9B)
            {
                return false;
            }

            return true;
        }

        // Ask the user to specify an algorithm. Offer and accept only asymmetric
        // algorithms.
        private bool GetAsymmetricAlgorithm(out PivAlgorithm algorithm)
        {
            algorithm = PivAlgorithm.None;
            string[] menuItems = new string[] {
                "RSA 1024",
                "RSA 2048",
                "RSA 3076",
                "RSA 4096",
                "ECC P-256",
                "ECC P-384",
                "ECC Ed25519",
                "ECC X25519"
            };

            int response = _menuObject.RunMenu("Which algorithm?", menuItems);

            algorithm = response switch
            {
                0 => PivAlgorithm.Rsa1024,
                1 => PivAlgorithm.Rsa2048,
                2 => PivAlgorithm.Rsa3072,
                3 => PivAlgorithm.Rsa4096,
                4 => PivAlgorithm.EccP256,
                5 => PivAlgorithm.EccP384,
                6 => PivAlgorithm.EccEd25519,
                7 => PivAlgorithm.EccX25519,
                _ => PivAlgorithm.None,
            };

            return algorithm != PivAlgorithm.None;
        }

        // Ask the user to specify a PIN-only mode. Offer and accept only valid
        // PivPinOnlyMode values.
        private bool GetRequestedPinOnlyMode(out PivPinOnlyMode mode)
        {
            mode = PivPinOnlyMode.None;
            string[] menuItems = new string[] {
                "None",
                "PinProtected",
                "PinDerived",
                "Both PinProtected and PinDerived"
            };

            int response = _menuObject.RunMenu(
                "Which PIN-only mode?\nNote that Yubico recommends NOT setting a YubiKey to PIN-Derived.", menuItems);

            mode = response switch
            {
                0 => PivPinOnlyMode.None,
                1 => PivPinOnlyMode.PinProtected,
                2 => PivPinOnlyMode.PinDerived,
                3 => PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived,
                _ => PivPinOnlyMode.PinProtectedUnavailable,
            };

            return mode != PivPinOnlyMode.PinProtectedUnavailable;
        }

        // Ask the user to specify the new retry counts
        private static bool GetNewRetryCounts(out byte newRetryCountPin, out byte newRetryCountPuk)
        {
            newRetryCountPin = 0;
            newRetryCountPuk = 0;
            SampleMenu.WriteMessage(MessageType.Title, 0, "\n    ----WARNING!----");
            SampleMenu.WriteMessage(MessageType.Title, 0, "    Changing the PIV PIN and PUK Retry Counts");
            SampleMenu.WriteMessage(MessageType.Title, 0, "    resets the PIN and PUK to their default values");
            SampleMenu.WriteMessage(MessageType.Title, 0, "    as well as settting the retry counts.\n");

            SampleMenu.WriteMessage(MessageType.Title, 0, "PIN retry count? (1 to 255)");
            _ = SampleMenu.ReadResponse(out int response);
            if (response != 0 && response <= 255)
            {
                newRetryCountPin = (byte)response;
                SampleMenu.WriteMessage(MessageType.Title, 0, "PUK retry count? (1 to 255)");
                _ = SampleMenu.ReadResponse(out response);
                if (response != 0 && response <= 255)
                {
                    newRetryCountPuk = (byte)response;
                    return true;
                }
            }

            return false;
        }

        // Ask the user to specify the PIN policy.
        private bool GetPinPolicy(out PivPinPolicy pinPolicy)
        {
            string[] menuItems = new string[] {
                "Default",
                "Never",
                "Once",
                "Always"
            };

            int response = _menuObject.RunMenu("What will the PIN policy be for this key?", menuItems);

            pinPolicy = response switch
            {
                0 => PivPinPolicy.Default,
                1 => PivPinPolicy.Never,
                2 => PivPinPolicy.Once,
                3 => PivPinPolicy.Always,
                _ => PivPinPolicy.None,

            };

            return pinPolicy != PivPinPolicy.None;
        }

        // Ask the user to specify the touch policy.
        private bool GetTouchPolicy(out PivTouchPolicy touchPolicy)
        {
            string[] menuItems = new string[] {
                "Default",
                "Never",
                "Always",
                "Cached"
            };

            int response = _menuObject.RunMenu("What will the touch policy be for this key?", menuItems);

            touchPolicy = response switch
            {
                0 => PivTouchPolicy.Default,
                1 => PivTouchPolicy.Never,
                2 => PivTouchPolicy.Always,
                3 => PivTouchPolicy.Cached,
                _ => PivTouchPolicy.None,

            };

            return touchPolicy != PivTouchPolicy.None;
        }

        // Get a pre-built key of the given algorithm.
        private static bool GetPemPrivateKey(PivAlgorithm algorithm, out string pemKey)
        {
            pemKey = null;

            switch (algorithm)
            {
                default:
                    return false;

                case PivAlgorithm.Rsa1024:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MIICdQIBADANBgkqhkiG9w0BAQEFAASCAl8wggJbAgEAAoGBAM3/hGLkpff2hl/G\n" +
                        "boq5KtOctPuAGk0LAHevXzhJdiEn06NxVIrB+IXWapZcd+AVAJraiVIjeVpPMS90\n" +
                        "95W9BNOhK+68twD8RccPdKABWApyNr7z/hizssdif0jfYvjuV3n0nWXJ+Fp6O/pU\n" +
                        "SMkSaIYRplPOnco0uWir46se7dQjAgMBAAECgYAktBtFd5HuzXkBxZxakUWFMM26\n" +
                        "ZgfJpGUv7gpcQBKRM8RswbubgZYjWqHhKpadUYCrFrcS8IklwyhzWTbn8ibSsGTR\n" +
                        "KjFlAgRfqrEeRfee9m8Hcg6JbGqVjpDK8l5/hP/JoxVaNC9USpDHmrwqaz//NovC\n" +
                        "MxrHbxZ5qrASXcE0uQJBAO8/xli8g4vOxvVj8xD9XPdM6L6zJfavZuMS42phV9eJ\n" +
                        "x2mys80ia3VpMC/1F6QMasM0t8eqJM7TBXaUGhcNhwcCQQDca8MJy4wpsAe60ACN\n" +
                        "kctywhH906yVmDzY0c2Lxmqez05KIhyGFi5KzSxp1ErocTsPw+poDRZfWMoXZBuL\n" +
                        "/wcFAkA9J6AbrpQxeHmC4DmRbjIFRLN5i3F4zP0PrhRTbO53OdCvQ+6R0OqG6IxY\n" +
                        "td2FIWdo3mDbuLIP7ADJfrHskpihAkB1OKX3vpUi0me59MZmg4Oj2wvAZmLhB55M\n" +
                        "XH8od3PaUzs6d5udv4wM4cJd4bWYmicjwjgV7+fW+xw2hlmUASOVAkB5ggL6KdSl\n" +
                        "6srJQODDNC++zYBH9R9ctlNDzbZ3dBZ1xrD0C5naMoVpxagRujWDeNOfviDBTkXo\n" +
                        "hbGmCCihSn5/\n" +
                        "-----END PRIVATE KEY-----";
                    break;

                case PivAlgorithm.Rsa2048:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQCsdrf1M3aXyE7/\n" +
                        "nB2SGhEnRAL6WZYisGa8cdWEXTJy3P/SKRHy4z94wdG1huirdCoqKxiWrNBq/Zkm\n" +
                        "Ie7iQzGTWkA6Ornw9Oqy8C2NkUGepHbOWQnss1TyY8ij5uGS2ZhEcWxNKadXFlaK\n" +
                        "OK6/SCBxc3R5IdTqGd/dNoz/IisZNJ9PidbuuJt4LL2DS3xt4SiXBw/rVr9/Rw+H\n" +
                        "JVgnJHmqhU+5lCsmZgeDgwJK5e9Fn9K5c6Yvhoj5422YD1GBhYHJbJK+v+NYNUxG\n" +
                        "YACLTP+HWWU5iLDb0dnZdqs/V3P4vItltjpFDTrK5/WXi2WDzd56RVkpvg1Q9dG4\n" +
                        "r9Lk2EAlAgMBAAECggEATz8V9GO7YK84LZfsto+nxiUoQSUdKb9o1bpw5Ct23PTT\n" +
                        "0BvzFWp6ZeCZnhHpo67zGQFIgSPTePYigzUgcXNyukTEMn19p0zC84oNRHm0b1Mf\n" +
                        "DF45gzw8EkzrivSyPioiH4EGxMYZEJlBFq5JDbf0wGzO3kI/dXqCNUG1tB5dM20L\n" +
                        "jJPjHX8eq+wAA6FmXlfuJNbMtxo5fYjVKGLpFJWiZy4Knoy0Y0WKKUSm5V8f1+qi\n" +
                        "w68kcHKStoDzcy7sjGRTpqN9lG6bhiz+nbPd6FZhhk6/ZH6Sx0FTDQeXzcPi9ACm\n" +
                        "TK+gLnTW9LsaagJyZwIA+tB/Rzn6R/I0DkHtBbfggQKBgQDWBUIddPjetfNF1m72\n" +
                        "W929DJvAgR+0yQzi3vwOJTCcJif5+er+iS0mZkrX6Q3UuiZ1DKUDYPNR1+eG5PxT\n" +
                        "IHFTsu/NWaPfaa7ev4PCh9eOjFi/Oyvza4vt3sy69yVqHdXGZd9orVuEkjdGSJvy\n" +
                        "SFPPTKmTYlmrNCjweExpeIpv0QKBgQDOSr54MWLe49TzrKU3TVBiXrudXsO+74nX\n" +
                        "F5eaXRg1Ehtiu7SBtav4mqdXMw3nphhhYPY3OK0iNghDFmn+2BX3Ao0gDPPBPjWX\n" +
                        "49cpdM8cgiZLJBtsnLuOcqRSImKxOWtU/tYuqT+mZWwOBHEnw8u8vsbJeH3srg/R\n" +
                        "JYrPy6TUFQKBgQCURSJPvAjqag12tZ88J9rPrRt+WzZ3Dc5Son7m4dbyZvDNGto1\n" +
                        "qx1PfBCf0kKVvL0F3FO5qoIHkldBOgShJlm8zbuafV6tWc8fXHjQ3UF17T9ShJDn\n" +
                        "W/ueOPuHD8+o27CNeWg0Yd2EU7PdilIXoQoHFKpqg/lxRXqTVhRCAZOO8QKBgHXl\n" +
                        "BYGPR9/1+OfhzPIT/1KYrUQ8ukXOg8onM38GoSUDWh9NAtX2S3fieqw9Az9WDyzn\n" +
                        "yw64F0or8wDUOHNqbvMhxCGDBXN06BAMKBULKqoyP0xGMF4cHJxGLF68RAbgt9R1\n" +
                        "Z1Z3Z2bjI6PHKhv9q9wMc3MEp4Kx31w5xmEHEwYZAoGBALdlmDq5p621QRDD6yF+\n" +
                        "lp35ONPnejHTKwAYBz2lRXMdTpwG+CspXozQvwhyvKv1S8L3JzYLkiY68SWp2Qff\n" +
                        "oCn92FI0EfvxDOgTU/SXWuQlyTIz1JfncTrJrww2BvASjqCiV57OQMp+NpMQzfr1\n" +
                        "zoEv0zhhtMJE0ETIBh3fbLQj\n" +
                        "-----END PRIVATE KEY-----";
                    break;

                case PivAlgorithm.EccP256:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgoFe+ousm98sd74Ky\n" +
                        "cqNsUqjDIVJBVYqmiMgWQbAMpOKhRANCAAQbHbViI6OY8IkzYStZ97GDGYmN2Wdk\n" +
                        "ySzDbGpHdajnIu1PeJhksYPyrnnrnCKVeMstUV4z11sH5aVLtHtcKzRi\n" +
                        "-----END PRIVATE KEY-----";
                    break;

                case PivAlgorithm.EccP384:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MIG2AgEAMBAGByqGSM49AgEGBSuBBAAiBIGeMIGbAgEBBDD4CfYAlVwOaNM/iPr1\n" +
                        "aEakUvQ2huBBo44IYCereLDAvQ9gMDo/6ri3kzriyIhSCKqhZANiAAS8Ffkt7aZ3\n" +
                        "oQY948aRlXbpTAUhfvajnRPNSBo1g24SFel3TpGgq2u4nIlvV4oE896ikU5U7X45\n" +
                        "3tD+iq9lgB+8QNDJP6C6KginR3H1jMNRPMvaNrQC/VBpse+1Z1t5pvo=\n" +
                        "-----END PRIVATE KEY-----";
                    break;
                
                
                case PivAlgorithm.EccEd25519:
                    pemKey = "-----BEGIN PRIVATE KEY-----\nMC4CAQAwBQYDK2VwBCIEIDuLFRxirWSFqyiMTPB65M4sWI+smRcCdyMEL8RtN7ib\n-----END PRIVATE KEY-----";
                    break;
                
                case PivAlgorithm.EccX25519:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MC4CAQAwBQYDK2VuBCIEIGCCufpem+pMrhHcQwUvrUxh0KQ9zrNjuAVxM/E4d5hN\n" +
                        "-----END PRIVATE KEY-----";
                    break;
            }

            return true;
        }

        private static bool GetPemPublicKey(
            PivAlgorithm algorithm,
            out string pemKey)
        {
            pemKey = null;

            switch (algorithm)
            {
                default:
                    return false;

                case PivAlgorithm.Rsa1024:
                case PivAlgorithm.Rsa2048:
                case PivAlgorithm.EccP256:
                case PivAlgorithm.EccP384:
                    break;

                case PivAlgorithm.EccEd25519:
                    pemKey ="-----BEGIN PUBLIC KEY-----\nMCowBQYDK2VwAyEAvvmMviNf0LdUmfr5dVNZQaC79t3Ga7xTaD62d+icCtE=\n-----END PUBLIC KEY-----";
                    break;
                
                case PivAlgorithm.EccX25519:
                    pemKey =
                        "-----BEGIN PUBLIC KEY-----\n" +
                        "MCowBQYDK2VuAyEAyZ3Gl2lM1X9SVyAFjGi5skd28d9mQtJW1uf/zlrIhCU=\n" +
                        "-----END PUBLIC KEY-----\n";
                    break;
            }

            return true;
        }
        private static byte[] GetArbitraryDataToSign()
        {
            string arbitraryData = "To demonstrate how to sign data we need data to sign. " +
                "For this sample code, it doesn't really matter what the data is, " +
                "so just return some arbitrary data.";
            
            arbitraryData = "Hello, Ed25519!";


            return System.Text.Encoding.ASCII.GetBytes(arbitraryData);
        }
        
        private static byte[] GetBytesFromPem(
            string pemData)
        {
            var base64 = StripPemHeaderFooter(pemData);
            return Convert.FromBase64String(base64);
        }
        
        private static string StripPemHeaderFooter(
            string pemData)
        {
            var base64 = pemData
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("-----BEGIN EC PRIVATE KEY-----", "")
                .Replace("-----END EC PRIVATE KEY-----", "")
                .Replace("-----BEGIN CERTIFICATE-----", "")
                .Replace("-----END CERTIFICATE-----", "")
                .Replace("-----BEGIN CERTIFICATE REQUEST-----", "")
                .Replace("-----END CERTIFICATE REQUEST-----", "")
                .Replace("\n", "")
                .Trim();
            return base64;
        }
    }
}
