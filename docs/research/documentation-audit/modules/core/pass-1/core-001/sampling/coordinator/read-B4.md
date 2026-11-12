PROPOSED SAMPLE — no verdict: core-001:unit:ca143a5e93e24ab0 / B4 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/HidDescriptorInfo.cs:35–40
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Device path or identifier.
    /// </summary>
    public string DevicePath { get; init; } = string.Empty;


PROPOSED SAMPLE — no verdict: core-001:unit:786cf2b8db7cd790 / B4 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/HidInterfaceClassifier.cs:59–73
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Gets the expected report communication method for an interface type.
    /// </summary>
    /// <param name="interfaceType">The YubiKey HID interface type.</param>
    /// <returns>The HID report type to use for communication.</returns>
    /// <exception cref="ArgumentException">Thrown when the interface type is not supported.</exception>
    public static HidReportType GetReportType(HidInterfaceType interfaceType) =>
        interfaceType switch
        {
            HidInterfaceType.Fido => HidReportType.InputOutput,
            HidInterfaceType.Otp => HidReportType.Feature,
            _ => throw new ArgumentException($"Unsupported interface type: {interfaceType}", nameof(interfaceType))
        };


PROPOSED SAMPLE — no verdict: core-001:unit:0f12bb2529841ebb / B4 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/HidInterfaceType.cs:1–20
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

namespace Yubico.YubiKit.Core.Transports.Hid;

/// <summary>
/// Represents the type of YubiKey HID interface.
/// This is determined from the HID descriptor UsagePage and Usage combination.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:5edf620e4c9511c1 / B4 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/HidReportDescriptorReader.cs:38–42
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>
    /// Main item type (bits 2-3 of the prefix byte == 0). Examples: Input, Output, Collection.
    /// </summary>
    internal const int TypeMain = 0;


PROPOSED SAMPLE — no verdict: core-001:unit:7dca7d8127ba99c9 / B4 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/HidReportType.cs:1–19
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

namespace Yubico.YubiKit.Core.Transports.Hid;

/// <summary>
/// Type of HID report used for communication.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:a974e10a3feb092e / B4 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Windows/WindowsHidDeviceListener.cs:31–36
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// GUID for the HID device interface class.
    /// </summary>
    private static readonly Guid GuidDevinterfaceHid = new("4D1E55B2-F16F-11CF-88CB-001111000030");


PROPOSED SAMPLE — no verdict: core-001:unit:46d8f841e1e6da66 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardState.cs:58–69
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     <para>
    ///         CurrentState: This flag has no meaning and should not be used.
    ///     </para>
    ///     <para>
    ///         EventState: The given reader name is not recognized by the resource manager. If this
    ///         bit is set, then CHANGED and IGNORE will also be set.
    ///     </para>
    /// </summary>
    UNKNOWN = 0x0004,


PROPOSED SAMPLE — no verdict: core-001:unit:22ae808505eb5d7a / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardState.cs:117–129
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     <para>
    ///         CurrentState: The application expects that the card in the reader is allocated for
    ///         exclusive use by another application. If this bit is set, PRESENT is assumed.
    ///     </para>
    ///     <para>
    ///         EventState: The card in the reader is allocated for exclusive use by another application.
    ///         If this bit is set, PRESENT will also be set.
    ///     </para>
    /// </summary>
    EXCLUSIVE = 0x0080,


PROPOSED SAMPLE — no verdict: core-001:unit:545dad17b3c12727 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/NativeMethods.cs:30–31
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        Local = 0x0,


PROPOSED SAMPLE — no verdict: core-001:unit:a0e16397556b7bfe / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.DeviceProperties.cs:146–147
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
 // 8-bit signed int (SBYTE)
        BYTE = 0x0000_0003,


PROPOSED SAMPLE — no verdict: core-001:unit:1139e69a203ad565 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.DeviceProperties.cs:147–148
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
 // 8-bit unsigned int (BYTE)
        INT16 = 0x0000_0004,


PROPOSED SAMPLE — no verdict: core-001:unit:d35c8e405be647ae / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:38–39
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        CR_INVALID_RES_DES = 0x00000006,


PROPOSED SAMPLE — no verdict: core-001:unit:7905855969fecdb7 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:47–48
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        CR_NO_SUCH_DEVINST = CR_NO_SUCH_DEVNODE,


PROPOSED SAMPLE — no verdict: core-001:unit:c49d22b32d967530 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:70–71
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        CR_NO_MORE_HW_PROFILES = 0x00000023,


PROPOSED SAMPLE — no verdict: core-001:unit:4289a4ab4c854929 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:73–74
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        CR_WRONG_TYPE = 0x00000026,


PROPOSED SAMPLE — no verdict: core-001:unit:82d3022818e7fe85 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:85–86
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
 // NT ONLY
        CR_NO_CM_SERVICES = 0x00000032,


PROPOSED SAMPLE — no verdict: core-001:unit:493cea048ce99ba9 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:89–90
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        CR_DEVICE_INTERFACE_ACTIVE = 0x00000036,


PROPOSED SAMPLE — no verdict: core-001:unit:f8f2e0d56c27796a / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:93–94
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        CR_INVALID_INDEX = 0x0000003A,


PROPOSED SAMPLE — no verdict: core-001:unit:9116749dadfa0cef / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:145–148
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        // Filter type: CM_NOTIFY_FILTER_TYPE.DEVICEHANDLE
        DEVICEQUERYREMOVE = 2,


PROPOSED SAMPLE — no verdict: core-001:unit:980b50873c8be7fa / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:156–158
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        DEVICEINSTANCEREMOVED = 9
    


PROPOSED SAMPLE — no verdict: core-001:unit:681dd8dc4e8e2100 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmDeviceProperty.cs:22–23
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    Class,


PROPOSED SAMPLE — no verdict: core-001:unit:fdbf650298d171d7 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Kernel32/Kernel32.Interop.cs:28–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        NONE = 0x0,


PROPOSED SAMPLE — no verdict: core-001:unit:46ba672a5df97762 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Kernel32/Kernel32.Interop.cs:29–31
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        FILE_READ_DATA = 0x0000_0001,


PROPOSED SAMPLE — no verdict: core-001:unit:bfc6d4ed213120ce / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Kernel32/Kernel32.Interop.cs:68–70
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        SPECIFIC_RIGHTS_ALL = 0x0000_FFFF,


PROPOSED SAMPLE — no verdict: core-001:unit:1aa330d64d67e641 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Kernel32/Kernel32.Interop.cs:77–79
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        GENERIC_READ = unchecked((int)0x8000_0000),


PROPOSED SAMPLE — no verdict: core-001:unit:180329a9026ee41f / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Kernel32/Kernel32.Interop.cs:90–91
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        DELETE = 0x04,


