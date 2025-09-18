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

using System;

namespace Yubico.YubiKey.Cryptography;

public static class IPublicKeyExtensions
{
    /// <summary>
    ///     Casts the given <see cref="IPublicKey" /> to the specified type.
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidCastException"></exception>
    public static T Cast<T>(this IPublicKey key) where T : class, IPublicKey =>
        key as T ?? throw new InvalidCastException($"Cannot cast {key.GetType()} to {typeof(T)}");
}
