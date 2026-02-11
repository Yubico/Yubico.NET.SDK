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

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
/// Constants for xUnit test trait categories.
/// Use with <c>[Trait(TestCategories.Category, TestCategories.RequiresHardware)]</c>.
/// </summary>
/// <remarks>
/// <para><strong>Filter Examples:</strong></para>
/// <code>
/// # Skip tests requiring user interaction (for CI/agents)
/// dotnet build.cs test --filter "Category!=RequiresUserPresence"
/// 
/// # Skip slow tests
/// dotnet build.cs test --filter "Category!=Slow"
/// 
/// # Skip hardware tests
/// dotnet build.cs test --filter "Category!=RequiresHardware"
/// 
/// # Run only unit tests (no hardware, no user presence, not slow)
/// dotnet build.cs test --filter "Category!=RequiresHardware&amp;Category!=RequiresUserPresence&amp;Category!=Slow"
/// </code>
/// </remarks>
public static class TestCategories
{
    /// <summary>
    /// The trait name for test categorization.
    /// </summary>
    public const string Category = "Category";

    /// <summary>
    /// Test requires physical YubiKey hardware to be connected.
    /// </summary>
    /// <remarks>
    /// Use for tests that need a YubiKey device but can run automatically once connected.
    /// </remarks>
    public const string RequiresHardware = "RequiresHardware";

    /// <summary>
    /// Test requires user interaction (insert/remove device, touch, etc.).
    /// </summary>
    /// <remarks>
    /// <para>Use for tests that require manual user actions such as:</para>
    /// <list type="bullet">
    ///   <item>Inserting or removing a YubiKey</item>
    ///   <item>Touching the YubiKey for user presence verification</item>
    ///   <item>Entering a PIN via physical interaction</item>
    /// </list>
    /// <para><strong>AI agents should skip these tests</strong> as they cannot perform physical actions.</para>
    /// </remarks>
    public const string RequiresUserPresence = "RequiresUserPresence";

    /// <summary>
    /// Test is slow (typically &gt;5 seconds execution time).
    /// </summary>
    /// <remarks>
    /// Use for tests with intentional delays (e.g., waiting for device events)
    /// or performance tests with many iterations.
    /// </remarks>
    public const string Slow = "Slow";

    /// <summary>
    /// Test is an integration test that exercises multiple components.
    /// </summary>
    public const string Integration = "Integration";

    /// <summary>
    /// Test requires specific firmware version features.
    /// </summary>
    public const string RequiresFirmware = "RequiresFirmware";
}
