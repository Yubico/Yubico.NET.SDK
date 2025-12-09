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

namespace Yubico.YubiKit.Core.Cryptography;


/// <summary>
/// Extension methods for <see cref="IPrivateKey"/> to provide type-safe casting operations.
/// </summary>
public static class IPrivateKeyExtensions
{
    /// <summary>
    /// Safely casts an <see cref="IPrivateKey"/> instance to a specific derived type.
    /// </summary>
    /// <typeparam name="T">The target type that implements <see cref="IPrivateKey"/>.</typeparam>
    /// <param name="key">The private key instance to cast.</param>
    /// <returns>The private key cast to the specified type <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidCastException">
    /// Thrown when the <paramref name="key"/> cannot be cast to type <typeparamref name="T"/>.
    /// The exception message includes both the source and target type names for debugging.
    /// </exception>
    /// <example>
    /// <code>
    /// IPrivateKey genericKey = GetPrivateKey();
    /// RsaPrivateKey rsaKey = genericKey.Cast&lt;RsaPrivateKey&gt;();
    /// // throws InvalidCastException if genericKey is not an RsaPrivateKey
    /// </code>
    /// </example>
    /// <remarks>
    /// This method provides a more explicit alternative to direct casting with clearer error messages.
    /// Prefer this over unsafe casting operations when type safety is critical for cryptographic operations.
    /// </remarks>
    public static T Cast<T>(this IPrivateKey key) where T : class, IPrivateKey =>
        key as T ?? throw new InvalidCastException($"Cannot cast {key.GetType()} to {typeof(T)}");
}
