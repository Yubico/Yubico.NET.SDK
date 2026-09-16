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

using System.Buffers;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

/// <summary>
/// <see cref="WebAuthnClientOptions"/> carries the client's ceremony-independent configuration.
/// A null public suffix checker or bad prompt-attempt limit is rejected when the options are built,
/// not part-way through a ceremony that has already touched the authenticator.
/// </summary>
public class WebAuthnClientOptionsTests
{
    private sealed class NullPrompt : ICredentialPrompt
    {
        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(
            CredentialPromptContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IMemoryOwner<byte>?>(null);
    }

    [Fact]
    public void MaxPromptAttempts_DefaultsToThree() =>
        Assert.Equal(3, new WebAuthnClientOptions { PublicSuffixChecker = _ => false }.MaxPromptAttempts);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void MaxPromptAttempts_NonPositive_RejectedAtConstruction(int attempts) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new WebAuthnClientOptions { PublicSuffixChecker = _ => false, MaxPromptAttempts = attempts });

    [Fact]
    public void PublicSuffixChecker_Null_RejectedAtConstruction() =>
        Assert.Throws<ArgumentNullException>(
            () => new WebAuthnClientOptions { PublicSuffixChecker = null! });

    [Fact]
    public void CredentialPrompt_DefaultsToNull_AndCanCarryAPrompt()
    {
        Assert.Null(new WebAuthnClientOptions { PublicSuffixChecker = _ => false }.CredentialPrompt);

        var prompt = new NullPrompt();
        Assert.Same(
            prompt,
            new WebAuthnClientOptions { PublicSuffixChecker = _ => false, CredentialPrompt = prompt }.CredentialPrompt);
    }

    [Fact]
    public void EnterpriseRpIds_DefaultsToEmpty_AndCanCarryIds()
    {
        Assert.Empty(new WebAuthnClientOptions { PublicSuffixChecker = _ => false }.EnterpriseRpIds);

        var options = new WebAuthnClientOptions
        {
            PublicSuffixChecker = _ => false,
            EnterpriseRpIds = new HashSet<string> { "partner.test" }
        };
        Assert.Contains("partner.test", options.EnterpriseRpIds);
    }
}