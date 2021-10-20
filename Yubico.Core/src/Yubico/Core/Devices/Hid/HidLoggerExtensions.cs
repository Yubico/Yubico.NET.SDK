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

using Yubico.Core.Logging;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid
{
    internal static class HidLoggerExtensions
    {
        public static void IOKitApiCall(this Logger logger, string apiName, kern_return_t result)
        {
            if (result == kern_return_t.KERN_SUCCESS)
            {
                logger.LogInformation("{APIName} called successfully.", apiName);
            }
            else
            {
                logger.LogError("{APIName} called and FAILED. kern_return_t = {Result}", apiName, result);
            }
        }
    }
}
