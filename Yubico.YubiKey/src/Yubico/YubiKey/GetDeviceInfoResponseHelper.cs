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
using Microsoft.Extensions.Logging;
using Yubico.Core.Iso7816;
using Yubico.Core.Logging;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey
{
    internal static class GetDeviceInfoResponseHelper
    {
        private static readonly ILogger Logger = Log.GetLogger(typeof(GetDeviceInfoResponseHelper).FullName!);

        /// <summary>
        /// Attempts to create a dictionary from a TLV-encoded byte array by parsing and extracting tag-value pairs.
        /// </summary>
        /// <param name="tlvData">The byte array containing TLV-encoded data.</param>
        /// <returns>A dictionary mapping integer tags to their corresponding values as byte arrays.</returns>
        internal static Dictionary<int, ReadOnlyMemory<byte>>? CreateApduDictionaryFromResponseData(
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
                var value = tlvReader.ReadValue(tag);
                result.Add(tag, value);
            }

            return result;
        }

        internal static Dictionary<int, ReadOnlyMemory<byte>> ParseResponse(
            ResponseApdu responseApdu,
            ResponseStatus status,
            string statusMessage,
            string responseClass)
        {
            if (status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(statusMessage);
            }

            if (responseApdu.Data.Length > 255)
            {
                throw new MalformedYubiKeyResponseException
                {
                    ResponseClass = responseClass,
                    ActualDataLength = responseApdu.Data.Length
                };
            }

            var result = CreateApduDictionaryFromResponseData(responseApdu.Data);
            return result ?? throw new MalformedYubiKeyResponseException
            {
                ResponseClass = responseClass,
            };
        }
    }
}
