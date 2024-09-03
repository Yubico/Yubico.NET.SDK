// Copyright 2024 Yubico AB
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

namespace Yubico.Core.Devices.Hid
{
    public class NullDevice : IHidDevice
    {
        public string Path => string.Empty;
        public string? ParentDeviceId => default;
        public DateTime LastAccessed => default;
        public short VendorId => default;
        public short ProductId => default;
        public short Usage => default;
        public HidUsagePage UsagePage => default;
        public IHidConnection ConnectToFeatureReports() => throw new NotImplementedException();
        public IHidConnection ConnectToIOReports() => throw new NotImplementedException();

        /// <summary>
        /// Creates a default <see cref="IHidDevice"/> with all it's properties set to its default values, indicating a null device.
        /// This might be used in cases where the <see cref="IHidDevice"/> otherwise would be null.
        /// </summary>
        internal static IHidDevice Instance => new NullDevice();
        private NullDevice() { }
    }
}
