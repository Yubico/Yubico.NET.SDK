using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Yubico.YubiKit.WebAuthn.Client;

namespace AuditCt001.Prepared.webauthn_pair_policy;

public static class Example
{
public static void Run()
{
var allowedPairs = new HashSet<(string Origin, string RpId)>
{
    ("https://login.example.com", "example.com"),
    ("https://login.example.com", "partner.test")
};
var allowlistOptions = new WebAuthnClientOptions
{
    RpIdValidator = (requestOrigin, rpId) => allowedPairs.Contains((requestOrigin.StringValue, rpId))
};
}
}
