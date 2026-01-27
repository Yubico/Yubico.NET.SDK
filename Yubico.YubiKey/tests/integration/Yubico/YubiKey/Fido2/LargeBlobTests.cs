// Copyright 2025 Yubico AB
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
using System.Transactions;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    [Trait(TraitTypes.Category, TestCategories.Elevated)]
    public class LargeBlobTests
    {
        static readonly byte[] _clientDataHash = {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
        };

        static readonly RelyingParty _rp = new RelyingParty("relyingparty1");

        private readonly byte[] _pin = {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36
        };

        private readonly IYubiKeyDevice _testDevice;

        public LargeBlobTests()
        {
            _testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);
        }

        //This test requires user interaction to reset the FIDO2 application.
        [SkippableFact(typeof(DeviceNotFoundException))]
        public void SetLargeBlob_Succeeds()
        {
            using (var fido2Session = new Fido2Session(_testDevice))
            {
                fido2Session.KeyCollector = Fido2ResetForTest.ResetForTestKeyCollectorDelegate;
                // isValid = fido2Session.TrySetPin(new ReadOnlyMemory<byte>(_pin));
                // Assert.True(isValid);
                var user1 = new UserEntity(new byte[] { 1, 2, 3, 4 })
                {
                    Name = "TestUser1",
                    DisplayName = "Test User"
                };

                var mcParams1 = new MakeCredentialParameters(_rp, user1)
                {
                    ClientDataHash = _clientDataHash
                };
                mcParams1.AddExtension(Extensions.LargeBlobKey, new byte[] { 0xF5 });
                mcParams1.AddOption(AuthenticatorOptions.rk, true);

                fido2Session.AddPermissions(PinUvAuthTokenPermissions.AuthenticatorConfiguration);
                MakeCredentialData mcData1 = fido2Session.MakeCredential(mcParams1);
                Assert.True(mcData1.VerifyAttestation(_clientDataHash));

                var user2 = new UserEntity(new byte[] { 5, 6, 7, 8 })
                {
                    Name = "TestUser2",
                    DisplayName = "Test User 2"
                };

                var mcParams2 = new MakeCredentialParameters(_rp, user2)
                {
                    ClientDataHash = _clientDataHash
                };
                mcParams2.AddExtension("largeBlobKey", new byte[] { 0xF5 });
                mcParams2.AddOption(AuthenticatorOptions.rk, true);

                MakeCredentialData mcData2 = fido2Session.MakeCredential(mcParams2);
                Assert.True(mcData2.VerifyAttestation(_clientDataHash));

                var gaParams = new GetAssertionParameters(_rp, _clientDataHash);
                gaParams.AddExtension("largeBlobKey", new byte[] { 0xF5 });

                IReadOnlyList<GetAssertionData> assertions = fido2Session.GetAssertions(gaParams);
                Assert.Equal(2, assertions.Count);

                SerializedLargeBlobArray blobArray = fido2Session.GetSerializedLargeBlobArray();
                _ = Assert.NotNull(blobArray.EncodedArray);

                byte[] blobData1 = {
                    0x31,
                    0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50,
                    0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50,
                    0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50
                };
                _ = Assert.NotNull(mcData1.LargeBlobKey);
                ReadOnlyMemory<byte> key1 = ReadOnlyMemory<byte>.Empty;
                if (!(mcData1.LargeBlobKey is null))
                {
                    key1 = mcData1.LargeBlobKey.Value;
                    blobArray.AddEntry(blobData1, key1);
                }
                Assert.Null(blobArray.Digest);

                byte[] blobData2 = {
                    0x32,
                    0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f, 0x70,
                    0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f, 0x70,
                    0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f, 0x70
                };
                _ = Assert.NotNull(mcData2.LargeBlobKey);
                ReadOnlyMemory<byte> key2 = ReadOnlyMemory<byte>.Empty;
                if (!(mcData2.LargeBlobKey is null))
                {
                    key2 = mcData2.LargeBlobKey.Value;
                    blobArray.AddEntry(blobData2, key2);
                }

                fido2Session.SetSerializedLargeBlobArray(blobArray);
                _ = Assert.NotNull(blobArray.Digest);

                blobArray = fido2Session.GetSerializedLargeBlobArray();
                Assert.Equal(2, blobArray.Entries.Count);

                bool isDecrypted = blobArray.Entries[0].TryDecrypt(key1, out Memory<byte> plaintext1);
                Assert.True(isDecrypted);
                // bool isValid = Fido2ResetForTest.DoReset(_testDevice.SerialNumber);
                // Assert.True(isValid);
                var isValid = plaintext1.Span.SequenceEqual(blobData1.AsSpan());
                Assert.True(isValid);
                isDecrypted = blobArray.Entries[1].TryDecrypt(key1, out _);
                Assert.False(isDecrypted);
                isDecrypted = blobArray.Entries[1].TryDecrypt(key2, out var plaintext2);
                Assert.True(isDecrypted);
                isValid = plaintext2.Span.SequenceEqual(blobData2.AsSpan());
                Assert.True(isValid);
            }
        }

        /// <summary>
        /// Tests whether YubiKeys auto-generate a largeBlobKey for resident credentials
        /// even when the extension is NOT requested at MakeCredential time.
        /// 
        /// Per CTAP 2.1 spec: "Authenticators MAY optionally generate a largeBlobKey for
        /// a credential if the Large Blob Key extension is absent."
        /// 
        /// This test verifies that on YubiKeys:
        /// 1. MakeCredential without extension returns null LargeBlobKey (per spec)
        /// 2. GetAssertion WITH extension can retrieve the auto-generated key
        /// 3. The retrieved key can be used to store and retrieve blob data
        /// 
        /// This addresses the JIRA request about relaxing large blob requirements,
        /// mimicking the workflow used by libfido2/fido2-token for SSH certificates.
        /// </summary>
        [SkippableFact(typeof(DeviceNotFoundException))]
        public void GetLargeBlobKey_ViaGetAssertion_ForCredentialCreatedWithoutExtension_Succeeds()
        {
            // Use a distinct relying party to avoid conflicts with other tests
            var rp = new RelyingParty("test.largeblob.noextension");

            using (var fido2Session = new Fido2Session(_testDevice))
            {
                // bool isValid = Fido2ResetForTest.DoReset(_testDevice.SerialNumber);
                // Assert.True(isValid);
                fido2Session.KeyCollector = Fido2ResetForTest.ResetForTestKeyCollectorDelegate;

                var user = new UserEntity(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD })
                {
                    Name = "TestUserNoExtension",
                    DisplayName = "Test User No Extension"
                };

                // STEP 1: Create credential WITHOUT largeBlobKey extension
                var mcParams = new MakeCredentialParameters(rp, user)
                {
                    ClientDataHash = _clientDataHash
                };
                mcParams.AddOption(AuthenticatorOptions.rk, true);
                // NOTE: Intentionally NOT adding largeBlobKey extension

                fido2Session.AddPermissions(PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion, rp.Id);
                MakeCredentialData mcData = fido2Session.MakeCredential(mcParams);
                Assert.True(mcData.VerifyAttestation(_clientDataHash));

                // Per CTAP spec: authenticator MUST NOT return unsolicited largeBlobKey
                Assert.Null(mcData.LargeBlobKey);

                // STEP 2: Retrieve largeBlobKey via GetAssertion WITH the extension
                var gaParams = new GetAssertionParameters(rp, _clientDataHash);
                gaParams.AddExtension(Extensions.LargeBlobKey, new byte[] { 0xF5 });

                IReadOnlyList<GetAssertionData> assertions = fido2Session.GetAssertions(gaParams);
                Assert.Single(assertions);

                // KEY TEST: YubiKeys should return the auto-generated largeBlobKey
                ReadOnlyMemory<byte>? retrievedKey = assertions[0].LargeBlobKey;
                Assert.NotNull(retrievedKey);

                // STEP 3: Verify blob storage works with the retrieved key
                byte[] testBlobData = {
                    0x53, 0x53, 0x48, 0x20, 0x43, 0x45, 0x52, 0x54, // "SSH CERT" in ASCII
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                };

                SerializedLargeBlobArray blobArray = fido2Session.GetSerializedLargeBlobArray();
                int initialCount = blobArray.Entries.Count;

                blobArray.AddEntry(testBlobData, retrievedKey.Value);
                fido2Session.SetSerializedLargeBlobArray(blobArray);

                // STEP 4: Verify round-trip - retrieve and decrypt
                blobArray = fido2Session.GetSerializedLargeBlobArray();
                Assert.Equal(initialCount + 1, blobArray.Entries.Count);

                // Find and decrypt our entry
                bool foundAndDecrypted = false;
                foreach (var entry in blobArray.Entries)
                {
                    if (entry.TryDecrypt(retrievedKey.Value, out Memory<byte> plaintext))
                    {
                        if (plaintext.Span.SequenceEqual(testBlobData.AsSpan()))
                        {
                            foundAndDecrypted = true;
                            break;
                        }
                    }
                }

                Assert.True(foundAndDecrypted, 
                    "Failed to find and decrypt blob data using largeBlobKey retrieved via GetAssertion");
            }
        }
    }
}
