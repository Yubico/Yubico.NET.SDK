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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Devices.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

public interface IYubiKeyFactory
{
    IYubiKey CreateAsync(IDevice device);
}

public class YubiKeyFactory : IYubiKeyFactory
{
    private readonly ISmartCardConnectionFactory _connectionFactory;
    private readonly ILoggerFactory _loggerFactory;

    public YubiKeyFactory(
        ILoggerFactory loggerFactory,
        ISmartCardConnectionFactory connectionFactory)
    {
        _loggerFactory = loggerFactory;
        _connectionFactory = connectionFactory;
    }

    #region IYubiKeyFactory Members

    public IYubiKey CreateAsync(IDevice device)
    {
        if (device is not ISmartCardDevice cardDevice)
            throw new NotSupportedException(
                $"Device type {device.GetType().Name} is not supported by this factory.");

        return device switch
        {
            PcscDevice => CreatePcscYubiKey(cardDevice),
            _ => throw new NotSupportedException(
                $"Device type {device.GetType().Name} is not supported by this factory.")
        };
    }

    #endregion

    private IYubiKey CreatePcscYubiKey(ISmartCardDevice cardDevice) =>
        new PcscYubiKey(
            _loggerFactory.CreateLogger<PcscYubiKey>(),
            cardDevice,
            _connectionFactory
        );
}