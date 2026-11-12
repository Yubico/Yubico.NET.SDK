using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Ctap2.Extensions;
using NSubstitute;
namespace Audit20261111.DocExamples.Test30;
public sealed class DocumentationBody30
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
