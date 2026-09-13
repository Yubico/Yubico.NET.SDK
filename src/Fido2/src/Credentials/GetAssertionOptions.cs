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

namespace Yubico.YubiKit.Fido2.Credentials;

/// <summary>
/// Options for the GetAssertion operation.
/// </summary>
/// <remarks>
/// <para>
/// These options correspond to CTAP2 authenticatorGetAssertion parameters:
/// - allowList (0x03): List of allowed credentials
/// - extensions (0x04): Extension inputs
/// - options (0x05): Boolean options (up, uv)
/// - pinUvAuthParam (0x06): PIN/UV auth parameter
/// - pinUvAuthProtocol (0x07): PIN/UV protocol version
/// </para>
/// </remarks>
public sealed class GetAssertionOptions
{
    /// <summary>
    /// Gets or sets the list of allowed credentials.
    /// </summary>
    /// <remarks>
    /// If provided, only credentials matching these descriptors will be used.
    /// If empty or null, discoverable credentials for the RP will be searched.
    /// </remarks>
    public IReadOnlyList<PublicKeyCredentialDescriptor>? AllowList { get; set; }

    /// <summary>
    /// Gets or sets whether user presence is required.
    /// </summary>
    /// <remarks>
    /// Defaults to true. Setting to false allows silent assertions.
    /// </remarks>
    public bool? UserPresence { get; set; }

    /// <summary>
    /// Gets or sets whether user verification is required.
    /// </summary>
    public bool? UserVerification { get; set; }

    /// <summary>
    /// Gets or sets the PIN/UV auth parameter for authorization.
    /// </summary>
    public ReadOnlyMemory<byte>? PinUvAuthParam { get; set; }

    /// <summary>
    /// Gets or sets the PIN/UV auth protocol version.
    /// </summary>
    public int? PinUvAuthProtocol { get; set; }

    /// <summary>
    /// Gets or sets extension inputs as a CBOR-encoded map.
    /// </summary>
    public ReadOnlyMemory<byte>? Extensions { get; set; }

    /// <summary>
    /// Creates default options.
    /// </summary>
    public GetAssertionOptions()
    {
    }

    /// <summary>
    /// Sets the user verification option.
    /// </summary>
    /// <param name="value">Whether to require user verification.</param>
    /// <returns>This options instance for chaining.</returns>
    public GetAssertionOptions WithUserVerification(bool value)
    {
        UserVerification = value;
        return this;
    }

    /// <summary>
    /// Sets the PIN/UV authentication parameters.
    /// </summary>
    /// <param name="param">The PIN/UV auth parameter.</param>
    /// <param name="protocol">The protocol version.</param>
    /// <returns>This options instance for chaining.</returns>
    public GetAssertionOptions WithPinUvAuth(ReadOnlyMemory<byte> param, int protocol)
    {
        PinUvAuthParam = param;
        PinUvAuthProtocol = protocol;
        return this;
    }

    /// <summary>
    /// Sets the allow list.
    /// </summary>
    /// <param name="allowList">The allowed credentials.</param>
    /// <returns>This options instance for chaining.</returns>
    public GetAssertionOptions WithAllowList(IReadOnlyList<PublicKeyCredentialDescriptor> allowList)
    {
        AllowList = allowList;
        return this;
    }

    /// <summary>
    /// Sets the allow list from credential IDs.
    /// </summary>
    /// <param name="credentialIds">The allowed credential IDs.</param>
    /// <returns>This options instance for chaining.</returns>
    public GetAssertionOptions WithAllowList(params ReadOnlyMemory<byte>[] credentialIds)
    {
        AllowList = credentialIds
            .Select(id => PublicKeyCredentialDescriptor.FromCredentialId(id))
            .ToList();
        return this;
    }
}