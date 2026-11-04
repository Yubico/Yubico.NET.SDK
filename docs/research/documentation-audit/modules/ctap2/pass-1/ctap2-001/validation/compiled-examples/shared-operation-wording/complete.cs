using Yubico.YubiKit.Core.Credentials;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuditCt001.Prepared.shared_operation_wording;

public static class Example
{
public static void Run(UserPresenceContext context)
{
string message = context.Operation switch
{
    UserPresenceOperations.Ctap2.MakeCredential => "Touch your YubiKey to create a passkey.",
    UserPresenceOperations.Ctap2.GetAssertion => "Touch your YubiKey to sign in.",
    UserPresenceOperations.Piv.SignOrDecrypt => "Touch your YubiKey for the PIV key operation.",
    _ when context.Application == UserPresenceApplications.Oath => "Touch your YubiKey to generate a code.",
    _ => "Touch your YubiKey to continue."
};
}
}
