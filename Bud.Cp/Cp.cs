using System;
using System.IO;
using System.Linq;
using static System.IO.Directory;
using static System.IO.Path;

namespace Bud {
  public static class Cp {
    public static void CopyDir(string sourceDir, string targetDir, string targetInfo) {
      var sourceFiles = Exists(sourceDir) ? EnumerateFiles(sourceDir) : Enumerable.Empty<string>();
      var sourceDirUri = new Uri(sourceDir + "/");
      CreateDirectory(targetDir);
      foreach (var sourceFile in sourceFiles) {
        var sourceFileUri = new Uri(sourceFile);
        var relPath = sourceDirUri.MakeRelativeUri(sourceFileUri);
        File.Copy(sourceFile, Combine(targetDir, relPath.ToString()));
      }
    }
  }
}