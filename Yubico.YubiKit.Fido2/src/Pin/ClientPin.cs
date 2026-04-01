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

using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Fido2.Cbor;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2.Pin;

/// <summary>
/// Provides PIN and UV token management operations for FIDO2 authenticators.
/// </summary>
/// <remarks>
/// <para>
/// ClientPin handles the CTAP2 authenticatorClientPin command, which manages:
/// <list type="bullet">
///   <item><description>Setting and changing PINs</description></item>
///   <item><description>Getting PIN and UV retry counts</description></item>
///   <item><description>Obtaining PIN/UV auth tokens for credential operations</description></item>
/// </list>
/// </para>
/// <para>
/// The ClientPin uses PIN/UV auth protocols (V1 or V2) for secure key agreement
/// and encrypted PIN operations.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#authenticatorClientPIN
/// </para>
/// </remarks>
public sealed class ClientPin : IDisposable
{
    private const int PinMinLength = 4;
    private const int PinMaxLength = 63;
    private const int PinBlockSize = 64;
    
    private readonly IFidoSession _session;
    private readonly IPinUvAuthProtocol _protocol;
    private bool _disposed;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientPin"/> class.
    /// </summary>
    /// <param name="session">The FIDO session to use for commands.</param>
    /// <param name="protocol">The PIN/UV auth protocol to use.</param>
    /// <exception cref="ArgumentNullException">If session or protocol is null.</exception>
    public ClientPin(IFidoSession session, IPinUvAuthProtocol protocol)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(protocol);
        
