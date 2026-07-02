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

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// Determines whether a domain is a public suffix.
/// </summary>
/// <param name="domain">The domain to check.</param>
/// <returns><see langword="true"/> when <paramref name="domain"/> is a public suffix; otherwise, <see langword="false"/>.</returns>
/// <remarks>
/// WebAuthn RP ID validation must reject public suffixes such as <c>com</c> and <c>co.uk</c>.
/// Applications should back this delegate with a Public Suffix List implementation.
/// </remarks>
public delegate bool PublicSuffixChecker(string domain);