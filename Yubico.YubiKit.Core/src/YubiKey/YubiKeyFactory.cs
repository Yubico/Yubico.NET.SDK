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
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

public interface IYubiKeyFactory
{
    IYubiKey Create(IDevice device);
}

public class YubiKeyFactory(
    ILoggerFactory loggerFactory,
    ISmartCardConnectionFactory connectionFactory) : IYubiKeyFactory
{
    #region IYubiKeyFactory Members

    public IYubiKey Create(IDevice device) =>
        device switch
        {
            IPcscDevice pcscDevice => CreatePcscYubiKey(pcscDevice),
            _ => throw new NotSupportedException(
                $"Device type {device.GetType().Name} is not supported by this factory.")
        };

    #endregion

    private PcscYubiKey CreatePcscYubiKey(IPcscDevice cardDevice) =>
        new(
            cardDevice,
            connectionFactory,
            loggerFactory.CreateLogger<PcscYubiKey>()
        );

    public static YubiKeyFactory Create(ILoggerFactory? loggerFactory = null) =>
        new(loggerFactory ?? NullLoggerFactory.Instance, SmartCardConnectionFactory.CreateDefault());
}