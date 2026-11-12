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
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Audit20261111.DocExamples.Test138;
public class MyTests
{
    // Test requires hardware (device must be connected, but runs automatically)
    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_ReturnsDevice() { }

    // Test requires user to insert/remove device (cannot run in CI/agents)
    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task WatchAsync_DetectsRemoval() { }

    // Slow test with long delays
    [Fact]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task Performance_ManyOperations() { }
}
