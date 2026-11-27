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

namespace Yubico.YubiKit.WebAuthn.Extensions.Inputs;

/// <summary>
/// Large blob support level for makeCredential.
/// </summary>
public enum LargeBlobSupport
{
    /// <summary>
    /// Large blob support is preferred but not required.
    /// </summary>
    Preferred,

    /// <summary>
    /// Requests required large blob support.
    /// </summary>
    /// <remarks>
    /// The client rejects an authenticator without the supported storage route, but currently does
    /// not fail every registration whose resulting credential lacks blob support. Request a
    /// discoverable credential with <c>ResidentKey = Required</c> and inspect <c>LargeBlob.Supported</c>;
    /// this limitation means the value alone is not a guarantee of usable blob storage.
    /// </remarks>
    Required
}