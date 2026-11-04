using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2.CredentialManagement;

namespace AuditCt001.Prepared.readme_admin;

public static class Example
{
public static async Task ExampleAsync(Yubico.YubiKit.Ctap2.CredentialManagement.CredentialManagement credentialManagement, ReadOnlyMemory<byte> rpIdHash)
{
// Existing helper and hash supplied by the caller; see the authorization prerequisites below.
using var credentialResult = await credentialManagement.EnumerateCredentialsAsync(rpIdHash);
foreach (StoredCredentialInfo credential in credentialResult.Credentials)
{
    Console.WriteLine(Convert.ToHexString(credential.CredentialId.Id.Span));
}
}
}
