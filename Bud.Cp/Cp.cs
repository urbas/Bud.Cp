using System;
using System.Collections.Generic;
using System.Linq;

namespace Bud {
  /// <summary>
  /// Contains the entire API of Bud.Cp.
  /// </summary>
  public static class Cp {
    /// <summary>
    ///   Copies source directories to a single target directory. The target directory will contain all files from the
    ///   source directories combined and only those files.
    /// </summary>
    /// <param name="sourceDirs">URIs of the directories from which to copy files.</param>
    /// <param name="targetDir">URI of the directory into which to copy files.</param>
    /// <param name="storage">the storage API this function will use to perform the copy.</param>
    /// <exception cref="Exception">thrown if the source directories contains files with the same name.</exception>
    public static void CopyDir(IEnumerable<Uri> sourceDirs, Uri targetDir, IStorage storage = null) {
      storage = storage ?? new LocalStorage();
      targetDir = AppendSlash(targetDir);

      storage.CreateDirectory(targetDir);

      var sourceDirUris = sourceDirs.Select(AppendSlash).ToList();
      SyncDirectories(sourceDirUris, targetDir, storage);

      var sourceRelUris = sourceDirUris.Select(sourceDir => GetRelPaths(storage, sourceDir)).ToList();
      AssertNoConflicts(sourceDirUris, sourceRelUris, targetDir);

      var targetRelUris = GetRelPaths(storage, targetDir);
      CopyMissingFiles(sourceDirUris, sourceRelUris, targetDir, targetRelUris, storage);
      OverwriteExistingFiles(sourceDirUris, sourceRelUris, targetDir, targetRelUris, storage);
      DeleteExtraneousFiles(sourceRelUris, targetDir, targetRelUris, storage);
    }

    /// <summary>
    ///   Copies source directories to a single target directory. The target directory will contain all files from the
    ///   source directories combined and only those files.
    /// </summary>
    /// <param name="sourceDirs">URIs of the directories from which to copy files.</param>
    /// <param name="targetDir">URI of the directory into which to copy files.</param>
    /// <param name="storage">the storage API this function will use to perform the copy.</param>
    /// <remarks>this function delegates to
    /// <see cref="CopyDir(System.Collections.Generic.IEnumerable{System.Uri},System.Uri,Bud.IStorage)"/>.</remarks>
    public static void CopyDir(IEnumerable<string> sourceDirs, string targetDir, IStorage storage = null)
      => CopyDir(sourceDirs.Select(path => new Uri(path)), new Uri(targetDir), storage);

    /// <summary>
    ///   Copies the source directory to a single target directory. The target directory will contain all files from the
    ///   source directories combined and only those files.
    /// </summary>
    /// <param name="sourceDirs">URI of the directory from which to copy files.</param>
    /// <param name="targetDir">URI of the directory into which to copy files.</param>
    /// <param name="storage">the storage API this function will use to perform the copy.</param>
    /// <see cref="CopyDir(System.Collections.Generic.IEnumerable{System.Uri},System.Uri,Bud.IStorage)"/>.</remarks>
    public static void CopyDir(string sourceDir, string targetDir, IStorage storage = null)
      => CopyDir(new Uri(sourceDir), new Uri(targetDir), storage);

    /// <summary>
    ///   Copies the source directory to a single target directory. The target directory will contain all files from the
    ///   source directories combined and only those files.
    /// </summary>
    /// <param name="sourceDirs">URI of the directory from which to copy files.</param>
    /// <param name="targetDir">URI of the directory into which to copy files.</param>
    /// <param name="storage">the storage API this function will use to perform the copy.</param>
    /// <see cref="CopyDir(System.Collections.Generic.IEnumerable{System.Uri},System.Uri,Bud.IStorage)"/>.</remarks>
    public static void CopyDir(Uri sourceDir, Uri targetDir, IStorage storage = null)
      => CopyDir(new[] {sourceDir}, targetDir, storage);

    private static void SyncDirectories(IEnumerable<Uri> sourceDirUris, Uri targetDir, IStorage storage) {
      var sourceSubDirs = sourceDirUris.Aggregate(new HashSet<Uri>(),
                                                  (allSubDirs, sourceDirUri) => {
                                                    allSubDirs.UnionWith(GetSubdirs(storage, sourceDirUri));
                                                    return allSubDirs;
                                                  });
      var targetSubDirs = new HashSet<Uri>(GetSubdirs(storage, targetDir));
      DeleteExtraneousSubDirs(storage, targetDir, targetSubDirs, sourceSubDirs);
      CreateMissingSubDirs(storage, targetDir, targetSubDirs, sourceSubDirs);
    }

