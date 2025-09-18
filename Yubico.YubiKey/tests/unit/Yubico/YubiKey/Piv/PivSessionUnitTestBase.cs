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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

public class PivSessionUnitTestBase : IDisposable
{
    private HollowYubiKeyDevice? _deviceMock;
    private bool _disposed;
    private FirmwareVersion? _firmwareVersion;
    private Func<KeyEntryData, bool>? _keyCollector;
    private PivSession? _pivSessionMock;

    protected HollowYubiKeyDevice DeviceMock => _deviceMock ??= new HollowYubiKeyDevice(true)
    {
        FirmwareVersion = FirmwareVersion
    };

    protected PivSession PivSessionMock => _pivSessionMock ??= GetNewPivSession();

    protected Func<KeyEntryData, bool>? KeyCollector
    {
        get => _keyCollector;
        set
        {
            _keyCollector = value;
            PivSessionMock.KeyCollector = value;
        }
    }

    protected FirmwareVersion FirmwareVersion
    {
        get => _firmwareVersion ??= FirmwareVersion.V5_0_0;
        set
        {
            _firmwareVersion = value;
            DeviceMock.FirmwareVersion = value!;
        }
    }

    #region IDisposable Members

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        PivSessionMock.Dispose();

        _disposed = true;
    }

    #endregion

    protected PivSession GetNewPivSession()
    {
        var pivSession = new PivSession(DeviceMock);
        if (KeyCollector == null)
        {
            var simpleCollector = new SimpleKeyCollector(false);
            pivSession.KeyCollector = simpleCollector.SimpleKeyCollectorDelegate;
        }
        else
        {
            pivSession.KeyCollector = KeyCollector;
        }

        return pivSession;
    }
}
