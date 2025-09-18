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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Yubico.Core.Iso7816;
using Yubico.Core.Logging;
using Yubico.PlatformInterop;
using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.Core.Devices.SmartCard;

internal class DesktopSmartCardDevice : SmartCardDevice
{
    private readonly ILogger _log = Log.GetLogger<DesktopSmartCardDevice>();
    private readonly string _readerName;

    public DesktopSmartCardDevice(string readerName, AnswerToReset? atr) :
        base(readerName, atr)
    {
        _readerName = readerName;
    }

    public static IReadOnlyList<ISmartCardDevice> GetList()
    {
        ILogger log = Log.GetLogger<DesktopSmartCardDevice>();
        using IDisposable? logScope = log.BeginScope("SmartCardDevice.GetList()");

        uint result = SCardEstablishContext(SCARD_SCOPE.USER, out SCardContext context);
        log.SCardApiCall(nameof(SCardEstablishContext), result);

        if (result != ErrorCode.SCARD_S_SUCCESS)
        {
            throw new SCardException(
                ExceptionMessages.SCardCantEstablish,
                result);
        }

        try
        {
            result = SCardListReaders(context, null, out string[] readerNames);

            if (result != ErrorCode.SCARD_E_NO_READERS_AVAILABLE)
            {
                log.SCardApiCall(nameof(SCardListReaders), result);
            }

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

            SCARD_READER_STATE[] readerStates = SCARD_READER_STATE.CreateFromReaderNames(readerNames);

            result = SCardGetStatusChange(
                context,
                0,
                readerStates,
                readerStates.Length);

            log.SCardApiCall(nameof(SCardGetStatusChange), result);
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

    private static ISmartCardDevice NewSmartCardDevice(string readerName, AnswerToReset? atr) =>
        SdkPlatformInfo.OperatingSystem switch
        {
            SdkPlatform.Windows => new DesktopSmartCardDevice(readerName, atr),
            SdkPlatform.MacOS => new DesktopSmartCardDevice(readerName, atr),
            SdkPlatform.Linux => new DesktopSmartCardDevice(readerName, atr),
            _ => throw new PlatformNotSupportedException()
        };

    public override ISmartCardConnection Connect()
    {
        uint result = SCardEstablishContext(SCARD_SCOPE.USER, out SCardContext? context);
        _log.SCardApiCall(nameof(SCardEstablishContext), result);

        if (result != ErrorCode.SCARD_S_SUCCESS)
        {
            throw new SCardException(
                ExceptionMessages.SCardCantEstablish,
                result);
        }

        SCardCardHandle? cardHandle = null;

        try
        {
            SCARD_SHARE shareMode = SCARD_SHARE.SHARED;

            if (AppContext.TryGetSwitch(CoreCompatSwitches.OpenSmartCardHandlesExclusively, out bool enabled) &&
                enabled)
            {
                shareMode = SCARD_SHARE.EXCLUSIVE;
            }

            result = SCardConnect(
                context,
                _readerName,
                shareMode,
                SCARD_PROTOCOL.Tx,
                out cardHandle,
                out SCARD_PROTOCOL activeProtocol);

            _log.SCardApiCall(nameof(SCardConnect), result);

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
                this,
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

    public void LogDeviceAccessTime()
    {
        LastAccessed = DateTime.Now;
        _log.LogInformation("Updating last used for {Device} to {LastAccessed:hh:mm:ss.fffffff}", this, LastAccessed);
    }
}
