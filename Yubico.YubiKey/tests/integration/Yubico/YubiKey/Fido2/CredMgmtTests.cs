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
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2;

[Trait(TraitTypes.Category, TestCategories.RequiresBio)]
[Trait(TraitTypes.Category, TestCategories.Elevated)]
public class CredMgmtTests : IClassFixture<BioFido2Fixture>
{
    private readonly BioFido2Fixture _bioFido2Fixture; // This test should be able to run without a bio key

    public CredMgmtTests(
        BioFido2Fixture bioFido2Fixture)
    {
        _bioFido2Fixture = bioFido2Fixture;
        if (_bioFido2Fixture.HasCredentials)
        {
            return;
        }

        _bioFido2Fixture.AddCredentials(2, 1);
        _bioFido2Fixture.AddCredentials(1, 0);
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void GetMetadata_Succeeds()
    {
        using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
        {
            fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
            fido2Session.AddPermissions(
                PinUvAuthTokenPermissions.MakeCredential, _bioFido2Fixture.RpInfoList[0].RelyingParty.Id);

            var (credCount, slotCount) = fido2Session.GetCredentialMetadata();
            Assert.Equal(3, credCount);
            Assert.Equal(22, slotCount);
        }
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void EnumerateRps_Succeeds()
    {
        using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
        {
            fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
            fido2Session.AddPermissions(PinUvAuthTokenPermissions.CredentialManagement, "rp-3");

            var rpList = fido2Session.EnumerateRelyingParties();
            Assert.Equal(2, rpList.Count);

            var rpInfo = _bioFido2Fixture.MatchRelyingParty(rpList[0]);
            var isValid = rpInfo.RelyingPartyIdHash.Span.SequenceEqual(rpList[0].RelyingPartyIdHash.Span);
            Assert.True(isValid);
        }
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void EnumerateCreds_Succeeds()
    {
        using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
        {
            fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
            fido2Session.AddPermissions(PinUvAuthTokenPermissions.MakeCredential, "rp-3");

            var rpInfo = _bioFido2Fixture.RpInfoList[0];
            var ykCredList =
                fido2Session.EnumerateCredentialsForRelyingParty(rpInfo.RelyingParty);
            Assert.Equal(rpInfo.DiscoverableCount, ykCredList.Count);

            var ykUser = ykCredList[0].User;

            var userInfo = _bioFido2Fixture.MatchUser(rpInfo.RelyingParty, ykUser);
            var targetKey = userInfo.Item2.LargeBlobKey
                            ?? throw new InvalidOperationException("No matching User.");
            var ykLargeBlobKey = ykCredList[0].LargeBlobKey
                                 ?? throw new InvalidOperationException("No matching User.");

            var isValid = targetKey.Span.SequenceEqual(ykLargeBlobKey.Span);
            Assert.True(isValid);
        }
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void DeleteCred_Succeeds()
    {
        _bioFido2Fixture.AddCredentials(1, 0);
        using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
        {
            fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
            fido2Session.AddPermissions(PinUvAuthTokenPermissions.AuthenticatorConfiguration);

            var credList =
                fido2Session.EnumerateCredentialsForRelyingParty(_bioFido2Fixture.RpInfoList[2].RelyingParty);
            var count = credList.Count;
            Assert.Equal(1, count);

            fido2Session.ClearAuthToken();
            fido2Session.AddPermissions(PinUvAuthTokenPermissions.AuthenticatorConfiguration);

            fido2Session.DeleteCredential(credList[0].CredentialId);

            credList = fido2Session.EnumerateCredentialsForRelyingParty(_bioFido2Fixture.RpInfoList[2].RelyingParty);
            Assert.NotNull(credList);
            Assert.True(credList.Count == count - 1);
        }
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void UpdateUserInfo_Succeeds()
    {
        var updatedDisplayName = "Updated Display Name";

        using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
        {
            fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
            fido2Session.AddPermissions(PinUvAuthTokenPermissions.AuthenticatorConfiguration);

            var credList =
                fido2Session.EnumerateCredentialsForRelyingParty(_bioFido2Fixture.RpInfoList[0].RelyingParty);
            Assert.NotEmpty(credList);

            fido2Session.ClearAuthToken();
            fido2Session.AddPermissions(PinUvAuthTokenPermissions.AuthenticatorConfiguration);

            var newInfo = credList[0].User;
            newInfo.DisplayName = updatedDisplayName;

            fido2Session.UpdateUserInfoForCredential(credList[0].CredentialId, newInfo);

            credList = fido2Session.EnumerateCredentialsForRelyingParty(_bioFido2Fixture.RpInfoList[0].RelyingParty);

            var displayName = credList[0].User.DisplayName ?? "";
            Assert.Equal(updatedDisplayName, displayName);
        }
    }
}
