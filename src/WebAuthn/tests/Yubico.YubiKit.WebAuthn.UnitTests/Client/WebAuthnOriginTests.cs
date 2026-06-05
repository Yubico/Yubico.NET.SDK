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

using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

public class WebAuthnOriginTests
{
    [Theory]
    [InlineData("https://example.com", "https", "example.com", -1, "https://example.com")]
    [InlineData("https://example.com:443", "https", "example.com", -1, "https://example.com")]
    [InlineData("https://example.com:8443", "https", "example.com", 8443, "https://example.com:8443")]
    [InlineData("http://localhost", "http", "localhost", -1, "http://localhost")]
    [InlineData("http://localhost:3000", "http", "localhost", 3000, "http://localhost:3000")]
    [InlineData("https://sub.example.com/path?query=val#frag", "https", "sub.example.com", -1, "https://sub.example.com")]
    public void TryParse_ValidSecureOrigins_Success(
        string url,
        string expectedScheme,
        string expectedHost,
        int expectedPort,
        string expectedStringValue)
    {
        // Act
        var success = WebAuthnOrigin.TryParse(url, out var origin);

        // Assert
        Assert.True(success);
        Assert.NotNull(origin);
        Assert.Equal(expectedScheme, origin.Scheme);
        Assert.Equal(expectedHost, origin.Host);
        Assert.Equal(expectedPort, origin.Port);
        Assert.Equal(expectedStringValue, origin.StringValue);
    }

    [Theory]
    [InlineData("data:text/html,<h1>Test</h1>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///Users/test/file.txt")]
    [InlineData("http://example.com")] // http without localhost
    [InlineData("ftp://example.com")]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("//example.com")] // scheme-relative URL
    public void TryParse_InvalidOrInsecureOrigins_Failure(string url)
    {
        // Act
        var success = WebAuthnOrigin.TryParse(url, out var origin);

        // Assert
        Assert.False(success);
        Assert.Null(origin);
    }

    [Fact]
    public void IsRpIdValid_ExactMatch_ReturnsTrue()
    {
        // Arrange
        var origin = WebAuthnOrigin.TryParse("https://example.com", out var o) ? o : throw new InvalidOperationException();
        static bool IsPublicSuffix(string domain) => domain is "com" or "co.uk";

        // Act
        var isValid = origin.IsRpIdValid("example.com", IsPublicSuffix);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsRpIdValid_SubdomainToRegistrableSuffix_ReturnsTrue()
    {
        // Arrange
        var origin = WebAuthnOrigin.TryParse("https://login.example.com", out var o) ? o : throw new InvalidOperationException();
        static bool IsPublicSuffix(string domain) => domain is "com";

        // Act - rpId "example.com" is the registrable suffix of "login.example.com"
        var isValid = origin.IsRpIdValid("example.com", IsPublicSuffix);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsRpIdValid_PublicSuffixAsRpId_ReturnsFalse()
    {
        // Arrange
        var origin = WebAuthnOrigin.TryParse("https://example.com", out var o) ? o : throw new InvalidOperationException();
        static bool IsPublicSuffix(string domain) => domain is "com";

        // Act - rpId cannot be a public suffix
        var isValid = origin.IsRpIdValid("com", IsPublicSuffix);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsRpIdValid_CrossOriginMismatch_ReturnsFalse()
    {
        // Arrange
        var origin = WebAuthnOrigin.TryParse("https://example.com", out var o) ? o : throw new InvalidOperationException();
        static bool IsPublicSuffix(string domain) => domain is "com" or "org";

        // Act - "other.org" is not a suffix of "example.com"
        var isValid = origin.IsRpIdValid("other.org", IsPublicSuffix);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsRpIdValid_EnterpriseAllowList_BypassesSuffixCheck()
    {
        // Arrange
        var origin = WebAuthnOrigin.TryParse("https://example.com", out var o) ? o : throw new InvalidOperationException();
        static bool IsPublicSuffix(string domain) => domain is "com";
        var enterpriseRpIds = new HashSet<string> { "internal.corp" };

        // Act - "internal.corp" is not a suffix of "example.com", but is in enterprise list
        var isValid = origin.IsRpIdValid("internal.corp", IsPublicSuffix, enterpriseRpIds);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Equals_SameOrigins_ReturnsTrue()
    {
        // Arrange
        var origin1 = WebAuthnOrigin.TryParse("https://example.com:8443", out var o1) ? o1 : throw new InvalidOperationException();
        var origin2 = WebAuthnOrigin.TryParse("https://example.com:8443", out var o2) ? o2 : throw new InvalidOperationException();

        // Assert
        Assert.Equal(origin1, origin2);
        Assert.True(origin1 == origin2);
        Assert.False(origin1 != origin2);
        Assert.Equal(origin1.GetHashCode(), origin2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentPorts_ReturnsFalse()
    {
        // Arrange
        var origin1 = WebAuthnOrigin.TryParse("https://example.com:8443", out var o1) ? o1 : throw new InvalidOperationException();
        var origin2 = WebAuthnOrigin.TryParse("https://example.com:9443", out var o2) ? o2 : throw new InvalidOperationException();

        // Assert
        Assert.NotEqual(origin1, origin2);
        Assert.False(origin1 == origin2);
        Assert.True(origin1 != origin2);
    }
}
