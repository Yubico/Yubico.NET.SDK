PROPOSED SAMPLE — no verdict: core-001:unit:4ca6e87f6dd1d2dc / Ctap2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Ctap2/examples/FidoTool/Cli/Output/OutputHelpers.cs:1–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright 2026 Yubico AB
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

using Spectre.Console;
using SharedConfirm = Yubico.YubiKit.Cli.Shared.Output.ConfirmationPrompts;
using SharedOutput = Yubico.YubiKit.Cli.Shared.Output.OutputHelpers;

namespace Yubico.YubiKit.Ctap2.Examples.FidoTool.Cli.Output;

/// <summary>
/// Provides Spectre.Console formatting utilities for consistent CTAP2 tool output.
/// Delegates common methods to the shared CLI library.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:0ead92999bf55101 / Ctap2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Ctap2/examples/FidoTool/FidoExamples/GetAuthenticatorInfo.cs:44–73
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Queries the authenticator for its capabilities and supported features.
    /// </summary>
    /// <param name="yubiKey">The YubiKey device to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticator info result.</returns>
    public static async Task<AuthenticatorInfoResult> QueryAsync(
        IYubiKey yubiKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateCtap2SessionAsync(
                cancellationToken: cancellationToken);

            var info = await session.GetInfoAsync(cancellationToken);

            return AuthenticatorInfoResult.Succeeded(info);
        }
        catch (CtapException ex)
        {
            return AuthenticatorInfoResult.Failed($"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})");
        }
        catch (Exception ex)
        {
            return AuthenticatorInfoResult.Failed($"Failed to query authenticator: {ex.Message}");
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:1bcc18573d7b9903 / Ctap2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Ctap2/examples/FidoTool/FidoTool.csproj:1–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>Yubico.YubiKit.Ctap2.Examples.FidoTool</RootNamespace>
    <AssemblyName>FidoTool</AssemblyName>
    <IsPackable>false</IsPackable>

    <!-- Existing sample exception for CA1852 (sealing internal types); other analyzer policy is unchanged. -->
    <NoWarn>$(NoWarn);CA1852</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="Spectre.Console" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Yubico.YubiKit.Ctap2.csproj" />
    <ProjectReference Include="..\..\..\Core\src\Yubico.YubiKit.Core.csproj" />
    <ProjectReference Include="..\..\..\Management\src\Yubico.YubiKit.Management.csproj" />
    <ProjectReference Include="..\..\..\Cli.Shared\src\Yubico.YubiKit.Cli.Shared.csproj" />
    <ProjectReference Include="..\..\..\Cli.Commands\src\Yubico.YubiKit.Cli.Commands.csproj" />
  </ItemGroup>

</Project>



PROPOSED SAMPLE — no verdict: core-001:unit:b477d6cc126ee4d5 / Ctap2 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Ctap2/PACKAGE_README.md:22–30
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Ctap2 --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.




PROPOSED SAMPLE — no verdict: core-001:unit:1792a8f1c5bb3ba1 / Ctap2 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Ctap2/README.md:40–71
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Getting started

```csharp
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Ctap2.Pin;

IYubiKey device = await YubiKeyManager.FindFirstAsync();
await using var session = await device.CreateCtap2SessionAsync();

var info = await session.GetInfoAsync();
Console.WriteLine($"CTAP versions: {string.Join(", ", info.Versions)}");
```

Later snippets use these directives and the same open `session`. `GetInfoAsync` needs no touch or PIN and
makes a fresh query; it does not refresh the session's construction-time firmware configuration.
Options can describe state: `clientPin: false` means PIN support with no PIN set, not no PIN support.
For a one-shot read, use `device.GetCtap2InfoAsync()` **instead of** opening this session, or after disposing
it. The helper opens/closes its own session and performs an initialization probe plus the requested read.

For decoded encrypted GetInfo metadata, absent `EncIdentifier` and `EncCredStoreState` fields currently
become present zero-length `ReadOnlyMemory<byte>?` values. `HasValue` is therefore not evidence that the
authenticator returned a field or supports the feature; nullable presence/length also cannot distinguish
absence from an encoded empty byte string. Inspect the original CBOR in `info.RawData` for field presence.




PROPOSED SAMPLE — no verdict: core-001:unit:f6d8011c4e3c3669 / Ctap2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/BioEnrollment/BioEnrollmentModelsTests.cs:1–29
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

using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2.BioEnrollment;

namespace Yubico.YubiKit.Ctap2.UnitTests.BioEnrollment;

/// <summary>
/// Tests for BioEnrollment model parsing.
/// </summary>
/// <remarks>
/// Synthetic fixtures check decoded values and selected raw-copy behavior, not sensor
/// enrollment. Flat template maps below reflect the current decoder, not CTAP 2.1's
/// key-7 template array with nested keys 1/2. Status-value assertions preserve current
/// enum mappings, including 0x0B-0x0D names that differ from the standard table. No test
/// here establishes rejection of missing sample fields or independent enrollment completion.
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:e0cd0f858f4da187 / Ctap2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cbor/CtapResponseParserTests.cs:1–22
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

using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2.Cbor;

namespace Yubico.YubiKit.Ctap2.UnitTests.Cbor;

/// <summary>
/// Tests for CtapResponseParser utility methods.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:c4935360f5100eac / Ctap2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Credentials/CredentialResponseTests.cs:1–28
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

using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.Ctap2.UnitTests.Credentials;

/// <summary>
/// Unit tests for MakeCredentialResponse and GetAssertionResponse decoding.
/// </summary>
/// <remarks>
/// Signatures and zero-coordinate public keys are synthetic parsing fixtures, not cryptographic proofs.
/// Missing-descriptor cases pin decoder permissiveness/fallbacks; CTAP 2.0 permits omission only for
/// an exactly singleton request allowList, while CTAP 2.1/2.3 require the descriptor.
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:0eb15437dd1797ab / Ctap2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/PinUvAuthProtocolV2Tests.cs:1–23
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

using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2.Pin;

namespace Yubico.YubiKit.Ctap2.UnitTests.Pin;

/// <summary>
/// Unit tests for <see cref="PinUvAuthProtocolV2"/>.
/// </summary>
/// <remarks>KDF cases check length/determinism, not an independent two-info HKDF vector. AES/MAC cases check local behavior, not device interoperability, token state or buffer clearing.</remarks>

