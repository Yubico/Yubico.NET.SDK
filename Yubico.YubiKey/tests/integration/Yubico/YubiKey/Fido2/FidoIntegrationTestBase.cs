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
using Yubico.YubiKey.Fido2.Commands;
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
    public static Memory<byte> TestPin1 => "11234567"u8.ToArray();
    public static Memory<byte> TestPin2 => "12234567"u8.ToArray();
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
    protected TestKeyCollector KeyCollector = new();

    private bool _disposed;
    private IYubiKeyDevice? _device;
    private Fido2Session? _session;
    private IYubiKeyConnection? _connection;

    protected FidoSessionIntegrationTestBase()
    {
        // Clean up any existing credentials for a fresh start
        try
        {
            using var session = GetSession();

            var relyingParties = session.EnumerateRelyingParties();
            foreach (var rp in relyingParties)
            {
                var credentials = session.EnumerateCredentialsForRelyingParty(rp);
                foreach (var cred in credentials)
                {
                    session.DeleteCredential(cred.CredentialId);
                }
            }
        }
        catch (Ctap2DataException)
        {
            // Ignore errors related to non-existent credentials
        }
        
        KeyCollector.ResetRequestCounts();
    }

    ~FidoSessionIntegrationTestBase()
    {
        Dispose(false);
    }

    protected Fido2Session GetSession(
        StandardTestDevice deviceType = StandardTestDevice.Any,
        FirmwareVersion? minFw = null,
        Transport? transport = Transport.All,
        ReadOnlyMemory<byte>? persistentPinUvAuthToken = null)
    {
        deviceType = TestDeviceType == StandardTestDevice.Any
            ? deviceType
            : TestDeviceType;

        minFw ??= FirmwareVersion.V5_4_3;
        transport ??= Transport.All;

        var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(
            deviceType,
            transport.Value,
            minFw);

        Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Fido2));

        Fido2Session? session = null;
        try
        {
            session = persistentPinUvAuthToken is null
                ? new Fido2Session(testDevice)
                : new Fido2Session(testDevice, persistentPinUvAuthToken);

            session.KeyCollector = KeyCollector.HandleRequest;

            if (session.AuthenticatorInfo.ForcePinChange == true ||
                session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.clientPin) == OptionValue.False)
            {
                session.TrySetPin(TestPin1);
            }

            var isValid = session.TryVerifyPin(TestPin1, permissions: PinUvAuthTokenPermissions.CredentialManagement, null, out _, out _);
            Assert.True(isValid, "Pin was incorrect. Please reset the key and try again.");

            return session;
        }
        catch
        {
            session?.Dispose();
            throw;
        }
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
            _session?.Dispose();
            _session = null;
        }

        _disposed = true;
    }

    #endregion
}
