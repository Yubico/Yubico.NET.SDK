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
using System.Diagnostics;
using System.Text;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2;

public class FidoSessionIntegrationTestBase : IDisposable
{
    public static Memory<byte> SimplePin => "123456"u8.ToArray();
    public static Memory<byte> ComplexPin => "11234567"u8.ToArray();

    protected StandardTestDevice TestDeviceType { get; set; } = StandardTestDevice.Any;

    /// <summary>
    /// Returns an authenticated Fido2Session.
    /// </summary>
    protected Fido2Session Session => _session ??= GetSession(true);

    protected IYubiKeyDevice Device => IntegrationTestDeviceEnumeration.GetTestDevice(TestDeviceType);

    private bool _disposed;
    private Fido2Session? _session;

    private bool UseComplexCreds => Device.IsFipsSeries || Device.IsPinComplexityEnabled;

    protected FidoSessionIntegrationTestBase()
    {
        using var session = GetSessionInternal(Device, false, false);
        // Might need reset logic

        // Add demo credential
    }

    ~FidoSessionIntegrationTestBase()
    {
        Dispose(false);
    }

    protected Fido2Session GetSession(
        bool authenticate = false) => GetSessionInternal(Device, authenticate, UseComplexCreds);

    protected Fido2Session GetSessionScp(
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

    private static Fido2Session GetSessionInternal(
        IYubiKeyDevice testDevice,
        bool authenticate,
        bool useComplexCreds,
        Scp03KeyParameters? keyParameters = null)
    {
        Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Fido2));

        Fido2Session? session = null;
        try
        {
            session = new Fido2Session(testDevice);

            if (session.AuthenticatorInfo.ForcePinChange == true)
            {
                session.TrySetPin(useComplexCreds ? ComplexPin : SimplePin);
            }

            if (session.KeyCollector == null)
            {
                var localKeyCollector = new Fido2SessionTestKeyCollector(useComplexCreds);
                session.KeyCollector = localKeyCollector.HandleRequest;
            }

            // if (authenticate)
            // {
            //     session.AuthenticateManagementKey();
            // }

            return session;
        }
        catch
        {
            session?.Dispose();
            throw;
        }
    }


    public class Fido2SessionTestKeyCollector(bool useComplexCreds)
    {
        public int LocalKeyCollectorVerifyPinCalls { get; set; }
        public bool HandleRequest(KeyEntryData arg)
        {
            switch (arg.Request)
            {
                case KeyEntryRequest.VerifyFido2Pin:
                    ++LocalKeyCollectorVerifyPinCalls;
                    arg.SubmitValue(useComplexCreds ? ComplexPin.Span : SimplePin.Span);
                    break;
                case KeyEntryRequest.VerifyFido2Uv:
                    Console.WriteLine("Fingerprint requested.");
                    break;
                case KeyEntryRequest.TouchRequest:
                    Console.WriteLine("Touch requested.");
                    break;
                case KeyEntryRequest.Release:
                    break;
                case KeyEntryRequest.SetFido2Pin:
                    arg.SubmitValue(useComplexCreds ? ComplexPin.Span : SimplePin.Span);
                    break;
                case KeyEntryRequest.ChangeFido2Pin:
                    if (arg.IsRetry)
                    {
                        arg.SubmitValues(SimplePin.Span, ComplexPin.Span);
                    }
                    else
                    {
                        arg.SubmitValues(ComplexPin.Span, SimplePin.Span);
                    }
                    break;
                default:
                    throw new NotSupportedException("Not supported by this test");
            }

            return true;
        }
    }
}