    private static void AssertNoConflicts(List<Uri> sourceDirUris, List<HashSet<Uri>> sourceRelUris, Uri targetDir) {
      var relPath2SrcDir = new Dictionary<Uri, Uri>();
      for (var dirIndex = 0; dirIndex < sourceDirUris.Count; dirIndex++) {
        var sourceDirUri = sourceDirUris[dirIndex];
        var relPaths = sourceRelUris[dirIndex];
        foreach (var relPath in relPaths) {
          Uri srcDir;
          if (relPath2SrcDir.TryGetValue(relPath, out srcDir)) {
            throw new CopyClashException(srcDir, sourceDirUri, targetDir, relPath);
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
        foreach (var relPathToCopy in srcFilesRelPaths) {
          if (!targetRelUris.Contains(relPathToCopy)) {
            var sourceAbsPath = new Uri(sourceDirUri, relPathToCopy);
            var targetAbsPath = new Uri(targetDir, relPathToCopy);
            storage.CopyFile(sourceAbsPath, targetAbsPath);
          }
        }
      }
    }

    private static void CreateMissingSubDirs(IStorage storage, Uri targetDir, HashSet<Uri> targetSubDirs, HashSet<Uri> sourceSubDirs) {
      foreach (var subDir in sourceSubDirs) {
        if (!targetSubDirs.Contains(subDir)) {
          storage.CreateDirectory(new Uri(targetDir, subDir));
        }
      }
    }

    private static void OverwriteExistingFiles(List<Uri> sourceDirUris, List<HashSet<Uri>> sourceRelUris,
                                               Uri targetDir, HashSet<Uri> targetRelUris, IStorage storage) {
      for (var dirIndex = 0; dirIndex < sourceDirUris.Count; dirIndex++) {
        var sourceDirUri = sourceDirUris[dirIndex];
        foreach (var relPathToOverwrite in sourceRelUris[dirIndex]) {
          if (targetRelUris.Contains(relPathToOverwrite)) {
            var sourceAbsPath = new Uri(sourceDirUri, relPathToOverwrite);
            var targetAbsPath = new Uri(targetDir, relPathToOverwrite);
            if (!FileSignaturesEqual(storage, sourceAbsPath, targetAbsPath)) {
              storage.CopyFile(sourceAbsPath, targetAbsPath);
            }
          }
        }
      }
    }

    private static void DeleteExtraneousFiles(IEnumerable<HashSet<Uri>> sourceRelUris, Uri targetDir,
                                              HashSet<Uri> targetRelUris, IStorage storage) {
      var allSourceRelPaths = sourceRelUris.Aggregate(new HashSet<Uri>(), (aggregate, relPaths) => {
        aggregate.UnionWith(relPaths);
        return aggregate;
      });
      foreach (var targetFileToDelete in targetRelUris) {
        if (!allSourceRelPaths.Contains(targetFileToDelete)) {
          storage.DeleteFile(new Uri(targetDir, targetFileToDelete));
        }
      }
    }

    private static void DeleteExtraneousSubDirs(IStorage storage, Uri targetDir, HashSet<Uri> targetSubDirs, HashSet<Uri> sourceSubDirs) {
      foreach (var subDir in targetSubDirs) {
        if (!sourceSubDirs.Contains(subDir)) {
          storage.DeleteDirectory(new Uri(targetDir, subDir));
        }
      }
    }

    private static bool FileSignaturesEqual(IStorage storage, Uri sourceAbsPath, Uri targetAbsPath)
      => storage.GetSignature(sourceAbsPath).SequenceEqual(storage.GetSignature(targetAbsPath));

    private static Uri AppendSlash(Uri targetDir)
      => targetDir.AbsolutePath.EndsWith("/") ? targetDir : new Uri(targetDir + "/");

    private static HashSet<Uri> GetRelPaths(IStorage storage, Uri dir)
      => new HashSet<Uri>(storage.EnumerateFiles(dir).Select(dir.MakeRelativeUri));

    private static IEnumerable<Uri> GetSubdirs(IStorage storage, Uri sourceDirUri)
      => storage.EnumerateDirectories(sourceDirUri).Select(sourceDirUri.MakeRelativeUri);
  }
}