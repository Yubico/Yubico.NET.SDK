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

namespace Yubico.YubiKey
{
    /// <summary>
    /// Reports the version number of the Image Processor. This is a chip
    /// specifically used to process the fingerprint images.
    /// </summary>
    public class ImageProcessorVersion : FirmwareVersion
    {
        /// <summary>
        /// Creates an instance of <see cref="ImageProcessorVersion"/> with a
        /// version of 0.0.0.
        /// </summary>
        public ImageProcessorVersion()
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ImageProcessorVersion"/> with a
        /// version of major.minor.patch. The minor and patch args have default
        /// values of 0.
        /// </summary>
        public ImageProcessorVersion(byte major, byte minor = 0, byte patch = 0)
            : base(major, minor, patch)
        {
        }
    }
}
