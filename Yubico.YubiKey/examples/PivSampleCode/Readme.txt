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

This directory contains a program that demonstrates how to use the .NET YubiKey SDK
to perform PIV operations.

This program needs to "link in" the "SharedSampleCode" program in order to work. The
SharedSampleCode class library contains code to run a Menu, along with List and Choose
YubiKeys. In addition the SharedSampleCode obtains the SDK using nuget, so that is how
this program will "link in" the SDK.

This program is a simple menu-driven demo. After building the program, run it to get a
menu of PIV operations. Select the operation and the demo will execute it.

The code to start and run the sample program is in the "Run" directory.

Most of the code that uses the SDK to perform PIV operations is in the "YubiKeyOperations"
directory. For example, there is a file KeyPairs.cs, and in it is a method
RunGenerateKeyPair. The method demonstrates what information is needed in order to
generate a key pair and how to make the call.

There are also operations that demonstrate how to use the SDK in conjunction with the .NET
Base Class Libraries (BCL). For example, in the "CertificateOperations" directory is code
demonstrating how to build a certificate request using the BCL and sign it using the
YubiKey.

Another directory, "Converters", contains code demonstrating how to convert elements
between formats. For example, some SDK calls require input of public keys. These methods
require the public key to be in the form of a PivPublicKey. Suppose you have the public
key as a PEM construction. There is a class with methods that demonstrate how to convert
that PEM public key into a PivPublicKey.

There is also a sample KeyCollector, which is needed by the SDK to obtain PINs, PUKs, and
management keys.

Another directory contains a "SamplePivSlotContents" class. This is a simple class that
holds slot information. Once you set up a slot with a private key and possibly a
certificate, you will want to keep track of what is in each slot. This sample code will
store information about each slot used during each invocation of the demo program. That
is, during the sample program, this information is available and updated. But once you
exit the sample program, this information is lost. If you want, you can extend it to save
the contents to a file, and restore it each time the sample program is run.
