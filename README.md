# The smallest ASP.NET backend-API website you can easily run on your local computer

---

## Prerequisites

1. You must install the .NET runtime _(preferably version 7, as that's what I coded this using)_ onto your local machine in a way that lets you run commands beginning with "`dotnet`" from your computer's command prompt _(the "dotnet" executable must be in your "PATH.")_.
    * Don't have Windows admin rights?  Check out David Kou's [Install the .NET runtime onto Windows without admin rights](https://dev.to/davidkou/install-anything-without-admin-rights-4p0j#install-dotnet-sdk-or-runtime-without-admin).
2. You must download a copy of this codebase onto your local computer.

---

## Building source code into a runtime

Open up a command line interface.

Ensure that its prompt indicates that your commands will be running within the context of the folder into which you downloaded a copy of this codebase.

Run the following command:

```sh
(cd ./src/web && dotnet publish --configuration Release --output ../../my_output && cd ../..)
```

The above command should execute within a second or two.

* It will add a new subfolder called `/bin/` as well as one called `/obj/` into `/src/web/` within the folder on your computer containing a copy of this codebase, but you can ignore those.
* More importantly, it will add a new subfolder called `/my_output/` into the top level of the folder on your computer containing a copy of this codebase.

Congratulations -- you have now built an executable "runtime" for ASP.NET that, when executed, will behave as a web server.

The entirety of your "runtime" that you just built lives in the `/my_output/` folder.  It should contain about 6 files, including one called `Handwritten.exe`.

---

## Running your web server

Open up a command line interface.

Ensure that its prompt indicates that your commands will be running within the context of the folder into which you downloaded a copy of this codebase.

Run the following command:

```sh
./my_output/Handwritten.exe
```

* **WARNING**:  Do _not_ close the command line just yet or it will be difficult to stop your web server later in this exercise.

The output you will see will say something like:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\example
```

---

## Visiting your website

Open a web browser and navigate to [http://localhost:5000/api/sayhello](http://localhost:5000/api/sayhello) _(being sure to use an alternative port number to `5000` if indicated "`now listening on`" message you saw when you executed your runtime)_.

Take a look in the upper-left corner of the webpage you just visited:  it should say "**Hello World**".

---

## Stopping your web server

Go back to the command line interface from which you ran `./my_output/Handwritten.exe`.

Hold <kbd>Ctrl</kbd> and hit <kbd>c</kbd>, then release them both.

---

## A build CI/CD pipeline for Azure DevOps Pipelines

I'm not sure I'll get around to making cloud instructions for this codebase any time soon, so if you'd like to guess your way through adapting lessons from [az-as-wa-001-node-express-tiny](https://github.com/kkgthb/az-as-wa-001-node-express-tiny) to apply to this codebase instead, here's the "build pipeline" YAML I used in Azure DevOps Pipelines:

```yaml
trigger:
- main

pool:
  vmImage: "windows-latest"

steps:

- task: UseDotNet@2
  displayName: "Install .NET Core SDK"
  inputs:
    version: 7.x
    performMultiLevelLookup: true
    includePreviewVersions: true

- task: DotNetCoreCLI@2
  displayName: "DotNet Publish"
  inputs:
    command: publish
    workingDirectory: "src/web"
    arguments: "--configuration Release --output $(Build.ArtifactStagingDirectory)"
    zipAfterPublish: True

- task: PublishBuildArtifacts@1
  displayName: "Publish build-artifact from staging directory to named artifact"
  inputs:
    PathToPublish: "$(Build.ArtifactStagingDirectory)"
    ArtifactName: "MyBuiltDotNetWebsite"
```

The "published artifact" output of running such a pipeline against this repository should be a folder called `MyBuiltDotNetWebsite` with one `.zip`-typed file in it.  If you were to download the `.zip` file to your computer _(click its name in the published artifact file-tree browser)_ and open it, its contents would look like those of the `/my_output/` folder you saw on your desktop computer earlier _(approximately 6 files, including one called `Handwritten.exe`)_.