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
    protected StandardTestDevice TestDeviceType { get; set; } = StandardTestDevice.Fw5;
    protected bool Authenticate { get; set; }
    protected PivSession Session => _session ??= GetSession(TestDeviceType, Authenticate);
    public IYubiKeyDevice Device { get; set; }

    protected PivSessionIntegrationTestBase()
    {
        Device = IntegrationTestDeviceEnumeration.GetTestDevice(TestDeviceType);
        Session.ResetApplication();
    }

    ~PivSessionIntegrationTestBase()
    {
        Dispose(false);
    }

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
            // _session?.ResetApplication();
            _session?.Dispose();
            _session = null;
        }

        _disposed = true;
    }

    protected PivSession GetSession(
        StandardTestDevice testDeviceType = StandardTestDevice.Fw5,
        bool authenticate = true) => GetSessionInternal(Device, authenticate);

    protected PivSession GetSessionScp(
        StandardTestDevice testDeviceType = StandardTestDevice.Fw5,
        bool authenticate = true) => GetSessionInternal(Device, authenticate, Scp03KeyParameters.DefaultKey);

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
