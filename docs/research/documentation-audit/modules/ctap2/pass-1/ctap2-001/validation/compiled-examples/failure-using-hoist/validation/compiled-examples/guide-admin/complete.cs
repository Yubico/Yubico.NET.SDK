using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.CredentialManagement;

namespace AuditCt001.Prepared.guide_admin;

public static class Example
{
public static async Task ExampleAsync(Ctap2Session session, IPinUvAuthProtocol protocol, byte[] pinToken, ReadOnlyMemory<byte> rpIdHash, PublicKeyCredentialDescriptor credentialId)
{
// With live session/protocol, authorized borrowed pinToken, returned RP hash and descriptor credentialId.
using var credMgmt = new CredentialManagement(session, protocol, pinToken);

// Get credential metadata
var metadata = await credMgmt.GetCredentialsMetadataAsync();

// Enumerate RPs. ReportedTotal is copied from the first response and can be null.
var rpResult = await credMgmt.EnumerateRelyingPartiesAsync();
var rps = rpResult.RelyingParties;

// Enumerate credentials for an RP
using var credentialResult = await credMgmt.EnumerateCredentialsAsync(rpIdHash);
var creds = credentialResult.Credentials;

// Delete credential
await credMgmt.DeleteCredentialAsync(credentialId);
}
}
