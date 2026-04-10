// Copyright 2026 Yubico AB
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

using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.BioEnrollment;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for FIDO2 biometric (fingerprint) enrollment.
/// Requires a YubiKey Bio series device with fingerprint sensor.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "BioEnrollment")]
public class FidoBioEnrollmentTests
{
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetFingerprintSensorInfo_ReturnsSensorCapabilities(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            // Check for bio enrollment support
            var hasBioEnroll = info.Options.TryGetValue("bioEnroll", out var bioEnrollValue);
            var hasPrototypeBioEnroll = info.Options.TryGetValue("userVerificationMgmtPreview", out var previewValue);

            if (!hasBioEnroll && !hasPrototypeBioEnroll)
            {
                Skip.If(true, "YubiKey does not support bio enrollment (no bioEnroll or userVerificationMgmtPreview option)");
                return;
            }

            var usePreview = !hasBioEnroll && hasPrototypeBioEnroll;

            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] bioToken;
            if (supportsPermissions)
            {
                bioToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.PinUtf8,
                    PinUvAuthTokenPermissions.BioEnrollment);
            }
            else
            {
                bioToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
            }

            try
            {
                var bioEnrollment = new FingerprintBioEnrollment(
                    session, clientPin.Protocol, bioToken, usePreview);

                var sensorInfo = await bioEnrollment.GetFingerprintSensorInfoAsync();

                Assert.NotNull(sensorInfo);
                Assert.True(sensorInfo.MaxCaptureSamplesRequiredForEnroll > 0,
                    "Sensor should require at least 1 capture sample for enrollment");
                Assert.True(
                    sensorInfo.FingerprintKind is FingerprintKind.Touch or FingerprintKind.Swipe,
                    "Fingerprint kind should be Touch or Swipe");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(bioToken);
            }
        });
}
