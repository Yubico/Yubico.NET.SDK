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
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Piv;
namespace Audit20261111.DocExamples.Test375;
public sealed class DocumentationBody375
{
[SkippableTheory]
[WithYubiKey(Capability = DeviceCapabilities.Piv, MinFirmware = "5.3.0")]
public async Task GetMetadata_ReadOnly_Succeeds(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    var metadata = await session.GetPinMetadataAsync();
    Assert.InRange(metadata.RetriesRemaining, 0, metadata.TotalRetries);
}
}
