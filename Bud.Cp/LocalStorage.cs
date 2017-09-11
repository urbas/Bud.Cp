using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.IO.SearchOption;

namespace Bud {
  /// <summary>
  /// A local filesystem implementation of <see cref="IStorage"/>.
  /// </summary>
  public class LocalStorage : IStorage {
    public void CreateDirectory(Uri dir) => Directory.CreateDirectory(dir.AbsolutePath);

    public IEnumerable<Uri> EnumerateFiles(Uri dir)
      => Directory.Exists(dir.AbsolutePath)
           ? Directory.EnumerateFiles(dir.AbsolutePath, "*", AllDirectories).Select(path => new Uri(path))
           : Enumerable.Empty<Uri>();

    public IEnumerable<Uri> EnumerateDirectories(Uri dir)
      => Directory.Exists(dir.AbsolutePath)
           ? Directory.EnumerateDirectories(dir.AbsolutePath, "*", AllDirectories).Select(path => new Uri(path))
           : Enumerable.Empty<Uri>();

    public void CopyFile(Uri sourceFile, Uri targetFile)
      => File.Copy(sourceFile.AbsolutePath, targetFile.AbsolutePath, overwrite: true);

    public void DeleteFile(Uri file) => File.Delete(file.AbsolutePath);

    public void DeleteDirectory(Uri dir) => Directory.Delete(dir.AbsolutePath);
  }
}