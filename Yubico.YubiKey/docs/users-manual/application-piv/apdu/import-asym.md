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

## Import asymmetric key pair

### Command APDU Info

| CLA | INS |     P1      |      P2       |     Lc     |                                                   Data                                                   |    Le    |
|:---:|:---:|:-----------:|:-------------:|:----------:|:--------------------------------------------------------------------------------------------------------:|:--------:|
| 00  | FE  | *algorithm* | *slot number* | *data len* | *set of TLV containing key elements* <br />\[AA 01 *\<pin policy\>*\] <br />\[AB 01 *\<touch policy\>*\] | (absent) |

The slot number can be one of the following (hex values):

```C
9A, 9C, 9D, 9E,
82, 93, 84, 85, 86, 87, 88, 89, 8A, 8B, 8C, 8D, 8E, 8F,
90, 91, 92, 93, 94, 95
F9
```

There are six choices for "alg" (algorithm and size): RSA-1024 (06),
RSA-2048 (07), RSA 3072 (08), RSA 4096 (09), ECC-P-256 (11), and ECC-P-384 (14).

The key data to load is a set of TLV constructions. The L (length) is DER encoding
format. The V is the integer in canonical form. If the key is an RSA private key, there
are five elements. If it is an ECC key, there is one element.

#### Table 3: List of Private Key Tags

| Algorithm |       Key Element       | Tag |
|:---------:|:-----------------------:|:---:|
|    RSA    |        prime *P*        | 01  |
|    RSA    |        prime *Q*        | 02  |
|    RSA    | prime *p* exponent *dP* | 03  |
|    RSA    | prime *q* exponent *dQ* | 04  |
|    RSA    | CRT coefficient *QInv*  | 05  |
|    ECC    |    private value *s*    | 06  |

### Response APDU Info: Management Key Authentication Missing

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 69  | 82  |

### Response APDU Info: Success

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |

### Examples

```C
To be added
```
