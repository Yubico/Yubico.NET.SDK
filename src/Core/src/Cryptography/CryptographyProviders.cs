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

namespace Yubico.YubiKit.Core.Cryptography
{
    /// <summary>
    /// This class contains properties that specify cryptographic providers.
    /// </summary>
    /// <remarks>
    /// These delegates configure the call sites that explicitly use them. Other SDK paths call
    /// runtime APIs directly, including EC providers, HKDF, HMAC, and static hash methods.
    /// Configure public delegates once at startup, before concurrent SDK use. Replacement affects
    /// subsequent delegate calls, rather than existing objects or every operation of that algorithm.
    /// <para>
    /// The properties in this class are delegates. They are functions that can
    /// build objects that will perform the required crypto. The reason these
    /// properties are methods that build objects, rather than objects
    /// themselves, is because the crypto classes implement <c>IDisposable</c>.
    /// In order to avoid the complex problem of ownership of an object that will
    /// be disposed once it goes out of scope, the SDK will create a new object
    /// each time one is needed. This new object will be in scope only for the
    /// duration of its use in the SDK, and will be disposed immediately when the
    /// SDK is done with it.
    /// </para>
    /// <para>
    /// Each replacement must return a fresh compatible object whose ownership transfers to the
    /// invoker. Dispose objects obtained by directly invoking a creator in application code.
    /// Disposal releases that object's resources; caller-owned arrays and other copies need their
    /// own cleanup. Internal ARKG, ECDH, and CMAC creators are SDK seams rather than public extension points.
    /// </para>
    /// <para>
    /// Most applications will not replace the defaults, but for those that want
    /// the SDK to use a hardware RNG, hardware accelerator, or any other
    /// specific implementation for whatever reason, it is possible.
    /// </para>
    /// </remarks>
    public static class CryptographyProviders
    {
        /// <summary>
        /// This property is a factory delegate. The method assigned
        /// will return an instance of <c>RandomNumberGenerator</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation calls this creator for random numbers it will generate them
        /// using an implementation of the
        /// <c>System.Security.Cryptography.RandomNumberGenerator</c> abstract
        /// class. However, when it needs the RNG class, it will ask this
        /// delegate to build an object, rather than ask for an object itself.
        /// This has to do with the fact that the <c>RandomNumberGenerator</c>
        /// class implements <c>IDisposable</c>. In order to avoid the complex
        /// problem of ownership of an object that will be disposed once it goes
        /// out of scope, the SDK will create a new object each time one is
        /// needed. This new object will be in scope only for the duration of its
        /// use in the SDK, and will be disposed immediately when the SDK is done
        /// with it.
        /// <para>
        /// The method loaded will return an object. This class is initialized
        /// with a method that will build and return an instance of the C#
        /// default implementation. For example, it could be used as follows.
        /// <code language="csharp">
        ///   using (RandomNumberGenerator randomObject = CryptographyProviders.RngCreator())
        ///   {
        ///       // Use the RNG within this disposal scope.
        ///   }
        /// </code>
        /// </para>
        /// <para>
        /// This startup fragment explicitly selects the runtime default; supply a compatible
        /// factory instead when replacing it.
        /// <code language="csharp">
        ///     CryptographyProviders.RngCreator = RandomNumberGenerator.Create;
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<RandomNumberGenerator> RngCreator { get; set; } = RandomNumberGenerator.Create;