PROPOSED SAMPLE — no verdict: core-001:unit:80fae54950b754d5 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Kernel32/Kernel32.Interop.cs:91–93
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        READWRITE = READ | WRITE,


PROPOSED SAMPLE — no verdict: core-001:unit:4a70b27981c65a50 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Kernel32/Kernel32.Interop.cs:112–113
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        HIDDEN = 0x0000_0002,


PROPOSED SAMPLE — no verdict: core-001:unit:239c09cb444ed505 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Kernel32/Kernel32.Interop.cs:138–139
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        RANDOM_ACCESS = 0x1000_0000,


PROPOSED SAMPLE — no verdict: core-001:unit:84bda4260bec1438 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/HidDeviceChangeKind.cs:21–25
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>
    /// The listener could not classify the HID topology change.
    /// </summary>
    Unknown = 0,


PROPOSED SAMPLE — no verdict: core-001:unit:bcd617d8643400a4 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/HidDeviceChangeKind.cs:25–30
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// A HID interface was added.
    /// </summary>
    Added = 1,


PROPOSED SAMPLE — no verdict: core-001:unit:3c80420b1cc74cf3 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/HidInterfaceType.cs:22–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>
    /// Unknown or unsupported HID interface type.
    /// </summary>
    Unknown = 0,


PROPOSED SAMPLE — no verdict: core-001:unit:aaea5963c3250eb8 / B4 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Keyboard/KeyboardLayout.cs:24–25
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    it_IT = 5,


PROPOSED SAMPLE — no verdict: core-001:unit:f41cccf6549a6c22 / B4 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:Directory.Packages.props:1–50
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
<Project>
  <PropertyGroup>
    <!-- Enable Central Package Management -->
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <!-- SDK Version - ALL packages ship with same version -->
    <YubiKitVersion>2.0.0-alpha.2</YubiKitVersion>
  </PropertyGroup>
  <ItemGroup>
    <!-- Microsoft Dependencies -->
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="10.0.2" />
    <PackageVersion Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.2" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="10.0.2" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.2" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Console" Version="10.0.2" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.2" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.2" />
    <PackageVersion Include="Microsoft.Extensions.Configuration" Version="10.0.2" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.2" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="10.0.2" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="10.0.2" />
    <!-- Third-party Dependencies -->
    <PackageVersion Include="System.Formats.Cbor" Version="10.0.15" />
    <PackageVersion Include="Yubico.NativeShims" Version="1.18.1" />
    <!-- Test Dependencies -->
    <PackageVersion Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.0.0" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.v3" Version="3.0.0" />
    <PackageVersion Include="xunit.v3.extensibility.core" Version="3.0.0" />
    <PackageVersion Include="xunit.core" Version="2.9.3" />
    <PackageVersion Include="xunit.abstractions" Version="2.0.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.4" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="FluentAssertions" Version="7.0.0" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
    <PackageVersion Include="Xunit.SkippableFact" Version="1.5.23" />
    <!-- CLI / Console Tools -->
    <PackageVersion Include="Spectre.Console" Version="0.49.1" />
    <PackageVersion Include="Spectre.Console.Testing" Version="0.49.1" />
    <PackageVersion Include="Spectre.Console.Cli" Version="0.49.1" />
    <!-- Build Tools -->
    <PackageVersion Include="BenchmarkDotNet" Version="0.15.8" />
    <PackageVersion Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0" />
    <PackageVersion Include="Microsoft.CodeAnalysis.PublicApiAnalyzers" Version="5.6.0" />
    <PackageVersion Include="Bullseye" Version="5.0.0" />
    <PackageVersion Include="SimpleExec" Version="12.0.0" />
    <!-- Used by crap.cs for source-level cyclomatic complexity. Not referenced by any shipping project. -->
    <PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="5.6.0" />
  </ItemGroup>
</Project>



PROPOSED SAMPLE — no verdict: core-001:unit:c24dad64c24dc498 / B4 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/Context.cs:18–19
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public required bool IncludeIntegration { get; init; }


PROPOSED SAMPLE — no verdict: core-001:unit:b260d67910547400 / B4 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/Conventions.cs:6–7
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public const string SolutionFile = "Yubico.YubiKit.sln";


PROPOSED SAMPLE — no verdict: core-001:unit:150f9ba47db6abe7 / B4 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/Conventions.cs:18–19
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public static string CoverageDir(string repoRoot) => Path.Combine(ArtifactsDir(repoRoot), "coverage");


PROPOSED SAMPLE — no verdict: core-001:unit:bb8e7043825fc0c6 / B4 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/Conventions.cs:19–20
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public static string DefaultNugetFeedPath(string repoRoot) => Path.Combine(ArtifactsDir(repoRoot), "nuget-feed");


PROPOSED SAMPLE — no verdict: core-001:unit:c16852000f9d8ea6 / B4 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/CoverageTargets.cs:214–217
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    static bool IsSettingTrue(string? value) =>
        string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);


PROPOSED SAMPLE — no verdict: core-001:unit:eceda43174a27efc / B4 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/Output.cs:24–25
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public static void PrintWarn(string message) => PrintColored($"{WarnMarker} {message}", ConsoleColor.Yellow);


