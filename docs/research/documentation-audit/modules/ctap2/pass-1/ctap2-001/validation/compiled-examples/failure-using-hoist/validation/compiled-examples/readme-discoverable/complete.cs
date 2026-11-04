using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Credentials;
using System.Collections.Generic;

namespace AuditCt001.Prepared.readme_discoverable;

public static class Example
{
public static async Task ExampleAsync(ICtap2Session session, byte[] authenticationHash)
{
// Alternative to the allow-list assertion above, not an additional use of its ceremony hash.
var first = await session.GetAssertionAsync("example.com", authenticationHash, new GetAssertionOptions());
var assertions = new List<GetAssertionResponse> { first };
int total = first.NumberOfCredentials ?? 1;
for (int index = 1; index < total; index++)
{
    assertions.Add(await session.GetNextAssertionAsync());
}
}
}
