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

## Put data

### Command APDU Info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | DB | 3F | FF | *data length* | *data element tag followed by data to put* | (absent)

### Response APDU info: success

Total Length: 2\
Data Length: 0

   Data    | SW1 | SW2 
:---------:|:---:|:---:
 (no data) | 90  | 00  

### Response APDU info: security status not satisfied

Total Length: *2*\
Data Length: *0*

   Data    | SW1 | SW2 
:---------:|:---:|:---:
 (no data) | 69  | 82  

### Examples

```C
To be added
```
