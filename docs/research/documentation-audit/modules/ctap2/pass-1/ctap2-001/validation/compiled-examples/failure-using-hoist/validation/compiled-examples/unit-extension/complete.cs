using System;
using System.Threading;
using System.Threading.Tasks;
using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2.Cbor;
using Yubico.YubiKit.Ctap2.Extensions;
using Xunit;

namespace AuditCt001.Prepared.unit_extension;

public sealed class Example
{
[Fact]
public void RegistrationExtensions_EncodePolicyAndHmacEnablement()
{
    var encoded = new ExtensionBuilder()
        .WithCredProtect(CredProtectPolicy.UserVerificationRequired)
        .WithHmacSecret()
        .Build() ?? throw new InvalidOperationException("Expected extension bytes.");
    var reader = new CborReader(encoded, CborConformanceMode.Ctap2Canonical);
    Assert.Equal(2, reader.ReadStartMap());
    Assert.Equal("credProtect", reader.ReadTextString());
    Assert.Equal((int)CredProtectPolicy.UserVerificationRequired, reader.ReadInt32());
    Assert.Equal("hmac-secret", reader.ReadTextString());
    Assert.True(reader.ReadBoolean());
    reader.ReadEndMap();
}
}
