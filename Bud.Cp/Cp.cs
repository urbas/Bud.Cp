using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.IO.Directory;

namespace Bud {
  public delegate void FileCopy(Uri sourceFile, Uri targetFile);

  public delegate byte[] FileSignature(Uri file);

  public static class Cp {
    public static void CopyDir(IEnumerable<Uri> sourceDirs, Uri targetDir,
                               FileCopy fileCopy = null, FileSignature fileSignatures = null) {
      fileCopy = fileCopy ?? LocalFileCopy;
      fileSignatures = fileSignatures ?? new Sha256FileSignatures().GetSignature;
      CreateDirectory(targetDir.AbsolutePath);

      var targetDirUri = new Uri(targetDir + "/");
      var targetRelPaths = GetRelPaths(targetDirUri);
      var sourceRelPaths = sourceDirs.Select(sourceDir => new Uri(sourceDir + "/"))
                                     .Select(sourceDir => Tuple.Create(sourceDir, GetRelPaths(sourceDir)))
                                     .ToList();

      AssertNoConflicts(sourceRelPaths, targetDirUri);

      CopyMissingFiles(sourceRelPaths, targetDirUri, targetRelPaths, fileCopy);
      OverwriteExistingFiles(sourceRelPaths, targetDirUri, targetRelPaths, fileCopy, fileSignatures);
      DeleteExtraneousFiles(sourceRelPaths, targetDirUri, targetRelPaths);
    }

    public static void CopyDir(IEnumerable<string> sourceDirs, string targetDir,
                               FileCopy fileCopy = null, FileSignature fileSignatures = null)
      => CopyDir(sourceDirs.Select(path => new Uri(path)), new Uri(targetDir), fileCopy, fileSignatures);

    public static void CopyDir(string sourceDir, string targetDir, FileCopy fileCopy = null,
                               FileSignature fileSignatures = null)
      => CopyDir(new Uri(sourceDir), new Uri(targetDir), fileCopy, fileSignatures);

    public static void CopyDir(Uri sourceDir, Uri targetDir, FileCopy fileCopy = null,
                               FileSignature fileSignatures = null)
      => CopyDir(new[] {sourceDir}, targetDir, fileCopy, fileSignatures);

    internal static void LocalFileCopy(Uri sourcefile, Uri targetfile)
      => File.Copy(sourcefile.AbsolutePath, targetfile.AbsolutePath, overwrite: true);

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
                                         HashSet<Uri> targetRelPaths, FileCopy fileCopy) {
      foreach (var dir2RelPaths in sourceRelPaths) {
        foreach (var relPathToCopy in dir2RelPaths.Item2.Except(targetRelPaths)) {
          var sourceAbsPath = new Uri(dir2RelPaths.Item1, relPathToCopy);
          var targetAbsPath = new Uri(targetDir, relPathToCopy);
          fileCopy(sourceAbsPath, targetAbsPath);
        }
      }
    }

    private static void OverwriteExistingFiles(IEnumerable<Tuple<Uri, HashSet<Uri>>> sourceDirs2RelPaths,
                                               Uri targetDir, HashSet<Uri> targetRelPaths,
                                               FileCopy fileCopy, FileSignature fileSignatures) {
      foreach (var dir2RelPaths in sourceDirs2RelPaths) {
        foreach (var relPathToOverwrite in dir2RelPaths.Item2.Intersect(targetRelPaths)) {
          var sourceAbsPath = new Uri(dir2RelPaths.Item1, relPathToOverwrite);
          var targetAbsPath = new Uri(targetDir, relPathToOverwrite);
          if (!FileSignaturesEqual(fileSignatures, sourceAbsPath, targetAbsPath)) {
            fileCopy(sourceAbsPath, targetAbsPath);
          }
        }
      }
    }

    private static bool FileSignaturesEqual(FileSignature fileSignature, Uri sourceAbsPath, Uri targetAbsPath)
      => fileSignature(sourceAbsPath).SequenceEqual(fileSignature(targetAbsPath));

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
  }
}