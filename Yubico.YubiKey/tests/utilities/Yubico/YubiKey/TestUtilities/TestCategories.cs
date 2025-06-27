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

namespace Yubico.YubiKey.TestUtilities
{
    public static class TestCategories
    {
        /// <summary>
        /// When touch is required, the user should touch the YubiKey.
        /// It's recommended to use a debug break point at the code where touch is required, so that you're
        /// aware of when touch is about to be expected.
        /// </summary>
        public const string RequiresTouch = "RequiresTouch";

        /// <summary>
        /// These tests are considered to be simple and should not require any special circumstances to run successfully.
        /// </summary>
        public const string Simple = "Simple";

        /// <summary>
        /// These tests require that you run your tests in an elevated session, e.g. 'Run as Administrator' on Windows.
        /// For example, all FIDO tests require an elevated session on Windows.
        /// </summary>
        public const string Elevated = "Elevated";

        /// <summary>
        /// These tests require a Yubikey with biometric capabilities
        /// </summary>
        public const string RequiresBio = "RequiresBio";

        /// <summary>
        /// These tests require certain setup on the Yubikey in order to succeed.
        /// </summary>
        public const string RequiresSetup = "RequiresSetup";

        /// <summary>
        /// These tests may require step debugging to avoid timing issues
        /// </summary>
        public const string RequiresStepDebug = "RequiresStepDebug";

        /// <summary>
        /// These tests require a Yubikey with FIPS
        /// </summary>
        public const string RequiresFips = "RequiresFips";
    }

    public static class TraitTypes
    {
        public const string Category = "Category";
    }
}
