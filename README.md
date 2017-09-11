[![Build status](https://ci.appveyor.com/api/projects/status/o6n3jbawcrj2wxp2/branch/master?svg=true)](https://ci.appveyor.com/project/urbas/bud-cp/branch/master)
 [![NuGet](https://img.shields.io/nuget/v/Bud.Cp.svg)](https://www.nuget.org/packages/Bud.Cp/)



# Bud.Cp

`Bud.Cp` is a C# library for copying directories. The library uses SHA256 signatures to make incremental copies a bit speedier.

Main idea behind `Bud.Cp` is that you can define your own file storage system.  You can implement web-based storage system or redefine how file signatures are calculated and cached (to speed up repeated incremental copies). For example, you can decrease the number of signature calculations if you store file signatures in a cache. This is particularly useful when using immutable target storage.

Bud.Cp was implemented to support all the copying in the [Bud build tool](https://github.com/urbas/bud).


## Use cases


### Copy a source directory to a target directory

```csharp
Bud.Cp.CopyDir(sourceDir: "mySourceDir", targetDir: "myTargetDir");
```

Warning: This will delete files in the target directory that no longer exist in the source directory.


### Copy multiple source directories into a single target dir

```csharp
Bud.Cp.CopyDir(sourceDirs: mySourceDirs, targetDir: "myTargetDir");
```

Warning: This will delete files in the target directory that no longer exist in the source directory.


### Use your own storage scheme

With the `storage` and `overwritePolicy` parameters you can convince `Bud.Cp.CopyDirs` to copy all kinds of resources:

```csharp
Bud.Cp.CopyDir(sourceDir: "mySourceDir", targetDir: "myTargetDir", storage: new MyWebStorage(), overwritePolicy: new MyETagOverwritePolicy());
Bud.Cp.CopyDir(sourceDirs: mySourceDirs, targetDir: "myTargetDir", storage: new MyWebStorage(), overwritePolicy: new MyETagOverwritePolicy());
```
