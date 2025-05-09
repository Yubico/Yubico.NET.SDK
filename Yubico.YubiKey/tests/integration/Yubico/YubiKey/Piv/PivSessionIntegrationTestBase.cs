// Copyright 2024 Yubico AB
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

public class PivSessionIntegrationTestBase : IDisposable
{
    private bool _disposed;
    private PivSession? _session;

    protected ReadOnlyMemory<byte> DefaultPin = "123456"u8.ToArray();
    protected ReadOnlyMemory<byte> DefaultPuk = "12345678"u8.ToArray();

    protected readonly ReadOnlyMemory<byte> DefaultManagementKey = new byte[] // Both Aes and TDes
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };

    protected KeyType DefaultManagementKeyType =>
        Device.FirmwareVersion > FirmwareVersion.V5_7_0 ? KeyType.AES192 : KeyType.TripleDES;

    protected StandardTestDevice TestDeviceType { get; set; } = StandardTestDevice.Fw5;
    protected PivSession Session => _session ??= GetSession(true);
    protected IYubiKeyDevice Device => IntegrationTestDeviceEnumeration.GetTestDevice(TestDeviceType);

    protected PivSessionIntegrationTestBase()
    {
        using var session = GetSessionInternal(Device, false);
        session.ResetApplication();
    }

    ~PivSessionIntegrationTestBase()
    {
        Dispose(false);
    }

    protected PivSession GetSession(
        bool authenticate = false) => GetSessionInternal(Device, authenticate);

    protected PivSession GetSessionScp(
        bool authenticate = false) => GetSessionInternal(Device, authenticate, Scp03KeyParameters.DefaultKey);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(
        bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _session?.Dispose();
            _session = null;
        }

        _disposed = true;
    }

    private static PivSession GetSessionInternal(
        IYubiKeyDevice testDevice,
        bool authenticate,
        Scp03KeyParameters? keyParameters = null)
    {
        Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

        PivSession? pivSession = null;
        try
        {
            pivSession = new PivSession(testDevice, keyParameters);
            if (pivSession.KeyCollector == null)
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
            }

            if (authenticate)
            {
                pivSession.AuthenticateManagementKey();
            }

            return pivSession;
        }
        catch
        {
            pivSession?.Dispose();
            throw;
        }
    }
}
