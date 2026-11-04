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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Ctap2.BioEnrollment;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Device sensor-info checks for a fingerprint-capable authenticator.
/// </summary>
/// <remarks>
/// The sole test asserts a positive decoded sample hint and Touch/Swipe kind; it never
/// enrolls, captures, cancels, renames or removes a fingerprint. Default known-PIN
/// normalization runs before callback capability checks and may set/change PIN state or
/// consume retries. This remains human-approved PIN work, not read-only setup. Sensor
/// metadata alone does not qualify biometric interoperability or fix the helper's standard
/// request/schema limitations. The explicitly acquired bio token is cleared in finally.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Feature", "BioEnrollment")]
public class Ctap2BioEnrollmentTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task GetFingerprintSensorInfo_ReturnsSensorCapabilities(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            // Option presence indicates support; false means no templates provisioned, not unsupported.
            var hasBioEnroll = info.Options.TryGetValue("bioEnroll", out var bioEnrollValue);
            var hasPrototypeBioEnroll = info.Options.TryGetValue("userVerificationMgmtPreview", out var previewValue);

            if (!hasBioEnroll && !hasPrototypeBioEnroll)
            {
                Skip.If(true, "YubiKey does not support bio enrollment (no bioEnroll or userVerificationMgmtPreview option)");
                return;
            }

            var usePreview = !hasBioEnroll && hasPrototypeBioEnroll;

            using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] bioToken;
            if (supportsPermissions)
            {
                bioToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    Ctap2TestData.PinUtf8,
                    PinUvAuthTokenPermissions.BioEnrollment);
            }
            else
            {
                bioToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
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