// Copyright 2023 Yubico AB
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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
    // This file contains the methods that demonstrate how to perform FIDO2
    // Protocol operations.
    public static class Fido2Protocol
    {
        public static bool RunGetAuthenticatorInfo(IYubiKeyDevice yubiKey, out AuthenticatorInfo authenticatorInfo)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                authenticatorInfo = fido2Session.AuthenticatorInfo;
            }

            return true;
        }

        // Note that this method will not check the length of credBlobData,
        // allowing you to see how the SDK handles improper input.
        public static bool RunMakeCredential(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            ReadOnlyMemory<byte> clientDataHash,
            string relyingPartyName,
            string relyingPartyId,
            string userName,
            string userDisplayName,
            ReadOnlyMemory<byte> userId,
            CredProtectPolicy credProtectPolicy,
            byte[] credBlobData,
            out MakeCredentialData makeCredentialData)
        {
            if (credBlobData is null)
            {
                throw new ArgumentNullException(nameof(credBlobData));
            }

            var relyingParty = new RelyingParty(relyingPartyId)
            {
                Name = relyingPartyName,
            };
            var userEntity = new UserEntity(userId)
            {
                Name = userName,
                DisplayName = userDisplayName,
            };

            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                var makeCredentialParameters = new MakeCredentialParameters(relyingParty, userEntity)
                {
                    ClientDataHash = clientDataHash,
                };

                // Although the standard specifies the Options as optional,
                // setting the option "rk" to true means that this credential
                // will be discoverable.
                // If a credential is discoverable, GetAssertion will be able to
                // find the credential given only the relying party ID.
                // If it is not discoverable, the credential ID must be supplied
                // in the AllowList for the call to GetAssertion.
                // This sample code wants all credentials to be discoverable.
                makeCredentialParameters.AddOption("rk", true);

                if (fido2Session.AuthenticatorInfo.Extensions.Contains("hmac-secret"))
                {
                    makeCredentialParameters.AddHmacSecretExtension(fido2Session.AuthenticatorInfo);
                }

                if (credProtectPolicy != CredProtectPolicy.None)
                {
                    makeCredentialParameters.AddCredProtectExtension(credProtectPolicy, fido2Session.AuthenticatorInfo);
                }

                if (credBlobData.Length > 0)
                {
                    makeCredentialParameters.AddCredBlobExtension(
                        credBlobData, fido2Session.AuthenticatorInfo);
                }

                // This method will automatically perform any PIN or fingerprint
                // verification needed.
                makeCredentialData = fido2Session.MakeCredential(makeCredentialParameters);

                // The MakeCredentialData contains an attestation statement (a
                // signature). It is possible to verify that signature.
                return makeCredentialData.VerifyAttestation(clientDataHash);
            }
        }

        public static bool RunGetAssertions(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            ReadOnlyMemory<byte> clientDataHash,
            string relyingPartyId,
            ReadOnlyMemory<byte> salt,
            out IReadOnlyList<GetAssertionData> assertions,
            out IReadOnlyList<byte[]> hmacSecrets)
        {
            assertions = new List<GetAssertionData>();
            var hmacSecretList = new List<byte[]>();
            hmacSecrets = hmacSecretList;
            var relyingParty = new RelyingParty(relyingPartyId);

            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                var getAssertionParameters = new GetAssertionParameters(relyingParty, clientDataHash);

                // If there is a credBlob, we want to get it. By setting the "credBlob"
                // extension, the YubiKey will return it if there is one (and return
                // nothing if there is none). In this case, the data to accompany the
                // name of the extension ("credBlob"), is the CBOR encoding of true.
                // That's simply the single byte 0xF5.
                getAssertionParameters.RequestCredBlobExtension();

                if (!salt.IsEmpty)
                {
                    getAssertionParameters.RequestHmacSecretExtension(salt);
                }

                // This method will automatically perform any PIN or fingerprint
                // verification needed.
                assertions = fido2Session.GetAssertions(getAssertionParameters);

                foreach (var assertionData in assertions)
                {
                    byte[] hmacSecret = assertionData.AuthenticatorData.GetHmacSecretExtension(fido2Session.AuthProtocol);
                    hmacSecretList.Add(hmacSecret);
                }
            }

            return true;
        }

        // This method will get information about the discoverable credentials on
        // the YubiKey and return it as a List of object.
        // The first in the list (index zero) will be the result of getting
        // metadata, and will be an object of type Tuple<int,int>.
        // If there are any credentials, the next in the list will be the first
        // relying party. It will be an object of type RelyingParty.
        // Following the relying party will be the credential or credentials. For
        // each entry after a RelyingParty, until reaching the next RelyingParty
        // object, it is a credential, an object of the class CredentialUserInfo.
        // If there are any other relying parties, the list will then contain
        // sets of relying party and credential entries.
        // For example, suppose there are three credentials, two for RP
        // example.com and one for RP sample.org. The list will contain
        // objects representing the following:
        //
        //      entry                  class
        // -------------------------------------------
        //   metadata             Tuple<int,int>
        //   RP example.com       RelyingParty
        //     cred               CredentialUserInfo
        //     cred               CredentialUserInfo
        //   RP sample.org        RelyingParty
        //     cred               CredentialUserInfo
        public static bool RunGetCredentialData(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            out IReadOnlyList<object> credentialData)
        {
            var returnValue = new List<object>();
            credentialData = returnValue;

            using (var fido2Session = new Fido2Session(yubiKey))
            {
                if (fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.credMgmt) != OptionValue.True)
                {
                    return false;
                }

                fido2Session.KeyCollector = KeyCollectorDelegate;

                (int credCount, int remainingCount) = fido2Session.GetCredentialMetadata();

                returnValue.Add(new Tuple<int, int>(credCount, remainingCount));

                var rpList = fido2Session.EnumerateRelyingParties();
                foreach (var currentRp in rpList)
                {
                    returnValue.Add(currentRp);

                    var credentialList =
                        fido2Session.EnumerateCredentialsForRelyingParty(currentRp);

                    foreach (var currentCredential in credentialList)
                    {
                        returnValue.Add(currentCredential);
                    }
                }
            }

            return true;
        }

        public static bool RunUpdateUserInfo(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            CredentialId credentialId,
            UserEntity updatedInfo)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                // This method will automatically perform any PIN or fingerprint
                // verification needed.
                fido2Session.UpdateUserInfoForCredential(credentialId, updatedInfo);
            }

            return true;
        }

        public static bool RunDeleteCredential(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            CredentialId credentialId)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                // This method will automatically perform any PIN or fingerprint
                // verification needed.
                fido2Session.DeleteCredential(credentialId);
            }

            return true;
        }

        public static bool RunGetLargeBlobArray(
            IYubiKeyDevice yubiKey,
            out SerializedLargeBlobArray blobArray)
        {
            blobArray = null;

            using (var fido2Session = new Fido2Session(yubiKey))
            {
                if (fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.largeBlobs) != OptionValue.True)
                {
                    return false;
                }

                // This method will automatically perform any PIN or fingerprint
                // verification needed.
                blobArray = fido2Session.GetSerializedLargeBlobArray();
            }

            return true;
        }

        // Cycle through the Array, trying to decrypt each entry. If it works,
        // return the decrypted data and the index at which the entry was found.
        // If there is no entry, return "" and -1.
        public static string GetLargeBlobEntry(
            SerializedLargeBlobArray blobArray,
            ReadOnlyMemory<byte> largeBlobKey,
            out int entryIndex)
        {
            if (blobArray is null)
            {
                throw new ArgumentNullException(nameof(blobArray));
            }

            var plaintext = Memory<byte>.Empty;
            byte[] plainArray = Array.Empty<byte>();
            entryIndex = -1;
            try
            {
                for (int index = 0; index < blobArray.Entries.Count; index++)
                {
                    if (blobArray.Entries[index].TryDecrypt(largeBlobKey, out plaintext))
                    {
                        entryIndex = index;
                        plainArray = plaintext.ToArray();
                        return Encoding.Unicode.GetString(plainArray);
                    }
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext.Span);
                CryptographicOperations.ZeroMemory(plainArray);
            }

            return "";
        }

        public static bool RunStoreLargeBlobArray(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            SerializedLargeBlobArray blobArray)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                if (fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.largeBlobs) != OptionValue.True)
                {
                    return false;
                }

                fido2Session.KeyCollector = KeyCollectorDelegate;

                // This method will automatically perform any PIN or fingerprint
                // verification needed.
                fido2Session.SetSerializedLargeBlobArray(blobArray);
            }

            return true;
        }

        public static bool RunGetBioInfo(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            out BioModality modality,
            out FingerprintSensorInfo sensorInfo,
            out IReadOnlyList<TemplateInfo> templates)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                modality = fido2Session.GetBioModality();

                if (modality != BioModality.None)
                {
                    sensorInfo = fido2Session.GetFingerprintSensorInfo();
                    templates = fido2Session.EnumerateBioEnrollments();
                }
                else
                {
                    sensorInfo = new FingerprintSensorInfo(0, 0, 0);
                    templates = new List<TemplateInfo>();
                }
            }

            return true;
        }

        public static TemplateInfo RunEnrollFingerprint(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            string friendlyName,
            int timeoutMilliseconds)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                // This method will automatically perform any PIN or fingerprint
                // verification needed.
                if (timeoutMilliseconds > 0)
                {
                    return fido2Session.EnrollFingerprint(
                        friendlyName,
                        timeoutMilliseconds);
                }
                else
                {
                    return fido2Session.EnrollFingerprint(
                        friendlyName,
                        null);
                }
            }
        }

        public static bool RunSetBioTemplateFriendlyName(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            ReadOnlyMemory<byte> templateId,
            string friendlyName)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                // This method will automatically perform any PIN or fingerprint
                // verification needed.
                fido2Session.SetBioTemplateFriendlyName(templateId, friendlyName);
            }

            return true;
        }

        public static bool RunRemoveBioEnrollment(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            ReadOnlyMemory<byte> templateId)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                // This method will automatically perform any PIN or fingerprint
                // verification needed.
                return fido2Session.TryRemoveBioTemplate(templateId);
            }
        }

        public static bool RunEnableEnterpriseAttestation(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                // This method will automatically perform any PIN or fingerprint
                // verification needed.
                return fido2Session.TryEnableEnterpriseAttestation();
            }
        }

        public static bool RunToggleAlwaysUv(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            out OptionValue newValue)
        {
            newValue = OptionValue.Unknown;

            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                // This method will automatically perform any PIN or fingerprint
                // verification needed.
                bool isValid = fido2Session.TryToggleAlwaysUv();
                if (isValid)
                {
                    newValue = fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.alwaysUv);
                }

                return isValid;
            }
        }

#nullable enable
        public static bool RunSetPinConfig(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            int? newMinPinLength,
            IReadOnlyList<string>? relyingPartyIds,
            bool? forceChangePin)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                // This method will automatically perform any PIN or fingerprint
                // verification needed.
                return fido2Session.TrySetPinConfig(newMinPinLength, relyingPartyIds, forceChangePin);
            }
        }
#nullable restore
    }
}
