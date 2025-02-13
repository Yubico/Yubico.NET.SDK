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

#pragma warning disable CA1707

namespace Yubico.PlatformInterop
{
    public static class LibcFcntlConstants
    {
        public const int F_GETFL = 3; // Get the file status flags.
        public const int F_SETFL = 4; // Set the file status flags to the value specified by arg.
        public const int O_NONBLOCK = 0x800; // Non-blocking mode flag.
    }
}
