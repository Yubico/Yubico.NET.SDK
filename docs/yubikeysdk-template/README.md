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

# SDK template for DocFX

This directory contains the files used to override the DocFX "statictoc" template.

A template consists of many files, but all we have to do to build a new template is to
use an existing template and override only those files we want to change. In the
docfx.json file, we specify the template as

```
    "template": [
      "statictoc",
      "../docs/yubikeysdk-template"
    ],
```

This means that the "statictoc" template will be used, except where there are template
files in the `../docs/yubikeysdk-template` directory.

Note that it is sometimes possible to make changes using the `docfx.json` file. For
example, the default DocFX results use DocFX logos at various places in the resulting
html pages. It is possible to change one such icon in `docfx.json`:

```
    "globalMetadata": {
      "_appFaviconPath": "images/favicon.ico"
    },
```

Another way to change output is to use the `filterConfig.yml` file. The SDK project
uses it to remove `System.Object` elements from the list of "Inherited Members" in each
class.

To change any more elements, determine if the changes can be made using `docfx.json`
and/or `filterConfig.yml`, and if not, then maybe use an alternate template file.

See https://dotnet.github.io/docfx/tutorial/walkthrough/advanced_walkthrough.html

## Current changes in yubikeysdk-template

* `styles/main.css`: Change colors.
* `partials/navbar.templ.partial`: Replace DocFX logo with Yubico logo.
* `token.json`: add `"summary": "Summary"`. This header is used in the Class description
to make it easier to see different elements.
* `partials/class.header.tmpl.partial`: Change the order elements are displayed in the
description of a Class.
