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
using Moq;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.TestUtilities
{
    // Use this class to get a random object.
    //
    // The class itself is a fake RNG (the caller specifies what bytes in what
    // order the RNG should generate). However, as written, an instance of this
    // class will likely not be very useful in your tests.
    //
    // Instead, use the static factory method to get an RNG. It will return a
    // much more useful object, because it will be an instance of
    // System.Security.Cryptography.RandomNumberGenerator.
    public class RandomObjectUtility
    {
        private int _offset;
        private readonly byte[] _theBytes;
        private Func<RandomNumberGenerator>? _original;

        // Create a new instance of this class, loading the given bytes.
        // Each call to GetBytes will return the bytes of the bytesToReturn, in
        // order.
        // Once the bytes have been consumed, another call to GetBytes will
        // result in an exception.
        // If you pass in no bytes to the constructor, the object will be created
        // with an empty array, so any call to GetBytes will result in an
        // exception.
        // This constructor copies the bytes you pass in, it does not simply copy
        // a reference to your byte array.
        // While you can create a new RandomObjectUtility object using this
        // constructor, it likely won't be very useful. Use the static method
        // GetRandomObject instead, and you can get an instance of
        // RandomNumberGenerator, which is much more useful.
        public RandomObjectUtility(byte[] bytesToReturn)
        {
            _offset = 0;

            if (bytesToReturn is null)
            {
                _theBytes = Array.Empty<byte>();
                return;
            }

            if (bytesToReturn.Length == 0)
            {
                _theBytes = Array.Empty<byte>();
                return;
            }

            _theBytes = new byte[bytesToReturn.Length];

            bytesToReturn.CopyTo(_theBytes, 0);
        }

        // Fill the given buffer with random bytes. That is, generate data.Length
        // random bytes, placing them into data.
        public void GetBytes(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            GetBytes(data, 0, data.Length);
        }

        // Generate count random bytes, placing them into data beginning at
        // offset.
        public void GetBytes(byte[] data, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if (offset + count > data.Length)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectDerivationLength);
            }

            if (count > _theBytes.Length - _offset)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectDerivationLength);
            }

            Array.Copy(_theBytes, _offset, data, offset, count);
            _offset += count;
        }

        // Build an object that is an implementation of RandomNumberGenerator.
        // See the documentation for
        // System.Security.Cryptography.RandomNumberGenerator.
        // This will build one of two objects. First, if you pass null for the
        // fixedBytes arg, it will build the default RandomNumberGenerator. This
        // will be an object that is created with a new seed every time. It will,
        // therefore, return different random bytes every new creation. You do not
        // have the option of creating this object with a specified seed to get it
        // to return the same bytes (based on that seed) every time. It is
        // "autoseeded".
        // The second object will be a Mock RandomNumberGenerator, with the
        // GetBytes methods overridden. If you supply a byte array for the
        // fixedBytes arg, the object returned will "generate" the bytes of that
        // buffer, and only those bytes. As soon as that buffer has been
        // consumed, any new attempt to generate bytes will result in an
        // exception.
        // For example,
        //
        //   byte[] randomBytes = new byte[8] {
        //       0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88
        //   };
        //   using var randomObject = RandomObjectUtility.GetRandomObject(randomBytes);
        //
        // Now let's say the randomObject's GetBytes method is called.
        //
        //   byte[] someBuffer = new byte[4];
        //   randomObject.GetBytes(someBuffer);
        //
        // Look at the contents of someBuffer:
        //
        //   someBuffer[0] = 0x11
        //   someBuffer[1] = 0x22
        //   someBuffer[2] = 0x33
        //   someBuffer[3] = 0x44
        //
        // Suppose there is another call.
        //
        //   byte[] otherBuffer = new byte[6];
        //   randomObject.GetBytes(otherBuffer, 1, 4);
        //
        // Look at the contents of otherBuffer:
        //
        //   otherBuffer[0] = 0x00
        //   otherBuffer[1] = 0x55
        //   otherBuffer[2] = 0x66
        //   otherBuffer[3] = 0x77
        //   otherBuffer[4] = 0x88
        //   otherBuffer[5] = 0x00
        //
        // At this point, all the bytes that were originally loaded have been
        // consumed. Another call to GetBytes will result in an exception.
        //
        // Currently there is no way to add more bytes. The only way to specify
        // bytes to return is in the GetRandomObject method.
        //
        // Note that only the following methods have been overridden.
        //
        //   void GetBytes(byte[])
        //   void GetBytes(byte[], int, int)
        //
        // If you get a Mock RNG, you can call other methods, such as GetInt32 or
        // GetNonZeroBytes, however, the behavior will likely be inappropriate.
        // If you need more methods overridden, add them to the Mock Setup.
        public static RandomNumberGenerator GetRandomObject(byte[]? fixedBytes)
        {
            if (fixedBytes is null)
            {
                return RandomNumberGenerator.Create();
            }

            var mock = new Mock<RandomNumberGenerator>();
            var redirect = new RandomObjectUtility(fixedBytes);

            // Set up the mock object so that calls to GetBytes will be redirected to
            // the fixed-output version.
            _ = mock.Setup(rand => rand.GetBytes(It.IsAny<byte[]>()))
                .Callback<byte[]>(randomBytes => redirect.GetBytes(randomBytes));
            _ = mock.Setup(rand => rand.GetBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback<byte[], int, int>((randomBytes, offset, count) => redirect.GetBytes(randomBytes, offset, count));

            return mock.Object;
        }

        // Use this method to load the FixedBytes version of the
        // RandomNumberGenerator into the CryptographyProviders class.
        // Note that this will change the global, so that other tests will use
        // this random object as well, unless, when your test is done with the
        // fixed-bytes random object, you set it back to what it was before.
        // This method will return an object you can use to restore the original.
        //
        //   RandomObjectUtility replacement =
        //       RandomObjectUtility.SetRandomProviderFixedBytes(fixedBytes);
        //   try
        //   {
        //         ...test code...
        //   }
        //   finally
        //   {
        //       replacement.RestoreRandomProvider();
        //   }
        public static RandomObjectUtility SetRandomProviderFixedBytes(byte[] fixedBytes)
        {
            ArgumentNullException.ThrowIfNull(fixedBytes);
            var randomUtility = new RandomObjectUtility(fixedBytes);
            randomUtility.ReplaceRandomProvider();

            return randomUtility;
        }

        // Save the current global random creation method and set the global to
        // be the fixedBytes version.
        // If the object already has something saved, this method will do nothing.
        public void ReplaceRandomProvider()
        {
            if (_original is null)
            {
                _original = CryptographyProviders.RngCreator;
                CryptographyProviders.RngCreator = CreateFixedBytesRng;
            }
        }

        // Restore the global random creation method to the original provider.
        public void RestoreRandomProvider()
        {
            if (!(_original is null))
            {
                CryptographyProviders.RngCreator = _original;
                _original = null;
            }
        }

        // Create a new instance of the Mock RandomNumberGenerator that returns
        // the fixed bytes.
        public RandomNumberGenerator CreateFixedBytesRng()
        {
            return GetRandomObject(_theBytes);
        }
    }
}
