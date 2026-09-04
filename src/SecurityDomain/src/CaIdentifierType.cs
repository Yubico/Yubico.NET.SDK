// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.SecurityDomain;

/// <summary>Selects the Security Domain certificate-authority identifier groups to retrieve.</summary>
[Flags]
public enum CaIdentifierType
{
    /// <summary>No identifier group.</summary>
    None = 0,

    /// <summary>Key Loading OCE Certificate identifiers.</summary>
    Kloc = 1,

    /// <summary>Key Loading Card Certificate identifiers.</summary>
    Klcc = 2
}
