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
using Yubico.YubiKey.Piv.Commands;

namespace Yubico.YubiKey.Piv
{
    public class PivDataTagTests
    {
        [Theory]
        [InlineData(PivDataTag.Chuid, true)]
        [InlineData(PivDataTag.Capability, true)]
        [InlineData(PivDataTag.Discovery, false)]
        [InlineData(PivDataTag.Authentication, true)]
        [InlineData(PivDataTag.Signature, true)]
        [InlineData(PivDataTag.KeyManagement, true)]
        [InlineData(PivDataTag.CardAuthentication, true)]
        [InlineData(PivDataTag.Retired1, true)]
        [InlineData(PivDataTag.Retired2, true)]
        [InlineData(PivDataTag.Retired3, true)]
        [InlineData(PivDataTag.Retired4, true)]
        [InlineData(PivDataTag.Retired5, true)]
        [InlineData(PivDataTag.Retired6, true)]
        [InlineData(PivDataTag.Retired7, true)]
        [InlineData(PivDataTag.Retired8, true)]
        [InlineData(PivDataTag.Retired9, true)]
        [InlineData(PivDataTag.Retired10, true)]
        [InlineData(PivDataTag.Retired11, true)]
        [InlineData(PivDataTag.Retired12, true)]
        [InlineData(PivDataTag.Retired13, true)]
        [InlineData(PivDataTag.Retired14, true)]
        [InlineData(PivDataTag.Retired15, true)]
        [InlineData(PivDataTag.Retired16, true)]
        [InlineData(PivDataTag.Retired17, true)]
        [InlineData(PivDataTag.Retired18, true)]
        [InlineData(PivDataTag.Retired19, true)]
        [InlineData(PivDataTag.Retired20, true)]
        [InlineData(PivDataTag.Printed, false)]
        [InlineData(PivDataTag.SecurityObject, true)]
        [InlineData(PivDataTag.IrisImages, true)]
        [InlineData(PivDataTag.FacialImage, true)]
        [InlineData(PivDataTag.Fingerprints, true)]
        [InlineData(PivDataTag.BiometricGroupTemplate, false)]
        [InlineData(PivDataTag.SecureMessageSigner, true)]
        [InlineData(PivDataTag.PairingCodeReferenceData, true)]
        public void IsValidForPut_CorrectResult(PivDataTag tag, bool result)
        {
            bool isValid = tag.IsValidTagForPut();

            Assert.Equal(result, isValid);
        }

        [Theory]
        [InlineData(PivDataTag.Chuid, true)]
        [InlineData(PivDataTag.Chuid, false)]
        [InlineData(PivDataTag.Capability, true)]
        [InlineData(PivDataTag.Capability, false)]
        [InlineData(PivDataTag.Authentication, true)]
        [InlineData(PivDataTag.Authentication, false)]
        [InlineData(PivDataTag.Signature, true)]
        [InlineData(PivDataTag.Signature, false)]
        [InlineData(PivDataTag.KeyManagement, true)]
        [InlineData(PivDataTag.KeyManagement, false)]
        [InlineData(PivDataTag.CardAuthentication, true)]
        [InlineData(PivDataTag.CardAuthentication, false)]
        [InlineData(PivDataTag.Retired1, true)]
        [InlineData(PivDataTag.Retired1, false)]
        [InlineData(PivDataTag.Retired2, true)]
        [InlineData(PivDataTag.Retired2, false)]
        [InlineData(PivDataTag.Retired3, true)]
        [InlineData(PivDataTag.Retired3, false)]
        [InlineData(PivDataTag.Retired4, true)]
        [InlineData(PivDataTag.Retired4, false)]
        [InlineData(PivDataTag.Retired5, true)]
        [InlineData(PivDataTag.Retired5, false)]
        [InlineData(PivDataTag.Retired6, true)]
        [InlineData(PivDataTag.Retired6, false)]
        [InlineData(PivDataTag.Retired7, true)]
        [InlineData(PivDataTag.Retired7, false)]
        [InlineData(PivDataTag.Retired8, true)]
        [InlineData(PivDataTag.Retired8, false)]
        [InlineData(PivDataTag.Retired9, true)]
        [InlineData(PivDataTag.Retired9, false)]
        [InlineData(PivDataTag.Retired10, true)]
        [InlineData(PivDataTag.Retired10, false)]
        [InlineData(PivDataTag.Retired11, true)]
        [InlineData(PivDataTag.Retired11, false)]
        [InlineData(PivDataTag.Retired12, true)]
        [InlineData(PivDataTag.Retired12, false)]
        [InlineData(PivDataTag.Retired13, true)]
        [InlineData(PivDataTag.Retired13, false)]
        [InlineData(PivDataTag.Retired14, true)]
        [InlineData(PivDataTag.Retired14, false)]
        [InlineData(PivDataTag.Retired15, true)]
        [InlineData(PivDataTag.Retired15, false)]
        [InlineData(PivDataTag.Retired16, true)]
        [InlineData(PivDataTag.Retired16, false)]
        [InlineData(PivDataTag.Retired17, true)]
        [InlineData(PivDataTag.Retired17, false)]
        [InlineData(PivDataTag.Retired18, true)]
        [InlineData(PivDataTag.Retired18, false)]
        [InlineData(PivDataTag.Retired19, true)]
        [InlineData(PivDataTag.Retired19, false)]
        [InlineData(PivDataTag.Retired20, true)]
        [InlineData(PivDataTag.Retired20, false)]
        [InlineData(PivDataTag.SecurityObject, true)]
        [InlineData(PivDataTag.SecurityObject, false)]
        [InlineData(PivDataTag.IrisImages, true)]
        [InlineData(PivDataTag.IrisImages, false)]
        [InlineData(PivDataTag.FacialImage, true)]
        [InlineData(PivDataTag.FacialImage, false)]
        [InlineData(PivDataTag.Fingerprints, true)]
        [InlineData(PivDataTag.Fingerprints, false)]
        [InlineData(PivDataTag.BiometricGroupTemplate, false)]
        [InlineData(PivDataTag.SecureMessageSigner, true)]
        [InlineData(PivDataTag.SecureMessageSigner, false)]
        [InlineData(PivDataTag.PairingCodeReferenceData, true)]
        [InlineData(PivDataTag.PairingCodeReferenceData, false)]
        public void IsValidEncoding_CorrectResult(PivDataTag tag, bool result)
        {
            byte[] encoding = PivCommandResponseTestData.PutDataEncoding(tag, result);

            bool isValid = tag.IsValidEncodingForPut(encoding);

            Assert.Equal(result, isValid);
        }
    }
}
