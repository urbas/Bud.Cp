using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bud {
  public class LocalStorage : IStorage {
    private readonly Sha256FileSignatures fileSignatures;

    public LocalStorage() {
      fileSignatures = new Sha256FileSignatures();
    }

    public void CreateDirectory(Uri dir) => Directory.CreateDirectory(dir.AbsolutePath);

    public IEnumerable<Uri> EnumerateFiles(Uri dir)
      => Directory.Exists(dir.AbsolutePath)
           ? Directory.EnumerateFiles(dir.AbsolutePath).Select(path => new Uri(path))
           : Enumerable.Empty<Uri>();

    public void CopyFile(Uri sourceFile, Uri targetFile)
      => File.Copy(sourceFile.AbsolutePath, targetFile.AbsolutePath, overwrite: true);

    public byte[] GetSignature(Uri file) => fileSignatures.GetSignature(file);

    public void DeleteFile(Uri file) => File.Delete(file.AbsolutePath);
  }
}