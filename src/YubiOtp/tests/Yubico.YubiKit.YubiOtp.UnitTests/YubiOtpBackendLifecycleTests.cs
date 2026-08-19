// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.YubiOtp.Backend;

namespace Yubico.YubiKit.YubiOtp.UnitTests;

/// <summary>
///     Pins the ownership contract: YubiOTP backends borrow the protocol handed to them and own no
///     disposable state, so neither the abstraction nor its implementations advertise <see cref="IDisposable" />.
///     Disposal belongs to the session (protocol) and to whoever created the connection.
/// </summary>
public class YubiOtpBackendLifecycleTests
{
    [Fact]
    public void IYubiOtpBackend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(IYubiOtpBackend)));
    }

    [Fact]
    public void SmartCardBackend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(SmartCardBackend)));
    }

    [Fact]
    public void HidBackend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(HidBackend)));
    }
}