PROPOSED SAMPLE — no verdict: core-001:unit:9ea1794bce816ec8 / B4 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/Output.cs:94–203
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static void PrintHelp(ToolchainContext ctx)
    {
        Console.WriteLine("""

            Yubico.YubiKit Toolchain Script
            ================================

            .NET 10 build automation script using Bullseye task runner.

            USAGE:
              dotnet toolchain.cs [target]
              dotnet toolchain.cs -- [target] [options]   (required before script long options)

             NOTE: The -- separator passes arguments to the script instead of dotnet.
                   It is required before every script long option, including --project:
               dotnet toolchain.cs -- --help
               dotnet toolchain.cs -- build --project Piv

             With no target the script prints a short usage summary; it does not run `default`.

            TARGETS:
            """);

        PrintTargets();

        Console.WriteLine("\nOPTIONS:");
        var optionRows = Options.All
            .Select(o => (Usage: o.Value is null ? o.Name : $"{o.Name} {o.Value}", o.Description))
            .Append((Usage: "-h, --help", Description: "Show this help message"))
            .ToArray();
        var usageWidth = optionRows.Max(r => r.Usage.Length) + 2;
        foreach (var (usage, description) in optionRows)
            Console.WriteLine($"  {usage.PadRight(usageWidth)}{description}");

        Console.WriteLine("""

            EXAMPLES:
              dotnet toolchain.cs build
              dotnet toolchain.cs -- build --project Piv
              dotnet toolchain.cs test
              dotnet toolchain.cs -- test --project Piv --filter "Method~Sign"
              dotnet toolchain.cs -- test --project WebAuthn.UnitTests --filter "FullyQualifiedName~YESDK1619|FullyQualifiedName~YESDK1613"
              dotnet toolchain.cs -- test --integration --project Piv --smoke
              dotnet toolchain.cs -- resilience --fast
              dotnet toolchain.cs docs-qa
              dotnet toolchain.cs coverage
              dotnet toolchain.cs -- complexity --complexity-args "--module Piv"
              dotnet toolchain.cs -- benchmark --benchmark-args "--list flat"
              dotnet toolchain.cs -- publish --package-version 1.0.0-preview.1
              dotnet toolchain.cs -- publish-remote --dry-run --nuget-feed-url https://nuget.pkg.github.com/Yubico/index.json --nuget-api-key fake

            FILTER SYNTAX (for --filter):
              FullyQualifiedName~MyClass     Tests containing 'MyClass' in full name
              Method~Sign                    Test methods containing 'Sign'
              Name=MyTestMethod              Exact test method name
              ClassName~Integration          Classes containing 'Integration'
              Category=Slow                  Tests with [Trait("Category", "Slow")]
              Category!=RequiresHardware     Exclude tests with that trait
              A&B                            All terms must match
              A|B                            Any term matches (alternatives of one kind; xUnit v3
                                             projects reject mixing & and |)

              Categories: RequiresHardware, RequiresUserPresence, Slow, Integration, RuntimeResilience.
              AI agents: always exclude RequiresUserPresence (cannot insert, remove or touch a device).

            TEST RUNNERS:
              The script detects each test project's runner: xUnit v3 projects (UseMicrosoftTestingPlatformRunner)
              run with `dotnet run` and xUnit v3 filter options, xUnit v2 projects with `dotnet test --filter`.
              --filter is translated for both, so always test through this script, not `dotnet test`.

              To run a unit project by hand, use `dotnet run --project <proj>` or its apphost, not
              `dotnet <proj>.dll`: Core's probes re-launch the test host from the entry assembly and fail
              spuriously under `dotnet X.dll`.

            READING THE OUTPUT (AI agents and CI):
              The run ends with a RESULT block. Read it, not the lines above it:
                RESULT: PASSED | FAILED   the outcome; the exit code is 0 only for PASSED (2: usage error)
                Targets:                  each target that ran, its duration and its error
                Projects (<target>):      each project once: ✓ PASS / ✗ FAIL / ○ NO-TESTS and why;
                                          under a failure, its failing tests with messages and its log
                Totals: ...               project and per-test totals
                Warnings: <n>             compiler/MSBuild warnings per step, by code (not shown above)
                Next: <command>           re-runs only what failed
                Run log: <path>           where everything is saved
              The first line of a run names its log, so a run cut short (a timeout) can still be read.
              `dotnet toolchain.cs -- last` reprints the newest RESULT block without running anything.
              While running, lines start with fixed markers: "▶ RUN" (a command starts, with
              "output: <log>" for its full output), "✓ PASS", "✗ FAIL", "○ NO-TESTS" (nothing executed:
              the filter matched nothing or every test was skipped; this fails the test target),
              "! WARN".
              Build and test output is not shown (only error lines); use --verbose to stream it.
              Project names omit the Yubico.YubiKit. prefix.

            RUN LOGS:
              Every run that executes a target is saved under artifacts/toolchain/runs/<time>-<targets>/:
              run.log (all console output), one log per build/test step, and result.json (the RESULT
              block as JSON). artifacts/toolchain/latest/ is the newest run (latest.txt names it where
              symlinks are unavailable). The 5 newest runs are kept; `clean` does not remove them.
            """);

        PrintColored($"Discovered {ctx.PackableProjects.Length} packable projects:", ConsoleColor.Cyan);
        PrintProjectList(ctx.PackableProjects);

        PrintColored($"\nDiscovered {ctx.TestProjects.Length} test projects:", ConsoleColor.Cyan);
        PrintProjectList(ctx.TestProjects.Select(p => p.Path));

        Console.WriteLine("\nSee TOOLCHAIN.md for full documentation.");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:03846cc18a211676 / B4 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/TargetRegistry.cs:17–23
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static void Define(string name, string description, string[] dependsOn, Action action)
    {
        s_targets.Add(new TargetInfo(name, description, dependsOn));
        Bullseye.Targets.Target(name, description, dependsOn, () => RunLog.RunTarget(name, action));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:4e2fa00807d432b0 / B4 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/TargetRegistry.cs:23–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static void Define(string name, string description, string[] dependsOn)
    {
        s_targets.Add(new TargetInfo(name, description, dependsOn));
        Bullseye.Targets.Target(name, description, dependsOn);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:062735c80dd15210 / B4 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/Usage.cs:11–15
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // Commands answered without running a target, so they must not be silenced or saved.
    public static readonly string[] ListingOptions =
        ["--list-dependencies", "-D", "--list-inputs", "-I", "--list-targets", "-T", "--list-tree", "-t"];


PROPOSED SAMPLE — no verdict: core-001:unit:e75f6865eb06248a / B4 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/HidInterfaceClassifier.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public static class HidInterfaceClassifier
{


PROPOSED SAMPLE — no verdict: core-001:unit:31498b7a3f6f5d32 / B4 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/HidReportDescriptorReader.cs:26–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal readonly record struct HidReportItem(int Type, int Tag, uint Value);


PROPOSED SAMPLE — no verdict: core-001:unit:626db51eb3663093 / B4 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Windows/WindowsHidDeviceListener.cs:46–47
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private bool _disposed;


PROPOSED SAMPLE — no verdict: core-001:unit:476e60c2f787e69d / B4 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Windows/WindowsHidIOReportConnection.cs:21–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly IWindowsHidReportAccess _reportAccess;


PROPOSED SAMPLE — no verdict: core-001:unit:2ae3f73734ec22cd / B4 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Windows/WindowsHidIOReportConnection.cs:23–28
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal WindowsHidIOReportConnection(string path)
        : this(new WindowsHidReportAccess(path))
    {
    }


PROPOSED SAMPLE — no verdict: core-001:unit:8764b168af334bfe / B4 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Windows/WindowsHidInterface.cs:44–78
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static IReadOnlyList<IHidInterface> GetList()
    {
        var devices = new List<IHidInterface>();

        foreach (var interfacePath in CmDevice.GetDevicePaths(CmInterfaceGuid.Hid, null))
        {
            if (string.IsNullOrEmpty(interfacePath))
                continue;

            try
            {
                var descriptorInfo = GetDescriptorInfo(interfacePath);

                if (descriptorInfo.VendorId == HidConstants.YubicoVendorId &&
                    HidInterfaceClassifier.IsSupported(descriptorInfo))
                {
                    devices.Add(new WindowsHidInterface(descriptorInfo));
                }
            }
            catch (PlatformApiException ex)
            {
                Logger.LogDebug(ex, "Skipping HID interface {InterfacePath}: ConfigMgr property read failed", interfacePath);
                // Ignore inaccessible or transient HID interfaces; one bad device must not abort discovery.
            }
            catch (NotSupportedException ex)
            {
                Logger.LogDebug(ex, "Skipping HID interface {InterfacePath}: unsupported or malformed ConfigMgr HID properties", interfacePath);
                // Ignore HID interfaces with unsupported/malformed ConfigMgr properties.
            }
        }

        return devices;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:001c58b1d1862d76 / B4 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:CLAUDE.md:175–230
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
#### Use `readonly struct` (PREFERRED for value types)

**✅ When:**
- Type is ≤16 bytes (2 machine words)
- Immutable data (no fields change after construction)
- Passed by value frequently
- Used in hot paths (APDU processing, protocol handling)

**✅ Benefits:**
- Prevents defensive copies
- Clear immutability contract
- Potential stack allocation
- No GC pressure
```csharp
// ✅ BEST - Small, immutable, value semantics
public readonly struct StatusWord
{
    public StatusWord(byte sw1, byte sw2) => (SW1, SW2) = (sw1, sw2);
    
    public byte SW1 { get; }
    public byte SW2 { get; }
    public ushort Value => (ushort)((SW1 << 8) | SW2);
    
    public bool IsSuccess => Value == 0x9000;
}

// ✅ GOOD - Wraps small data
public readonly struct SlotNumber
{
    public SlotNumber(byte value) => Value = value;
    public byte Value { get; }
}

// ✅ GOOD - Protocol metadata
public readonly struct ApduHeader
{
    public ApduHeader(byte cla, byte ins, byte p1, byte p2)
    {
        CLA = cla;
        INS = ins;
        P1 = p1;
        P2 = p2;
    }
    
    public byte CLA { get; }
    public byte INS { get; }
    public byte P1 { get; }
    public byte P2 { get; }
}
```

**❌ Don't use for:**
- Types >16 bytes (expensive copying)
- Mutable data
- Types stored in collections (boxing overhead)




PROPOSED SAMPLE — no verdict: core-001:unit:a9e434874ba55d57 / B4 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/v1-to-v2-comparison.md:25–41
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Deliberate design decisions

These aren't regressions, and it'd be misleading to describe them as such.
V1 made almost everything public, which meant almost every internal
improvement became a breaking change. V2 keeps low-level primitives internal
so the SDK can keep moving.

| V1 capability | V2 status | Why |
|---|---|---|
| `TlvReader`/`TlvWriter` typed, sequential TLV parsing | Replaced by a thinner `Tlv`/`TlvHelper`/`DisposableTlvList` surface — tag/value containers and static helpers only | V1's public typed TLV API was a broad extensibility surface; every change to it was a breaking change |
| `Base16`/`Base32`/`Bcd`/`ModHex` standalone codecs | The full codec surface is not restored; public `Core.Utilities.Base32` provides decoding only | The small shared decoder supports OATH input without restoring the v1 encoding framework |
| Pluggable crypto primitives (`IAesGcmPrimitives`, `IEcdhPrimitives`, `ICmacPrimitives`) | No longer public extension points | Same reasoning; AES-CMAC is now hardcoded internal to SCP code |

If your code used the removed APIs directly, you'll need an alternative. For Base32 decoding, use
[`Yubico.YubiKit.Core.Utilities.Base32`](../src/Core/README.md#decode-base32-text); it does not provide encoding
or replacements for the other codecs.




PROPOSED SAMPLE — no verdict: core-001:unit:c800fb08d134d876 / B4 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/v1-to-v2-comparison.md:127–157
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Already fixed

Flagged as missing on 2026-07-21 and shipped since — most in the 2026-07-30
changelog entry, the YubiHSM Auth password change later in the alpha series.
The gaps table above already accounts for these:

- PIV PIN-only (`PinProtected`) management-key mode, plus typed CHUID, CCC,
  AdminData, and KeyHistory data objects.
- OATH `IsPasswordProtected` and `AuthenticateAndRetryAsync`, plus a
  dedicated `OathException`.
- The cross-applet `IUserPresencePrompt` touch contract, replacing the alpha-only
  PIV and YubiHSM Auth `OnTouchRequired` callbacks and covering FIDO2/WebAuthn,
  OATH, OpenPGP, and YubiOTP as well.
- YubiHSM Auth's `HsmAuthRetryException` for enrollment, deletion, management-key changes, CALCULATE and
  password changes, plus the `Counter` → `RetriesRemaining` rename. `GetChallengeAsync` uses general APDU
  errors; applications own retry decisions for both paths.
- YubiHSM Auth password inputs are back to UTF-8 `ReadOnlyMemory<byte>`, as in
  v1, so callers can clear them after use. Its parameters are named plainly
  (`credentialPassword`, `derivationPassword`, `currentPassword`,
  and `newPassword`). FIDO2, OpenPGP, and OATH secret parameters
  were renamed the same way, which breaks named-argument call sites. Current PIV
  parameters also use plain role names such as `pin`, `currentPin` and `currentPuk`.
- YubiOTP keyboard-layout-aware static passwords and Yubico-OTP-algorithm
  challenge-response.
- Dedicated exception types for SecurityDomain (`SecureChannelException`) and
  OpenPGP (`OpenPgpInvalidPinException`).

One caveat: v1's PIN-*derived* management-key mode (as distinct from
PIN-*protected*) is still deprecated and can't be newly enabled in v2, though
v2 can detect and recover an existing PIN-derived configuration.




PROPOSED SAMPLE — no verdict: core-001:unit:a146f9d1cba78f0d / B4 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/README.md:643–661
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Related

For repeatable `FindAllAsync`/`CachedDeviceInfo` hardware validation, use the
[targeted read-only discovery checks](../../docs/usage/device-discovery.md#read-only-scoped-discovery-hardware-checks).
They cover every attached authorized serial-bearing USB key, use sequential connections only, and keep the
live-read Management dependency in its integration project. Scope exclusion and disjoint ownership limits
are checked deterministically rather than by racing native hardware connections.

- Application modules: [Management](../Management/README.md), [PIV](../Piv/README.md),
  [CTAP2](../Ctap2/README.md), [WebAuthn](../WebAuthn/README.md), [OATH](../Oath/README.md),
  [YubiOTP](../YubiOtp/README.md), [OpenPGP](../OpenPgp/README.md),
  [Security Domain](../SecurityDomain/README.md), [YubiHSM Auth](../YubiHsm/README.md).
- [Physical device model](../../docs/architecture/physical-device-model.md): one `IYubiKey` per key, transport selection, ownership.
- [Device discovery guarantees](../../docs/architecture/device-discovery-guarantees.md): grouping guarantees per platform.
- [Raw access tiers](../../docs/architecture/raw-access-tiers.md): applet sessions, raw sessions, raw connections.
- [Device discovery](../../docs/usage/device-discovery.md), [User interaction](../../docs/usage/user-interaction.md),
  and [TLV processing](../../docs/usage/tlv-processing.md).
- [Logging](../../docs/LOGGING.md): configure `YubiKitLogging.LoggerFactory` once at startup.
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.



PROPOSED SAMPLE — no verdict: core-001:unit:fd66aba03a4df30b / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCard.Interop.cs:48–55
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [LibraryImport(Libraries.NativeShims, EntryPoint = "Native_SCardEndTransaction")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial uint SCardEndTransaction(
        SCardCardHandle cardHandle,
        SCARD_DISPOSITION disposition
    );


PROPOSED SAMPLE — no verdict: core-001:unit:1b2598d5c8018603 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardCardHandle.cs:1–21
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

using Microsoft.Win32.SafeHandles;

namespace Yubico.YubiKit.Core.Native.Desktop.SCard;

/// <summary>
///     A safe-handle wrapper for the SCard card handle.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:9714306e3122ebf3 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardCardHandle.cs:35–38
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    protected override bool ReleaseHandle() =>
        NativeMethods.SCardDisconnect(handle, ReleaseDisposition) == ErrorCode.SCARD_S_SUCCESS;


PROPOSED SAMPLE — no verdict: core-001:unit:4f6e767f4c54a490 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardContext.cs:1–21
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

using Microsoft.Win32.SafeHandles;

namespace Yubico.YubiKit.Core.Native.Desktop.SCard;

/// <summary>
///     A safe-handle wrapper for the SCard context handle.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:43f70d8ce8b06967 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardError.cs:82–87
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     The data buffer for returned data is too small for the returned data.
    /// </summary>
    public const uint SCARD_E_INSUFFICIENT_BUFFER = 0x80100008;


PROPOSED SAMPLE — no verdict: core-001:unit:c9ca0b9368626f3c / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardError.cs:102–107
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     One or more of the supplied parameters could not be properly interpreted.
    /// </summary>
    public const uint SCARD_E_INVALID_PARAMETER = 0x80100004;


PROPOSED SAMPLE — no verdict: core-001:unit:33e8ed5c0350a62d / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardError.cs:147–152
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     No smart card reader is available.
    /// </summary>
    public const uint SCARD_E_NO_READERS_AVAILABLE = 0x8010002E;


PROPOSED SAMPLE — no verdict: core-001:unit:aa0de53f1f164811 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardError.cs:213–218
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     The smart card resource manager has shut down.
    /// </summary>
    public const uint SCARD_E_SERVICE_STOPPED = 0x8010001E;


PROPOSED SAMPLE — no verdict: core-001:unit:e338723f893fc4dc / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardError.cs:283–288
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     The operation has been aborted to allow the server application to exit.
    /// </summary>
    public const uint SCARD_P_SHUTDOWN = 0x80100018;


PROPOSED SAMPLE — no verdict: core-001:unit:779f4d2fba09f19d / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardError.cs:339–344
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Access was denied because of a security violation.
    /// </summary>
    public const uint SCARD_W_SECURITY_VIOLATION = 0x8010006A;


PROPOSED SAMPLE — no verdict: core-001:unit:bcf29106aefd5221 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardError.cs:344–350
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Power has been removed from the smart card, so that further communication is not
    ///     possible.
    /// </summary>
    public const uint SCARD_W_UNPOWERED_CARD = 0x80100067;


PROPOSED SAMPLE — no verdict: core-001:unit:cc2d794921aed56e / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Desktop/SCard/SCardReaderStateExtensions.cs:56–59
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static int GetCurrentSequence(this in SCARD_READER_STATE state) =>
        (int)((state.CurrentState & SequenceMask) >> 16);


PROPOSED SAMPLE — no verdict: core-001:unit:ab9aa77d46bb36ce / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/SafeLibraryHandle.cs:26–27
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed class SafeWindowsLibraryHandle : SafeLibraryHandle
{


PROPOSED SAMPLE — no verdict: core-001:unit:d3c6bc7e580d7979 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/SafeLibraryHandle.cs:27–28
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    protected override bool ReleaseHandle() => NativeMethods.FreeLibrary(handle);


PROPOSED SAMPLE — no verdict: core-001:unit:6437aa1c1acf75e5 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.DeviceProperties.cs:41–44
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
 // DEVPROP_TYPE.STRING

    internal static readonly DEVPROPKEY DEVPKEY_Device_ClassGuid =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 10);


PROPOSED SAMPLE — no verdict: core-001:unit:7040d06e9ab75822 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:172–173
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    internal const int OffsetFlags = 4;


PROPOSED SAMPLE — no verdict: core-001:unit:3bc18bc281447f54 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:173–174
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    internal const int OffsetFilterType = 8;


PROPOSED SAMPLE — no verdict: core-001:unit:61cbe3c150122640 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:178–179
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    internal const int OffsetGuidData4 = 24;


PROPOSED SAMPLE — no verdict: core-001:unit:7044ceb286f08690 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:180–182
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = CmNotifyFilterSize)]



PROPOSED SAMPLE — no verdict: core-001:unit:13a340e298116ce6 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs:186–187
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        internal CM_NOTIFY_FILTER_TYPE FilterType;


PROPOSED SAMPLE — no verdict: core-001:unit:4645c4f382dcc83c / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmClassGuid.cs:80–83
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static readonly Guid Firmware = new(0xf2e7dd72, 0x6468, 0x4e36, 0xb6, 0xf1, 0x64, 0x88, 0xf4, 0x2c, 0x1b,
        0x52);


PROPOSED SAMPLE — no verdict: core-001:unit:a976a2de7d4e2112 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmClassGuid.cs:83–86
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static readonly Guid FloppyDisk = new(0x4d36e980, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1,
        0x03, 0x18);


PROPOSED SAMPLE — no verdict: core-001:unit:0508aa95b2692f54 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmClassGuid.cs:89–92
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static readonly Guid Hdc = new(0x4d36e96a, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03,
        0x18);


PROPOSED SAMPLE — no verdict: core-001:unit:ee41995193a31bce / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmClassGuid.cs:113–116
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static readonly Guid Media = new(0x4d36e96c, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03,
        0x18);


PROPOSED SAMPLE — no verdict: core-001:unit:79ce1f718c08e8a6 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmClassGuid.cs:140–143
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static readonly Guid Net = new(0x4d36e972, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03,
        0x18);


PROPOSED SAMPLE — no verdict: core-001:unit:34b950df7557a08a / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmClassGuid.cs:143–146
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static readonly Guid NetClient = new(0x4d36e973, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1,
        0x03, 0x18);


PROPOSED SAMPLE — no verdict: core-001:unit:3096532c5be176ee / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmClassGuid.cs:185–188
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static readonly Guid ScmVolume = new(0x53ccb149, 0xe543, 0x4c84, 0xb6, 0xe0, 0xbc, 0xe4, 0xf6, 0xb7,
        0xe8, 0x06);


PROPOSED SAMPLE — no verdict: core-001:unit:e54524d6a143f40d / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmClassGuid.cs:191–194
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static readonly Guid SecurityAccelerator =
        new(0x268c95a1, 0xedfe, 0x11d3, 0x95, 0xc3, 0x00, 0x10, 0xdc, 0x40, 0x50, 0xa5);


PROPOSED SAMPLE — no verdict: core-001:unit:d326d43993bfb623 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmClassGuid.cs:224–227
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static readonly Guid Unknown = new(0x4d36e97e, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03,
        0x18);


PROPOSED SAMPLE — no verdict: core-001:unit:2560815de4900918 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmClassGuid.cs:233–236
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static readonly Guid Volume = new(0x71a27cdd, 0x812a, 0x11d0, 0xbe, 0xc7, 0x08, 0x00, 0x2b, 0xe2, 0x09,
        0x2f);


PROPOSED SAMPLE — no verdict: core-001:unit:56b38190860b305d / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmClassGuid.cs:236–239
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static readonly Guid VolumeSnapshot =
        new(0x533c5b84, 0xec70, 0x11d2, 0x95, 0x05, 0x00, 0xc0, 0x4f, 0x79, 0xde, 0xaf);


PROPOSED SAMPLE — no verdict: core-001:unit:73668e7a40023989 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Cfgmgr32/CmDevice.cs:58–59
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public string? InterfacePath { get; private set; }


PROPOSED SAMPLE — no verdict: core-001:unit:a994aae01d0bcb2b / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Interop.Libraries.cs:18–19
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    internal const string CfgMgr = "Cfgmgr32.dll";


PROPOSED SAMPLE — no verdict: core-001:unit:545ed185c93bd746 / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Kernel32/Kernel32.Interop.cs:21–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal static partial class NativeMethods
{


PROPOSED SAMPLE — no verdict: core-001:unit:9ed0b72d7d260d4f / B4 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Windows/Kernel32/Kernel32.Interop.cs:166–178
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    //SYSLIB1092: The usage of 'LibraryImportAttribute' does not follow recommendations. It is recommended to use explicit '[In]' and '[Out]' attributes on array parameters.
    [LibraryImport(Libraries.Kernel32, EntryPoint = "WriteFile", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool WriteFile(
        SafeFileHandle handle,
        [In] byte[] lpBuffer,
        int numBytesToWrite,
        out int numBytesWritten,
        IntPtr mustBeZero
    );


PROPOSED SAMPLE — no verdict: core-001:unit:de225fee64cd6c24 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/IntegrationTestBase.cs:21–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;


PROPOSED SAMPLE — no verdict: core-001:unit:616c3c83bbf106ae / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Transports/Hid/HidDeviceListenerIntegrationTests.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public class HidDeviceListenerIntegrationTests
{


PROPOSED SAMPLE — no verdict: core-001:unit:71c994106bebfb92 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/AdapterContractRegistry.cs:7–7
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed record AdapterContractRow(string Route, string Operation, string Owner, string Symbol, AdapterContractProfile[] Profiles);


PROPOSED SAMPLE — no verdict: core-001:unit:d397e0fce7ff2058 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/AdapterContractRegistry.cs:9–10
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal static class AdapterContractRegistry
{


PROPOSED SAMPLE — no verdict: core-001:unit:28491c24d4088184 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/AdapterContractRegistry.cs:129–141
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static void ValidateProfile(AdapterContractRow row, AdapterContractProfile profile, string[]? required,
        HashSet<string> roles, HashSet<string> seenProfiles, Assembly tests, List<string> errors)
    {
        if (!roles.Add(profile.Role))
            errors.Add($"duplicate role: {row.Route}/{row.Operation}/{profile.Role}");
        if (!seenProfiles.Add(profile.Method))
            errors.Add($"duplicate profile: {profile.Method}");
        if (required is null || !required.Contains(profile.Role))
            errors.Add($"unexpected role: {row.Route}/{row.Operation}/{profile.Role}");
        ValidateTestMethod(profile.Method, tests, errors);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:626bda583b1870ab / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/AdapterContractRegistryTests.cs:11–30
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void Migrated_routes_have_resolved_nonempty_contract_profiles()
    {
        var rows = Load();
        // Independent acceptance totals, not values derived from the manifest under test:
        // baseline 27 operations / 49 profile links + Linux 16 operations / 27 links.
        // Linux: FIDO 4/10, OTP 4/9, direct input 4/4, direct feature 4/4 (operations/links).
        Assert.Equal(43, rows.Length);
        Assert.Equal(76, rows.Sum(row => row.Profiles.Length));
        Assert.Empty(AdapterContractRegistry.Validate(rows));
        Assert.Equal(4, rows.Count(row => row.Route == "macOS FIDO"));
        Assert.Equal(4, rows.Count(row => row.Route == "macOS OTP"));
        Assert.Equal(5, rows.Count(row => row.Route == "portable PCSC"));
        Assert.Equal(4, rows.Count(row => row.Route == "macOS direct input (sync)"));
        Assert.Equal(4, rows.Count(row => row.Route == "macOS direct feature (sync)"));
        Assert.Equal(2, rows.Count(row => row.Route == "macOS listener (sync)"));
        Assert.Equal(4, rows.Count(row => row.Route == "portable PCSC (sync)"));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:307f00bc683ddfb8 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/BoundaryInventoryTests.cs:84–100
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void Baseline_rejects_duplicate_and_non_native_family_waiver()
    {
        var sites = BoundaryScanner.Scan([("Fixture.cs", Fixture)]);
        var native = sites.Single(site => site.Kind == "native-import");
        var entry = "Native#" + BoundaryManifest.Fingerprint(native);
        var family = new BoundaryFamily("Fixture.cs", "Core maintainers", "Native import at caller thread",
            ["Fixture.cs native declaration"], ["documented", "outstanding"], [entry, entry]);
        Assert.Contains(BoundaryManifest.Validate([native], new BoundaryManifest(1, [family])),
            error => error.StartsWith("duplicate:", StringComparison.Ordinal));
        var wait = sites.First(site => site.Kind == "blocking-wait");
        Assert.Contains(BoundaryManifest.Validate([wait], new BoundaryManifest(1,
            [family with { Entries = ["ReadAsync#" + BoundaryManifest.Fingerprint(wait)] }])),
            error => error.Contains("non-native", StringComparison.Ordinal));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:10b1c56e936a716a / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/BoundaryInventoryTests.cs:121–143
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void Callback_conversion_native_delegate_and_unmanaged_address_are_separate_sites()
    {
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            using System.Runtime.CompilerServices;
            unsafe class Hooks {
                delegate void Handler();
                [DllImport("example")] static extern void Register(Handler callback);
                [UnmanagedCallersOnly] static void OnNative() { }
                static void OnManaged() { }
                static void Setup() { Register(OnManaged); Marshal.GetFunctionPointerForDelegate((Handler)OnManaged); delegate* unmanaged<void> ptr = &OnNative; }
            }
            """;
        var sites = BoundaryScanner.Scan([("Hooks.cs", source)]);
        Assert.Contains(sites, site => site.Kind == "callback-registration" && site.Target.Contains("Hooks.Register", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "callback-conversion" && site.Target.Contains("GetFunctionPointerForDelegate", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "callback-address" && site.Target.Contains("Hooks.OnNative", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "callback-registration" && site.Owner.Contains("Hooks.Setup", StringComparison.Ordinal));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:109e638492d171d0 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/BoundaryManifest.cs:6–7
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed record BoundaryRow(string Id, string Kind, string Owner, string Execution, string ReviewOwner,
    string Justification, string[] Evidence, string[] Status);


PROPOSED SAMPLE — no verdict: core-001:unit:ad8ea188f8abea31 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/BoundaryScanner.cs:22–33
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal static string CoreSourceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "toolchain.cs")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found"), "src", "Core", "src");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:8b6b35d0d7b58375 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/MigrationDiagnosticsTests.cs:28–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static CancellationToken Ct => TestContext.Current.CancellationToken;


PROPOSED SAMPLE — no verdict: core-001:unit:159b9708889ef3d4 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/MigrationDiagnosticsTests.cs:110–123
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static string[] LogInvocationsInSource(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        return [.. tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax member &&
                member.Name.Identifier.ValueText.StartsWith("Log", StringComparison.Ordinal))
            .Select(invocation => invocation.ArgumentList.Arguments
                .Select(argument => argument.Expression)
                .OfType<LiteralExpressionSyntax>()
                .Single(literal => literal.Kind() == SyntaxKind.StringLiteralExpression)
                .Token.ValueText)];
    }


PROPOSED SAMPLE — no verdict: core-001:unit:af095e43ddd71540 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/MigrationDiagnosticsTests.cs:123–170
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PcscOpenAndTransactionEnd_LogMetadataButNotCommand(bool asyncEnd)
    {
        const string marker = "ISC56-command-PIN-key-payload";
        byte[] command = Encoding.ASCII.GetBytes(marker);
        var api = new ControlledSCardConnectionApi { EndTransactionResult = ErrorCode.SCARD_E_NOT_TRANSACTED };
        api.ReleaseTransmit.Set();
        using var provider = new RecordingProvider();
        using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(provider));
        using var temporary = YubiKitLogging.UseTemporary(factory);

        try
        {
            var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
            try
            {
                ReadOnlyMemory<byte> response = await connection.TransmitAndReceiveAsync(command, Ct);
                Assert.Equal(new byte[] { 0x90, 0x00 }, response.ToArray());
                IDisposable transaction = asyncEnd
                    ? await connection.BeginTransactionAsync(Ct)
                    : connection.BeginTransaction(Ct);
                if (asyncEnd)
                    await ((IAsyncDisposable)transaction).DisposeAsync();
                else
                    transaction.Dispose();
            }
            finally
            {
                _ = await Assert.ThrowsAsync<SCardException>(() => connection.DisposeAsync().AsTask());
            }

            string[] events = provider.Events;
            Assert.Contains(events, entry => entry.Contains("Initializing smart card connection to reader", StringComparison.Ordinal));
            Assert.Contains(events, entry => entry.Contains("Smart card connection initialized to reader", StringComparison.Ordinal));
            Assert.Contains(events, entry => entry.Contains("SCardEndTransaction returned", StringComparison.Ordinal));
            Assert.Equal(1, api.TransmitCalls);
            Assert.Equal(1, api.EndTransactionCalls);
            AssertNoPayload(events, command);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(command);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:a6156549268d7731 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/MigrationDiagnosticsTests.cs:325–326
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class FailingOtpConnection(byte[] response, string resetError) : IOtpHidConnection
    {


PROPOSED SAMPLE — no verdict: core-001:unit:c445c590b4c5c140 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/MigrationDiagnosticsTests.cs:329–330
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int FeatureReportSize => 8;


PROPOSED SAMPLE — no verdict: core-001:unit:c9a2776ed3a43232 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/MigrationDiagnosticsTests.cs:358–360
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void Dispose() { }


PROPOSED SAMPLE — no verdict: core-001:unit:11388c9856d1a295 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/MigrationDiagnosticsTests.cs:416–417
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;


PROPOSED SAMPLE — no verdict: core-001:unit:707b6889c0be9c0f / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/MigrationDiagnosticsTests.cs:423–424
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(_events);


PROPOSED SAMPLE — no verdict: core-001:unit:ffcc8b1ef6333aee / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/PublicSyncBoundaryRegistry.cs:10–37
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    // Independent of the rows: removing an entry cannot silently reduce the supported scope.
    private static readonly (string Entry, string PublicType, string PublicSymbol, string Owner, string Symbol)[] Required =
    [
        ("linux input open", "Yubico.YubiKit.Core.Transports.Hid.IHidInterface", "ConnectToIOReports", "Yubico.YubiKit.Core.Transports.Hid.Linux.LinuxHidInterface", "ConnectToIOReports"),
        ("linux input get", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "GetReport", "Yubico.YubiKit.Core.Transports.Hid.Linux.LinuxHidIOReportConnection", "GetReport"),
        ("linux input set", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "SetReport", "Yubico.YubiKit.Core.Transports.Hid.Linux.LinuxHidIOReportConnection", "SetReport"),
        ("linux input dispose", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.Hid.Linux.LinuxHidIOReportConnection", "Dispose"),
        ("linux feature open", "Yubico.YubiKit.Core.Transports.Hid.IHidInterface", "ConnectToFeatureReports", "Yubico.YubiKit.Core.Transports.Hid.Linux.LinuxHidInterface", "ConnectToFeatureReports"),
        ("linux feature get", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "GetReport", "Yubico.YubiKit.Core.Transports.Hid.Linux.LinuxHidFeatureReportConnection", "GetReport"),
        ("linux feature set", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "SetReport", "Yubico.YubiKit.Core.Transports.Hid.Linux.LinuxHidFeatureReportConnection", "SetReport"),
        ("linux feature dispose", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.Hid.Linux.LinuxHidFeatureReportConnection", "Dispose"),
        ("input open", "Yubico.YubiKit.Core.Transports.Hid.IHidInterface", "ConnectToIOReports", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidInterface", "ConnectToIOReports"),
        ("input get", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "GetReport", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", "GetReport"),
        ("input set", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "SetReport", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", "SetReport"),
        ("input dispose", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", "Dispose"),
        ("feature open", "Yubico.YubiKit.Core.Transports.Hid.IHidInterface", "ConnectToFeatureReports", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidInterface", "ConnectToFeatureReports"),
        ("feature get", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "GetReport", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", "GetReport"),
        ("feature set", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "SetReport", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", "SetReport"),
        ("feature dispose", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", "Dispose"),
        ("smart begin", "Yubico.YubiKit.Core.Transports.SmartCard.ISmartCardConnection", "BeginTransaction", "Yubico.YubiKit.Core.Transports.SmartCard.UsbSmartCardConnection", "BeginTransaction"),
        ("smart async default", "Yubico.YubiKit.Core.Transports.SmartCard.ISmartCardConnection", "BeginTransactionAsync", "Yubico.YubiKit.Core.Transports.SmartCard.ISmartCardConnection", "BeginTransactionAsync"),
        ("smart scope end", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.SmartCard.PcscConnectionNativeState+TransactionScope", "Dispose"),
        ("smart dispose", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.SmartCard.UsbSmartCardConnection", "Dispose"),
        ("listener start", "Yubico.YubiKit.Core.Transports.Hid.HidDeviceListener", "Start", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidDeviceListener", "Start"),
        ("listener stop", "Yubico.YubiKit.Core.Transports.Hid.HidDeviceListener", "Stop", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidDeviceListener", "Stop"),
        ("HID scan dispatch", "Yubico.YubiKit.Core.Transports.Hid.FindHidInterfaces", "FindAllAsync", "Yubico.YubiKit.Core.Transports.Hid.FindHidInterfaces", "FindAllAsync")
    ];


PROPOSED SAMPLE — no verdict: core-001:unit:8d9236eb1f3ca11c / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/ResponsivenessProbeTests.cs:11–33
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    [Fact]
    public async Task SynchronousExpertMacOpen_BlocksOnlyItsCallerWhileWorkerOpens()
    {
        using var probe = new WithheldNativeProbe();
        using var native = new BlockingOtpLifetime(probe) { HoldOpen = true };
        // The direct report constructor is synchronous by contract; native open belongs to the worker.
        probe.Start(() => Task.FromResult(new MacOSHidFeatureReportConnection(42, native)));
        try
        {
            Assert.True(native.OpenEntered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.NotEqual(probe.CallerThread, native.OpenThread);
            Assert.False(probe.Returned.IsSet);
        }
        finally
        {
            probe.Release.Set();
            // Drain and dispose a late-opened connection even when the assertion fails.
            var opening = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            using var connection = await ((Task<MacOSHidFeatureReportConnection>)opening)
                .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:3a6730a0f15ef1e3 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/ResponsivenessProbeTests.cs:188–202
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task BuiltInMacOtpReceive_ReturnsWhileNativeGetIsWithheld()
    {
        using var probe = new WithheldNativeProbe();
        using var native = new BlockingOtpLifetime(probe) { HoldGet = true };
        await using var connection = await MacOSOtpHidConnection.OpenAsync(42, native, TestContext.Current.CancellationToken);
        probe.Start(() => connection.ReceiveAsync(TestContext.Current.CancellationToken));

        try { probe.AssertResponsive(); }
        finally { await probe.ReleaseAndDrainAsync(); }
        var receiving = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(8, (await ((Task<ReadOnlyMemory<byte>>)receiving).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Length);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:51ef719c3b27b0d5 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/ResponsivenessProbeTests.cs:232–234
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int GetFeatureReport(nint device, byte[] buffer, ref long length)
        { probe.EnterNative(); length = 8; return 0; }


PROPOSED SAMPLE — no verdict: core-001:unit:53f0fbdc4617a3ea / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/ResponsivenessProbeTests.cs:252–253
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int WaitShutdown(nint owner) => 0;


PROPOSED SAMPLE — no verdict: core-001:unit:bc1447a0b92049be / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/WithheldNativeProbe.cs:9–10
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private Thread? _caller;


PROPOSED SAMPLE — no verdict: core-001:unit:c3a64e08bb8351f2 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/WithheldNativeProbe.cs:12–13
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private int _callerThread;


PROPOSED SAMPLE — no verdict: core-001:unit:1c31fe1fabe2193c / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/WithheldNativeProbe.cs:15–16
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private long _nativeEnteredAt;


PROPOSED SAMPLE — no verdict: core-001:unit:029aa538df8f8024 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/WithheldNativeProbe.cs:52–64
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal void AssertResponsive()
    {
        Assert.True(_entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
            "native operation was never entered");
        Assert.NotEqual(CallerThread, Volatile.Read(ref _nativeThread));
        Assert.True(Returned.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
            "invocation did not return before native release");
        Assert.False(Release.IsSet);
        Assert.NotNull(_operation);
        Assert.False(_operation.IsCompleted, "operation completed while native work was still held");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:091e43e130b7ade2 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/RuntimeResilience/RuntimeResilienceStaticScanTests.cs:139–164
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void Scanner_FlagsSleepBeforeReadyToWritePoll()
    {
        const string source = """
            private async Task AwaitReadyToWriteAsync(CancellationToken cancellationToken)
            {
                while (stopwatch.ElapsedMilliseconds < timeLimitMs)
                {
                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);

                    var report = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
                    if ((report.Span[OtpConstants.FeatureReportDataSize] & OtpConstants.SlotWriteFlag) == 0)
                    {
                        return;
                    }
                }
            }
            """;

        var finding = Assert.Single(RuntimeResilienceScanner.Scan("OldOtpHidProtocol.cs", source));

        Assert.Equal("SleepBeforeReadyToWritePoll", finding.Category);
        Assert.Equal(5, finding.LineNumber);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:a40eee27cf7e20f4 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/WindowsHidConnectionOpenFailureTests.cs:72–73
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public bool WasDisposed { get; private set; }


PROPOSED SAMPLE — no verdict: core-001:unit:ecaaac968d427796 / B4 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/WindowsHidConnectionOpenFailureTests.cs:73–75
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public string DevicePath => @"\\?\fake-hid-path";


PROPOSED SAMPLE — no verdict: core-001:unit:79e58d98b2c6b23c / B4 / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/Conventions.cs:4–5
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
static class Conventions
{


PROPOSED SAMPLE — no verdict: core-001:unit:b7006f3e6dc237e1 / B4 / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/DocsTargets.cs:6–7
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
static class DocsTargets
{
