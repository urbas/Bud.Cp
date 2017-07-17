using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.IO.SearchOption;

namespace Bud {
  internal class LocalStorage : IStorage {
    private readonly Sha256FileSignatures fileSignatures;

    public LocalStorage() {
      fileSignatures = new Sha256FileSignatures();
    }

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

    public byte[] GetSignature(Uri file) => fileSignatures.GetSignature(file);

    public void DeleteFile(Uri file) => File.Delete(file.AbsolutePath);

    public void DeleteDirectory(Uri dir) => Directory.Delete(dir.AbsolutePath);
  }
}