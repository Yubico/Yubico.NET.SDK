// Copyright 2021 Yubico AB
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
using System.Security.Cryptography;
using Yubico.Core.Cryptography;

namespace Yubico.YubiKey.Cryptography
{
    /// <summary>
    /// This class contains properties that specify cryptographic providers.
    /// </summary>
    /// <remarks>
    /// During the course of operations, the SDK will need to perform
    /// cryptographic operations, such as random number generation, HMAC,
    /// Triple-DES encryption, and so on. Any SDK operation that needs crypto
    /// will get it from this class.
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
    /// This class will return default implementations, but can be replaced. That
    /// is, if you do nothing, the SDK will use default C# cryptography. If you
    /// want the SDK to use your own implementations, you can do so. See the
    /// User's Manual entry on
    /// <xref href="UsersManualAlternateCrypto"> alternate crypto implementations</xref>
    /// for a detailed description of replacing the defaults. Generally, you will
    /// simply need to set the appropriate property with a new function.
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
        /// This property is a delegate (function pointer). The method loaded
        /// will return an instance of <c>RandomNumberGenerator</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation needs random numbers it will generate them
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
        ///   RandomNumberGenerator randomObject =
        ///   CryptographyProviders.RngCreator();
        /// </code>
        /// </para>
        /// <para>
        /// If you want to replace the implementation, you will likely do
        /// something like this in your application.
        /// <code language="csharp">
        ///     CryptographyProviders.RngCreator = () =>
        ///     {
        ///         Handle rngHandle = RngImpl.GetRngHandle();
        ///         return RngImpl.GetRandomObject(rngHandle);
        ///     };
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<RandomNumberGenerator> RngCreator { get; set; } = RandomNumberGenerator.Create;

        /// <summary>
        /// This property is a delegate (function pointer). The method loaded
        /// will return an instance of <c>SHA1</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation needs to digest data using SHA-1, it will do so
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
        ///   SHA1 sha1Object = CryptographyProviders.Sha1Creator();
        /// </code>
        /// </para>
        /// <para>
        /// If you want to replace the implementation, you will likely do
        /// something like this in your application.
        /// <code language="csharp">
        ///     CryptographyProviders.Sha1Creator = () =>
        ///     {
        ///         Handle sha1Handle = Sha1Impl.GetSha1Handle();
        ///         return Sha1Impl.GetSha1Object(sha1Handle);
        ///     };
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<SHA1> Sha1Creator { get; set; } = SHA1.Create;

        /// <summary>
        /// This property is a delegate (function pointer). The method loaded
        /// will return an instance of <c>SHA256</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation needs to digest data using SHA-256, it will do
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
        ///   SHA256 sha256Object = CryptographyProviders.Sha256Creator();
        /// </code>
        /// </para>
        /// <para>
        /// If you want to replace the implementation, you will likely do
        /// something like this in your application.
        /// <code language="csharp">
        ///     CryptographyProviders.Sha256Creator = () =>
        ///     {
        ///         Handle sha256Handle = Sha256Impl.GetSha256Handle();
        ///         return Sha256Impl.GetSha256Object(sha256Handle);
        ///     };
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<SHA256> Sha256Creator { get; set; } = SHA256.Create;

        /// <summary>
        /// This property is a delegate (function pointer). The method loaded
        /// will return an instance of <c>SHA384</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation needs to digest data using SHA-384, it will do
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
        ///   SHA384 sha384Object = CryptographyProviders.Sha384Creator();
        /// </code>
        /// </para>
        /// <para>
        /// If you want to replace the implementation, you will likely do
        /// something like this in your application.
        /// <code language="csharp">
        ///     CryptographyProviders.Sha384Creator = () =>
        ///     {
        ///         Handle sha384Handle = Sha384Impl.GetSha384Handle();
        ///         return Sha384Impl.GetSha384Object(sha384Handle);
        ///     };
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<SHA384> Sha384Creator { get; set; } = SHA384.Create;

        /// <summary>
        /// This property is a delegate (function pointer). The method loaded
        /// will return an instance of <c>SHA512</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation needs to digest data using SHA-512, it will do
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
        ///   SHA512 sha512Object = CryptographyProviders.Sha512Creator();
        /// </code>
        /// </para>
        /// <para>
        /// If you want to replace the implementation, you will likely do
        /// something like this in your application.
        /// <code language="csharp">
        ///     CryptographyProviders.Sha512Creator = () =>
        ///     {
        ///         Handle sha512Handle = Sha512Impl.GetSha512Handle();
        ///         return Sha512Impl.GetSha512Object(sha512Handle);
        ///     };
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<SHA512> Sha512Creator { get; set; } = SHA512.Create;

