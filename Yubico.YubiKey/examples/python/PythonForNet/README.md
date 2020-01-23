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

# Python for NET Example
This code enumerates the YubiKeys connected to the system, and prints some basic information about each.

## How to Run
1. Ensure all requirements listed below are met
2. Create a Python virtual environment and install the packages from `requirements.txt`
3. Connect YubiKeys to the system
4. Run the program from the directory using `python ./PythonForNet.py`

## Requirements

### YubiKeys
YubiKey v5.1+ (as of Yubico.Core v0.2.0-alpha5)

### Python
This example uses Python 3.8, which must be installed on the system. All Python packages are managed with pip, and are listed in `requirements.txt`.

### C# Libraries
Place the following libraries in the same directory as `PythonForNet.py`. After building the SDK, the .NET YubiKey SDK libraries can be found in their respective `src/bin/...`, and System library dlls will be in your local nuget cache (for Windows, nuget saves packages to `%localappdata%/.nuget/packages`).

#### .NET YubiKey SDK
- Yubico.Core.dll (v0.2.0-alpha5+)
- Yubico.YubiKey.dll

#### System Libraries
- System.Buffers.dll
- System.Collections.Immutable.dll
- System.Memory.dll
- System.Numerics.Vectors.dll
- System.Runtime.CompilerServices.Unsafe.dll

## Links
- [Python in Visual Studio](https://docs.microsoft.com/en-us/visualstudio/python/?view=vs-2019)
- [Virtual Environments](https://docs.microsoft.com/en-us/visualstudio/python/managing-python-environments-in-visual-studio?view=vs-2019)
- [pip and requirements.txt](https://pip.pypa.io/en/latest/user_guide/#requirements-files)

