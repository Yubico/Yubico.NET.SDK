using System;
using System.Linq;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2;

// These test should be able to run without a bio key, although might fail
// on incorrect pin-verify assumptions
[Trait(TraitTypes.Category, TestCategories.Elevated)]
public class Fido2Tests : FidoSessionIntegrationTestBase
{
    #region AuthenticatorInfo

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void Session_AuthenticatorIdentifier_Returns_SameIdentifier()
    {
        ReadOnlyMemory<byte>? identifier1;

        // First run
        using (var session = GetSession(minFw: FirmwareVersion.V5_8_0))
        {
            identifier1 = session.AuthenticatorIdentifier;
            Assert.True(identifier1.HasValue);
            Assert.NotEmpty(identifier1.Value.ToArray());
            Assert.All(identifier1.Value.ToArray(), b => Assert.NotEqual(0, b));
        }

        // First run
        using (var session = GetSession())
        {
            var identifier2 = session.AuthenticatorIdentifier;
            Assert.True(identifier2!.Value.Span.SequenceEqual(identifier1.Value.Span));
        }
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void AuthenticatorInfo_GetIdentifier_BothRuns_Returns_SameIdentifier()
    {
        ReadOnlyMemory<byte>? identifier1;
        ReadOnlyMemory<byte>? identifier2;
        ReadOnlyMemory<byte>? ppuat;

        // First run
        using (var session = GetSession(minFw: FirmwareVersion.V5_8_0))
        {
            session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);
            Assert.NotNull(session.AuthTokenPersistent);

            ppuat = session.AuthTokenPersistent.Value.ToArray();
            identifier1 = session.AuthenticatorInfo.GetIdentifier(ppuat.Value);

            Assert.NotNull(identifier1);
            Assert.NotEmpty(identifier1.Value.ToArray());
        }

        // Second run, reuse ppuat
        using (var session = GetSession(persistentPinUvAuthToken: ppuat.Value))
        {
            session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);
            Assert.NotNull(session.AuthTokenPersistent);

            identifier2 = session.AuthenticatorInfo.GetIdentifier(ppuat.Value);

            Assert.NotNull(identifier2);
            Assert.NotEmpty(identifier2.Value.ToArray());
        }

