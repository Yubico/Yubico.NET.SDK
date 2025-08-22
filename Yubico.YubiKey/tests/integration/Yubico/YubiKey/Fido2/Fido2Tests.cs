
using System;
using System.Linq;
using System.Numerics;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2;

[Trait(TraitTypes.Category, TestCategories.Elevated)]
public class Fido2Tests : FidoSessionIntegrationTestBase
{
    public int LocalKeyCollectorVerifyPinCalls { get; set; }

    // [SkippableFact(typeof(DeviceNotFoundException))]
    // public void CredentialManagement_ReuseTokenFromPreviousSession_Succeeds()
    // {
    //     byte[] persistentPinUvAuthToken = [];

    //     // Test GetCredentialMetadata
    //     using (var fido2Session = new Fido2Session(Device))
    //     {
    //         Assert.Null(fido2Session.AuthTokenPersistent);
    //         fido2Session.KeyCollector = LocalKeyCollector;

    //         // Will require pin
    //         fido2Session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);
    //         Assert.NotNull(fido2Session.AuthTokenPersistent);
    //         Assert.Equal(1, LocalKeyCollectorVerifyPinCalls);

    //         // Clear the key collector (to test missing ability to generate new tokens)
    //         fido2Session.KeyCollector = null;

    //         // Will not require pin
    //         var (discoverableCredentialCount, remainingCredentialCount) = fido2Session.GetCredentialMetadata();
    //         Assert.True(remainingCredentialCount > 0);
    //         Assert.Equal(1, LocalKeyCollectorVerifyPinCalls); // Should be unchanged

    //         // Save for later
    //         persistentPinUvAuthToken = fido2Session.AuthTokenPersistent!.Value.ToArray();
    //     }

    //     // Reset the call count for the next test
    //     LocalKeyCollectorVerifyPinCalls = 0;

    //     using (var fido2Session = new Fido2Session(Device))
    //     {
    //         Assert.Null(fido2Session.AuthTokenPersistent);
    //         fido2Session.AuthTokenPersistent = persistentPinUvAuthToken;
    //         // fido2Session.AuthProtocol.Encapsulate()
    //         // Will not require pin
    //         var (discoverableCredentialCount, remainingCredentialCount) = fido2Session.GetCredentialMetadata();
    //         Assert.True(remainingCredentialCount > 0);
    //         Assert.Equal(1, LocalKeyCollectorVerifyPinCalls); // Should be unchanged
    //     }
    // }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void CredentialManagement_Succeeds_WithRO_Token()
    {
        // Test GetCredentialMetadata
        using (var fido2Session = new Fido2Session(Device))
        {
            Assert.Null(fido2Session.AuthTokenPersistent);
            fido2Session.KeyCollector = LocalKeyCollector;

            // Will require pin
            fido2Session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);
            Assert.NotNull(fido2Session.AuthTokenPersistent);
            Assert.Equal(1, LocalKeyCollectorVerifyPinCalls);

            // Clear the key collector (to test missing ability to generate new tokens)
            fido2Session.KeyCollector = null;

            // Will not require pin
            var (discoverableCredentialCount, remainingCredentialCount) = fido2Session.GetCredentialMetadata();
            Assert.True(remainingCredentialCount > 0);
            Assert.Equal(1, LocalKeyCollectorVerifyPinCalls); // Should be unchanged
        }

        // Reset the call count for the next test
        LocalKeyCollectorVerifyPinCalls = 0;

        // Test EnumerateRelyingParties
        using (var fido2Session = new Fido2Session(Device))
        {
            Assert.Null(fido2Session.AuthTokenPersistent);

            fido2Session.KeyCollector = LocalKeyCollector;

            // Will require pin
            fido2Session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);
            Assert.NotNull(fido2Session.AuthTokenPersistent);
            Assert.Equal(1, LocalKeyCollectorVerifyPinCalls);

            // Clear the key collector (to test missing ability to generate new tokens)
            fido2Session.KeyCollector = null;

            // Will not require pin
            var relyingParties = fido2Session.EnumerateRelyingParties();
            Assert.Equal(1, LocalKeyCollectorVerifyPinCalls); // Should be unchanged
        }

        // Reset the call count for the next test
        LocalKeyCollectorVerifyPinCalls = 0;

        // Test DeleteCredential
        using (var fido2Session = new Fido2Session(Device))
        {
            Assert.Null(fido2Session.AuthTokenPersistent);

            fido2Session.KeyCollector = LocalKeyCollector;

            // Will require pin
            fido2Session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);
            Assert.NotNull(fido2Session.AuthTokenPersistent);
            Assert.Equal(1, LocalKeyCollectorVerifyPinCalls);

            // Clear the key collector (to test missing ability to generate new tokens)
            fido2Session.KeyCollector = null;

            // Will not require pin
            var cred = fido2Session.EnumerateCredentialsForRelyingParty("demo.yubico.com").FirstOrDefault();
            Assert.Equal(1, LocalKeyCollectorVerifyPinCalls); // Should be unchanged

            // Send command to delete credential with RO token (fails)
            var response = fido2Session.Connection.SendCommand(new DeleteCredentialCommand(
                cred!.CredentialId,
                fido2Session.AuthTokenPersistent.Value,
                fido2Session.AuthProtocol));

            Assert.Equal(CtapStatus.PinAuthInvalid, response.CtapStatus);

            fido2Session.KeyCollector = LocalKeyCollector;
            response = fido2Session.Connection.SendCommand(new DeleteCredentialCommand(
                cred!.CredentialId,
                fido2Session.GetAuthToken(false, PinUvAuthTokenPermissions.CredentialManagement,
                    "demo.yubico.com"),
                fido2Session.AuthProtocol));
            Assert.Equal(CtapStatus.Ok, response.CtapStatus);
            Assert.Equal(2, LocalKeyCollectorVerifyPinCalls);
        }
    }
    private bool LocalKeyCollector(KeyEntryData arg)
    {
        switch (arg.Request)
        {
            case KeyEntryRequest.VerifyFido2Pin:
                ++LocalKeyCollectorVerifyPinCalls;
                arg.SubmitValue(ComplexPin.Span);
                break;
            case KeyEntryRequest.VerifyFido2Uv:
                Console.WriteLine("Fingerprint requested.");
                break;
            case KeyEntryRequest.TouchRequest:
                Console.WriteLine("Touch requested.");
                break;
            case KeyEntryRequest.Release:
                break;
            default:
                throw new NotSupportedException("Not supported by this test");
        }

        return true;
    }
}
