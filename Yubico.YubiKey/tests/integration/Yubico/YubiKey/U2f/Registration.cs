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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.U2f;

public class Registration
{
    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void RegisterCredential_BasicTest(
        StandardTestDevice testDeviceType)
    {
        var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

        using (var u2fSession = new U2fSession(testDevice))
        {
            u2fSession.KeyCollector = k => k.Request switch
            {
                KeyEntryRequest.TouchRequest => true,
                _ => throw new NotSupportedException("Test requested a key that is not supported by this test case.")
            };

            var applicationId = U2fSession.EncodeAndHashString("https://fido.example.com/app");
            // This is not a well-formed challenge. That's OK - we're not really trying to log in here.
            var clientDataHash = U2fSession.EncodeAndHashString("FakeChallenge");

            var registrationData = u2fSession.Register(applicationId, clientDataHash, new TimeSpan(0, 0, 5));

            Assert.True(registrationData.VerifySignature(applicationId, clientDataHash));
        }
    }
}
