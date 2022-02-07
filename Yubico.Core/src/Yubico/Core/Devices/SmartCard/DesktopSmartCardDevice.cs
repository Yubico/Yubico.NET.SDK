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
using System.Globalization;
using System.Linq;
using Yubico.Core.Iso7816;
using Yubico.Core.Logging;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.SmartCard
{
    internal class DesktopSmartCardDevice : SmartCardDevice
    {
        private readonly string _readerName;
        private readonly Logger _log = Log.GetLogger();

        public static IReadOnlyList<ISmartCardDevice> GetList()
        {
            Logger log = Log.GetLogger();
            using IDisposable logScope = log.BeginScope("SmartCardDevice.GetList()");

            uint result = PlatformLibrary.Instance.SCard.EstablishContext(SCARD_SCOPE.USER, out SCardContext context);
            log.SCardApiCall(nameof(SCard.EstablishContext), result);

            if (result != ErrorCode.SCARD_S_SUCCESS)
            {
                throw new SCardException(
                    ExceptionMessages.SCardCantEstablish,
                    result);
            }

            try
            {
                result = PlatformLibrary.Instance.SCard.ListReaders(context, null, out string[] readerNames);
                log.SCardApiCall(nameof(SCard.ListReaders), result);

                // It's OK if there are no readers on the system. Treat this the same as if we
                // didn't find any devices.
                if (result == ErrorCode.SCARD_E_NO_READERS_AVAILABLE || readerNames.Length == 0)
                {
                    log.LogInformation("No smart card devices found.");
                    return new List<ISmartCardDevice>();
                }

                log.LogInformation("Found {NumSmartCards} smart card devices.", readerNames.Length);

                if (result != ErrorCode.SCARD_S_SUCCESS)
                {
                    throw new SCardException(
                        ExceptionMessages.SCardListReadersFailed,
                        result);
                }

                // We could probably get away with zero, however this small delay can help us
                // be resilient to race conditions with device arrival.
                int timeoutMs = 100;

                using var readerStates = new SCardReaderStates(readerNames.Length);
                for (int i = 0; i < readerStates.Count; i++)
                {
                    readerStates[i].ReaderName = readerNames[i];
                }

                result = PlatformLibrary.Instance.SCard.GetStatusChange(
                    context,
                    timeoutMs,
                    readerStates);
                log.SCardApiCall(nameof(SCard.GetStatusChange), result);
                log.LogInformation("Updated SCard reader states: {ReaderStates}", readerStates);

                if (result != ErrorCode.SCARD_S_SUCCESS)
                {
                    throw new SCardException(
                        ExceptionMessages.SCardGetStatusChangeFailed,
                        result);
                }

                return readerStates
                    .Select(readerState => NewSmartCardDevice(readerState.ReaderName, readerState.Atr))
                    .ToArray();
            }
            finally
            {
                context.Dispose();
            }
        }

        private static ISmartCardDevice NewSmartCardDevice(string readerName, AnswerToReset? atr) => SdkPlatformInfo.OperatingSystem switch
        {
            SdkPlatform.Windows => new WindowsSmartCardDevice(readerName, atr),
            SdkPlatform.MacOS => new DesktopSmartCardDevice(readerName, atr),
            SdkPlatform.Linux => new DesktopSmartCardDevice(readerName, atr),
            _ => throw new PlatformNotSupportedException()
        };

        public DesktopSmartCardDevice(string readerName, AnswerToReset? atr) :
            base(readerName, atr)
        {
            _readerName = readerName;
            _log = Log.GetLogger();
        }

        public override ISmartCardConnection Connect()
        {
            uint result = PlatformLibrary.Instance.SCard.EstablishContext(SCARD_SCOPE.USER, out SCardContext? context);
            _log.SCardApiCall(nameof(SCard.EstablishContext), result);

            if (result != ErrorCode.SCARD_S_SUCCESS)
            {
                throw new SCardException(
                    ExceptionMessages.SCardCantEstablish,
                    result);
            }

            SCardCardHandle? cardHandle = null;

            try
            {
                result = PlatformLibrary.Instance.SCard.Connect(
                    context,
                    _readerName,
                    SCARD_SHARE.SHARED,
                    SCARD_PROTOCOL.Tx,
                    out cardHandle,
                    out SCARD_PROTOCOL activeProtocol);
                _log.SCardApiCall(nameof(SCard.Connect), result);

                if (result != ErrorCode.SCARD_S_SUCCESS)
                {
                    throw new SCardException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.SCardCardCantConnect,
                            Path),
                        result);
                }

                _log.LogInformation(
                    "Connected to smart card [{ReaderName}]. Active protocol is [{ActiveProtocol}]",
                    _readerName,
                    activeProtocol);

                var connection = new DesktopSmartCardConnection(
                    context,
                    cardHandle,
                    activeProtocol);

                // We are transferring ownership to SmartCardConnection
                _log.LogInformation("Transferred context and cardHandle to connection instance.");
                context = null;
                cardHandle = null;

                return connection;
            }
            finally
            {
                if (context != null)
                {
                    context?.Dispose();
                    _log.LogInformation("Context disposed.");
                }

                if (cardHandle != null)
                {
                    cardHandle?.Dispose();
                    _log.LogInformation("CardHandle disposed.");
                }
            }
        }
    }
}
