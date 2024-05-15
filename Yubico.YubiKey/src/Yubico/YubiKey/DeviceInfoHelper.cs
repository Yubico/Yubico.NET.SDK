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
using Yubico.YubiKey.Management.Commands;

namespace Yubico.YubiKey
{
    internal static class DeviceInfoHelper
    {
        /// <summary>
        /// Fetches and aggregates device configuration details from a YubiKey using multiple APDU commands,
        /// paging through the data as needed until all configuration data is retrieved.
        /// This method processes the responses, accumulating TLV-encoded data into a single dictionary.
        /// </summary>
        /// <typeparam name="T">The type of the YubiKey response which must include data.</typeparam>
        /// <param name="connection">The connection interface to communicate with a YubiKey.</param>
        /// <param name="command">The command to be sent to the YubiKey. This command should be capable of handling pagination.</param>
        /// <returns>A YubiKeyDeviceInfo object containing all relevant device information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the command fails to retrieve successful response statuses from the YubiKey.</exception>
        public static YubiKeyDeviceInfo GetDeviceInfo<T>(
            IYubiKeyConnection connection,
            IPagedGetDeviceInfoCommand<T> command) 
            where T : IYubiKeyResponseWithData<Dictionary<int, ReadOnlyMemory<byte>>> 
        {
            Logger log = Log.GetLogger();
            
            int page = 0;
            var pages = new Dictionary<int, ReadOnlyMemory<byte>>();
            
            bool hasMoreData = true;
            while (hasMoreData)
            {
                command.Page = (byte)page++;
                T response = connection.SendCommand(command);
                if (response.Status == ResponseStatus.Success)
                {
                    Dictionary<int, ReadOnlyMemory<byte>> tlvData = response.GetData();
                    foreach ((int tag, ReadOnlyMemory<byte> value) in tlvData)
                    {
                        pages.Add(tag, value);
                    }

                    const int moreDataTag = 0x10;
                    hasMoreData = tlvData.TryGetValue(moreDataTag, out ReadOnlyMemory<byte> hasMoreDataByte)
                        && hasMoreDataByte.Span.Length == 1
                        && hasMoreDataByte.Span[0] == 1;
                }
                else
                {
                    log.LogError("Failed to get device info page-{Page}: {Error} {Message}",
                        page, response.StatusWord, response.StatusMessage);

                    return new YubiKeyDeviceInfo(); // TODO What to return here? Null? Empty? Exception? 
                }
            }

            return YubiKeyDeviceInfo.CreateFromResponseData(pages);
        }
        
        /// <summary>
        /// Attempts to create a dictionary from a TLV-encoded byte array by parsing and extracting tag-value pairs.
        /// </summary>
        /// <param name="tlvData">The byte array containing TLV-encoded data.</param>
        /// <param name="result">When successful, contains a dictionary mapping integer tags to their corresponding values as byte arrays.</param>
        /// <returns>True if the dictionary was successfully created; false otherwise.</returns>
        public static bool TryCreateApduDictionaryFromResponseData(
            ReadOnlyMemory<byte> tlvData, out Dictionary<int, ReadOnlyMemory<byte>> result)
        {
            Logger log = Log.GetLogger();
            result = new Dictionary<int, ReadOnlyMemory<byte>>();

            if (tlvData.IsEmpty)
            {
                log.LogWarning("ResponseAPDU data was empty!");
                return false;
            }

            int tlvDataLength = tlvData.Span[0];
            if (tlvDataLength == 0 || 1 + tlvDataLength > tlvData.Length)
            {
                log.LogWarning("TLV Data length was out of expected ranges. {Length}", tlvDataLength);
                return false;
            }

            var tlvReader = new TlvReader(tlvData.Slice(1, tlvDataLength));
            while (tlvReader.HasData)
            {
                int tag = tlvReader.PeekTag();
                ReadOnlyMemory<byte> value = tlvReader.ReadValue(tag);
                result.Add(tag, value);
            }

            return true;
        }
    }
}
