[![Build status](https://ci.appveyor.com/api/projects/status/o6n3jbawcrj2wxp2/branch/master?svg=true)](https://ci.appveyor.com/project/urbas/bud-cp/branch/master)
 [![NuGet](https://img.shields.io/nuget/v/Bud.Cp.svg)](https://www.nuget.org/packages/Bud.Cp/)



# Bud.Cp

`Bud.Cp` is a C# library for copying directories. The library uses SHA256 signatures to make incremental copies a bit speedier.

Main idea behind `Bud.Cp` is that you can define your own file signatures system. This can be used by build systems to make incremental copying even faster. For example, a signature of a file might be calculated from the signature of the task that created the file instead of reading every byte of the file and calculating a cryptographic hash. To make incremental copies even faster build systems with immutable source directories and output directories can also persist signatures in a file.


## Use cases


__Copy a source directory to a target directory__:

```csharp
Bud.Cp.CopyDir(sourceDir: "mySourceDir", targetDir: "myTargetDir");
```

Warning: This will delete files in the target directory that no longer exist in the source directory.


__Copy multiple source directories into a single target dir__:

```csharp
Bud.Cp.CopyDirs(sourceDirs: mySourceDirs, targetDir: "myTargetDir");
```

Warning: This will delete files in the target directory that no longer exist in the source directory.


__Use your own file signatures__:

```csharp
Bud.Cp.CopyDir(sourceDir: "mySourceDir", targetDir: "myTargetDir", fileSignatures: new MyFileSignatures());
Bud.Cp.CopyDirs(sourceDirs: mySourceDirs, targetDir: "myTargetDir", fileSignatures: new MyFileSignatures());
```