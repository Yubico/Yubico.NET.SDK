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
using Xunit;

namespace Yubico.YubiKey
{
    public class YubiKeyApplicationExtensionsTests
    {
        [Theory]
        [InlineData(YubiKeyApplication.Management, new byte[] { 0xa0, 0x00, 0x00, 0x05, 0x27, 0x47, 0x11, 0x17 })]
        [InlineData(YubiKeyApplication.Otp, new byte[] { 0xa0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01, 0x01 })]
        [InlineData(YubiKeyApplication.FidoU2f, new byte[] { 0xa0, 0x00, 0x00, 0x06, 0x47, 0x2f, 0x00, 0x01 })]
        [InlineData(YubiKeyApplication.Piv, new byte[] { 0xa0, 0x00, 0x00, 0x03, 0x08 })]
        public void GetIso7816ApplicationId_ReturnsCorrectId(YubiKeyApplication application, byte[] expectedId)
        {
            byte[] result = application.GetIso7816ApplicationId();
            Assert.Equal(expectedId, result);
        }

        [Fact]
        public void GetIso7816ApplicationId_ThrowsForUnsupportedApplication()
        {
            var unsupportedApp = (YubiKeyApplication)999;
            Assert.Throws<NotSupportedException>(() => unsupportedApp.GetIso7816ApplicationId());
        }

        [Fact]
        public void ApplicationIds_ContainsAllApplications()
        {
            var result = YubiKeyApplicationExtensions.Iso7816ApplicationIds;
            Assert.Equal(11, result.Count);
            Assert.Contains(YubiKeyApplication.Management, result.Keys);
            Assert.Contains(YubiKeyApplication.Otp, result.Keys);
            Assert.Contains(YubiKeyApplication.FidoU2f, result.Keys);
            // Add assertions for other applications
        }

        [Theory]
        [InlineData(new byte[] { 0xa0, 0x00, 0x00, 0x05, 0x27, 0x47, 0x11, 0x17 }, YubiKeyApplication.Management)]
        [InlineData(new byte[] { 0xa0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01, 0x01 }, YubiKeyApplication.Otp)]
        [InlineData(new byte[] { 0xa0, 0x00, 0x00, 0x06, 0x47, 0x2f, 0x00, 0x01 }, YubiKeyApplication.FidoU2f)]
        public void GetById_ReturnsCorrectApplication(byte[] applicationId, YubiKeyApplication expectedApplication)
        {
            var result = YubiKeyApplicationExtensions.GetYubiKeyApplication(applicationId);
            Assert.Equal(expectedApplication, result);
        }

        [Fact]
        public void GetById_ThrowsForUnknownApplicationId()
        {
            byte[] unknownId = new byte[] { 0x00, 0x11, 0x22, 0x33 };
            Assert.Throws<ArgumentException>(() => YubiKeyApplicationExtensions.GetYubiKeyApplication(unknownId));
        }
    }
}
