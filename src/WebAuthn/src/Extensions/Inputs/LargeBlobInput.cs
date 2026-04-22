// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.WebAuthn.Extensions.Inputs;

/// <summary>
/// Large blob support level for registration.
/// </summary>
public enum LargeBlobSupport
{
    /// <summary>
    /// Large blob support is required. Credential creation fails if not supported.
    /// </summary>
    Required,

    /// <summary>
    /// Large blob support is preferred but not required.
    /// </summary>
    Preferred
}

/// <summary>
/// Input for the largeBlob extension during registration or authentication.
/// </summary>
/// <param name="Support">The required support level.</param>
public sealed record class LargeBlobInput(LargeBlobSupport Support);
