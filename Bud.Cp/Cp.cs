using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.IO.Directory;

namespace Bud {
  public static class Cp {
    public static void CopyDir(IEnumerable<string> sourceDirs, string targetDir,
                               Action<string, string> copyFunction = null, IFileSignatures fileSignatures = null) {
      copyFunction = copyFunction ?? CopyFile;
      fileSignatures = fileSignatures ?? new Sha256FileSignatures();
      CreateDirectory(targetDir);

      var targetDirUri = new Uri(targetDir + "/");
      var targetRelPaths = GetRelPaths(targetDirUri);
      var sourceRelPaths = sourceDirs.Select(sourceDir => new Uri(sourceDir + "/"))
                                     .Select(sourceDir => Tuple.Create(sourceDir, GetRelPaths(sourceDir)))
                                     .ToList();

      CopyMissingFiles(sourceRelPaths, targetDirUri, targetRelPaths, copyFunction);
      OverwriteExistingFiles(sourceRelPaths, targetDirUri, targetRelPaths, copyFunction, fileSignatures);
      DeleteExtraneousFiles(sourceRelPaths, targetDirUri, targetRelPaths);
    }

    public static void CopyDir(string sourceDir, string targetDir, Action<string, string> copyFunction = null,
                               IFileSignatures fileSignatures = null)
      => CopyDir(new[] {sourceDir}, targetDir, copyFunction, fileSignatures);

    private static void CopyMissingFiles(IEnumerable<Tuple<Uri, HashSet<Uri>>> sourceRelPaths, Uri targetDir,
                                         HashSet<Uri> targetRelPaths, Action<string, string> copyFunction) {
      foreach (var dir2RelPaths in sourceRelPaths) {
        foreach (var relPathToCopy in dir2RelPaths.Item2.Except(targetRelPaths)) {
          var sourceAbsPath = new Uri(dir2RelPaths.Item1, relPathToCopy);
          var targetAbsPath = new Uri(targetDir, relPathToCopy);
          copyFunction(sourceAbsPath.AbsolutePath, targetAbsPath.AbsolutePath);
        }
      }
    }

    private static void OverwriteExistingFiles(IEnumerable<Tuple<Uri, HashSet<Uri>>> sourceDirs2RelPaths,
                                               Uri targetDir, HashSet<Uri> targetRelPaths,
                                               Action<string, string> copyFunction, IFileSignatures fileSignatures) {
      foreach (var dir2RelPaths in sourceDirs2RelPaths) {
        foreach (var relPathToOverwrite in dir2RelPaths.Item2.Intersect(targetRelPaths)) {
          var sourceAbsPath = new Uri(dir2RelPaths.Item1, relPathToOverwrite);
          var targetAbsPath = new Uri(targetDir, relPathToOverwrite);
          if (!FileSignaturesEqual(fileSignatures, sourceAbsPath, targetAbsPath)) {
            copyFunction(sourceAbsPath.AbsolutePath, targetAbsPath.AbsolutePath);
          }
        }
      }
    }

    private static bool FileSignaturesEqual(IFileSignatures fileSignatures, Uri sourceAbsPath, Uri targetAbsPath)
      => fileSignatures.GetSignature(sourceAbsPath.AbsolutePath)
                       .SequenceEqual(fileSignatures.GetSignature(targetAbsPath.AbsolutePath));

    private static void DeleteExtraneousFiles(IEnumerable<Tuple<Uri, HashSet<Uri>>> sourceRelPaths,
                                              Uri targetDir, HashSet<Uri> targetRelPaths) {
      var allSourceRelPaths = sourceRelPaths.Aggregate(new HashSet<Uri>(), (aggregate, sourceDir2RelPaths) => {
        aggregate.UnionWith(sourceDir2RelPaths.Item2);
        return aggregate;
      });
      foreach (var targetFileToDelete in targetRelPaths.Except(allSourceRelPaths)) {
        File.Delete(new Uri(targetDir, targetFileToDelete).AbsolutePath);
      }
    }

    private static HashSet<Uri> GetRelPaths(Uri dir) {
      var absPaths = Exists(dir.AbsolutePath) ? EnumerateFiles(dir.AbsolutePath) : Enumerable.Empty<string>();
      return new HashSet<Uri>(absPaths.Select(absPath => ToRelPath(dir, absPath)));
    }

    private static Uri ToRelPath(Uri basePath, string absPath)
      => basePath.MakeRelativeUri(new Uri(absPath));

    internal static void CopyFile(string sourceFile, string targetFile)
      => File.Copy(sourceFile, targetFile, overwrite: true);
  }
}