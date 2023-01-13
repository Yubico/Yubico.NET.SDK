// Copyright 2022 Yubico AB
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
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class LargeBlobTests
    {
        static readonly byte[] _clientDataHash = {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
        };

        static readonly RelyingParty _rp = new RelyingParty("relyingparty1");

        private readonly byte[] _pin = new byte[] {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36
        };

        [Fact(Skip = "This test requires user interation to reset the FIDO2 application.")]
        public void SetLargeBlob_Succeeds()
        {
            IYubiKeyDevice yubiKey = IntegrationTestDeviceEnumeration.GetTestDevices()
                .Where(key => key.SerialNumber.HasValue).First();

            bool isValid = DoReset(yubiKey.SerialNumber);
            Assert.True(isValid);

            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = Fido2ResetForTest.ResetForTestKeyCollectorDelegate;
                isValid = fido2Session.TrySetPin(new ReadOnlyMemory<byte>(_pin));
                Assert.True(isValid);

                var user1 = new UserEntity(new byte[] { 1, 2, 3, 4 })
                {
                    Name = "TestUser1",
                    DisplayName = "Test User"
                };

                var mcParams1 = new MakeCredentialParameters(_rp, user1)
                {
                    ClientDataHash = _clientDataHash
                };
                mcParams1.AddExtension("largeBlobKey", new byte[] { 0xF5 });
                mcParams1.AddOption(AuthenticatorOptions.rk, true);

                isValid = fido2Session.TryVerifyPin(
                    new ReadOnlyMemory<byte>(_pin), null, null, out int? retries, out bool? reboot);
                Assert.True(isValid);

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

                isValid = fido2Session.TryVerifyPin(
                    new ReadOnlyMemory<byte>(_pin), null, null, out retries, out reboot);
                Assert.True(isValid);

                MakeCredentialData mcData2 = fido2Session.MakeCredential(mcParams2);
                Assert.True(mcData2.VerifyAttestation(_clientDataHash));

                var gaParams = new GetAssertionParameters(_rp, _clientDataHash);
                gaParams.AddExtension("largeBlobKey", new byte[] { 0xF5 });

                isValid = fido2Session.TryVerifyPin(
                    new ReadOnlyMemory<byte>(_pin), null, null, out retries, out reboot);
                Assert.True(isValid);

                IList<GetAssertionData> assertions = fido2Session.GetAssertions(gaParams);
                Assert.Equal(2, assertions.Count);

                SerializedLargeBlobArray blobArray = fido2Session.GetSerializedLargeBlobArray();
                Assert.NotNull(blobArray.EncodedArray);

                byte[] blobData1 = new byte[] {
                    0x31,
                    0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50,
                    0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50,
                    0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50
                };
                Assert.NotNull(mcData1.LargeBlobKey);
                ReadOnlyMemory<byte> key1 = ReadOnlyMemory<byte>.Empty;
                if (!(mcData1.LargeBlobKey is null))
                {
                    key1 = mcData1.LargeBlobKey.Value;
                    blobArray.AddEntry(blobData1, key1);
                }
                Assert.Null(blobArray.Digest);

                byte[] blobData2 = new byte[] {
                    0x32,
                    0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f, 0x70,
                    0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f, 0x70,
                    0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f, 0x70
                };
                Assert.NotNull(mcData2.LargeBlobKey);
                ReadOnlyMemory<byte> key2 = ReadOnlyMemory<byte>.Empty;
                if (!(mcData2.LargeBlobKey is null))
                {
                    key2 = mcData2.LargeBlobKey.Value;
                    blobArray.AddEntry(blobData2, key2);
                }

                fido2Session.SetSerializedLargeBlobArray(blobArray);
                Assert.NotNull(blobArray.Digest);

                blobArray = fido2Session.GetSerializedLargeBlobArray();
                Assert.Equal(2, blobArray.Entries.Count);

                bool isDecrypted = blobArray.Entries[0].TryDecrypt(key1, out Memory<byte> plaintext1);
                Assert.True(isDecrypted);
                isValid = MemoryExtensions.SequenceEqual<byte>(plaintext1.Span, blobData1.AsSpan());
                Assert.True(isValid);
                isDecrypted = blobArray.Entries[1].TryDecrypt(key1, out Memory<byte> plaintext2);
                Assert.False(isDecrypted);
                isDecrypted = blobArray.Entries[1].TryDecrypt(key2, out plaintext2);
                Assert.True(isDecrypted);
                isValid = MemoryExtensions.SequenceEqual<byte>(plaintext2.Span, blobData2.AsSpan());
                Assert.True(isValid);
            }
        }

        private bool DoReset(int? serialNum)
        {
            var fido2Reset = new Fido2ResetForTest(serialNum);
            ResponseStatus status = fido2Reset.RunFido2Reset(Fido2ResetForTest.ResetForTestKeyCollectorDelegate);

            return status == ResponseStatus.Success;
        }
    }
}
