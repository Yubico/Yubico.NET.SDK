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

using System.Reflection;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

/// <summary>
/// Pins the shape of the public WebAuthn surface. The backend is a test seam, not an extension
/// point, so it must not be reachable from outside the assembly; these assertions fail if it
/// is re-exported by accident.
/// </summary>
public class WebAuthnSurfaceTests
{
    private static readonly Assembly WebAuthnAssembly = typeof(WebAuthnClient).Assembly;

    private static Type RequireType(string fullName) =>
        WebAuthnAssembly.GetType(fullName, throwOnError: false)
        ?? throw new InvalidOperationException($"Expected type {fullName} to exist.");

    [Fact]
    public void WebAuthnClient_ExposesExactlyOnePublicConstructor_TakingAFidoSession()
    {
        var constructor = Assert.Single(
            typeof(WebAuthnClient).GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        Assert.Equal(typeof(IFidoSession), constructor.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void WebAuthnClient_BackendConstructor_IsNotPublic()
    {
        var backendConstructor = Assert.Single(
            typeof(WebAuthnClient)
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(c => c.GetParameters()[0].ParameterType.Name == "IWebAuthnBackend"));

        Assert.False(backendConstructor.IsPublic);
    }

    [Theory]
    [InlineData("Yubico.YubiKit.WebAuthn.Client.IWebAuthnBackend")]
    [InlineData("Yubico.YubiKit.WebAuthn.Client.WebAuthnBackend")]
    [InlineData("Yubico.YubiKit.WebAuthn.Client.BackendMakeCredentialRequest")]
    [InlineData("Yubico.YubiKit.WebAuthn.Client.BackendGetAssertionRequest")]
    [InlineData("Yubico.YubiKit.WebAuthn.Client.PinUvAuthMethod")]
    [InlineData("Yubico.YubiKit.WebAuthn.Client.PinUvAuthTokenSession")]
    public void BackendContractTypes_AreNotPublic(string fullName) =>
        Assert.False(RequireType(fullName).IsPublic);

    [Fact]
    public void ConcreteBackend_IsNamedWebAuthnBackend_NotFidoSessionWebAuthnBackend() =>
        Assert.Null(WebAuthnAssembly.GetType(
            "Yubico.YubiKit.WebAuthn.Client.FidoSessionWebAuthnBackend", throwOnError: false));

    [Theory]
    [InlineData("MakeCredentialStreamAsync")]
    [InlineData("GetAssertionStreamAsync")]
    public void StatusStreamMethods_DoNotExist(string methodName) =>
        Assert.Empty(typeof(WebAuthnClient)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static)
            .Where(m => m.Name == methodName));

    [Fact]
    public void StatusTypes_AndTheirNamespace_DoNotExist()
    {
        var survivors = WebAuthnAssembly
            .GetTypes()
            .Where(t =>
                t.Namespace?.StartsWith("Yubico.YubiKit.WebAuthn.Client.Status", StringComparison.Ordinal) == true ||
                t.Name.StartsWith("WebAuthnStatus", StringComparison.Ordinal) ||
                t.Name.StartsWith("StatusChannel", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(survivors);
    }

    /// <summary>
    /// The client-creation factory has to be able to carry the client's configuration; a caller
    /// that starts from a <see cref="IYubiKeyExtensions"/> must not be forced onto defaults.
    /// </summary>
    [Fact]
    public void CreateWebAuthnClientAsync_TakesWebAuthnClientOptions_AndHasNoOverloadWithout()
    {
        var overloads = typeof(IYubiKeyExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "CreateWebAuthnClientAsync")
            .ToList();

        Assert.NotEmpty(overloads);
        Assert.All(overloads, m => Assert.Contains(
            m.GetParameters(), p => p.ParameterType == typeof(WebAuthnClientOptions)));
    }
}
