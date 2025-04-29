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
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

public class PivSessionIntegrationTestBase : IDisposable
{
    private bool _disposed;
    private PivSession? _session;
    protected StandardTestDevice DeviceType { get; set; } = StandardTestDevice.Fw5;
    protected bool Authenticate { get; set; }
    protected PivSession Session => _session ??= GetSession(DeviceType, Authenticate);
    protected static Func<KeyEntryData, bool>? KeyCollector { get; set; }

    protected static PivSession GetSession(
        StandardTestDevice testDeviceType = StandardTestDevice.Fw5,
        bool authenticate = true)
    {
        var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
        return GetSessionInternal(testDevice, authenticate);
    }
    
    protected static PivSession GetSessionScp(
        StandardTestDevice testDeviceType = StandardTestDevice.Fw5,
        bool authenticate = true)
    {
        var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
        return GetSessionInternal(testDevice, authenticate, Scp03KeyParameters.DefaultKey);
    }

    // Implement IDisposable pattern correctly
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Dispose managed resources
            _session?.ResetApplication();
            _session?.Dispose();
            _session = null;
        }

        // Dispose unmanaged resources (none in this case)

        _disposed = true;
    }

    // Finalizer should only clean up unmanaged resources
    ~PivSessionIntegrationTestBase()
    {
        Dispose(false);
    }

    private static PivSession GetSessionInternal(
        IYubiKeyDevice testDevice,
        bool authenticate = true,
        Scp03KeyParameters? keyParameters = null)
    {
        Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

        PivSession? pivSession = null;
        try
        {
            pivSession = new PivSession(testDevice, keyParameters);
            if (KeyCollector == null)
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
            }
            else
            {
                pivSession.KeyCollector = KeyCollector;
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
