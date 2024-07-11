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

namespace Yubico.Core
{
    /// <summary>
    /// Compatibility switch names that can be used with `AppContext.SetSwitch` to control breaking behavioral changes
    /// within the `Yubico.Core` layer.
    /// </summary>
    public static class CoreCompatSwitches
    {
        /// <summary>
        /// If set to true, Yubico.Core will attempt to open smart card handles exclusively. False will open shared.
        /// Default is false / shared.
        /// </summary>
        public const string OpenSmartCardHandlesExclusively = nameof(OpenSmartCardHandlesExclusively);
    }
}
