// Copyright 2021 Yubico AB
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
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Yubico.Core.Iso7816;
using Yubico.Core.Logging;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.SmartCard
{
    /// <summary>
    /// Base class for <see cref="DesktopSmartCardDevice"/>.
    /// </summary>
    public abstract class SmartCardDevice : ISmartCardDevice
    {
        private readonly ILogger _log = Logging.Loggers.GetLogger<SmartCardDevice>();

        /// <inheritdoc />
        public DateTime LastAccessed { get; protected set; } = DateTime.MinValue;

        /// <inheritdoc />
        public string Path { get; }

        /// <inheritdoc />
        public string? ParentDeviceId { get; protected set; }

        /// <summary>
        /// The "answer to reset" (ATR) for the smart card.
        /// </summary>
        /// <remarks>
        /// The ATR for a smart card can act as an identifier for the type of card that is inserted.
        /// </remarks>
        public AnswerToReset? Atr { get; }

        /// <summary>
        /// Gets the smart card's connection type.
        /// </summary>
        public SmartCardConnectionKind Kind { get; private set; }

        /// <summary>
        /// Returns the set of smart card reader devices available to the system.
        /// </summary>
        /// <returns>A read-only list of <see cref="SmartCardDevice"/> objects.</returns>
        public static IReadOnlyList<ISmartCardDevice> GetSmartCardDevices() => SdkPlatformInfo.OperatingSystem switch
        {
            SdkPlatform.Windows => DesktopSmartCardDevice.GetList(),
            SdkPlatform.MacOS => DesktopSmartCardDevice.GetList(),
            SdkPlatform.Linux => DesktopSmartCardDevice.GetList(),
            _ => throw new PlatformNotSupportedException()
        };

        /// <summary>
        /// Creates a new smart card device object.
        /// </summary>
        /// <param name="readerName">Device reader name.</param>
        /// <param name="atr">The optional <see cref="AnswerToReset"/> identifier for the smart card device.</param>
        /// <returns>A <see cref="SmartCardDevice"/> object.</returns>
        public static ISmartCardDevice Create(string readerName, AnswerToReset? atr) => SdkPlatformInfo.OperatingSystem switch
        {
            SdkPlatform.Windows => new DesktopSmartCardDevice(readerName, atr),
            SdkPlatform.MacOS => new DesktopSmartCardDevice(readerName, atr),
            SdkPlatform.Linux => new DesktopSmartCardDevice(readerName, atr),
            _ => throw new PlatformNotSupportedException()
        };

        /// <summary>
        /// Constructs a <see cref="SmartCardDevice"/> with the specified properties.
        /// </summary>
        /// <param name="path">Device path.</param>
        /// <param name="atr"><see cref="AnswerToReset"/> properties.</param>
        protected SmartCardDevice(string path, AnswerToReset? atr)
        {
            Path = path;
            Atr = atr;

            _log.LogInformation("SmartCardDevice instance created [path = {Path}, atr = {Atr}]", path, atr);
        }

        /// <summary>
        /// Establishes an active connection to the smart card for the transmittal of data.
        /// </summary>
        /// <returns>An already opened connection to the smart card reader.</returns>
        public abstract ISmartCardConnection Connect();

        public override string ToString() => $"Smart Card: {Path}";
    }
}
