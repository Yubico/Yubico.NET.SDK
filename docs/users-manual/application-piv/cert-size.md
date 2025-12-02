---
uid: UsersManualPivCertSizes
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

# Maximum certificate sizes

It is possible to store up to 24 private key/certificate pairs in the PIV slots for YubiKeys with firmware version 4.x and higher. However, there are limits to the size of each certificate and the total space available for all certificates.

> [!NOTE]
> In practice, the size of a key/certificate pair is determined by the choice of algorithm and key length (e.g. RSA 1024 vs RSA 4096), certificate complexity (e.g. use of OIDs, size attributes), the presence of PIV attestation objects, etc.

## Maximum size for a single certificate

If you attempt to load a certificate that is larger than the YubiKey's maximum allowable certificate size (as indicated in the table below), the YubiKey will reject it, and the SDK will throw an exception.

| YubiKey (Model and Firmware)     | Maximum Size in Bytes |
|:--------------------------------:|:---------------------:|
| NEO (prior to 4.x)               |         2025          |
| 4 Series (4.x)                   |         3052          |
| 4 FIPS Series (4.x)              |         3052          |
| 5 Series (5.x)                   |         3052          |
| 5 FIPS Series (5.x)              |         3052          |

## Total space available for certificates

Although YubiKeys with firmware version 4.x and higher will allow 3052-byte certificates, they will not be able to store 24 certificates of that size due to the YubiKey's total certificate space limit. Even if a YubiKey has empty certificate slots available, you cannot fill them once the maximum certificate space has been reached.  

However, a YubiKey NEO, which only has four slots, will be able to hold four certificates of the maximum
length.

Note that that total amount of storage on a YubiKey (for certificates, PUT DATA objects,
etc.) is about 51,000 bytes. Hence, if a YubiKey is loaded with 49,000 bytes of certificates,
there will be very little space left for anything else.

| YubiKey<br/>(Model and Firmware) | Maximum Total Certificate<br/>Space Available | Maximum Average<br/>Certificate Size | Number of Certificates<br/>at Maximum Size |
|:--------------------------------:|:---------------------------------------------:|:------------------------------------:|:------------------------------------------:|
| NEO (prior to 4.x)               |                  8100                         |    4 certs at 2025 bytes             |        4 certs at 2025 bytes               |
|    4 Series (4.x)                |              about 49,800                     |   24 certs at 2075 bytes             |       16 certs at 3052 bytes               |
|  4 FIPS Series (4.x)             |              about 49,800                     |   24 certs at 2075 bytes             |       16 certs at 3052 bytes               |
|    5 Series (5.x)                |              about 50,000                     |   24 certs at 2084 bytes             |       16 certs at 3052 bytes               |
|  5 FIPS Series (5.x)             |              about 49,890                     |   24 certs at 2079 bytes             |       16 certs at 3052 bytes               |