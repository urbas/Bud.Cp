using System;
using System.IO;
using System.Linq;
using static System.IO.Directory;
using static System.IO.Path;

namespace Bud {
  public static class Cp {
    public static void CopyDir(string sourceDir, string targetDir, string targetInfo,
                               Action<string, string> copyFunction = null) {
      copyFunction = copyFunction ?? File.Copy;
      CreateDirectory(targetDir);
      var sourceFiles = Exists(sourceDir) ? EnumerateFiles(sourceDir) : Enumerable.Empty<string>();
      var sourceDirUri = new Uri(sourceDir + "/");
      foreach (var sourceFile in sourceFiles) {
        var sourceFileUri = new Uri(sourceFile);
        var relPath = sourceDirUri.MakeRelativeUri(sourceFileUri).ToString();
        var targetPath = Combine(targetDir, relPath);
        if (!File.Exists(targetPath) || File.GetLastWriteTimeUtc(sourceFile) > File.GetLastWriteTimeUtc(targetPath)) {
          copyFunction(sourceFile, targetPath);
        }
      }
    }
  }
}