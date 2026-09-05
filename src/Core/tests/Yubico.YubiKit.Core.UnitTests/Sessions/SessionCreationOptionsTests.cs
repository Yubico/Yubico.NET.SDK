// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

public sealed class SessionCreationOptionsTests
{
    [Fact]
    public void WithPreferredConnectionType_CopiesEveryOptionAndOverridesConnectionType()
    {
        using var scpKeyParameters = Scp03KeyParameters.Default;
        var configuration = new ProtocolConfiguration
        {
            ForceShortApdus = true,
            InsSendRemaining = 0xA5
        };
        var firmwareVersion = new FirmwareVersion(5, 7, 2);
        var options = new SessionCreationOptions
        {
            ProtocolConfiguration = configuration,
            ScpKeyParameters = scpKeyParameters,
            PreferredConnectionType = ConnectionType.HidFido,
            FirmwareVersionOverride = firmwareVersion
        };

        SessionCreationOptions copy = options.WithPreferredConnectionType(ConnectionType.SmartCard);

        Assert.NotSame(options, copy);
        Assert.Equal(configuration, copy.ProtocolConfiguration);
        Assert.Same(scpKeyParameters, copy.ScpKeyParameters);
        Assert.Equal(ConnectionType.SmartCard, copy.PreferredConnectionType);
        Assert.Same(firmwareVersion, copy.FirmwareVersionOverride);
        Assert.Equal(ConnectionType.HidFido, options.PreferredConnectionType);
    }
}