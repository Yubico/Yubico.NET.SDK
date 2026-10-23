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

using NSubstitute;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Extensions.Processing;

namespace Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

internal static class TestExtensionContext
{
    /// <summary>A context whose authenticator advertises every extension the processors know.</summary>
    public static ExtensionContext SupportingAll(IWebAuthnBackend? backend = null) =>
        For(MockFido2Responses.CreateMockAuthenticatorInfo(extensions: MockFido2Responses.AllKnownExtensions, largeBlobsSupported: true), backend);

    public static ExtensionContext For(AuthenticatorInfo info, IWebAuthnBackend? backend = null) =>
        new(info, new TestPinUvAuthProtocol(), backend ?? Substitute.For<IWebAuthnBackend>());
}