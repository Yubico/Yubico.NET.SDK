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

using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Pin;

using CredentialManagementClass = Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement;
using CtapException = Yubico.YubiKit.Fido2.Ctap.CtapException;
using CtapStatus = Yubico.YubiKit.Fido2.Ctap.CtapStatus;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Helper methods for FIDO2 integration tests.
/// </summary>
public static class FidoTestHelpers
{
    /// <summary>
    /// Sets the PIN if not already configured, or validates the PIN is correct.
    /// </summary>
    /// <param name="session">The FIDO session.</param>
    /// <param name="pin">The PIN to set or verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ClientPin instance for further operations.</returns>
    public static async Task<ClientPin> SetOrVerifyPinAsync(
        IFidoSession session,
        string pin,
        CancellationToken cancellationToken = default)
    {
        var info = await session.GetInfoAsync(cancellationToken).ConfigureAwait(false);
        
        // Check if PIN is configured
        var pinConfigured = info.Options.TryGetValue("clientPin", out var clientPinValue) 
            && clientPinValue;
        
        // Determine which protocol to use (prefer v2 if available)
        var protocolVersion = info.PinUvAuthProtocols.Contains(2) ? 2 : 1;
        IPinUvAuthProtocol protocol = protocolVersion == 2 
            ? new PinUvAuthProtocolV2() 
            : new PinUvAuthProtocolV1();
        
        var clientPin = new ClientPin(session, protocol);
        
        if (!pinConfigured)
        {
            await clientPin.SetPinAsync(pin, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Verify by getting a PIN token
            _ = await clientPin.GetPinTokenAsync(pin, cancellationToken).ConfigureAwait(false);
        }
        
        return clientPin;
    }
    
    /// <summary>
    /// Gets a PIN/UV auth token with credential management permission.
    /// </summary>
    /// <param name="session">The FIDO session.</param>
    /// <param name="pin">The PIN.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The PIN token, ClientPin instance, and protocol for credential management.</returns>
    public static async Task<(byte[] PinToken, ClientPin ClientPin, IPinUvAuthProtocol Protocol)> 
        GetCredManTokenAsync(
            IFidoSession session,
            string pin,
            CancellationToken cancellationToken = default)
    {
        var info = await session.GetInfoAsync(cancellationToken).ConfigureAwait(false);
        
        // Determine which protocol to use
        var protocolVersion = info.PinUvAuthProtocols.Contains(2) ? 2 : 1;
        IPinUvAuthProtocol protocol = protocolVersion == 2 
            ? new PinUvAuthProtocolV2() 
            : new PinUvAuthProtocolV1();
        
        var clientPin = new ClientPin(session, protocol);
        
        // Get token with credential management permission
        byte[] pinToken;
        
        // Check if device supports CTAP 2.1 permissions
        var supportsPermissions = info.Versions.Contains("FIDO_2_1") || 
                                   info.Versions.Contains("FIDO_2_1_PRE");
        
        if (supportsPermissions)
        {
            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin, 
                PinUvAuthTokenPermissions.CredentialManagement,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Fallback to basic PIN token
            pinToken = await clientPin.GetPinTokenAsync(pin, cancellationToken).ConfigureAwait(false);
        }
        
        return (pinToken, clientPin, protocol);
    }
    
    /// <summary>
    /// Deletes all credentials for the specified relying party.
    /// </summary>
    /// <param name="session">The FIDO session.</param>
    /// <param name="rpId">The relying party ID.</param>
    /// <param name="pin">The PIN.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task DeleteAllCredentialsForRpAsync(
        FidoSession session,
        string rpId,
        string pin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (pinToken, clientPin, protocol) = await GetCredManTokenAsync(
                session, pin, cancellationToken).ConfigureAwait(false);
            
            using (clientPin)
            {
                var credMan = new CredentialManagementClass(session, protocol, pinToken);
                
                // Get RP ID hash
                var rpIdHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rpId));
                
                // Enumerate and delete credentials
                var credentials = await credMan.EnumerateCredentialsAsync(rpIdHash, cancellationToken)
                    .ConfigureAwait(false);
                
                foreach (var cred in credentials)
                {
                    await credMan.DeleteCredentialAsync(cred.CredentialId, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
        catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
        {
            // No credentials to delete - that's fine
        }
    }
    
    /// <summary>
    /// Creates a FIDO session from the first available HID FIDO device.
    /// </summary>
    /// <param name="yubiKeyManager">The YubiKey manager.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created FIDO session and connection.</returns>
    public static async Task<(FidoSession Session, IFidoHidConnection Connection)> CreateSessionAsync(
        IYubiKeyManager yubiKeyManager,
        CancellationToken cancellationToken = default)
    {
        var devices = await yubiKeyManager.FindAllAsync(ConnectionType.HidFido).ConfigureAwait(false);
        var device = devices.FirstOrDefault() 
            ?? throw new InvalidOperationException("No FIDO2 YubiKey found.");
        
        var connection = await device.ConnectAsync<IFidoHidConnection>().ConfigureAwait(false);
        var session = await FidoSession.CreateAsync(connection, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        
        return (session, connection);
    }
    
    /// <summary>
    /// Computes PIN/UV auth param for MakeCredential.
    /// </summary>
    public static byte[] ComputeMakeCredentialAuthParam(
        IPinUvAuthProtocol protocol,
        byte[] pinToken,
        byte[] clientDataHash)
    {
        return protocol.Authenticate(pinToken, clientDataHash);
    }
    
    /// <summary>
    /// Computes PIN/UV auth param for GetAssertion.
    /// </summary>
    public static byte[] ComputeGetAssertionAuthParam(
        IPinUvAuthProtocol protocol,
        byte[] pinToken,
        byte[] clientDataHash)
    {
        return protocol.Authenticate(pinToken, clientDataHash);
    }
    
    /// <summary>
    /// Creates a FIDO session from an NFC SmartCard connection.
    /// </summary>
    /// <remarks>
    /// FIDO2 over SmartCard is only supported via NFC transport.
    /// This method finds a device via CCID that reports NFC transport.
    /// </remarks>
    /// <param name="yubiKeyManager">The YubiKey manager.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created FIDO session and SmartCard connection.</returns>
    /// <exception cref="InvalidOperationException">No NFC-connected YubiKey found.</exception>
    public static async Task<(FidoSession Session, ISmartCardConnection Connection)> CreateNfcSessionAsync(
        IYubiKeyManager yubiKeyManager,
        CancellationToken cancellationToken = default)
    {
        var devices = await yubiKeyManager.FindAllAsync(ConnectionType.SmartCard).ConfigureAwait(false);
        
        // Find a device that connects via NFC (not USB CCID)
        foreach (var device in devices)
        {
            var connection = await device.ConnectAsync<ISmartCardConnection>().ConfigureAwait(false);
            
            if (connection.Transport == Transport.Nfc)
            {
                var session = await FidoSession.CreateAsync(connection, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return (session, connection);
            }
            
            // Not NFC - dispose and try next device
            await connection.DisposeAsync().ConfigureAwait(false);
        }
        
        throw new InvalidOperationException(
            "No NFC-connected YubiKey found. Ensure an NFC reader is connected and a YubiKey is present on the reader.");
    }
}