        /// <summary>
        /// This property is a factory delegate. The method assigned
        /// will return an instance of <c>SHA1</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation calls this creator to digest data using SHA-1, it will do so
        /// using an implementation of the
        /// <c>System.Security.Cryptography.SHA1</c> abstract class. However,
        /// when it needs the SHA1 class, it will ask this delegate to build an
        /// object, rather than ask for an object itself. This has to do with the
        /// fact that the <c>SHA1</c> class implements <c>IDisposable</c>. In
        /// order to avoid the complex problem of ownership of an object that
        /// will be disposed once it goes out of scope, the SDK will create a new
        /// object each time one is needed. This new object will be in scope only
        /// for the duration of its use in the SDK, and will be disposed
        /// immediately when the SDK is done with it.
        /// <para>
        /// The method loaded will return an object. This class is initialized
        /// with a method that will build and return an instance of the C#
        /// default implementation. For example, it could be used as follows.
        /// <code language="csharp">
        ///   using (SHA1 sha1Object = CryptographyProviders.Sha1Creator())
        ///   {
        ///       // Use the hash object within this disposal scope.
        ///   }
        /// </code>
        /// </para>
        /// <para>
        /// This startup fragment selects the runtime default. A replacement factory must
        /// return a fresh compatible object on each invocation.
        /// <code language="csharp">
        ///     CryptographyProviders.Sha1Creator = SHA1.Create; // Runtime default.
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<SHA1> Sha1Creator { get; set; } = SHA1.Create;

        /// <summary>
        /// This property is a factory delegate. The method assigned
        /// will return an instance of <c>SHA256</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation calls this creator to digest data using SHA-256, it will do
        /// so using an implementation of the
        /// <c>System.Security.Cryptography.SHA256</c> abstract class. However,
        /// when it needs the SHA256 class, it will ask this delegate to build an
        /// object, rather than ask for an object itself. This has to do with the
        /// fact that the <c>SHA256</c> class implements <c>IDisposable</c>. In
        /// order to avoid the complex problem of ownership of an object that
        /// will be disposed once it goes out of scope, the SDK will create a new
        /// object each time one is needed. This new object will be in scope only
        /// for the duration of its use in the SDK, and will be disposed
        /// immediately when the SDK is done with it.
        /// <para>
        /// The method loaded will return an object. This class is initialized
        /// with a method that will build and return an instance of the C#
        /// default implementation. For example, it could be used as follows.
        /// <code language="csharp">
        ///   using (SHA256 sha256Object = CryptographyProviders.Sha256Creator())
        ///   {
        ///       // Use the hash object within this disposal scope.
        ///   }
        /// </code>
        /// </para>
        /// <para>
        /// This startup fragment selects the runtime default. A replacement factory must
        /// return a fresh compatible object on each invocation.
        /// <code language="csharp">
        ///     CryptographyProviders.Sha256Creator = SHA256.Create; // Runtime default.
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<SHA256> Sha256Creator { get; set; } = SHA256.Create;

        /// <summary>
        /// This property is a factory delegate. The method assigned
        /// will return an instance of <c>SHA384</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation calls this creator to digest data using SHA-384, it will do
        /// so using an implementation of the
        /// <c>System.Security.Cryptography.SHA384</c> abstract class. However,
        /// when it needs the SHA384 class, it will ask this delegate to build an
        /// object, rather than ask for an object itself. This has to do with the
        /// fact that the <c>SHA384</c> class implements <c>IDisposable</c>. In
        /// order to avoid the complex problem of ownership of an object that
        /// will be disposed once it goes out of scope, the SDK will create a new
        /// object each time one is needed. This new object will be in scope only
        /// for the duration of its use in the SDK, and will be disposed
        /// immediately when the SDK is done with it.
        /// <para>
        /// The method loaded will return an object. This class is initialized
        /// with a method that will build and return an instance of the C#
        /// default implementation. For example, it could be used as follows.
        /// <code language="csharp">
        ///   using (SHA384 sha384Object = CryptographyProviders.Sha384Creator())
        ///   {
        ///       // Use the hash object within this disposal scope.
        ///   }
        /// </code>
        /// </para>
        /// <para>
        /// This startup fragment selects the runtime default. A replacement factory must
        /// return a fresh compatible object on each invocation.
        /// <code language="csharp">
        ///     CryptographyProviders.Sha384Creator = SHA384.Create; // Runtime default.
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<SHA384> Sha384Creator { get; set; } = SHA384.Create;

