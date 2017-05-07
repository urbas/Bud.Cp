[![NuGet](https://img.shields.io/nuget/v/Bud.Cp.svg)](https://www.nuget.org/packages/Bud.Cp/)



# Bud.Cp

`Bud.Cp` is a C# library for copying directories incrementally. The library does some book-keeping of file signatures to make the copying process a bit speedier.



## Use cases


__Copy a source directory to a target directory__:

```csharp
Bud.Cp.CopyDir(sourceDir: mySourceDirs,
               targetDir: "myTargetDir",
               targetInfo: ".myTargetDir.cp_info.yml");
```

Note: the `.cp_info.yml` file is where `Bud.Cp` caches SHA256 signatures of copied files. `Bud.Cp` considers two files the same if they have the same relative path and the same signature.


__Copy multiple source directories into a single target dir__:

Note: the source directories must have accompanying `.cp_info` directories.

```csharp
Bud.Cp.CopyDirs(sourceDirs: mySourceDirs,
                targetDir: "myTargetDir",
                sourceInfos: mySourceInfos,
                targetInfo: ".myTargetDir.cp_info.yml");
```


__Load a `.cp_info.yml` file for a directory__:

```csharp
Bud.Cp.LoadSignatures(".myDir.cp_info.yml");
```


__Create a `.cp_info.yml` file for a directory__:

```csharp
Bud.Cp.StoreSignatures(".myDir.cp_info.yml", fileToSignatures);
```



## `.cp_info.yml` schema

```yaml
files:
  foo/bar.txt:
    sha256: 0123456789abcdef
  foo/baz.txt:
    sha256: 0123456789abcdef
  ...
```