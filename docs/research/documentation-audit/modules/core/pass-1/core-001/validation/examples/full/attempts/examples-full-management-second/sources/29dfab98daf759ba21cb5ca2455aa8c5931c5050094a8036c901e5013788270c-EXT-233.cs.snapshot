using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Xunit;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.Management;
namespace Audit20261111.DocExamples.External233;
public sealed class DocumentationBody
{
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
public async Task ReadMetadata_OverOtpHid(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, _) =>
    {
        Assert.Equal(ConnectionType.HidOtp, mgmt.ConnectionType);
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);
    }, preferredConnection: state.ConnectionType);
}
}