        Assert.True(identifier1.Value.Span.SequenceEqual(identifier2.Value.Span));
    }

    #endregion

    #region CredentialManagement

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void GetMetadata_Succeeds()
    {
        _ = Session.GetCredentialMetadata();
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void CredentialManagement_ReuseTokenFromPreviousSession_Succeeds()
    {
        byte[] persistentPinUvAuthToken;

        // Test GetCredentialMetadata
        using (var session = GetSession(minFw: FirmwareVersion.V5_8_0))
        {
            Assert.Null(session.AuthTokenPersistent);

            // Will require pin
            session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);

            Assert.NotNull(session.AuthTokenPersistent);
            Assert.Equal(1, KeyCollector.RequestCount(KeyEntryRequest.VerifyFido2Pin));

            // Clear the key collector (to test missing ability to generate new tokens)
            session.KeyCollector = null;

            // Will not require pin
            _ = session.GetCredentialMetadata();

            Assert.Equal(1, KeyCollector.RequestCount(KeyEntryRequest.VerifyFido2Pin)); // Should be unchanged

            // Save for later
            persistentPinUvAuthToken = session.AuthTokenPersistent!.Value.ToArray();
        }

        // Reset the call count for the next test
        KeyCollector.ResetRequestCounts();

        // Reusing stored persistentPinUvAuthToken
        using (var session = GetSession(persistentPinUvAuthToken: persistentPinUvAuthToken))
        {
            // Should not require pin
            _ = session.GetCredentialMetadata();

            Assert.Equal(0, KeyCollector.RequestCount(KeyEntryRequest.VerifyFido2Pin));
        }
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void CredentialManagement_GetCredentialMetadata_WithROToken_Succeeds_AfterChangePin()
    {
        Assert.Null(Session.AuthTokenPersistent);

        // Should require pin
        Session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);

        Assert.NotNull(Session.AuthTokenPersistent);
        Assert.Equal(1, KeyCollector.RequestCount(KeyEntryRequest.VerifyFido2Pin));

        // Should NOT require pin
        _ = Session.GetCredentialMetadata();
        Assert.Equal(1, KeyCollector.RequestCount(KeyEntryRequest.VerifyFido2Pin)); // Should be unchanged

        // Should NOT require pin
        _ = Session.GetCredentialMetadata();
        Assert.Equal(1, KeyCollector.RequestCount(KeyEntryRequest.VerifyFido2Pin)); // Should be unchanged

        // Trigger internal PersistenUvAuthToken reset
        Session.TryChangePin(TestPin1, TestPin1);

        // Should require pin
        _ = Session.GetCredentialMetadata();
        Assert.Equal(2,
            KeyCollector.RequestCount(KeyEntryRequest
                .VerifyFido2Pin)); // Will have changed, as we did set the pin,requiring reset of persistent token
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void CredentialManagement_Succeeds_WithRO_Token()
    {
        // Create a credential to enumerate
        MakeCredentialParameters.AddOption(AuthenticatorOptions.rk, true);
        Session.MakeCredential(MakeCredentialParameters);
        KeyCollector.ResetRequestCounts();

        // Test GetCredentialMetadata
        using (var session = GetSession(minFw: FirmwareVersion.V5_8_0))
        {
            Assert.Null(session.AuthTokenPersistent);

            // Will require pin
            session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);

            Assert.NotNull(session.AuthTokenPersistent);
            Assert.Equal(1, KeyCollector.RequestCount(KeyEntryRequest.VerifyFido2Pin));

            // Clear the key collector (to test missing ability to generate new tokens)
            session.KeyCollector = null;

            // Will not require pin
            _ = session.GetCredentialMetadata();
            Assert.Equal(1, KeyCollector.RequestCount(KeyEntryRequest.VerifyFido2Pin)); // Should be unchanged
        }

        // Reset the call count for the next test
        KeyCollector.ResetRequestCounts();

        // Test EnumerateRelyingParties
        using (var session = GetSession())
        {
            Assert.Null(session.AuthTokenPersistent);

            // Will require pin
            session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);

            Assert.NotNull(session.AuthTokenPersistent);
            Assert.Equal(1, KeyCollector.RequestCount(KeyEntryRequest.VerifyFido2Pin));

            // Clear the key collector (to test missing ability to generate new tokens)
            session.KeyCollector = null;

            // Will not require pin
            _ = session.EnumerateRelyingParties();
            Assert.Equal(1, KeyCollector.RequestCount(KeyEntryRequest.VerifyFido2Pin)); // Should be unchanged
        }

        // Reset the call count for the next test
        KeyCollector.ResetRequestCounts();

        // Test DeleteCredential, will not succeed with RO token.
        using (var session = GetSession())
        {
            Assert.Null(session.AuthTokenPersistent);

            // Will require pin
            session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);

            Assert.NotNull(session.AuthTokenPersistent);
            Assert.Equal(1, KeyCollector.RequestCount(KeyEntryRequest.VerifyFido2Pin));

            // Clear the key collector (to test missing ability to generate new tokens)
            session.KeyCollector = null;

            // Will not require pin
            var cred = session.EnumerateCredentialsForRelyingParty("demo.yubico.com").FirstOrDefault();

            Assert.NotNull(cred);
            Assert.Equal(1, KeyCollector.RequestCount(KeyEntryRequest.VerifyFido2Pin)); // Should be unchanged

            // Send command to delete credential with RO token (fails)
            var response = session.Connection.SendCommand(new DeleteCredentialCommand(
                cred!.CredentialId,
                session.AuthTokenPersistent.Value,
                session.AuthProtocol));

            Assert.Equal(CtapStatus.PinAuthInvalid, response.CtapStatus);
        }
    }

    [SkippableTheory(typeof(DeviceNotFoundException))]
    [InlineData(false)]
    [InlineData(true)]
    public void CredentialManagement_with_commands_Succeeds_WithRO_Token(
        bool useEncryptedToken)
    {
        // Arrange
        //  Create a credential to enumerate
        MakeCredentialParameters.AddOption(AuthenticatorOptions.rk, true);
        Session.MakeCredential(MakeCredentialParameters);

        var protocol = new PinUvAuthProtocolTwo();
        var publicKey = GetAuthenticatorPublicKey(Connection, protocol);

        protocol.Encapsulate(publicKey);
        var persistentPinUvAuthToken = useEncryptedToken
            ? GetReadOnlyPinToken(Connection, protocol, false).ToArray()
            : GetReadOnlyPinToken(Connection, protocol, true).ToArray();

        // Enumerate with PPUAT token should work
        EnumerateRpsBeginCommand enumRpsCommand;
        if (useEncryptedToken)
        {
            enumRpsCommand = new EnumerateRpsBeginCommand(persistentPinUvAuthToken, protocol);
        }
        else
        {
            var message = EnumerateRpsBeginCommand.GetAuthenticationMessage();
            var authParam = protocol.Authenticate(persistentPinUvAuthToken, message);
            enumRpsCommand = new EnumerateRpsBeginCommand(authParam, protocol.Protocol);
        }

        // Act
        var enumRpsResponse = Connection.SendCommand(enumRpsCommand);
        Assert.Equal(ResponseStatus.Success, enumRpsResponse.Status);

        var (totalRelyingPartyCount, relyingParty) = enumRpsResponse.GetData();
        Assert.Equal("demo.yubico.com", relyingParty.Id);
        Assert.Equal(1, totalRelyingPartyCount);

        // Enumerate with PPUAT token should work again
        EnumerateCredentialsBeginCommand enumCreds;
        if (useEncryptedToken)
        {
            enumCreds = new EnumerateCredentialsBeginCommand(relyingParty, persistentPinUvAuthToken, protocol);
        }
        else
        {
            var message = EnumerateCredentialsBeginCommand.GetAuthenticationMessage(relyingParty);
            var authParam = protocol.Authenticate(persistentPinUvAuthToken, message);
            enumCreds = new EnumerateCredentialsBeginCommand(relyingParty, authParam, protocol.Protocol);
        }

        var enumCresResponse = Connection.SendCommand(enumCreds);
        Assert.Equal(ResponseStatus.Success, enumCresResponse.Status);

        var (credentialCount, _) = enumCresResponse.GetData();
        Assert.Equal(1, credentialCount);
    }

    #endregion

    #region Extensions

    [SkippableFact(typeof(DeviceNotFoundException))]
    [Trait(TraitTypes.Category, TestCategories.RequiresTouch)]
    public void Extensions_MakeCredential_ThirdPartyPayment()
    {
        var isSupported = Session.AuthenticatorInfo.IsExtensionSupported("thirdPartyPayment");
        Skip.IfNot(isSupported);

        MakeCredentialParameters.AddThirdPartyPaymentExtension();
        var makeCredentialData = Session.MakeCredential(MakeCredentialParameters);

        GetAssertionParameters.RequestThirdPartyPayment();
        GetAssertionParameters.AllowCredential(makeCredentialData.AuthenticatorData.CredentialId!);
        var assertions = Session.GetAssertions(GetAssertionParameters);

        Assert.Single(assertions);
        Assert.NotNull(assertions.First().AuthenticatorData.Extensions);
        Assert.True(assertions.First().AuthenticatorData.GetThirdPartyPaymentExtension());
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    [Trait(TraitTypes.Category, TestCategories.RequiresTouch)]
    public void Extensions_MakeCredential_HmacSecretMc_OneSalt_Succeeds()
    {
        var isSupported = Session.AuthenticatorInfo.IsExtensionSupported("hmac-secret-mc");
        Skip.IfNot(isSupported);

        byte[] salt1 =
        [
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
        ];

        MakeCredentialParameters.AddHmacSecretMcExtension(Session.AuthenticatorInfo, salt1);
        var makeCredentialData = Session.MakeCredential(MakeCredentialParameters);

        Assert.NotNull(makeCredentialData.AuthenticatorData.Extensions);
        Assert.True(makeCredentialData.AuthenticatorData.Extensions.ContainsKey("hmac-secret"));
        Assert.True(makeCredentialData.AuthenticatorData.Extensions.ContainsKey("hmac-secret-mc"));
        Assert.NotEmpty(makeCredentialData.AuthenticatorData.Extensions["hmac-secret"]);
        Assert.NotEmpty(makeCredentialData.AuthenticatorData.Extensions["hmac-secret-mc"]);

        var hmacSecretOutput = makeCredentialData.AuthenticatorData.GetHmacSecretExtension(Session.AuthProtocol);
        Assert.NotNull(hmacSecretOutput);
        Assert.Equal(32, hmacSecretOutput.Length);
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    [Trait(TraitTypes.Category, TestCategories.RequiresTouch)]
    public void Extensions_HmacSecretMc_TwoSalts_Succeeds()
    {
        var isSupported = Session.AuthenticatorInfo.IsExtensionSupported("hmac-secret-mc");
        Skip.IfNot(isSupported);

        byte[] salt1 =
        [
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
        ];

        MakeCredentialParameters.AddHmacSecretMcExtension(Session.AuthenticatorInfo, salt1, salt1);
        var makeCredentialData = Session.MakeCredential(MakeCredentialParameters);

        Assert.NotNull(makeCredentialData.AuthenticatorData.Extensions);
        Assert.True(makeCredentialData.AuthenticatorData.Extensions.ContainsKey("hmac-secret"));
        Assert.True(makeCredentialData.AuthenticatorData.Extensions.ContainsKey("hmac-secret-mc"));
        Assert.NotEmpty(makeCredentialData.AuthenticatorData.Extensions["hmac-secret"]);
        Assert.NotEmpty(makeCredentialData.AuthenticatorData.Extensions["hmac-secret-mc"]);

        var hmacSecretOutput = makeCredentialData.AuthenticatorData.GetHmacSecretExtension(Session.AuthProtocol);
        Assert.NotNull(hmacSecretOutput);
        Assert.Equal(64, hmacSecretOutput.Length);
    }

    #endregion

    #region Helpers

    private static ReadOnlyMemory<byte> GetReadOnlyPinToken(
        IYubiKeyConnection connection,
        PinUvAuthProtocolTwo protocol,
        bool decryptToken)
    {
        var token = GetPinUvAuthToken(connection, protocol,
            PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly).ToArray();
        return decryptToken
            ? protocol.Decrypt(token, 0, token.Length)
            : token;
    }

    private static ReadOnlyMemory<byte> GetPinUvAuthToken(
        IYubiKeyConnection connection,
        PinUvAuthProtocolTwo protocol,
        PinUvAuthTokenPermissions permissions)
    {
        var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(protocol, TestPin1, permissions, null);
        var getTokenRsp = connection.SendCommand(getTokenCmd);
        Assert.Equal(ResponseStatus.Success, getTokenRsp.Status);
        return getTokenRsp.GetData();
    }

    private static CoseEcPublicKey GetAuthenticatorPublicKey(
        IYubiKeyConnection connection,
        PinUvAuthProtocolBase protocol)
    {
        var getKeyAgreementCommand = new GetKeyAgreementCommand(protocol.Protocol);
        var getKeyAgreementResponse = connection.SendCommand(getKeyAgreementCommand);
        Assert.Equal(ResponseStatus.Success, getKeyAgreementResponse.Status);
        return getKeyAgreementResponse.GetData();
    }

    #endregion
}
