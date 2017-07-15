using System;
using System.Collections.Generic;
using System.Linq;

namespace Bud {
  public static class Cp {
    public static void CopyDir(IEnumerable<Uri> sourceDirs, Uri targetDir, IStorage storage = null) {
      storage = storage ?? new LocalStorage();
      targetDir = targetDir.AbsolutePath.EndsWith("/") ? targetDir : new Uri(targetDir + "/");
      
      storage.CreateDirectory(targetDir);
      
      var targetRelUris = GetRelPaths(storage, targetDir);
      var sourceDirUris = sourceDirs.Select(sourceDir => new Uri(sourceDir + "/")).ToList();
      var sourceRelUris = sourceDirUris.Select(sourceDir => GetRelPaths(storage, sourceDir)).ToList();
      
      AssertNoConflicts(sourceDirUris, sourceRelUris, targetDir);
      
      SyncDirectories(sourceDirUris, targetDir, storage);
      CopyMissingFiles(sourceDirUris, sourceRelUris, targetDir, targetRelUris, storage);
      OverwriteExistingFiles(sourceDirUris, sourceRelUris, targetDir, targetRelUris, storage);
      DeleteExtraneousFiles(sourceRelUris, targetDir, targetRelUris, storage);
    }

    private static void SyncDirectories(IEnumerable<Uri> sourceDirUris, Uri targetDir, IStorage storage) {
      var srcSubDirs = sourceDirUris.Aggregate(new HashSet<Uri>(),
                                               (allSubDirs, sourceDirUri) => {
                                                 allSubDirs.UnionWith(GetSubdirs(storage, sourceDirUri));
                                                 return allSubDirs;
                                               });
      foreach (var subDir in srcSubDirs) {
        storage.CreateDirectory(new Uri(targetDir, subDir));
      }
    }

    public static void CopyDir(IEnumerable<string> sourceDirs, string targetDir, IStorage storage = null)
      => CopyDir(sourceDirs.Select(path => new Uri(path)), new Uri(targetDir), storage);

    public static void CopyDir(string sourceDir, string targetDir, IStorage storage = null)
      => CopyDir(new Uri(sourceDir), new Uri(targetDir), storage);

    public static void CopyDir(Uri sourceDir, Uri targetDir, IStorage storage = null)
      => CopyDir(new[] {sourceDir}, targetDir, storage);

    private static void AssertNoConflicts(List<Uri> sourceDirUris, List<HashSet<Uri>> sourceRelUris, Uri targetDir) {
      var relPath2SrcDir = new Dictionary<Uri, Uri>();
      for (var dirIndex = 0; dirIndex < sourceDirUris.Count; dirIndex++) {
        var sourceDirUri = sourceDirUris[dirIndex];
        var relPaths = sourceRelUris[dirIndex];
        foreach (var relPath in relPaths) {
          Uri srcDir;
          if (relPath2SrcDir.TryGetValue(relPath, out srcDir)) {
            throw new Exception($"Could not copy directories '{srcDir.AbsolutePath}' and " +
                                $"'{sourceDirUri.AbsolutePath}' to '{targetDir.AbsolutePath}'. " +
                                $"Both source directories contain file '{relPath}'.");
          }
          relPath2SrcDir.Add(relPath, sourceDirUri);
        }
      }
    }

    private static void CopyMissingFiles(List<Uri> sourceDirUris, List<HashSet<Uri>> sourceRelUris,
                                         Uri targetDir, HashSet<Uri> targetRelUris, IStorage storage) {
      for (var dirIndex = 0; dirIndex < sourceDirUris.Count; dirIndex++) {
        var sourceDirUri = sourceDirUris[dirIndex];
        var srcFilesRelPaths = sourceRelUris[dirIndex];
        foreach (var relPathToCopy in srcFilesRelPaths.Except(targetRelUris)) {
          var sourceAbsPath = new Uri(sourceDirUri, relPathToCopy);
          var targetAbsPath = new Uri(targetDir, relPathToCopy);
          storage.CopyFile(sourceAbsPath, targetAbsPath);
        }
      }
    }

    private static void OverwriteExistingFiles(List<Uri> sourceDirUris, List<HashSet<Uri>> sourceRelUris,
                                               Uri targetDir, HashSet<Uri> targetRelUris, IStorage storage) {
      for (var dirIndex = 0; dirIndex < sourceDirUris.Count; dirIndex++) {
        var sourceDirUri = sourceDirUris[dirIndex];
        foreach (var relPathToOverwrite in sourceRelUris[dirIndex].Intersect(targetRelUris)) {
          var sourceAbsPath = new Uri(sourceDirUri, relPathToOverwrite);
          var targetAbsPath = new Uri(targetDir, relPathToOverwrite);
          if (!FileSignaturesEqual(storage, sourceAbsPath, targetAbsPath)) {
            storage.CopyFile(sourceAbsPath, targetAbsPath);
          }
        }
      }
    }

    private static bool FileSignaturesEqual(IStorage storage, Uri sourceAbsPath, Uri targetAbsPath)
      => storage.GetSignature(sourceAbsPath).SequenceEqual(storage.GetSignature(targetAbsPath));

    private static void DeleteExtraneousFiles(IEnumerable<HashSet<Uri>> sourceRelUris, Uri targetDir,
                                              HashSet<Uri> targetRelUris, IStorage storage) {
      var allSourceRelPaths = sourceRelUris.Aggregate(new HashSet<Uri>(), (aggregate, relPaths) => {
        aggregate.UnionWith(relPaths);
        return aggregate;
      });
      foreach (var targetFileToDelete in targetRelUris.Except(allSourceRelPaths)) {
        storage.DeleteFile(new Uri(targetDir, targetFileToDelete));
      }
    }

    private static HashSet<Uri> GetRelPaths(IStorage storage, Uri dir)
      => new HashSet<Uri>(storage.EnumerateFiles(dir).Select(dir.MakeRelativeUri));

    private static IEnumerable<Uri> GetSubdirs(IStorage storage, Uri sourceDirUri)
      => storage.EnumerateDirectories(sourceDirUri).Select(sourceDirUri.MakeRelativeUri);
  }
}