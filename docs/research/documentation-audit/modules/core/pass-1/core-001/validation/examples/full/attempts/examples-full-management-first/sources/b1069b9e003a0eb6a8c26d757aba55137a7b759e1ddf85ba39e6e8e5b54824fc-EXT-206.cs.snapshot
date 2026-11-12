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
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Management;
    using System.Threading.Tasks;
    using Yubico.YubiKit.Tests.Shared;
    using Yubico.YubiKit.Tests.Shared.Infrastructure;
    using Xunit;
namespace Audit20261111.DocExamples.External206;
    public sealed class SerialRangeFilter : IYubiKeyFilter
    {
        public bool Matches(YubiKeyTestState device)
        {
            // Narrow the already authorized fixture list; never authorize production keys.
            return device.SerialNumber > 10_000_000;
        }

        public string GetDescription()
        {
            return "Dedicated test keys with SN > 10000000";
        }
    }

    public sealed class MetadataTests
    {
        [SkippableTheory]
        [WithYubiKey(CustomFilter = typeof(SerialRangeFilter))]
        public async Task ReadMetadata(YubiKeyTestState state)
        {
            await state.WithManagementAsync(async (session, cachedInfo) =>
            {
                var freshInfo = await session.GetDeviceInfoAsync();
                Assert.Equal(cachedInfo.SerialNumber, freshInfo.SerialNumber);
            });
        }
    }
        