        /// <summary>
        /// This property is a factory delegate. The method assigned
        /// will return an instance of <c>SHA512</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation calls this creator to digest data using SHA-512, it will do
        /// so using an implementation of the
        /// <c>System.Security.Cryptography.SHA512</c> abstract class. However,
        /// when it needs the SHA512 class, it will ask this delegate to build an
        /// object, rather than ask for an object itself. This has to do with the
        /// fact that the <c>SHA512</c> class implements <c>IDisposable</c>. In
        /// order to avoid the complex problem of ownership of an object that
        /// will be disposed once it goes out of scope, the SDK will create a new
        /// object each time one is needed. This new object will be in scope only
        /// for the duration of its use in the SDK, and will be disposed
        /// immediately when the SDK is done with it.
        /// <para>
        /// The method loaded will return an object. This class is initialized
        /// with a method that will build and return an instance of the C#
        /// default implementation. For example, it could be used as follows.
        /// <code language="csharp">
        ///   using (SHA512 sha512Object = CryptographyProviders.Sha512Creator())
        ///   {
        ///       // Use the hash object within this disposal scope.
        ///   }
        /// </code>
        /// </para>
        /// <para>
        /// This startup fragment selects the runtime default. A replacement factory must
        /// return a fresh compatible object on each invocation.
        /// <code language="csharp">
        ///     CryptographyProviders.Sha512Creator = SHA512.Create; // Runtime default.
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<SHA512> Sha512Creator { get; set; } = SHA512.Create;

        /// <summary>
        /// This property is a factory delegate. The method assigned
        /// will return an instance of <c>Aes</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation calls this creator to encrypt or decrypt data using
        /// AES, it will do so using an implementation of the
        /// <c>System.Security.Cryptography.Aes</c> abstract class.
        /// However, when it needs the AES class, it will ask this delegate to
        /// build an object, rather than ask for an object itself. This has to do
        /// with the fact that the <c>Aes</c> class implements
        /// <c>IDisposable</c>. In order to avoid the complex problem of
        /// ownership of an object that will be disposed once it goes out of
        /// scope, the SDK will create a new object each time one is needed. This
        /// new object will be in scope only for the duration of its use in the
        /// SDK, and will be disposed immediately when the SDK is done with it.
        /// <para>
        /// The method loaded will return an object. This class is initialized
        /// with a method that will build and return an instance of the C#
        /// default implementation. For example, it could be used as follows.
        /// <code language="csharp">
        ///   using (Aes aesObject = CryptographyProviders.AesCreator())
        ///   {
        ///       // Use the cipher within this disposal scope.
        ///   }
        /// </code>
        /// </para>
        /// <para>
        /// This startup fragment selects the runtime default. A replacement factory must
        /// return a fresh compatible object on each invocation.
        /// <code language="csharp">
        ///     CryptographyProviders.AesCreator = Aes.Create; // Runtime default.
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<Aes> AesCreator { get; set; } = Aes.Create;

        /// <summary>
        /// This property is a factory delegate. The method assigned
        /// will return an instance of ARKG-P256 cryptographic primitives.
        /// </summary>
        /// <remarks>
        /// When an SDK operation needs to perform ARKG-P256 cryptographic primitives
        /// (point validation, ECDH, key derivation), it will do so using the
        /// internal IArkgPrimitives interface. However, when it needs an instance,
        /// it will ask this delegate to build an object, rather than build it itself.
        /// <para>
        /// The method loaded will return an object. This class is initialized
        /// with a method that will build and return an instance of the default
        /// OpenSSL-backed implementation.
        /// </para>
        /// <para>
        /// This creator is internal. Applications cannot replace it through the public SDK API.
        /// </para>
        /// </remarks>
        internal static Func<IArkgPrimitives> ArkgPrimitivesCreator { get; set; } = ArkgPrimitives.Create;

        internal static Func<IEcdhPrimitives> EcdhPrimitivesCreator { get; set; } = EcdhPrimitives.Create;

        internal static Func<CmacBlockCipherAlgorithm, ICmacPrimitives> CmacPrimitivesCreator { get; set; } =
            CmacPrimitives.Create;

        // /// <summary>
        // /// This property is a delegate (function pointer). This method will return
        // /// an instance of <c>HMAC</c>.
        // /// </summary>
        // public static Func<string, HMAC> HmacCreator { get; set; } = HMAC.Create;
    }
}