using System;
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
      var sourceFiles = Exists(sourceDir) ? EnumerateFiles(sourceDir) : Enumerable.Empty<string>();
      var sourceDirUri = new Uri(sourceDir + "/");
      var buffer = new byte[16384];
      foreach (var sourceFile in sourceFiles) {
        var sourceFileUri = new Uri(sourceFile);
        var relPath = sourceDirUri.MakeRelativeUri(sourceFileUri).ToString();
        var targetPath = Combine(targetDir, relPath);
        if (!File.Exists(targetPath) || !FileDigestsEqual(sourceFile, buffer, targetPath)) {
          copyFunction(sourceFile, targetPath);
        }
      }
    }

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