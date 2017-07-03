using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using static System.IO.Directory;
using static System.IO.Path;

namespace Bud {
  public static class Cp {
    public static void CopyDir(IEnumerable<string> sourceDirs, string targetDir,
                               Action<string, string> copyFunction = null) {
      copyFunction = copyFunction ?? CopyFile;
      CreateDirectory(targetDir);
      var targetRelPaths = GetRelPaths(targetDir);
      var sourceRelPaths = sourceDirs.Select(sourceDir => Tuple.Create(sourceDir, GetRelPaths(sourceDir))).ToList();
      CopyMissingFiles(sourceRelPaths, targetDir, targetRelPaths, copyFunction);
      OverwriteExistingFiles(sourceRelPaths, targetDir, targetRelPaths, copyFunction);
      DeleteExtraneousFiles(sourceRelPaths, targetDir, targetRelPaths);
    }

    public static void CopyDir(string sourceDir, string targetDir, Action<string, string> copyFunction = null)
      => CopyDir(new[] {sourceDir}, targetDir, copyFunction);

    private static void CopyMissingFiles(IEnumerable<Tuple<string, HashSet<string>>> sourceRelPaths, string targetDir,
                                         HashSet<string> targetRelPaths, Action<string, string> copyFunction) {
      foreach (var dir2RelPaths in sourceRelPaths) {
        foreach (var relPathToCopy in dir2RelPaths.Item2.Except(targetRelPaths)) {
          var sourceAbsPath = GetFullPath(Combine(dir2RelPaths.Item1, relPathToCopy));
          var targetAbsPath = GetFullPath(Combine(targetDir, relPathToCopy));
          copyFunction(sourceAbsPath, targetAbsPath);
        }
      }
    }

    private static void OverwriteExistingFiles(IEnumerable<Tuple<string, HashSet<string>>> sourceDirs2RelPaths,
                                               string targetDir, HashSet<string> targetRelPaths,
                                               Action<string, string> copyFunction) {
      var buffer = new byte[16384];
      foreach (var dir2RelPaths in sourceDirs2RelPaths) {
        foreach (var relPathToOverwrite in dir2RelPaths.Item2.Intersect(targetRelPaths)) {
          var sourceAbsPath = GetFullPath(Combine(dir2RelPaths.Item1, relPathToOverwrite));
          var targetAbsPath = GetFullPath(Combine(targetDir, relPathToOverwrite));
          if (!FileDigestsEqual(sourceAbsPath, buffer, targetAbsPath)) {
            copyFunction(sourceAbsPath, targetAbsPath);
          }
        }
      }
    }

    private static void DeleteExtraneousFiles(IEnumerable<Tuple<string, HashSet<string>>> sourceRelPaths,
                                              string targetDir, HashSet<string> targetRelPaths) {
      var allSourceRelPaths = sourceRelPaths.Aggregate(new HashSet<string>(), (aggregate, sourceDir2RelPaths) => {
        aggregate.UnionWith(sourceDir2RelPaths.Item2);
        return aggregate;
      });
      foreach (var targetFileToDelete in targetRelPaths.Except(allSourceRelPaths)) {
        File.Delete(Combine(targetDir, targetFileToDelete));
      }
    }

    private static HashSet<string> GetRelPaths(string dir) {
      var dirUri = new Uri(dir + "/");
      var absPaths = Exists(dir) ? EnumerateFiles(dir) : Enumerable.Empty<string>();
      return new HashSet<string>(absPaths.Select(absPath => ToRelPath(dirUri, absPath)));
    }

    private static string ToRelPath(Uri basePath, string absPath)
      => basePath.MakeRelativeUri(new Uri(absPath)).ToString();

    private static bool FileDigestsEqual(string sourceFile, byte[] buffer, string targetPath)
      => DigestFile(sourceFile, buffer).SequenceEqual(DigestFile(targetPath, buffer));

    private static byte[] DigestFile(string file, byte[] buffer) {
      var hashAlgorithm = SHA256.Create();
      hashAlgorithm.Initialize();
      using (var fileStream = File.OpenRead(file)) {
        int readBytes;
        do {
          readBytes = fileStream.Read(buffer, 0, buffer.Length);
          hashAlgorithm.TransformBlock(buffer, 0, readBytes, buffer, 0);
        } while (readBytes == buffer.Length);
      }
      hashAlgorithm.TransformFinalBlock(buffer, 0, 0);
      return hashAlgorithm.Hash;
    }

    internal static void CopyFile(string sourceFile, string targetFile)
      => File.Copy(sourceFile, targetFile, overwrite: true);
  }
}