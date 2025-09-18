// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKey.TestUtilities;

public class SimpleIntegrationTestConnection : IDisposable
{
    private readonly IYubiKeyDevice? _device;
    private IYubiKeyConnection? _connection;
    private bool _disposed;
    private int? _serialNumber;

    public SimpleIntegrationTestConnection(
        YubiKeyApplication application,
        StandardTestDevice device = StandardTestDevice.Fw5)
    {
        _device = IntegrationTestDeviceEnumeration.GetTestDevice(device);
        _connection = _device.Connect(application);
        _serialNumber = _device.SerialNumber;
    }

    public IYubiKeyConnection Connection =>
        _connection ?? throw new ObjectDisposedException("Connection unavailable.");

    public IYubiKeyDevice Device =>
        _device ?? throw new ObjectDisposedException("Device unavailable.");

    public int SerialNumber =>
        _serialNumber ?? throw new InvalidOperationException("No serial number.");

    #region IDisposable Members

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    protected virtual void Dispose(
        bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection?.Dispose();
                _serialNumber = null;
                _connection = null;
            }

            _disposed = true;
        }
    }
}