        _session = session;
        _protocol = protocol;
    }
    
    /// <summary>
    /// Gets the PIN/UV auth protocol used by this instance.
    /// </summary>
    public IPinUvAuthProtocol Protocol => _protocol;
    
    /// <summary>
    /// Gets the number of PIN retries remaining before the authenticator locks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><description>PinRetries: Number of PIN attempts remaining.</description></item>
    ///   <item><description>PowerCycleRequired: If true, device requires power cycle before next PIN attempt.</description></item>
    /// </list>
    /// </returns>
    public async Task<(int PinRetries, bool PowerCycleRequired)> GetPinRetriesAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        
        var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
            .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
            .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.GetRetries)
            .Build();
        
        var response = await _session.SendCborRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        
        return ParsePinRetriesResponse(response);
    }
    
    /// <summary>
    /// Gets the number of UV retries remaining (CTAP 2.1).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><description>UvRetries: Number of UV attempts remaining.</description></item>
    ///   <item><description>PowerCycleRequired: If true, device requires power cycle before next UV attempt.</description></item>
    /// </list>
    /// </returns>
    public async Task<(int UvRetries, bool PowerCycleRequired)> GetUvRetriesAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        
        var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
            .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
            .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.GetUvRetries)
            .Build();
        
        var response = await _session.SendCborRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        
        return ParseUvRetriesResponse(response);
    }
    
    /// <summary>
    /// Sets a new PIN on the authenticator (first-time setup).
    /// </summary>
    /// <param name="newPin">The PIN to set (4-63 characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">If the PIN length is invalid.</exception>
    /// <exception cref="CtapException">If the authenticator already has a PIN set.</exception>
    public async Task SetPinAsync(string newPin, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ValidatePin(newPin);
        
        // Get authenticator's key agreement key
        var authenticatorKey = await GetKeyAgreementAsync(cancellationToken)
            .ConfigureAwait(false);
        
        // Perform ECDH key agreement
        var (platformKey, sharedSecret) = _protocol.Encapsulate(authenticatorKey);
        
        try
        {
            // Pad and encrypt PIN
            var pinBytes = PadPin(newPin);
            var newPinEnc = _protocol.Encrypt(sharedSecret, pinBytes);
            
            // Compute pinUvAuthParam = authenticate(sharedSecret, newPinEnc)
            var pinUvAuthParam = _protocol.Authenticate(sharedSecret, newPinEnc);
            
            var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
                .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
                .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.SetPin)
                .WithMap(ClientPinParam.KeyAgreement, writer => WriteCoseKey(writer, platformKey))
                .WithBytes(ClientPinParam.NewPinEnc, newPinEnc)
                .WithBytes(ClientPinParam.PinUvAuthParam, pinUvAuthParam)
                .Build();
            
            await _session.SendCborRequestAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
        }
    }
    
    /// <summary>
    /// Changes the existing PIN on the authenticator.
    /// </summary>
    /// <param name="currentPin">The current PIN.</param>
    /// <param name="newPin">The new PIN to set (4-63 characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">If either PIN length is invalid.</exception>
    /// <exception cref="CtapException">If the current PIN is incorrect.</exception>
    public async Task ChangePinAsync(
        string currentPin,
        string newPin,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ValidatePin(currentPin);
        ValidatePin(newPin);
        
        // Get authenticator's key agreement key
        var authenticatorKey = await GetKeyAgreementAsync(cancellationToken)
            .ConfigureAwait(false);
        
        // Perform ECDH key agreement
        var (platformKey, sharedSecret) = _protocol.Encapsulate(authenticatorKey);
        
        try
        {
            // Compute PIN hash for current PIN: LEFT(SHA-256(currentPin), 16)
            var currentPinHash = ComputePinHash(currentPin);
            var pinHashEnc = _protocol.Encrypt(sharedSecret, currentPinHash);
            
            // Pad and encrypt new PIN
            var newPinBytes = PadPin(newPin);
            var newPinEnc = _protocol.Encrypt(sharedSecret, newPinBytes);
            
            // Compute pinUvAuthParam = authenticate(sharedSecret, newPinEnc || pinHashEnc)
            var message = new byte[newPinEnc.Length + pinHashEnc.Length];
            newPinEnc.CopyTo(message.AsSpan());
            pinHashEnc.CopyTo(message.AsSpan(newPinEnc.Length));
            
            var pinUvAuthParam = _protocol.Authenticate(sharedSecret, message);
            
            var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
                .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
                .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.ChangePin)
                .WithMap(ClientPinParam.KeyAgreement, writer => WriteCoseKey(writer, platformKey))
                .WithBytes(ClientPinParam.PinHashEnc, pinHashEnc)
                .WithBytes(ClientPinParam.NewPinEnc, newPinEnc)
                .WithBytes(ClientPinParam.PinUvAuthParam, pinUvAuthParam)
                .Build();
            
            await _session.SendCborRequestAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
        }
    }
    
    /// <summary>
    /// Gets a PIN token using the PIN.
    /// </summary>
    /// <param name="pin">The PIN.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decrypted PIN token.</returns>
    /// <exception cref="CtapException">If the PIN is incorrect.</exception>
    /// <remarks>
    /// The returned token can be used with <see cref="IPinUvAuthProtocol.Authenticate"/>
    /// to create pinUvAuthParam values for subsequent CTAP commands.
    /// </remarks>
    public async Task<byte[]> GetPinTokenAsync(
        string pin,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ValidatePin(pin);
        
        // Get authenticator's key agreement key
        var authenticatorKey = await GetKeyAgreementAsync(cancellationToken)
            .ConfigureAwait(false);
        
        // Perform ECDH key agreement
        var (platformKey, sharedSecret) = _protocol.Encapsulate(authenticatorKey);
        
        try
        {
            // Compute PIN hash: LEFT(SHA-256(pin), 16)
            var pinHash = ComputePinHash(pin);
            var pinHashEnc = _protocol.Encrypt(sharedSecret, pinHash);
            
            var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
                .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
                .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.GetPinToken)
                .WithMap(ClientPinParam.KeyAgreement, writer => WriteCoseKey(writer, platformKey))
                .WithBytes(ClientPinParam.PinHashEnc, pinHashEnc)
                .Build();
            
            var response = await _session.SendCborRequestAsync(request, cancellationToken)
                .ConfigureAwait(false);
            
            var encryptedToken = ParsePinTokenResponse(response);
            return _protocol.Decrypt(sharedSecret, encryptedToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
        }
    }
    
    /// <summary>
    /// Gets a PIN/UV auth token using PIN with specified permissions (CTAP 2.1).
    /// </summary>
    /// <param name="pin">The PIN.</param>
    /// <param name="permissions">The permissions to request.</param>
    /// <param name="rpId">Optional RP ID for credential-related permissions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decrypted PIN/UV auth token.</returns>
    /// <exception cref="CtapException">If the PIN is incorrect or permissions are invalid.</exception>
    /// <remarks>
    /// <para>
    /// CTAP 2.1 introduces scoped tokens with explicit permissions. This method
    /// should be used instead of <see cref="GetPinTokenAsync"/> when working with
    /// CTAP 2.1 authenticators.
    /// </para>
    /// <para>
    /// The rpId parameter is required for <see cref="PinUvAuthTokenPermissions.MakeCredential"/>
    /// and <see cref="PinUvAuthTokenPermissions.GetAssertion"/> permissions.
    /// </para>
    /// </remarks>
    public async Task<byte[]> GetPinUvAuthTokenUsingPinAsync(
        string pin,
        PinUvAuthTokenPermissions permissions,
        string? rpId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ValidatePin(pin);
        
        if (permissions == PinUvAuthTokenPermissions.None)
        {
            throw new ArgumentException("At least one permission must be specified.", nameof(permissions));
        }
        
        // Get authenticator's key agreement key
        var authenticatorKey = await GetKeyAgreementAsync(cancellationToken)
            .ConfigureAwait(false);
        
        // Perform ECDH key agreement
        var (platformKey, sharedSecret) = _protocol.Encapsulate(authenticatorKey);
        
        try
        {
            // Compute PIN hash: LEFT(SHA-256(pin), 16)
            var pinHash = ComputePinHash(pin);
            var pinHashEnc = _protocol.Encrypt(sharedSecret, pinHash);
            
            var builder = CtapRequestBuilder.Create(CtapCommand.ClientPin)
                .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
                .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.GetPinUvAuthTokenUsingPinWithPermissions)
                .WithMap(ClientPinParam.KeyAgreement, writer => WriteCoseKey(writer, platformKey))
                .WithBytes(ClientPinParam.PinHashEnc, pinHashEnc)
                .WithUInt(ClientPinParam.Permissions, (uint)permissions);
            
            if (!string.IsNullOrEmpty(rpId))
            {
                builder.WithString(ClientPinParam.RpId, rpId);
            }
            
            var request = builder.Build();
            var response = await _session.SendCborRequestAsync(request, cancellationToken)
                .ConfigureAwait(false);
            
            var encryptedToken = ParsePinTokenResponse(response);
            return _protocol.Decrypt(sharedSecret, encryptedToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
        }
    }
    
    /// <summary>
    /// Gets a PIN/UV auth token using UV with specified permissions (CTAP 2.1).
    /// </summary>
    /// <param name="permissions">The permissions to request.</param>
    /// <param name="rpId">Optional RP ID for credential-related permissions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decrypted PIN/UV auth token.</returns>
    /// <exception cref="CtapException">If UV fails or permissions are invalid.</exception>
    /// <remarks>
    /// <para>
    /// This method uses user verification (e.g., biometrics) instead of PIN.
    /// The authenticator must support UV (check authenticatorGetInfo options.uv).
    /// </para>
    /// <para>
    /// The rpId parameter is required for <see cref="PinUvAuthTokenPermissions.MakeCredential"/>
    /// and <see cref="PinUvAuthTokenPermissions.GetAssertion"/> permissions.
    /// </para>
    /// <para>
    /// <b>Note:</b> This operation requires user interaction and cannot be automated.
    /// </para>
    /// </remarks>
    public async Task<byte[]> GetPinUvAuthTokenUsingUvAsync(
        PinUvAuthTokenPermissions permissions,
        string? rpId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        
        if (permissions == PinUvAuthTokenPermissions.None)
        {
            throw new ArgumentException("At least one permission must be specified.", nameof(permissions));
        }
        
        // Get authenticator's key agreement key
        var authenticatorKey = await GetKeyAgreementAsync(cancellationToken)
            .ConfigureAwait(false);
        
        // Perform ECDH key agreement
        var (platformKey, sharedSecret) = _protocol.Encapsulate(authenticatorKey);
        
        try
        {
            var builder = CtapRequestBuilder.Create(CtapCommand.ClientPin)
                .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
                .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.GetPinUvAuthTokenUsingUvWithPermissions)
                .WithMap(ClientPinParam.KeyAgreement, writer => WriteCoseKey(writer, platformKey))
                .WithUInt(ClientPinParam.Permissions, (uint)permissions);
            
            if (!string.IsNullOrEmpty(rpId))
            {
                builder.WithString(ClientPinParam.RpId, rpId);
            }
            
            var request = builder.Build();
            var response = await _session.SendCborRequestAsync(request, cancellationToken)
                .ConfigureAwait(false);
            
            var encryptedToken = ParsePinTokenResponse(response);
            return _protocol.Decrypt(sharedSecret, encryptedToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
        }
    }
    
    /// <summary>
    /// Gets the authenticator's key agreement public key.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The COSE public key as a dictionary.</returns>
    internal async Task<Dictionary<int, object?>> GetKeyAgreementAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        
        var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
            .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
            .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.GetKeyAgreement)
            .Build();
        
        var response = await _session.SendCborRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        
        return ParseKeyAgreementResponse(response);
    }
    
    private static void ValidatePin(string pin)
    {
        ArgumentNullException.ThrowIfNull(pin);
        
        if (pin.Length < PinMinLength)
        {
            throw new ArgumentException(
                $"PIN must be at least {PinMinLength} characters.", nameof(pin));
        }
        
        if (pin.Length > PinMaxLength)
        {
            throw new ArgumentException(
                $"PIN must not exceed {PinMaxLength} characters.", nameof(pin));
        }
    }
    
    private static byte[] PadPin(string pin)
    {
        // Convert PIN to UTF-8 and pad with zeros to 64 bytes
        var pinBytes = Encoding.UTF8.GetBytes(pin);
        var padded = new byte[PinBlockSize];
        
        if (pinBytes.Length > PinBlockSize)
        {
            throw new ArgumentException(
                $"PIN UTF-8 encoding exceeds {PinBlockSize} bytes.", nameof(pin));
        }
        
        pinBytes.CopyTo(padded.AsSpan());
        return padded;
    }
    
    private static byte[] ComputePinHash(string pin)
    {
        // PIN hash = LEFT(SHA-256(pin), 16)
        var pinBytes = Encoding.UTF8.GetBytes(pin);
        var hash = SHA256.HashData(pinBytes);
        return hash.AsSpan(0, 16).ToArray();
    }
    
    private static void WriteCoseKey(CborWriter writer, Dictionary<int, object?> key)
    {
        // Write COSE_Key as map, keys sorted numerically (negative < positive)
        var sortedKeys = key.Keys.OrderBy(k => k).ToList();
        
        writer.WriteStartMap(key.Count);
        
        foreach (var k in sortedKeys)
        {
            writer.WriteInt32(k);
            
            switch (key[k])
            {
                case int intVal:
                    writer.WriteInt32(intVal);
                    break;
                case byte[] bytes:
                    writer.WriteByteString(bytes);
                    break;
                case null:
                    writer.WriteNull();
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported COSE key value type: {key[k]?.GetType().Name}");
            }
        }
        
        writer.WriteEndMap();
    }
    
    private static (int Retries, bool PowerCycleRequired) ParsePinRetriesResponse(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
        var mapLength = reader.ReadStartMap();
        
        int retries = 0;
        var powerCycleRequired = false;
        
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            
            switch (key)
            {
                case ClientPinResponse.PinRetries:
                    retries = reader.ReadInt32();
                    break;
                case ClientPinResponse.PowerCycleState:
                    powerCycleRequired = reader.ReadBoolean();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        return (retries, powerCycleRequired);
    }
    
    private static (int Retries, bool PowerCycleRequired) ParseUvRetriesResponse(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
        var mapLength = reader.ReadStartMap();
        
        int retries = 0;
        var powerCycleRequired = false;
        
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            
            switch (key)
            {
                case ClientPinResponse.UvRetries:
                    retries = reader.ReadInt32();
                    break;
                case ClientPinResponse.PowerCycleState:
                    powerCycleRequired = reader.ReadBoolean();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        return (retries, powerCycleRequired);
    }
    
    private static Dictionary<int, object?> ParseKeyAgreementResponse(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
        var mapLength = reader.ReadStartMap();
        
        Dictionary<int, object?>? keyAgreement = null;
        
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            
            if (key == ClientPinResponse.KeyAgreement)
            {
                keyAgreement = ParseCoseKey(reader);
            }
            else
            {
                reader.SkipValue();
            }
        }
        
        reader.ReadEndMap();
        
        return keyAgreement ?? throw new InvalidOperationException(
            "Response missing required keyAgreement field.");
    }
    
    private static byte[] ParsePinTokenResponse(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
        var mapLength = reader.ReadStartMap();
        
        byte[]? pinToken = null;
        
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            
            if (key == ClientPinResponse.PinUvAuthToken)
            {
                pinToken = reader.ReadByteString();
            }
            else
            {
                reader.SkipValue();
            }
        }
        
        reader.ReadEndMap();
        
        return pinToken ?? throw new InvalidOperationException(
            "Response missing required pinUvAuthToken field.");
    }
    
    private static Dictionary<int, object?> ParseCoseKey(CborReader reader)
    {
        var mapLength = reader.ReadStartMap();
        var result = new Dictionary<int, object?>((int)mapLength!);
        
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            
            switch (reader.PeekState())
            {
                case CborReaderState.UnsignedInteger:
                case CborReaderState.NegativeInteger:
                    result[key] = reader.ReadInt32();
                    break;
                case CborReaderState.ByteString:
                    result[key] = reader.ReadByteString();
                    break;
                case CborReaderState.TextString:
                    result[key] = reader.ReadTextString();
                    break;
                case CborReaderState.Null:
                    reader.ReadNull();
                    result[key] = null;
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        return result;
    }
    
    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        
        _protocol.Dispose();
        _disposed = true;
    }
}
