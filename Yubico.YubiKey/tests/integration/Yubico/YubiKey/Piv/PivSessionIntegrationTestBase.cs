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
using System.Diagnostics;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

public class PivSessionIntegrationTestBase : IDisposable
{
    public static Memory<byte> DefaultPin => "123456"u8.ToArray();
    public static Memory<byte> DefaultPuk => "12345678"u8.ToArray();
    public static Memory<byte> ComplexPuk => "gjH@5K!8"u8.ToArray();
    public static Memory<byte> ComplexPin => "1@$#5s!8"u8.ToArray();

    public static Memory<byte> DefaultManagementKey => new byte[] // Both Aes and TDes
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };

    public static readonly byte[] ComplexManagementKey =
    {
        0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
        0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x12,
        0x23, 0x34, 0x45, 0x56, 0x67, 0x78, 0x89, 0x9A
    };

    // public void SetFipsApprovedCredentials(PivSession? session)
    // {
    //     session ??= Session;
    //     session.TryChangePin(DefaultPin, ComplexPin, out _);
    //     session.TryChangePuk(DefaultPuk, ComplexPuk, out _);
    //     session.TryChangeManagementKey(DefaultManagementKey, ComplexManagementKey, PivTouchPolicy.Always);
    //     Assert.True(session.TryVerifyPin(ComplexPin, out _));
    // }

    // public void SetFipsApprovedCredentials(
    //     IYubiKeyDevice? device,
    //     ScpKeyParameters parameters)
    // {
    //     device ??= Device;
    //     using var session = new PivSession(device, parameters);
    //     SetFipsApprovedCredentials(session);
    // }

    protected KeyType DefaultManagementKeyType =>
        Device.FirmwareVersion > FirmwareVersion.V5_7_0 ? KeyType.AES192 : KeyType.TripleDES;

    protected StandardTestDevice TestDeviceType { get; set; } = StandardTestDevice.Any;

    /// <summary>
    /// Returns an authenticated PivSession.
    /// </summary>
    protected PivSession Session => _session ??= GetSession(true);

    protected IYubiKeyDevice Device => IntegrationTestDeviceEnumeration.GetTestDevice(TestDeviceType);

    private bool _disposed;
    private PivSession? _session;

    private bool UseComplexCreds => Device.IsFipsSeries || Device.IsPinComplexityEnabled;

    protected PivSessionIntegrationTestBase()
    {
        using var session = GetSessionInternal(Device, false, false);
        session.ResetApplication();
    }

    ~PivSessionIntegrationTestBase()
    {
        Dispose(false);
    }

    protected PivSession GetSession(
        bool authenticate = false) => GetSessionInternal(Device, authenticate, UseComplexCreds);

    protected PivSession GetSessionScp(
        bool authenticate = false) =>
        GetSessionInternal(Device, authenticate, UseComplexCreds, Scp03KeyParameters.DefaultKey);

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
        bool useComplexCreds,
        Scp03KeyParameters? keyParameters = null)
    {
        Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

        PivSession? session = null;
        try
        {
            session = new PivSession(testDevice, keyParameters);

            if (useComplexCreds)
            {
                session.TryChangePin(DefaultPin, ComplexPin, out _);
                session.TryChangePuk(DefaultPuk, ComplexPuk, out _);
                var managementKeyChanged = session.TryChangeManagementKey(DefaultManagementKey, ComplexManagementKey);
                Debug.Assert(managementKeyChanged, "Failed to change management");
                Assert.True(session.TryVerifyPin(ComplexPin, out _));
            }

            if (session.KeyCollector == null)
            {
                var collectorObj = new Simple39KeyCollector(false, useComplexCreds);
                session.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
            }

            if (authenticate)
            {
                session.AuthenticateManagementKey();
            }

            return session;
        }
        catch
        {
            session?.Dispose();
            throw;
        }
    }
}
