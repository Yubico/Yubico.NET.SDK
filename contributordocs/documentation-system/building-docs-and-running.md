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

# Building the docs and running

We use DocFX to build the documentation for the Yubico.YubiKey project. This is a tool that
generates documentation from comments in the source code. The comments are written in Markdown
format.

In order to build the documentation, use either of the following methods: 
1. Command Line: `docfx build docfx.json`
2. VS Code: Run the build task `DocFXBuild` using [VS Code Tasks](https://code.visualstudio.com/docs/editor/tasks) 
   
> Note: In order to run `docfx` commands, you need to have docfx installed. Install `docfx` using the following command: `dotnet tool install -g docfx`

The result is in `docs/_site`. The home page is `index.html`.

There might be placeholders for content we will have to write but you can click on "Api Documentation"
to get to the classes. The comments you wrote will be reflected in the documentation.

Note that even if there is a class and/or method with no documentation, there is an entry for it.
Everything is there except the content.


## Local web server

Note that there is another way to launch DocFX:

```shell
$ docfx docfx.json --serve
```

Now open a browser and go to [http://localhost:8080](http://localhost:8080). There you see your home
page for this project.

## Git

Git should be set up so that the `docfx.json` file along with a few others (such as the subdirectory
`namespaces` with its contents) will be part of the repo. However, the html results should not be
added/committed/pushed.
