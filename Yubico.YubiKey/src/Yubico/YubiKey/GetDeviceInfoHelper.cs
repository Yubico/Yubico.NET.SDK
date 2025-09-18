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
using Microsoft.Extensions.Logging;
using Yubico.Core.Logging;

namespace Yubico.YubiKey;

internal static class GetDeviceInfoHelper
{
    private static readonly ILogger Logger = Log.GetLogger(typeof(GetDeviceInfoHelper).FullName!);

    /// <summary>
    ///     Fetches and aggregates device configuration details from a YubiKey using multiple APDU commands,
    ///     paging through the data as needed until all configuration data is retrieved.
    ///     This method processes the responses, accumulating TLV-encoded data into a single dictionary.
    /// </summary>
    /// <typeparam name="TCommand">
    ///     The specific type of IGetPagedDeviceInfoCommand, e.g. GetPagedDeviceInfoCommand, which will
    ///     then allow for returning the appropriate response.
    /// </typeparam>
    /// <param name="connection">The connection interface to communicate with a YubiKey.</param>
    /// <returns>A YubiKeyDeviceInfo? object containing all relevant device information if successful, otherwise null.</returns>
    public static YubiKeyDeviceInfo? GetDeviceInfo<TCommand>(IYubiKeyConnection connection)
        where TCommand
        : IGetPagedDeviceInfoCommand<IYubiKeyResponseWithData<Dictionary<int, ReadOnlyMemory<byte>>>>,
        new()
    {
        int page = 0;
        var combinedPages = new Dictionary<int, ReadOnlyMemory<byte>>();

        bool hasMoreData = true;
        while (hasMoreData)
        {
            var response = connection.SendCommand(new TCommand { Page = (byte)page++ });
            if (response.Status == ResponseStatus.Success)
            {
                var tlvData = response.GetData();
                foreach (var tlv in tlvData)
                {
                    combinedPages.Add(tlv.Key, tlv.Value);
                }

                const int moreDataTag = 0x10;

                hasMoreData = tlvData.TryGetValue(moreDataTag, out var hasMoreDataByte)
                    && hasMoreDataByte.Span.Length == 1
                    && hasMoreDataByte.Span[0] == 1;
            }
            else
            {
                Logger.LogError(
                    "Failed to get device info page-{Page}: {Error} {Message}", page,
                    response.StatusWord, response.StatusMessage);

                return null;
            }
        }

        return YubiKeyDeviceInfo.CreateFromResponseData(combinedPages);
    }
}
