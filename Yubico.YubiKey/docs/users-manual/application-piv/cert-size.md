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

It is possible to store up to 24 private key/certificate pairs in the PIV slots. However,
there are space limitations.

In the real world, certificates are generally less than 1,000 bytes. Some large certs are
over 1,000 bytes, but rarely over 2,000. It is unlikely that you will run into limitations
on the YubiKey.

Nonetheless, these are the space limitations for certs in the PIV application on the
YubiKey.

## Maximum size for a single certificate

|    YubiKey Version    | Maximum Size in Bytes |
|:---------------------:|:---------------------:|
| before 4.0 (e.g. NEO) |         2025          |
|          4.x          |         3052          |
|       4.x FIPS        |         3052          |
|          5.x          |         3052          |
|       5.x FIPS        |         3052          |

## Total space available for certificates

Although a YubiKey 5.x will allow a 3052-byte cert in one of the slots, it will not be
able to store 24 certs that big.

A NEO (pre-4.0), only has four slots, and will be able to hold four certs of the maximum
length.

|    YubiKey Version    | Maximum Total Cert<br/>Space Available | Number of Certs<br/>at Size | Number of Certs<br/>at Maximum Size |
|:---------------------:|:--------------------------------------:|:---------------------------:|:-----------------------------------:|
| before 4.0 (e.g. NEO) |                  8100                  |    4 certs at 2025 bytes    |        4 certs at 2025 bytes        |
|          4.x          |              about 49,800              |   24 certs at 2075 bytes    |       16 certs at 3052 bytes        |
|       4.x FIPS        |              about 49,800              |   24 certs at 2075 bytes    |       16 certs at 3052 bytes        |
|          5.x          |              about 50,000              |   24 certs at 2084 bytes    |       16 certs at 3052 bytes        |
|       5.x FIPS        |              about 49,890              |   24 certs at 2079 bytes    |       16 certs at 3052 bytes        |

Note that that total amount of storage on a YubiKey (for certs, for PUT DATA objects,
etc.) is about 51,000 bytes. Hence, if a YubiKey is loaded with 49,000 bytes of certs,
then there will be very little space left for anything else.

## Summary

On a 5.x YubiKey, it is possible to store a 3,052-byte cert in a slot. If a cert is
bigger than 3,052 bytes, the YubiKey will reject it and the SDK will throw an exception.

It is certainly possible to store several 3,052-byte certs on a 5.x YubiKey, but once the
total size limit is reached, the YubiKey won't be able to store any more, even if some of
the slots are empty.

However, because a real world application will probably not use certs bigger than 2,000
bytes, it is not likely it will ever run into a total space limitation and will be able
to store up to 24 certs.