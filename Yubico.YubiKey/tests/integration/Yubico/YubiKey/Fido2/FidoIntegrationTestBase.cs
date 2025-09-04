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
using Xunit;
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2;

public class FidoSessionIntegrationTestBase : IDisposable
{
    #region TestData

    protected MakeCredentialParameters MakeCredentialParameters = new(Rp, UserEntity)
    {
        ClientDataHash = ClientDataHash
    };

    protected GetAssertionParameters GetAssertionParameters = new(Rp, ClientDataHash);
    public static Memory<byte> SimplePin => "123456"u8.ToArray();
    public static Memory<byte> ComplexPin => "11234567"u8.ToArray();
    public static Memory<byte> ComplexPin2 => "12234567"u8.ToArray();
    public static readonly byte[] ClientDataHash = "12345678123456781234567812345678"u8.ToArray();
    public static readonly RelyingParty Rp = new("demo.yubico.com")
    {
        Name = "demo.yubico.com"
    };

    public static readonly UserEntity UserEntity = new(new byte[] { 1, 2, 3, 4, 5 })
    {
        Name = "yubico-demo",
        DisplayName = "yubico-demo"
    };

    #endregion


    #region Device and connection

    protected StandardTestDevice TestDeviceType { get; set; } = StandardTestDevice.Any;
    protected Fido2Session Session => _session ??= GetSession();
    protected IYubiKeyConnection Connection => _connection ??= Device.Connect(YubiKeyApplication.Fido2);
    protected IYubiKeyDevice Device => _device ??= IntegrationTestDeviceEnumeration.GetTestDevice(TestDeviceType);
    protected TestKeyCollector KeyCollector;

    private bool _disposed;
    private IYubiKeyDevice? _device;
    private Fido2Session? _session;
    private IYubiKeyConnection? _connection;
    private bool UseComplexCreds => Device.IsFipsSeries || Device.IsPinComplexityEnabled;

    protected FidoSessionIntegrationTestBase()
    {
        KeyCollector = new TestKeyCollector(UseComplexCreds);
        using var session = GetSessionInternal(Device, UseComplexCreds);
        
        MakeCredentialParameters.AddOption(AuthenticatorOptions.rk, true);

        // Might need reset logic
    }

    ~FidoSessionIntegrationTestBase()
    {
        Dispose(false);
    }

    protected Fido2Session GetSession(
        ReadOnlyMemory<byte>? ppuat = null) => GetSessionInternal(Device, UseComplexCreds, ppuat);

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

    private Fido2Session GetSessionInternal(
        IYubiKeyDevice testDevice,
        bool useComplexCreds,
        ReadOnlyMemory<byte>? ppuat = null,
        Scp03KeyParameters? keyParameters = null)
    {
        Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Fido2));

        Fido2Session? session = null;
        try
        {
            session = ppuat is null
                ? new Fido2Session(testDevice)
                : new Fido2Session(testDevice, ppuat);

            if (session.AuthenticatorInfo.ForcePinChange == true)
            {
                session.TrySetPin(useComplexCreds ? ComplexPin : SimplePin);
            }

            session.KeyCollector = KeyCollector.HandleRequest;

            return session;
        }
        catch
        {
            session?.Dispose();
            throw;
        }
    }

    #endregion
}
