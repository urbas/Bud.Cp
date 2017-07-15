using System;
using System.Collections.Generic;
using System.Linq;

namespace Bud {
  public static class Cp {
    public static void CopyDir(IEnumerable<Uri> sourceDirs, Uri targetDir, IStorage storage = null) {
      storage = storage ?? new LocalStorage();
      storage.CreateDirectory(targetDir);
      var targetDirUri = new Uri(targetDir + "/");
      var targetRelPaths = GetRelPaths(storage, targetDirUri);
      var sourceRelPaths = sourceDirs.Select(sourceDir => new Uri(sourceDir + "/"))
                                     .Select(sourceDir => Tuple.Create(sourceDir, GetRelPaths(storage, sourceDir)))
                                     .ToList();
      AssertNoConflicts(sourceRelPaths, targetDirUri);
      CopyMissingFiles(sourceRelPaths, targetDirUri, targetRelPaths, storage);
      OverwriteExistingFiles(sourceRelPaths, targetDirUri, targetRelPaths, storage);
      DeleteExtraneousFiles(sourceRelPaths, targetDirUri, targetRelPaths, storage);
    }

    public static void CopyDir(IEnumerable<string> sourceDirs, string targetDir, IStorage storage = null)
      => CopyDir(sourceDirs.Select(path => new Uri(path)), new Uri(targetDir), storage);

    public static void CopyDir(string sourceDir, string targetDir, IStorage storage = null)
      => CopyDir(new Uri(sourceDir), new Uri(targetDir), storage);

    public static void CopyDir(Uri sourceDir, Uri targetDir, IStorage storage = null)
      => CopyDir(new[] {sourceDir}, targetDir, storage);

    private static void AssertNoConflicts(List<Tuple<Uri, HashSet<Uri>>> sourceRelPaths, Uri targetDir) {
      var relPath2SrcDir = new Dictionary<Uri, Uri>();
      foreach (var abs2RelPath in sourceRelPaths) {
        foreach (var relPath in abs2RelPath.Item2) {
          Uri srcDir;
          if (relPath2SrcDir.TryGetValue(relPath, out srcDir)) {
            throw new Exception($"Could not copy directories '{srcDir.AbsolutePath}' and " +
                                $"'{abs2RelPath.Item1.AbsolutePath}' to '{targetDir.AbsolutePath}'. " +
                                $"Both source directories contain file '{relPath}'.");
          }
          relPath2SrcDir.Add(relPath, abs2RelPath.Item1);
        }
      }
    }

    private static void CopyMissingFiles(IEnumerable<Tuple<Uri, HashSet<Uri>>> sourceRelPaths, Uri targetDir,
                                         HashSet<Uri> targetRelPaths, IStorage storage) {
      foreach (var dir2RelPaths in sourceRelPaths) {
        foreach (var relPathToCopy in dir2RelPaths.Item2.Except(targetRelPaths)) {
          var sourceAbsPath = new Uri(dir2RelPaths.Item1, relPathToCopy);
          var targetAbsPath = new Uri(targetDir, relPathToCopy);
          storage.CopyFile(sourceAbsPath, targetAbsPath);
        }
      }
    }

    private static void OverwriteExistingFiles(IEnumerable<Tuple<Uri, HashSet<Uri>>> sourceDirs2RelPaths,
                                               Uri targetDir, HashSet<Uri> targetRelPaths, IStorage storage) {
      foreach (var dir2RelPaths in sourceDirs2RelPaths) {
        foreach (var relPathToOverwrite in dir2RelPaths.Item2.Intersect(targetRelPaths)) {
          var sourceAbsPath = new Uri(dir2RelPaths.Item1, relPathToOverwrite);
          var targetAbsPath = new Uri(targetDir, relPathToOverwrite);
          if (!FileSignaturesEqual(storage, sourceAbsPath, targetAbsPath)) {
            storage.CopyFile(sourceAbsPath, targetAbsPath);
          }
        }
      }
    }

    private static bool FileSignaturesEqual(IStorage storage, Uri sourceAbsPath, Uri targetAbsPath)
      => storage.GetSignature(sourceAbsPath).SequenceEqual(storage.GetSignature(targetAbsPath));

    private static void DeleteExtraneousFiles(IEnumerable<Tuple<Uri, HashSet<Uri>>> sourceRelPaths,
                                              Uri targetDir, HashSet<Uri> targetRelPaths, IStorage storage) {
      var allSourceRelPaths = sourceRelPaths.Aggregate(new HashSet<Uri>(), (aggregate, sourceDir2RelPaths) => {
        aggregate.UnionWith(sourceDir2RelPaths.Item2);
        return aggregate;
      });
      foreach (var targetFileToDelete in targetRelPaths.Except(allSourceRelPaths)) {
        storage.DeleteFile(new Uri(targetDir, targetFileToDelete));
      }
    }

    private static HashSet<Uri> GetRelPaths(IStorage storage, Uri dir)
      => new HashSet<Uri>(storage.EnumerateFiles(dir).Select(dir.MakeRelativeUri));
  }
}