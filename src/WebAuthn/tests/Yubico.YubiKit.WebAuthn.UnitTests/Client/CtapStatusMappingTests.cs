// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

public class CtapStatusMappingTests
{
    [Theory]
    // NotAllowed group
    [InlineData(CtapStatus.PinAuthInvalid, WebAuthnClientErrorCode.NotAllowed)]
    [InlineData(CtapStatus.PinInvalid, WebAuthnClientErrorCode.NotAllowed)]
    [InlineData(CtapStatus.PinAuthBlocked, WebAuthnClientErrorCode.NotAllowed)]
    [InlineData(CtapStatus.PinBlocked, WebAuthnClientErrorCode.NotAllowed)]
    [InlineData(CtapStatus.PinPolicyViolation, WebAuthnClientErrorCode.NotAllowed)]
    [InlineData(CtapStatus.PuatRequired, WebAuthnClientErrorCode.NotAllowed)]
    [InlineData(CtapStatus.PinTokenExpired, WebAuthnClientErrorCode.NotAllowed)]
    [InlineData(CtapStatus.NotAllowed, WebAuthnClientErrorCode.NotAllowed)]
    [InlineData(CtapStatus.OperationDenied, WebAuthnClientErrorCode.NotAllowed)]
    // Constraint group
    [InlineData(CtapStatus.KeyStoreFull, WebAuthnClientErrorCode.Constraint)]
    [InlineData(CtapStatus.LargeBlobStorageFull, WebAuthnClientErrorCode.Constraint)]
    [InlineData(CtapStatus.FpDatabaseFull, WebAuthnClientErrorCode.Constraint)]
    [InlineData(CtapStatus.LimitExceeded, WebAuthnClientErrorCode.Constraint)]
    [InlineData(CtapStatus.RequestTooLarge, WebAuthnClientErrorCode.Constraint)]
    [InlineData(CtapStatus.UserActionTimeout, WebAuthnClientErrorCode.Constraint)]
    [InlineData(CtapStatus.ActionTimeout, WebAuthnClientErrorCode.Constraint)]
    [InlineData(CtapStatus.Timeout, WebAuthnClientErrorCode.Constraint)]
    // NotSupported group
    [InlineData(CtapStatus.UnsupportedAlgorithm, WebAuthnClientErrorCode.NotSupported)]
    [InlineData(CtapStatus.UnsupportedOption, WebAuthnClientErrorCode.NotSupported)]
    [InlineData(CtapStatus.InvalidOption, WebAuthnClientErrorCode.NotSupported)]
    // Security group
    [InlineData(CtapStatus.PinNotSet, WebAuthnClientErrorCode.Security)]
    [InlineData(CtapStatus.UpRequired, WebAuthnClientErrorCode.Security)]
    // InvalidState group
    [InlineData(CtapStatus.NoCredentials, WebAuthnClientErrorCode.InvalidState)]
    [InlineData(CtapStatus.InvalidCredential, WebAuthnClientErrorCode.InvalidState)]
    // Default group (not a member of any arm above)
    [InlineData(CtapStatus.Other, WebAuthnClientErrorCode.Unknown)]
    public void MapCtapStatusToWebAuthnError_MapsStatusToExpectedCode(
        CtapStatus status,
        WebAuthnClientErrorCode expectedCode)
    {
        var ctapException = new CtapException(status, "ctap failure message");

        var result = WebAuthnClient.MapCtapStatusToWebAuthnError(ctapException);

        Assert.Equal(expectedCode, result.Code);
    }

    [Fact]
    public void MapCtapStatusToWebAuthnError_PropagatesMessageAndInnerException()
    {
        var ctapException = new CtapException(CtapStatus.PinInvalid, "specific ctap message");

        var result = WebAuthnClient.MapCtapStatusToWebAuthnError(ctapException);

        Assert.Equal("specific ctap message", result.Message);
        Assert.Same(ctapException, result.InnerException);
    }
}