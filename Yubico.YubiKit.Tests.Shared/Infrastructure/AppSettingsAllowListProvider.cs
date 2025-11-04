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

using Microsoft.Extensions.Configuration;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Provides allowed serial numbers from appsettings.json configuration.
/// </summary>
/// <remarks>
///     <para>
///     Expected configuration structure:
///     <code>
///     {
///       "YubiKeyTests": {
///         "AllowedSerialNumbers": [12345678, 87654321],
///         "EnableHardFail": true
///       }
///     }
///     </code>
///     </para>
/// </remarks>
public class AppSettingsAllowListProvider : IAllowListProvider
{
    private const string ConfigSection = "YubiKeyTests";
    private const string AllowedSerialsKey = "AllowedSerialNumbers";

    private readonly IConfiguration _configuration;

    /// <summary>
    ///     Initializes a new instance of <see cref="AppSettingsAllowListProvider"/>.
    /// </summary>
    /// <param name="configuration">
    ///     The configuration instance. If null, attempts to load from appsettings.json in current directory.
    /// </param>
    public AppSettingsAllowListProvider(IConfiguration? configuration = null)
    {
        _configuration = configuration ?? BuildDefaultConfiguration();
    }

    /// <summary>
    ///     Gets the list of allowed serial numbers from configuration.
    /// </summary>
    /// <returns>List of allowed serial numbers, or empty list if not configured.</returns>
    public IReadOnlyList<int> GetList()
    {
        try
        {
            var serials = _configuration
                .GetSection(ConfigSection)
                .GetSection(AllowedSerialsKey)
                .Get<int[]>();

            return serials ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    /// <summary>
    ///     Gets error message when allow list is not configured.
    /// </summary>
    public string OnInvalidInputErrorMessage()
    {
        return """
               ╔═══════════════════════════════════════════════════════════════════════════════╗
               ║                        CRITICAL: ALLOW LIST NOT CONFIGURED                    ║
               ╚═══════════════════════════════════════════════════════════════════════════════╝

               Integration tests CANNOT run without an allow list of test device serial numbers.
               This is a safety mechanism to prevent tests from running on production YubiKeys.

               To configure the allow list:

               1. Create or edit appsettings.json in the integration test project:

                  {
                    "YubiKeyTests": {
                      "AllowedSerialNumbers": [
                        12345678,
                        87654321
                      ],
                      "EnableHardFail": true
                    }
                  }

               2. Add ONLY test device serial numbers (NEVER production keys!)

               3. Mark appsettings.json to copy to output directory in .csproj:

                  <ItemGroup>
                    <None Update="appsettings.json">
                      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
                    </None>
                  </ItemGroup>

               WARNING: Only use YubiKeys dedicated for testing. Integration tests perform
                        destructive operations (reset, key changes, etc.) that WILL ERASE DATA.

               ═══════════════════════════════════════════════════════════════════════════════
               """;
    }

    /// <summary>
    ///     Gets error message when a device is not in the allow list.
    /// </summary>
    public string OnNotAllowedErrorMessage(int serialNumber)
    {
        return $$"""
                ╔═══════════════════════════════════════════════════════════════════════════════╗
                ║                  CRITICAL: DEVICE NOT AUTHORIZED FOR TESTING                  ║
                ╚═══════════════════════════════════════════════════════════════════════════════╝

                Device with serial number {{serialNumber}} is NOT in the allow list.

                This safety mechanism prevents integration tests from running on unauthorized
                YubiKeys (especially production keys).

                To authorize this device for testing:

                1. VERIFY this is a TEST device (not a production key!)

                2. Add the serial number to appsettings.json:

                   {
                     "YubiKeyTests": {
                       "AllowedSerialNumbers": [
                         {{serialNumber}}
                       ]
                     }
                   }

                3. Re-run the tests

                WARNING: Only add test devices! Integration tests perform destructive operations
                         (reset, credential changes, etc.) that WILL ERASE ALL DATA on the device.

                ═══════════════════════════════════════════════════════════════════════════════
                """;
    }

    /// <summary>
    ///     Builds default configuration from appsettings.json in current directory.
    /// </summary>
    private static IConfiguration BuildDefaultConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();
    }
}
