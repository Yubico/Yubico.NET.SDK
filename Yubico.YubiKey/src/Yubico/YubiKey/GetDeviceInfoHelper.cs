// Copyright 2023 Yubico AB
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
using Yubico.Core.Logging;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey
{
    internal static class GetDeviceInfoHelper
    {
        private static readonly Logger Logger = Log.GetLogger();

        /// <summary>
        /// Fetches and aggregates device configuration details from a YubiKey using multiple APDU commands,
        /// paging through the data as needed until all configuration data is retrieved.
        /// This method processes the responses, accumulating TLV-encoded data into a single dictionary.
        /// </summary>
        /// <typeparam name="TCommand">The specific type of IGetPagedDeviceInfoCommand, e.g. GetPagedDeviceInfoCommand, which will then allow for returning the appropriate response.</typeparam>
        /// <param name="connection">The connection interface to communicate with a YubiKey.</param>
        /// <returns>A YubiKeyDeviceInfo? object containing all relevant device information if successful, otherwise null.</returns>
        public static YubiKeyDeviceInfo? GetDeviceInfo<TCommand>(IYubiKeyConnection connection)
            where TCommand : IGetPagedDeviceInfoCommand<IYubiKeyResponseWithData<Dictionary<int, ReadOnlyMemory<byte>>>>, new()
        {
            int page = 0;
            var pages = new Dictionary<int, ReadOnlyMemory<byte>>();

            bool hasMoreData = true;
            while (hasMoreData)
            {
                IYubiKeyResponseWithData<Dictionary<int, ReadOnlyMemory<byte>>> response =
                    connection.SendCommand(new TCommand { Page = (byte)page++ });

                if (response.Status == ResponseStatus.Success)
                {
                    Dictionary<int, ReadOnlyMemory<byte>> tlvData = response.GetData();
                    foreach (KeyValuePair<int, ReadOnlyMemory<byte>> tlv in tlvData)
                    {
                        pages.Add(tlv.Key, tlv.Value);
                    }

                    const int moreDataTag = 0x10;
                    hasMoreData = tlvData.TryGetValue(moreDataTag, out ReadOnlyMemory<byte> hasMoreDataByte)
                        && hasMoreDataByte.Span.Length == 1
                        && hasMoreDataByte.Span[0] == 1;
                }
                else
                {
                    Logger.LogError("Failed to get device info page-{Page}: {Error} {Message}", page,
                        response.StatusWord, response.StatusMessage);

                    return null;
                }
            }

            return YubiKeyDeviceInfo.CreateFromResponseData(pages);
        }

        /// <summary>
        /// Attempts to create a dictionary from a TLV-encoded byte array by parsing and extracting tag-value pairs.
        /// </summary>
        /// <param name="tlvData">The byte array containing TLV-encoded data.</param>
        /// <returns>A dictionary mapping integer tags to their corresponding values as byte arrays.</returns>
        public static Dictionary<int, ReadOnlyMemory<byte>>? CreateApduDictionaryFromResponseData(
            ReadOnlyMemory<byte> tlvData)
        {
            if (tlvData.IsEmpty)
            {
                Logger.LogWarning("ResponseAPDU data was empty!");
                return null;
            }

            // Certain transports (such as OTP keyboard) may return a buffer that is larger than the
            // overall TLV size. We want to make sure we're only parsing over real TLV data here, so
            // check the first byte to get the overall TLV length and slice accordingly.
            int tlvDataLength = tlvData.Span[0];
            if (tlvDataLength == 0 || 1 + tlvDataLength > tlvData.Length)
            {
                Logger.LogWarning("TLV Data length was out of expected ranges. {Length}", tlvDataLength);
                return null;
            }

            var result = new Dictionary<int, ReadOnlyMemory<byte>>();
            var tlvReader = new TlvReader(tlvData.Slice(1, tlvDataLength));
            while (tlvReader.HasData)
            {
                int tag = tlvReader.PeekTag();
                ReadOnlyMemory<byte> value = tlvReader.ReadValue(tag);
                result.Add(tag, value);
            }

            return result;
        }
    }
}
