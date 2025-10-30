// Copyright (C) 2024 Yubico.
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

using System;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
/// Delegate for encrypting data using the DEK (Data Encryption Key) of a current SCP session.
/// </summary>
/// <param name="data">The data to encrypt.</param>
/// <returns>The encrypted data.</returns>
internal delegate byte[] DataEncryptor(ReadOnlySpan<byte> data);
