---
uid: UsersManualSensitive
summary: *content
---

<!-- Copyright 2021 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# Sensitive data (PINs, passwords, and keys)

There are commands and operations in the SDK that require a PIN, password, or key. How
should code be written to handle this sensitive data?

Your code will likely include some component that reads the PIN or password entered at
the keyboard. You will keep the input in some object, which means it will be in some
memory location, and hand it off to some SDK class or method. What class should hold
that sensitive data?

This entry in the User's Manual offers a suggestion, but first, let's look at why this
issue is not as simple as it seems.

## Vulnerability

While the PIN or password is in memory, it is in the clear. Anyone with access to that
memory will be able to see the sensitive data.

First of all, if an attacker has access to the memory while the program is running, your
system is completely compromised and there's nothing you can do to protect sensitive
data other than never using it.

Instead, you want to prevent the data leaking out after your program has released the
memory holding the sensitive data. After a program has stopped, or maybe even while
running and simply releasing blocks of memory, the region of memory used by your program
might be visible to an attacker. For example, malware running on a device might search
through the memory looking for passwords left around from other programs. Some platforms
swap memory to disk to extend the amount of memory available. The malware might look
through swap space for passwords.

## Mitigation

If there is no way to avoid processing sensitive data, your program will be vulnerable.
The best you can do is limit the exposure. To do so, there are three things you can do:

* Make sure the memory holding the data is overwritten after using it
* Limit the amount of time sensitive material remains in memory (overwrite as soon as possible)
* Make sure you control all buffers holding your sensitive data

### Overwrite the buffer

When the program no longer needs the data, overwrite it. Remember, some classes do not
allow you to change data, only create new objects. So use mutable classes.

If you overwrite a buffer directly, many compilers will recognize that the results of
the overwrite will never be used, so they decide the operation is pointless. They
optimize by simply not performing the overwrite and garbage collecting the object.
Hence, it is generally useful to create a method to overwrite the data.

When the memory is released, it will no longer contain the PIN or password. The attacker
searching through your memory will not see the information.

### Limit the amount of time

Try to write your code so that you don't collect sensitive data until just before you
need it. Then as soon as you have finished using it, overwrite it. This will limit the
opportunity for the operating system to swap out the memory to disk, or the garbage
collector to consolidate buffers or perform other efficiency operations.

Sometimes applications keep PINs, passwords, or keys in memory for caching purposes.
The idea is that an operation might require a PIN to be entered several times to
complete its tasks. Having the user enter the PIN once, then reusing it is a convenience
offered to the customer.

If your application does that, you are trading security for usability. Sometimes that is
unavoidable. Simply be aware that sensitive data is hanging around and make sure you
overwrite it as soon as possible. For example, create an option to specify the caching
time (15 seconds? 1 minute?) and overwrite after that time has passed. 

### Control the buffer

With C#, many classes will allocate memory, then reallocate. That is, it is possible to
load some sensitive data into an object, then later on perform some operation (such as
loading more sensitive data) and the object reallocates, copying the previous data into
a new buffer and releasing the old one. You did not get a chance to overwrite the buffer
before it was released. In this case, it is possible your sensitive data exists in part
or entirely somewhere in the released memory.

It is possible to create an object that cannot be changed without creating a new object.
For example, if you create an instance of the `string` class and set it to the value
"password", there is a buffer in memory somewhere containing the bytes 'p', 'a', 's', and
so on. If you want to change the 'p' to 'P', you could use the `Replace` method.

However, what happens is a new object is created, the old data, "password" is copied
over, and then in the new object, the 'p' is replaced with 'P'. The old object is
released and available to be garbage collected. There is now some place in memory that
contains "password", and you have no control over it.

You want to make sure you write your code so that you can be very confident any buffer
created and holding sensitive data will exist in only one location and you can change
its contents.

## SecureString class

C# contains a class called `SecureString`. It was originally designed to hold passwords
and PINs. It was a way to pass sensitive data between classes and methods.

However, Microsoft no longer recommends using this class. In the documentation for
`SecureString`, they say

> "We don't recommend that you use the SecureString class for new development."

They link to a doc describing their reasons. In that doc, they provide their
recomendation:

> "The general approach of dealing with credentials is to avoid them and instead rely
> on other means to authenticate, such as certificates or Windows authentication."

In other words, don't use PINs and passwords.

But what should a program do if "not using" is not an option?

## Suggestion

Collect sensitive data in byte arrays (`byte[]`) and overwrite the buffer using
`CryptographicOperations.ZeroMemory` as soon as possible.

If you need to deal with portions of the array, if possible, use a `Span` or
`ReadOnlySpan` and slices.

Many SDK methods will take in or return sensitive data as a byte array. Others take in the
data as a `ReadOnlySpan<byte>`. That is, the SDK makes it easy to use byte arrays to hold
sensitive data.

### Create buffers with a maximum size

It is possible to resize byte arrays, see the `Array.Resize` method. You can create bigger
arrays (similar to the C function `realloc`) or trim the end of an existing array to make
it smaller. However, this method will create a new buffer and copy the old data into the
new. You now have sensitive data in two buffers, make sure you overwrite both.

To avoid resizing to make buffers bigger, determine the maximum size (an AES key is never
longer than 64 btes, a PIV PIN is never longer than 8 bytes), allocate that size and
collect your data. If you need to look at a smaller section of the input, use a `Span`
and slice.

```C#
  private const int MaximumPinLength = 8;

  var pin = new byte[MaximumPinLength];

  // Collect the PIN from the user, storing it in the given byte array.
  // If there is a PIN already in the array, the method will replace it.
  // This method will load up to pin.Length bytes. If the user tries to
  // enter more bytes, the method will throw an exception.
  public void CollectPin(byte[] pin);
```

This method will read a byte from the keyboard, likely writing an asterisk to the screen
for each character entered.

### Use `CryptographicOperations.ZeroMemory` as soon as possible

```C#
    var managementKey = new byte[ManagementKeySize];

    try
    {
        CollectManagementKey(managementKey);
        AuthenticateManagementKey(managementKey);
        CryptographicOperations.ZeroMemory(managementKey);

        // Continue with the operation.
    }
    finally
    {
        CryptographicOperations.ZeroMemory(managementKey);
    }
```

In this code example, after calling `AuthenticateManagementKey`, we overwrite the
`managementKey` buffer. We're overwriting it as soon as we're done with it. We also call
the `ZeroMemory` method in the finally clause so we guarantee it is called. If there is an
exception in the `AuthenticateManagementKey` method, the buffer is overwritten. If there
is no exception, the buffer is overwritten immediately. It will be overwritten again in
the finally clause, but that's a minor cost.

It would certainly be possible to call the overwrite method in the finally clause only. If
there is no exception thrown, the finally clause will be executed and the buffer will be
overwritten. But if you want to make sure the buffer is overwritten as soon as possible
(how much time will be taken up by the "// Continue with the operation."?), don't wait for
the finally.

## Conclusion

Following this suggestion will not guarantee sensitive data will never be leaked. It
is still possible a buffer will be swapped to disk or the garbage collector will
consolidate memory. If so, looking at memory or swap space after your program has run
might turn up sensitive data.

However, it does limit exposure, and with C#, that is about all you can do.