        /// <summary>
        /// This property is a delegate (function pointer). The method loaded
        /// will return an instance of <c>AES</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation needs to encrypt or decrypt data using
        /// AES, it will do so using an implementation of the
        /// <c>System.Security.Cryptography.AES</c> abstract class.
        /// However, when it needs the AES class, it will ask this delegate to
        /// build an object, rather than ask for an object itself. This has to do
        /// with the fact that the <c>AES</c> class implements
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
        ///   AES aesObject = CryptographyProviders.AesCreator();
        /// </code>
        /// </para>
        /// <para>
        /// If you want to replace the implementation, you will likely do
        /// something like this in your application.
        /// <code language="csharp">
        ///     CryptographyProviders.AesCreator = () =>
        ///     {
        ///         Handle aesHandle = GetHandle();
        ///         return AesImpl.GetAesObject(aesHandle);
        ///     };
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<Aes> AesCreator { get; set; } = Aes.Create;

        /// <summary>
        /// This property is a delegate (function pointer). The method loaded
        /// will return an instance of <c>TripleDES</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation needs to encrypt or decrypt data using
        /// Triple-DES, it will do so using an implementation of the
        /// <c>System.Security.Cryptography.TripleDES</c> abstract class.
        /// However, when it needs the Triple-DES class, it will ask this
        /// delegate to build an object, rather than ask for an object itself.
        /// This has to do with the fact that the <c>TripleDES</c> class
        /// implements <c>IDisposable</c>. In order to avoid the complex problem
        /// of ownership of an object that will be disposed once it goes out of
        /// scope, the SDK will create a new object each time one is needed. This
        /// new object will be in scope only for the duration of its use in the
        /// SDK, and will be disposed immediately when the SDK is done with it.
        /// <para>
        /// The method loaded will return an object. This class is initialized
        /// with a method that will build and return an instance of the C#
        /// default implementation. For example, it could be used as follows.
        /// <code language="csharp">
        ///   TripleDES tripleDesObject = CryptographyProviders.TripleDesCreator();
        /// </code>
        /// </para>
        /// <para>
        /// If you want to replace the implementation, you will likely do
        /// something like this in your application.
        /// <code language="csharp">
        ///     CryptographyProviders.TripleDesCreator = () =>
        ///     {
        ///         Handle tripleDesHandle = GetHandle();
        ///         return TDesImpl.GetTripleDesObject(tripleDesHandle);
        ///     };
        /// </code>
        /// </para>
        /// </remarks>
#pragma warning disable CA5350 // The PIV standard requires Triple-DES.
        public static Func<TripleDES> TripleDesCreator { get; set; } = TripleDES.Create;
#pragma warning restore CA5350

        /// <summary>
        /// This property is a delegate (function pointer). The method loaded
        /// will return an instance of <c>DES</c>.
        /// </summary>
        /// <remarks>
        /// When an SDK operation needs to encrypt or decrypt data using
        /// DES, it will do so using an implementation of the
        /// <c>System.Security.Cryptography.DES</c> abstract class.
        /// However, when it needs the DES class, it will ask this
        /// delegate to build an object, rather than ask for an object itself.
        /// This has to do with the fact that the <c>DES</c> class
        /// implements <c>IDisposable</c>. In order to avoid the complex problem
        /// of ownership of an object that will be disposed once it goes out of
        /// scope, the SDK will create a new object each time one is needed. This
        /// new object will be in scope only for the duration of its use in the
        /// SDK, and will be disposed immediately when the SDK is done with it.
        /// <para>
        /// The method loaded will return an object. This class is initialized
        /// with a method that will build and return an instance of the C#
        /// default implementation. For example, it could be used as follows.
        /// <code language="csharp">
        ///   DES desObject = CryptographyProviders.DesCreator();
        /// </code>
        /// </para>
        /// <para>
        /// If you want to replace the implementation, you will likely do
        /// something like this in your application.
        /// <code language="csharp">
        ///     CryptographyProviders.DesCreator = () =>
        ///     {
        ///         Handle desHandle = GetHandle();
        ///         return DesImpl.GetDesObject(desHandle);
        ///     };
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<DES> DesCreator { get; set; } = DES.Create;

        /// <summary>
        /// This property is a delegate (function pointer). This method will return
        /// an instance of <see cref="IEcdhPrimitives"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When an SDK operation needs to perform compute the ECDH shared secret,
        /// it will do so using an implementation of the <see cref="IEcdhPrimitives" />
        /// interface. However, when it needs an instance, it will as this delegate
        /// to build an object, rather than build it itself.
        /// </para>
        /// <para>
        /// If you want to replace the implementation, you will likely do something
        /// like this in your application. The body of the delegate is responsible
        /// for creating an instance of ECDH based on an implementation of your own
        /// choosing.
        /// <code language="csharp">
        ///     CryptographyProviders.EcdhPrimitivesCreator = () =>
        ///     {
        ///         Handle ecdhHandle = GetHandle();
        ///         return EcdhImpl.GetEcdhObject(ecdhHandle);
        ///     }
        /// </code>
        /// </para>
        /// </remarks>
        public static Func<IEcdhPrimitives> EcdhPrimitivesCreator { get; set; } = EcdhPrimitives.Create;

        /// <summary>
        /// This property is a delegate (function pointer). This method will return
        /// an instance of <c>HMAC</c>.
        /// </summary>
        public static Func<string, HMAC> HmacCreator { get; set; } = HMAC.Create;
    }
}
