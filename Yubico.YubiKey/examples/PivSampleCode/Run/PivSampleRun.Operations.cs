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
            PivMainMenuItem.GetMetadata => RunGetMetadata(),
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

        public bool RunGetMetadata()
        {
            if (_yubiKeyChosen is null)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "You must choose a YubiKey first.");
                return true; 
            }

            try
            {
                using (var pivSession = new PivSession(_yubiKeyChosen))
                {
                    _ = GetAsymmetricSlotNumber(out byte slotNumber);
                    PivMetadata metadata = pivSession.GetMetadata(slotNumber);

                    if (metadata is null)
                    {
                        SampleMenu.WriteMessage(MessageType.Special, 0, $"\nNo key or certificate found in slot {GetPivSlotName(slotNumber)}.\n");
                        return true;
                    }

                    SampleMenu.WriteMessage(MessageType.Title, 0, "Slot: " + GetPivSlotName(slotNumber));
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Algorithm: " + metadata.PublicKeyParameters.KeyType);
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Key status: " + metadata.KeyStatus);
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Pin policy: " + metadata.PinPolicy);
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Touch policy: " + metadata.TouchPolicy + "\n");
                }
            }
            catch (Exception e)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, $"Error getting metadata: {e.Message}");
            }

            return true; // Keep the menu running
        }
        public static string GetPivSlotName(int slotNumber)
        {
            string name = (byte)slotNumber switch
            {
                0x9a => "PIV Authentication",
                0x9c => "Digital Signature",
                0x9d => "Key Management",
                0x9e => "Card Authentication",
                0xf9 => "Attestation",

                >= 0x82 and <= 0x95 => $"Retired Key {slotNumber - 0x82 + 1}",

                _ => "Unknown Slot"
            };

            return $"{name} ({(byte)slotNumber:X2})";
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

            if (!GetPemPrivateKey(algorithm, out string pemKey))
            {
                return true;
            }

            var privateKey = KeyConverter.GetPrivateKeyFromPem(pemKey.ToCharArray());

            // Special case for curve25519 keys, as they are not supported
            // by .NET's AsymmetricAlgorithm classes.
            // Therefore a public key cannot be derived from the private key without external libraries
            if (algorithm.IsCurve25519())
            {
                try
                {
                    using (var pivSession = new PivSession(_yubiKeyChosen))
                    {
                        pivSession.KeyCollector = _keyCollector.SampleKeyCollectorDelegate;
                        pivSession.ImportPrivateKey(slotNumber, privateKey, pinPolicy, touchPolicy);
                    }
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Private key imported successfully.\n");
                    return true;
                }
                catch (Exception ex)
                {
                    SampleMenu.WriteMessage(MessageType.Special, 0, $"Failed to import key: {ex.Message}\n");
                    return false;
                }
            }
            // For all other algorithms, derive the public key from the private key
            // Using .NET's AsymmetricAlgorithm class
            else
            {
                var publicKey = KeyConverter.GetPublicKeyFromPem(pemKey.ToCharArray());

                if (KeyPairs.RunImportPrivateKey(
                _yubiKeyChosen,
                _keyCollector.SampleKeyCollectorDelegate,
                privateKey,
                publicKey,
                slotNumber,
                pinPolicy,
                touchPolicy,
                out SamplePivSlotContents newSlotContents))
                {
                    if (newSlotContents is not null)
                    {
                        AddSlotContents(newSlotContents);
                    }
                    return true;
                }

                return false;
            }
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
            if (signSlotContents.Algorithm != KeyType.ECP384)
            {
                hashAlgorithm = HashAlgorithmName.SHA256;
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
            if (!decryptSlotContents.Algorithm.IsRSA())
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
            if (!keyAgreeSlotContents.PublicKey.KeyType.IsECDsa())
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
            if (keyAgreeSlotContents.Algorithm == KeyType.ECP384)
            {
                hashAlgorithm = HashAlgorithmName.SHA384;
            }

            if (!PublicKeyOperations.SampleKeyAgreeEcc(
                keyAgreeSlotContents.PublicKey,
                hashAlgorithm,
                out char[] correspondentPublicKeyPem,
                out byte[] correspondentSharedSecret))
            {
                return false;
            }

            IPublicKey correspondentKey;
            try
            {
                byte[] derEncodedKey = PemOperations.GetEncodingFromPem(
                correspondentPublicKeyPem,
                "PUBLIC KEY");

                correspondentKey = ECPublicKey.CreateFromSubjectPublicKeyInfo(derEncodedKey);
            }
            catch (Exception e)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, $"Failed to decode key: {e.Message}");
                return false;
            }

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

        private bool GetAsymmetricAlgorithm(out KeyType algorithm)
        {
            algorithm = KeyType.None;
            string[] menuItems = new string[] {
                "RSA 1024",
                "RSA 2048",
                "RSA 3072",
                "RSA 4096",
                "ECC P-256",
                "ECC P-384",
                "ED25519",    
                "X25519"      
            };

            int response = _menuObject.RunMenu("Which algorithm?", menuItems);

            algorithm = response switch
            {
                0 => KeyType.RSA1024,
                1 => KeyType.RSA2048,
                2 => KeyType.RSA3072,
                3 => KeyType.RSA4096,
                4 => KeyType.ECP256,
                5 => KeyType.ECP384,
                6 => KeyType.Ed25519,
                7 => KeyType.X25519,
                _ => KeyType.None,
            };

            return algorithm != KeyType.None;
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
        private static bool GetPemPrivateKey(KeyType algorithm, out string pemKey)
        {
            pemKey = null;

            switch (algorithm)
            {
                default:
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Pre-built key not available for that algorithm." + "\n");
                    return false;

                case KeyType.RSA1024:
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

                case KeyType.RSA2048:
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
                case KeyType.RSA3072:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MIIG/QIBADANBgkqhkiG9w0BAQEFAASCBucwggbjAgEAAoIBgQCYgde5qDyav2I7\n" +
                        "FO0C5eXSymXiWOo195r6bchQIi/MK3aGzkgVyhCP0wnyyU2Q7HTxJNvB4ObhHll+\n" +
                        "fWVMHxp1PY2vFLj+V82dShy6eL8rzjrNKrVAf1Mp4A+fkNGBbahhjB0bgOLabYJ0\n" +
                        "IrX6kuBpyDFNQW/53hd19P3Ftit2IHYltIUr7k+tsGGXl2k9FaieWbPAMdQW//wi\n" +
                        "cPB+rr/7JKTglcu3IF8bQhoWZ19rbA1tDiMVbIY7LUhtVAxLMfl7E2jinyUG3nLJ\n" +
                        "9Eo5/gnuBemHOaP+oCiwXia/oRUtmtUoyGRgqNQWE5TVuPzm24mIHIlbNW8fqVho\n" +
                        "aDKpetnjQnVjsvKtcvPNMvF1J1zL4rXkDC407unpZ+UHDeYU/qWB5wv7O7xhmzD5\n" +
                        "SlW+WIJ041k4u+lI7gfOQ2SkJMtKleywHPq89Chde82iR328DqjtlGLJzorrQ1BT\n" +
                        "jzf1eVbGF7BZY/HWKOI//1A1tNDRH+4YvWnXbbCeLC8j8I6EM0cCAwEAAQKCAYAF\n" +
                        "55XcBbjkkoWRttUYwdPst5KYRkvTIq7cBIVQub7saPMu2iyNg2qx8HOeWpyBM7zk\n" +
                        "KHXFtn9AUHXm+tROmfzi2ysLXOiiS4rJDNLt7IZ76LRht/lkJON2zjEGQyVWNuuu\n" +
                        "n/q+IBBbpDyWLuqpNMFkw+NJk9k4Of9IihK+2LdedhIWkm9LH+/zA2g6TIELl2h7\n" +
                        "sQqcZcSwNOFxmyCeMJ/h1mDEo6gYHM2+qjseV1d11K1azCuGXp9+PQ0xbNainRQP\n" +
                        "YobfJS9VA40MiICEg6zmytU1jpQFnY3pD7GudgGbZWfTXKJs9Z7088qxB37UEJn2\n" +
                        "qSrysc806nqt4F0c8VqJ3PGMOEdaCfXtKo8V2anDHjxlmC5B/W6Ozo/+C8PO8IJ6\n" +
                        "aFa3mjcpYLBblynF3GpE5XaFTQT+Gz6dohmbiegjgVhazRwfHiugdZjiSfND43wn\n" +
                        "jqDFOKYfLG+5/kXWokWEsKKaXJZrnv3kHohZGbHc4lSF25lH82MyJOjiH8q43FkC\n" +
                        "gcEAz9HsJxhijEnVYFzH7cj2D3m58EZHCgdumdEBs5lhOdUtuarudIUZnm3qslmy\n" +
                        "aGBv6FtKesVUQZ1St/ChIswtxvD2DK7vfMvol3zXVda+dKYpF46/7eyrHMrJsYh7\n" +
                        "zlatqUF8heukpIrnG82GiZk5IeQvoQIslFGFc6p4gSiYo0XqnomHeYbwsEFr67u2\n" +
                        "v2j6vU1TXXTfawMVDtReomObyGXzD8uEJ48vgTH6dLqDvbEty7hcDJfy54jq8iTj\n" +
                        "BQvVAoHBALvdHZCuuYpAJJPKDNGlYzPytfogn/KPnSasWIvPMoJepHic6DEajker\n" +
                        "MGGVLlRTYvxHTul7ZP070yCc4P89bKYQ8kxmOOmdOHThZiw770bz9SgzrKak4ud1\n" +
                        "apz0wKsSH6AvxJ0HvNEI7hZG21SmafDlwaikDm83CoxnzgG9fyCinH9RfudDiPir\n" +
                        "8zBRTkHX0ZjSf5vT1YeewYfh/caSzKFso90EkJwJB6TKQtf00PuFsWkdmL56nzst\n" +
                        "YgjYuHIcqwKBwDC54YrRFtoZvaPYXTANfFPokIYblDBvyaja7nEzty4eI5hy0XIU\n" +
                        "ewtAblTe3wvGALcUIIRkm/q+blSeYMmN4fXRLX+PzKsQDDrolHyV2xXyl5Pkbm/U\n" +
                        "m9ImYd/0RkL8477Zkd68f1/tCX7lU3QTrueZXul7XwRvkMCr6ZEu+Yreq8H8MP13\n" +
                        "fBt3W1xsKM78SD32UWOKMZAfquJNPNsKS85SyQidCSFVWygJldWknZruXfR0B3EU\n" +
                        "d2l+Gsgnier2+QKBwQCef77a+9+EmfuCST0pf+1Dven0/6OTJcHECDKouoZ14d3H\n" +
                        "+TIZg7s5EmC+Y/vzn2rrSEp2yOn6kYfegx19m1hYgAG9nZ001LX2PtlSRrrpVRio\n" +
                        "83geHQ1nlPP/Oqx3aNIP911d01Jl1q/xUZTpRYIqgd4zJz8abAjVTxtK8pMYeLmq\n" +
                        "3ZpBCgS9MW37fQ2Wlby7wBVz5nTIeJP1ziCrcd198EgMSDatvxyY1yEwTNgo7bIx\n" +
                        "6oudYZ2IcxC8QATWGgMCgcBi2AWkmiJ7717YN+w/RulgNovQD1sbiRC0nTOhLuWz\n" +
                        "v9ZLzaIux0LB6jqvgdFb9iuD6vLHhJGti2lBOKlKA1RxvHpZZB/MnAW8v67Qz01q\n" +
                        "fpVU7f9q9ctVYHaF3vPN1fLxZNAkAg6u+nyVhZpkUC97J5TQIzfhEqsjkpL5u7EX\n" +
                        "RN132FhLFpdzjcWnI0UIAa7BltuTTVbOzOGnuKeuSeanEqShvr4vvD3wfzR+0LYA\n" +
                        "sDzWnEdDtARm+OFyjvo4qU0=\n" +
                        "-----END PRIVATE KEY-----";;
                    break;
                case KeyType.RSA4096:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MIIJQgIBADANBgkqhkiG9w0BAQEFAASCCSwwggkoAgEAAoICAQDGveAL2mUaE14e\n" +
                        "4jxuSNsIOX92UmVkdugMze373fWyHLbCLfAkiNj5lnGtV8Z5zZg8qkVS5+EtKWne\n" +
                        "dvbSfhE7dcntqNrnXqqtMWoNHAP9Kix2Dlzb8gNLn2SOQa16TfEbzFGBVRXwVYOb\n" +
                        "6ZTEL3LWl4AKydvqUPa2Onbrj5rNBccynpgze7v32OsWXa3truqdjZKERmazs4zX\n" +
                        "wLjKTK50LVviZrl8luVeePPFrohPvKCfK8SjkF8tzIQzq6dZOMIxKWODBa7K8BDO\n" +
                        "RjdcUM6najxjDJ55F3mWGMzSHngCHwTdvbz50CNxULCiSGy7vNPflFq8xM+6xI4t\n" +
                        "pMIsUXYsoAHSS62sMw7JTpYxSZ2l2HcHBYuH8w7+Rv5/wo9BM17vlKddizFCcCmk\n" +
                        "FxJZbN9zkNCaSMlBOvN9MGdi2GznXEXunLXeu8TZp7JXCT9fXvBIxpVhih9N6e1r\n" +
                        "ENpTRDdV8ElteuwsSaDE1Q8DWbLrLTyAQKB5W+elRGGJQC7AQTuC4QUgr0qIj9vm\n" +
                        "ZkO+ziFstOnd9OvkftwcDSrUU8imd19UFlUw7X5IhnHw8DolJ3r+FKY9meWDOWXi\n" +
                        "bysNTn/5/izBdonX0vw6Hffzfhd4al0Hze8uvY3lzZMPtv/0rDAkDfkO5zY/sOgo\n" +
                        "P2GjJkS7u4cyNoH+lbAvZMyuZz/JuwIDAQABAoICABWM8FcIsw7dS8b4jEn/L3UY\n" +
                        "Wwh3DdSTij0dNXGq02Ihd/Xdal1j03dZB3GfA4AguaHWatb/Gu09QOQlLUWM8wxq\n" +
                        "DN/u//G307UdFx1dzNbudEzG6O3Ws+HG4m4ElC2fdwYnJS1rjwn1E+TbssyFQqQf\n" +
                        "YHyLAARMDDydYVjQxR33Qu7rwKBQigTpqjBOLzaHUZyNBfa+9ZMF5L9egAs7vm0N\n" +
                        "oBmQPwvSBwQ0BGcKsnBHCXnJErUTyiZat3ks42Qq4e/Xx5klDBuoZYIgng8uGgKQ\n" +
                        "ZATvkN2bnI0YmlksgaHlQC9VTEEgfz9h2w114giHhMgJO7+dbdMYTjyH0aBhovkp\n" +
                        "43Dv2VMyxZRPtaHqQ5eIls3yRXNMPX7BLWskv5MJxAsUqn13hcUwpf5ioNYXtl9Z\n" +
                        "Ey1yn4x4KBR7TvGJOwWJteRiBV1gIx+6fAwRzugYLGyN/nK8eYCbBvxTceHNpndv\n" +
                        "NnSvlfa2UwiiQS1ncOQRynzd1NDUWILxErcq8nMO2jcucjOLVs0g6yC5yzb6T/Vs\n" +
                        "wQ0QHtJ5BXIA1PbcqaLqYK5iZOysDTWXGWRYg7BjE0VIN+otzC0XusD1hAYZPeKN\n" +
                        "v8522uNUmLVt7xDQMVFUJ/KDIFJ1y/z0gNrsWqedlvTY8xplH7IsW9pb7bOlSYSX\n" +
                        "HMunCDeYsWip2emirRPRAoIBAQDid6XU66FaMn9JVPtuzUzVTABjj5mXxj+mOsss\n" +
                        "/ngRQmQQT2jTrXLfc2nY1MDxcnMi/NGtPS818s++9nfIMvvGbUS871eyFvYKXCzq\n" +
                        "1Q802CXbrjOWKDWE/w54KUvS1djKtWIygW5X1fKdWP6PwF/pKoUJ0mglxWdtj3AE\n" +
                        "BtXhVj79zHjOTPyI9314EHsi1TDQYv/l9RmKTHLtnt6p+H8o0kLVnXYZW8dGLBJf\n" +
                        "xh3iSjZyHmAhHPx+ZBbawJV+M2XJPJl+pd0uYMQURvn+tJ9HJXnem94fMfBz7Ih6\n" +
                        "88ZMS9VR3Bf6KphPyGqWw4s0LT4MtGKBcxxM2aiR4NjCjWxLAoIBAQDgqKNCLCXA\n" +
                        "yXIofNMVGXJGt7tb9Vq1PfAU85rbiHjTLm+lNlv+0IhsDSsuVAxZeZuQxN5/vLwP\n" +
                        "dzUX1e7t++V4lHMxnJT/La/TvWxszY+d32jOTOazmdPy5VfYB/ZnFh/74p1bKWci\n" +
                        "xnIiCCDUKt28HbOEH128vaLPQAh/DA0gUo8QAKhmViEseS5gmDXDXRo0A2OE0Y8u\n" +
                        "ISPc4vwt0h0vC43RoOgXxd2fMfNRLUzSZXy06d7QpkWjwmxg2Vyf3cdId8IkeYqX\n" +
                        "yBM4fF8g0fGGvVbRBIv2eWX+3Eca/kuUqdgKjkgYrz5C4aWRIM03MlnMh2MMi6oJ\n" +
                        "i3WKlWWgIdJRAoIBACZAy5QhkQmpSfLbFfVrXDUTN2WZ1fnbFNlBSRx6h1FzA2/1\n" +
                        "2eEXhTXVSuXDWivuhyA70DcRBK56Kzk4bJc2dWzY/CllzExasIijdTrdbkog0JRA\n" +
                        "4pnUhOXIJ2uInjQoxwvGg6XAUyEnFGobpDQn7It4ESzNi6YFqCjLd8JWXT5I0S8R\n" +
                        "oL5IJsgD9f+X2RTTKgGpF0yCkCPaMfeNRFM1lFUS3xMyG8bAx/JEc34V+upEWtn/\n" +
                        "44D0Ynn+8hVVPmsox2Ksh8jqv2ecFMLQEl5BqD3eSK2fam+egd0y8QLDtpUgohHH\n" +
                        "uY0aMMwZMFfzA8p2ceq3dYQkK32Xrm+lqTeDp+0CggEARUr+gAyJ4HrB4UcO/DUL\n" +
                        "EFDfUy/MOJbQFEZG/2uKiOiLuxOXMHM1gM5XAUUfQgHGP9LZJeEayFJmZ+Gufmzx\n" +
                        "jE2NckHvmv2Ge/KzHKQSpgkglHEXv1G1E/g1LgbWs1kZqGFvU4zjqNA4p9KF/arz\n" +
                        "FXC7zAa4rNx4+R+w/y7CZbPROIhbaKUsOkFuUpDgFFAFIwHgkjjoxrumCh1g1uk1\n" +
                        "4yrXJU9SBvMatl17xRAJ3+M5obt45DZEyIvRTdX9QbnwG6QEl6d9Xe9yLjv+Q2s9\n" +
                        "6edAfdu/J9it4vwiWmsQ+NuiLS9RgXub4pkiri7F3T6EgBdKL7ZsTeFb8dC+tbN4\n" +
                        "4QKCAQEAr/HPEi+QlEniXweYMFlduCO7wc+DXjXyn4A8GHSyCM49d0b8wRwYr+C1\n" +
                        "LIlKeqyZkP/CbhSAuiIv4kdXHZ1mxxx2Ooz+oO37wX68aEiQyisdm8s4OzMZHTOD\n" +
                        "0RvQBQYMShI7qO+9d4ZN6ziFueJPnRO492ysZQSd8FwjBcsCD/w4an0/ZQQBzRJr\n" +
                        "P4z1QfOPGoKgTofMa6tTJFTM6HsCEeEHgWtTaxpQBAiCJorIgJM3ybIYWRtqvvt9\n" +
                        "s1Mtg5dSmuX6gU5ZGA2hNUoNBcTH0vobOuxW85uJKGZt89TxDxpeLUvyILQHpmX9\n" +
                        "tPUHi94EBqDVlCctWL04ro3DY1dEsw==\n" +
                        "-----END PRIVATE KEY-----";
                    break;
                case KeyType.ECP256:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgoFe+ousm98sd74Ky\n" +
                        "cqNsUqjDIVJBVYqmiMgWQbAMpOKhRANCAAQbHbViI6OY8IkzYStZ97GDGYmN2Wdk\n" +
                        "ySzDbGpHdajnIu1PeJhksYPyrnnrnCKVeMstUV4z11sH5aVLtHtcKzRi\n" +
                        "-----END PRIVATE KEY-----";
                    break;

                case KeyType.ECP384:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MIG2AgEAMBAGByqGSM49AgEGBSuBBAAiBIGeMIGbAgEBBDD4CfYAlVwOaNM/iPr1\n" +
                        "aEakUvQ2huBBo44IYCereLDAvQ9gMDo/6ri3kzriyIhSCKqhZANiAAS8Ffkt7aZ3\n" +
                        "oQY948aRlXbpTAUhfvajnRPNSBo1g24SFel3TpGgq2u4nIlvV4oE896ikU5U7X45\n" +
                        "3tD+iq9lgB+8QNDJP6C6KginR3H1jMNRPMvaNrQC/VBpse+1Z1t5pvo=\n" +
                        "-----END PRIVATE KEY-----";
                    break;
                case KeyType.Ed25519:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MC4CAQAwBQYDK2VwBCIEIDuLFRxirWSFqyiMTPB65M4sWI+smRcCdyMEL8RtN7ib\n" +
                        "-----END PRIVATE KEY-----";
                    break;
                case KeyType.X25519:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MC4CAQAwBQYDK2VuBCIEIGCCufpem+pMrhHcQwUvrUxh0KQ9zrNjuAVxM/E4d5hN\n" +
                        "-----END PRIVATE KEY-----";
                    break;
            }

            return true;
        }

        private static byte[] GetArbitraryDataToSign()
        {
            string arbitraryData = "To demonstrate how to sign data we need data to sign. " +
                "For this sample code, it doesn't really matter what the data is, " +
                "so just return some arbitrary data.";

            return System.Text.Encoding.ASCII.GetBytes(arbitraryData);
        }
    }
}
