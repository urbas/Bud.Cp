using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using static System.IO.Directory;
using static System.IO.Path;

namespace Bud {
  public static class Cp {
    public static void CopyDir(string sourceDir, string targetDir, Action<string, string> copyFunction = null) {
      copyFunction = copyFunction ?? CopyFile;
      CreateDirectory(targetDir);
      var targetRelPaths = GetRelPaths(targetDir);
      var sourceRelPaths = GetRelPaths(sourceDir);
      CopyMissingFiles(sourceDir, targetDir, copyFunction, sourceRelPaths, targetRelPaths);
      OverwriteExistingFiles(sourceDir, targetDir, copyFunction, sourceRelPaths, targetRelPaths);
      DeleteExtraneousFiles(targetDir, targetRelPaths, sourceRelPaths);
    }

    private static void CopyMissingFiles(string sourceDir, string targetDir, Action<string, string> copyFunction, 
                                         HashSet<string> sourceRelPaths, HashSet<string> targetRelPaths) {
      foreach (var relPathToCopy in sourceRelPaths.Except(targetRelPaths)) {
        var sourceAbsPath = GetFullPath(Combine(sourceDir, relPathToCopy));
        var targetAbsPath = GetFullPath(Combine(targetDir, relPathToCopy));
        copyFunction(sourceAbsPath, targetAbsPath);
      }
    }

    private static void OverwriteExistingFiles(string sourceDir, string targetDir, Action<string, string> copyFunction,
                                               HashSet<string> sourceRelPaths, HashSet<string> targetRelPaths) {
      var buffer = new byte[16384];
      foreach (var relPathToOverwrite in sourceRelPaths.Intersect(targetRelPaths)) {
        var sourceAbsPath = GetFullPath(Combine(sourceDir, relPathToOverwrite));
        var targetAbsPath = GetFullPath(Combine(targetDir, relPathToOverwrite));
        if (!FileDigestsEqual(sourceAbsPath, buffer, targetAbsPath)) {
          copyFunction(sourceAbsPath, targetAbsPath);
        }
      }
    }

    private static void DeleteExtraneousFiles(string targetDir, HashSet<string> targetRelPaths, 
                                              HashSet<string> sourceRelPaths) {
      foreach (var targetFileToDelete in targetRelPaths.Except(sourceRelPaths)) {
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