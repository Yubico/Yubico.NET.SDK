<# Copyright 2021 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. #>

function Build-YubiKeySdkProject
{
    $ProjectPaths = @{
        "CoreRef" = "./Yubico.Core/ref/Yubico.Core.Ref.csproj"
        "CoreSrc" = "./Yubico.Core/src/Yubico.Core.csproj"
        "YubiKey" = "./Yubico.YubiKey/src/Yubico.YubiKey.csproj"
    }

    $CurrentConfiguration = "Debug"

    # Test for presence of .NET tooling
    if ((Test-DotNetCliPresent) -eq $false)
    {
        Write-Error "The dotnet CLI was not found. You can install all prerequisites using Install-YubiKeySdkTools."
    }

    Build-Project $ProjectPaths["CoreRef"] $CurrentConfiguration
    Build-Project $ProjectPaths["CoreSrc"] $CurrentConfiguration
    Build-Project $ProjectPaths["YubiKey"] $CurrentConfiguration
}

function Test-YubiKeySdkProject
{
    $CurrentConfiguration = "Debug"

    $ProjectPaths = @{
        "Core" = "./Yubico.Core/tests/Yubico.Core.UnitTests.csproj"
        "YubiKey" = "./Yubico.YubiKey/tests/Yubico.YubiKey.UnitTests.csproj"
    }

    # We should test core for each RID that we build for
    Test-Project $ProjectPaths["Core"] $CurrentConfiguration "win-x64"
    Test-Project $ProjectPaths["Core"] $CurrentConfiguration "osx-x64"

    Test-Project $ProjectPaths["YubiKey"] $CurrentConfiguration
}

function Clear-YubiKeySdkCache
{
    Get-ChildItem bin -Recurse | Remove-Item -Recurse -Force -Confirm:$false
    Get-ChildItem obj -Recurse | Remove-Item -Recurse -Force -Confirm:$false
    & dotnet nuget locals -c all
}

function Test-DotNetCliPresent
{
    return [bool](Get-Command dotnet -ErrorAction Ignore)
}

function Build-Project($ProjectPath, $Configuration)
{
    Write-Host "Building" $ProjectPath
    & dotnet build $ProjectPath --configuration $Configuration --nologo --verbosity minimal
}

function Test-Project($ProjectPath, $Configuration, $Runtime = $null)
{
    Write-Host "Running test for" $ProjectPath
    if ($Runtime -eq $null)
    {
        & dotnet test $ProjectPath --configuration $Configuration --nologo --verbosity minimal
    }
    else
    {
        & dotnet test $ProjectPath --configuration $Configuration --runtime $Runtime --nologo --verbosity minimal
    }
}
